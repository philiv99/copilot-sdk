using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Hubs;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Middleware;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;

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
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Bind CopilotClient configuration from appsettings
builder.Services.Configure<CopilotClientConfig>(
    builder.Configuration.GetSection("CopilotClient"));

// Register persistence service (must be registered before managers that depend on it)
builder.Services.AddSingleton<IPersistenceService, PersistenceService>();

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

// Register hosted service for automatic client startup/shutdown
builder.Services.AddHostedService<CopilotClientHostedService>();

var app = builder.Build();

// Initialize SessionManager with EventDispatcher and load persisted data
var sessionManager = app.Services.GetRequiredService<SessionManager>();
var eventDispatcher = app.Services.GetRequiredService<SessionEventDispatcher>();
sessionManager.SetEventDispatcher(eventDispatcher);

// Load persisted data on startup
await LoadPersistedDataAsync(app.Services);

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

    // Sessions are no longer cached in memory - they are read from persistence on demand
    logger.LogInformation("Session persistence is file-based only (no in-memory caching)");
}
