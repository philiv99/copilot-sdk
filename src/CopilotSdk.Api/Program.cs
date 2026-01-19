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

// Register Copilot services
builder.Services.AddSingleton<CopilotClientManager>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<CopilotClientManager>>();
    var manager = new CopilotClientManager(logger);
    
    // Apply configuration from appsettings
    var config = builder.Configuration.GetSection("CopilotClient").Get<CopilotClientConfig>();
    if (config != null)
    {
        manager.UpdateConfig(config);
    }
    
    return manager;
});
builder.Services.AddSingleton<ICopilotClientManager>(sp => sp.GetRequiredService<CopilotClientManager>());
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddSingleton<SessionEventDispatcher>();
builder.Services.AddSingleton<IToolExecutionService, ToolExecutionService>();
builder.Services.AddScoped<ICopilotClientService, CopilotClientService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// Register hosted service for automatic client startup/shutdown
builder.Services.AddHostedService<CopilotClientHostedService>();

var app = builder.Build();

// Initialize SessionManager with EventDispatcher
var sessionManager = app.Services.GetRequiredService<SessionManager>();
var eventDispatcher = app.Services.GetRequiredService<SessionEventDispatcher>();
sessionManager.SetEventDispatcher(eventDispatcher);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use error handling middleware
app.UseErrorHandling();

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
