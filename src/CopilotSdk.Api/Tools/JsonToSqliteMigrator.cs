using System.Text.Json;
using System.Text.Json.Serialization;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CopilotSdk.Api.Tools;

/// <summary>
/// Migrates existing JSON file-based persistence data into the SQLite database.
/// Reads client-config.json and sessions/*.json and writes them into SQLite via IPersistenceService.
/// 
/// Usage:
///   Can be invoked from Program.cs at startup, or run as a standalone migration.
///   After migration completes successfully, the JSON files can be archived or removed.
/// </summary>
public static class JsonToSqliteMigrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Result of a migration run.
    /// </summary>
    public class MigrationResult
    {
        public bool ClientConfigMigrated { get; set; }
        public int SessionsMigrated { get; set; }
        public int TotalMessagesMigrated { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Skipped { get; set; } = new();
    }

    /// <summary>
    /// Runs the migration from JSON files to SQLite.
    /// Checks for existing data in SQLite and skips already-migrated records.
    /// </summary>
    /// <param name="persistenceService">The SQLite persistence service to write into.</param>
    /// <param name="dataDirectory">The data directory containing JSON files.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Migration result with counts and any errors.</returns>
    public static async Task<MigrationResult> MigrateAsync(
        IPersistenceService persistenceService,
        string dataDirectory,
        ILogger? logger = null)
    {
        var result = new MigrationResult();

        logger?.LogInformation("Starting JSON → SQLite migration from {DataDirectory}", dataDirectory);

        // 1. Migrate client configuration
        await MigrateClientConfigAsync(persistenceService, dataDirectory, result, logger);

        // 2. Migrate sessions
        var sessionsDir = Path.Combine(dataDirectory, "sessions");
        if (Directory.Exists(sessionsDir))
        {
            var jsonFiles = Directory.GetFiles(sessionsDir, "*.json");
            logger?.LogInformation("Found {Count} session JSON files to migrate", jsonFiles.Length);

            foreach (var filePath in jsonFiles)
            {
                await MigrateSessionFileAsync(persistenceService, filePath, result, logger);
            }
        }
        else
        {
            logger?.LogInformation("No sessions directory found at {SessionsDir}, skipping", sessionsDir);
        }

        logger?.LogInformation(
            "Migration complete: ClientConfig={ConfigMigrated}, Sessions={SessionCount}, Messages={MessageCount}, Errors={ErrorCount}, Skipped={SkippedCount}",
            result.ClientConfigMigrated, result.SessionsMigrated, result.TotalMessagesMigrated,
            result.Errors.Count, result.Skipped.Count);

        return result;
    }

    private static async Task MigrateClientConfigAsync(
        IPersistenceService persistenceService,
        string dataDirectory,
        MigrationResult result,
        ILogger? logger)
    {
        var configPath = Path.Combine(dataDirectory, "client-config.json");

        if (!File.Exists(configPath))
        {
            logger?.LogDebug("No client-config.json found, skipping");
            return;
        }

        try
        {
            // Check if config already exists in SQLite
            var existingConfig = await persistenceService.LoadClientConfigAsync();
            if (existingConfig != null)
            {
                logger?.LogInformation("Client config already exists in SQLite, skipping migration");
                result.Skipped.Add("client-config.json (already exists in SQLite)");
                return;
            }

            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CopilotClientConfig>(json, JsonOptions);

            if (config != null)
            {
                await persistenceService.SaveClientConfigAsync(config);
                result.ClientConfigMigrated = true;
                logger?.LogInformation("Migrated client-config.json to SQLite");
            }
        }
        catch (Exception ex)
        {
            var error = $"Failed to migrate client-config.json: {ex.Message}";
            result.Errors.Add(error);
            logger?.LogError(ex, "Failed to migrate client-config.json");
        }
    }

    private static async Task MigrateSessionFileAsync(
        IPersistenceService persistenceService,
        string filePath,
        MigrationResult result,
        ILogger? logger)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        try
        {
            // Check if session already exists in SQLite
            if (persistenceService.SessionExists(fileName))
            {
                logger?.LogDebug("Session {SessionId} already exists in SQLite, skipping", fileName);
                result.Skipped.Add($"{fileName} (already exists in SQLite)");
                return;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var sessionData = JsonSerializer.Deserialize<PersistedSessionData>(json, JsonOptions);

            if (sessionData == null)
            {
                result.Errors.Add($"{fileName}: deserialized to null");
                logger?.LogWarning("Session file {FileName} deserialized to null", fileName);
                return;
            }

            // Ensure the session ID matches the filename
            if (string.IsNullOrEmpty(sessionData.SessionId))
            {
                sessionData.SessionId = fileName;
            }

            var messageCount = sessionData.Messages?.Count ?? 0;

            // Save the full session (metadata + messages) to SQLite
            await persistenceService.SaveSessionAsync(sessionData);

            result.SessionsMigrated++;
            result.TotalMessagesMigrated += messageCount;

            logger?.LogInformation(
                "Migrated session {SessionId}: {MessageCount} messages, created {CreatedAt}",
                sessionData.SessionId, messageCount, sessionData.CreatedAt);
        }
        catch (Exception ex)
        {
            var error = $"Failed to migrate {fileName}: {ex.Message}";
            result.Errors.Add(error);
            logger?.LogError(ex, "Failed to migrate session file {FilePath}", filePath);
        }
    }
}
