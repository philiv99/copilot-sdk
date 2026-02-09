using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Middleware;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using CopilotSdk.Api.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add SignalR for real-time event streaming
builder.Services.AddSignalR();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Bind CopilotClient configuration from appsettings
builder.Services.Configure<CopilotClientConfig>(
    builder.Configuration.GetSection("CopilotClient"));

// Register persistence service (must be registered before managers that depend on it)
// Uses SQLite for atomic writes, queryable data, and proper concurrency handling
builder.Services.AddSingleton<IPersistenceService, SqlitePersistenceService>();

// Register Copilot services
builder.Services.AddSingleton<CopilotClientManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CopilotClientManager>>();
    var persistenceService = sp.GetRequiredService<IPersistenceService>();
    var manager = new CopilotClientManager(logger, persistenceService);
    
    // Apply configuration from appsettings (will be overridden by persisted config if available)
    var config = builder.Configuration.GetSection("CopilotClient").Get<CopilotClientConfig>();
    if (config != null)
    {
        manager.UpdateConfig(config);
    }
    
    return manager;
});
builder.Services.AddSingleton<ICopilotClientManager>(sp => sp.GetRequiredService<CopilotClientManager>());

// Register SessionManager with persistence
builder.Services.AddSingleton<SessionManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SessionManager>>();
    var persistenceService = sp.GetRequiredService<IPersistenceService>();
    return new SessionManager(logger, persistenceService);
});

// Register SessionEventDispatcher with SessionManager for persistence
builder.Services.AddSingleton<SessionEventDispatcher>(sp =>
{
    var hubContext = sp.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<SessionHub>>();
    var logger = sp.GetRequiredService<ILogger<SessionEventDispatcher>>();
    var sessionManager = sp.GetRequiredService<SessionManager>();
    return new SessionEventDispatcher(hubContext, logger, sessionManager);
});

// Add memory cache for models service
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IToolExecutionService, ToolExecutionService>();
builder.Services.AddSingleton<IDevServerService, DevServerService>();
builder.Services.AddScoped<ICopilotClientService, CopilotClientService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IPromptRefinementService, PromptRefinementService>();
builder.Services.AddScoped<IModelsService, ModelsService>();
builder.Services.AddScoped<ISystemPromptTemplateService, SystemPromptTemplateService>();
builder.Services.AddScoped<IUserService, UserService>();

// Register hosted service for automatic client startup/shutdown
builder.Services.AddHostedService<CopilotClientHostedService>();

var app = builder.Build();

// Initialize SessionManager with EventDispatcher and load persisted data
var sessionManager = app.Services.GetRequiredService<SessionManager>();
var eventDispatcher = app.Services.GetRequiredService<SessionEventDispatcher>();
sessionManager.SetEventDispatcher(eventDispatcher);

// Load persisted data on startup
await LoadPersistedDataAsync(app.Services);

// Migrate any existing JSON data to SQLite (idempotent — skips already-migrated records)
await MigrateJsonToSqliteAsync(app.Services);

// Seed default admin user if no users exist
await SeedDefaultAdminAsync(app.Services);

// Seed default creator "Fred" and assign orphaned sessions
await SeedDefaultCreatorAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use error handling middleware
app.UseErrorHandling();

// Use rate limiting middleware for prompt refinement endpoint
app.UseRateLimiting();

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors("AllowReactApp");

app.MapControllers();

// Map SignalR hub with CORS policy
app.MapHub<SessionHub>("/hubs/session").RequireCors("AllowReactApp");

app.Run();

/// <summary>
/// Loads persisted client configuration and sessions on application startup.
/// </summary>
static async Task LoadPersistedDataAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Load persisted client configuration
        var clientManager = services.GetRequiredService<CopilotClientManager>();
        await clientManager.LoadPersistedConfigAsync();
        logger.LogInformation("Loaded persisted client configuration");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to load persisted client configuration, using defaults");
    }

    // Sessions are no longer cached in memory - they are read from SQLite on demand
    logger.LogInformation("Session persistence is SQLite-backed (no in-memory caching)");
}

/// <summary>
/// Runs the JSON → SQLite migration on startup.
/// This is idempotent: sessions/config already present in SQLite are skipped.
/// </summary>
static async Task MigrateJsonToSqliteAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    var persistenceService = services.GetRequiredService<IPersistenceService>();

    try
    {
        var result = await JsonToSqliteMigrator.MigrateAsync(
            persistenceService,
            persistenceService.DataDirectory,
            logger);

        if (result.SessionsMigrated > 0 || result.ClientConfigMigrated)
        {
            logger.LogInformation(
                "JSON → SQLite migration: {Sessions} sessions ({Messages} messages) migrated, ClientConfig={Config}",
                result.SessionsMigrated, result.TotalMessagesMigrated, result.ClientConfigMigrated);
        }

        if (result.Errors.Count > 0)
        {
            logger.LogWarning("Migration had {ErrorCount} errors: {Errors}",
                result.Errors.Count, string.Join("; ", result.Errors));
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "JSON → SQLite migration failed (non-fatal, SQLite data is still usable)");
    }
}

/// <summary>
/// Seeds the default admin account if no users exist in the database.
/// </summary>
static async Task SeedDefaultAdminAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        using var scope = services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await userService.EnsureDefaultAdminAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to seed default admin account");
    }
}

/// <summary>
/// Seeds the default creator account "Fred" and assigns orphaned sessions to Fred.
/// </summary>
static async Task SeedDefaultCreatorAsync(IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        using var scope = services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        await userService.EnsureDefaultCreatorAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Failed to seed default creator account");
    }
}
