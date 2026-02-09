using System.Text.Json;
using System.Text.Json.Serialization;
using CopilotSdk.Api.Models.Domain;
using Microsoft.Data.Sqlite;

namespace CopilotSdk.Api.Services;

/// <summary>
/// SQLite-backed implementation of <see cref="IPersistenceService"/>.
/// Replaces the JSON file-based PersistenceService with atomic, queryable SQLite storage.
/// </summary>
public class SqlitePersistenceService : IPersistenceService, IAsyncDisposable
{
    private readonly ILogger<SqlitePersistenceService> _logger;
    private readonly string _dataDirectory;
    private readonly string _connectionString;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    public SqlitePersistenceService(ILogger<SqlitePersistenceService> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Get data directory from configuration or use default
        var configuredPath = configuration.GetValue<string>("Persistence:DataDirectory");
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            var baseDir = AppContext.BaseDirectory;
            var srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            _dataDirectory = Path.Combine(srcDir, "data");
        }
        else if (!Path.IsPathRooted(configuredPath))
        {
            var baseDir = AppContext.BaseDirectory;
            var srcDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
            _dataDirectory = Path.GetFullPath(Path.Combine(srcDir, configuredPath));
        }
        else
        {
            _dataDirectory = configuredPath;
        }

        // Ensure the data directory exists
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
            _logger.LogInformation("Created data directory: {DataDirectory}", _dataDirectory);
        }

        var dbPath = Path.Combine(_dataDirectory, "copilot-sdk.db");
        _connectionString = $"Data Source={dbPath}";

        // Initialize the database schema
        InitializeDatabase();
    }

    /// <summary>
    /// Constructor for testing that accepts a connection string directly.
    /// </summary>
    internal SqlitePersistenceService(ILogger<SqlitePersistenceService> logger, string dataDirectory, string connectionString)
    {
        _logger = logger;
        _dataDirectory = dataDirectory;
        _connectionString = connectionString;

        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }

        InitializeDatabase();
    }

    /// <inheritdoc/>
    public string DataDirectory => _dataDirectory;

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Enable WAL mode for better concurrent read/write performance
        using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        walCmd.ExecuteNonQuery();

        return connection;
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ClientConfig (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    ConfigJson TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS Sessions (
                    SessionId TEXT PRIMARY KEY NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    LastActivityAt TEXT,
                    MessageCount INTEGER NOT NULL DEFAULT 0,
                    Summary TEXT,
                    IsRemote INTEGER NOT NULL DEFAULT 0,
                    ConfigJson TEXT,
                    CreatorUserId TEXT,
                    UpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );

                CREATE TABLE IF NOT EXISTS Messages (
                    Id TEXT PRIMARY KEY NOT NULL,
                    SessionId TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL DEFAULT '',
                    TransformedContent TEXT,
                    MessageId TEXT,
                    ToolCallId TEXT,
                    ToolName TEXT,
                    ToolResult TEXT,
                    ToolError TEXT,
                    ReasoningContent TEXT,
                    ParentToolCallId TEXT,
                    Source TEXT,
                    AttachmentsJson TEXT,
                    ToolRequestsJson TEXT,
                    SortOrder INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (SessionId) REFERENCES Sessions(SessionId) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS IX_Messages_SessionId ON Messages(SessionId);
                CREATE INDEX IF NOT EXISTS IX_Messages_SessionId_SortOrder ON Messages(SessionId, SortOrder);

                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT PRIMARY KEY NOT NULL,
                    Username TEXT NOT NULL COLLATE NOCASE,
                    Email TEXT NOT NULL COLLATE NOCASE,
                    DisplayName TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    PasswordSalt TEXT NOT NULL,
                    Role INTEGER NOT NULL DEFAULT 0,
                    AvatarType INTEGER NOT NULL DEFAULT 0,
                    AvatarData TEXT,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    LastLoginAt TEXT
                );

                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users(Username);
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users(Email);
            ";
            cmd.ExecuteNonQuery();

            // Migration: Add CreatorUserId column to Sessions if it doesn't exist
            using var migrateCmd = connection.CreateCommand();
            migrateCmd.Transaction = transaction;
            migrateCmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Sessions') WHERE name='CreatorUserId';";
            var hasColumn = (long)(migrateCmd.ExecuteScalar() ?? 0L) > 0;
            if (!hasColumn)
            {
                using var alterCmd = connection.CreateCommand();
                alterCmd.Transaction = transaction;
                alterCmd.CommandText = "ALTER TABLE Sessions ADD COLUMN CreatorUserId TEXT;";
                alterCmd.ExecuteNonQuery();
                _logger.LogInformation("Migrated Sessions table: added CreatorUserId column");
            }

            transaction.Commit();
            _logger.LogInformation("SQLite database initialized at {DataDirectory}", _dataDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite database");
            throw;
        }
    }

    #region Client Configuration

    /// <inheritdoc/>
    public async Task SaveClientConfigAsync(CopilotClientConfig config, CancellationToken cancellationToken = default)
    {
        try
        {
            var configJson = JsonSerializer.Serialize(config, JsonOptions);

            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ClientConfig (Id, ConfigJson, UpdatedAt)
                VALUES (1, @configJson, @updatedAt)
                ON CONFLICT(Id) DO UPDATE SET
                    ConfigJson = @configJson,
                    UpdatedAt = @updatedAt;
            ";
            cmd.Parameters.AddWithValue("@configJson", configJson);
            cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
            _logger.LogDebug("Saved client configuration to SQLite");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save client configuration");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CopilotClientConfig?> LoadClientConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ConfigJson FROM ClientConfig WHERE Id = 1;";

            var result = await Task.Run(() => cmd.ExecuteScalar(), cancellationToken);
            if (result == null || result == DBNull.Value)
            {
                _logger.LogDebug("No client configuration found in SQLite");
                return null;
            }

            var config = JsonSerializer.Deserialize<CopilotClientConfig>((string)result, JsonOptions);
            _logger.LogDebug("Loaded client configuration from SQLite");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load client configuration");
            return null;
        }
    }

    #endregion

    #region Session Data

    /// <inheritdoc/>
    public async Task SaveSessionAsync(PersistedSessionData sessionData, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            // Upsert session metadata
            using var sessionCmd = connection.CreateCommand();
            sessionCmd.Transaction = transaction;
            sessionCmd.CommandText = @"
                INSERT INTO Sessions (SessionId, CreatedAt, LastActivityAt, MessageCount, Summary, IsRemote, ConfigJson, CreatorUserId, UpdatedAt)
                VALUES (@sessionId, @createdAt, @lastActivityAt, @messageCount, @summary, @isRemote, @configJson, @creatorUserId, @updatedAt)
                ON CONFLICT(SessionId) DO UPDATE SET
                    LastActivityAt = @lastActivityAt,
                    MessageCount = @messageCount,
                    Summary = @summary,
                    IsRemote = @isRemote,
                    ConfigJson = @configJson,
                    CreatorUserId = COALESCE(@creatorUserId, CreatorUserId),
                    UpdatedAt = @updatedAt;
            ";
            sessionCmd.Parameters.AddWithValue("@sessionId", sessionData.SessionId);
            sessionCmd.Parameters.AddWithValue("@createdAt", sessionData.CreatedAt.ToString("O"));
            sessionCmd.Parameters.AddWithValue("@lastActivityAt", sessionData.LastActivityAt?.ToString("O") ?? (object)DBNull.Value);
            sessionCmd.Parameters.AddWithValue("@messageCount", sessionData.MessageCount);
            sessionCmd.Parameters.AddWithValue("@summary", (object?)sessionData.Summary ?? DBNull.Value);
            sessionCmd.Parameters.AddWithValue("@isRemote", sessionData.IsRemote ? 1 : 0);
            sessionCmd.Parameters.AddWithValue("@configJson", sessionData.Config != null ? JsonSerializer.Serialize(sessionData.Config, JsonOptions) : (object)DBNull.Value);
            sessionCmd.Parameters.AddWithValue("@creatorUserId", (object?)sessionData.CreatorUserId ?? DBNull.Value);
            sessionCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));

            await Task.Run(() => sessionCmd.ExecuteNonQuery(), cancellationToken);

            // If there are messages, replace them all (full save)
            if (sessionData.Messages.Count > 0)
            {
                // Delete existing messages for this session
                using var deleteCmd = connection.CreateCommand();
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM Messages WHERE SessionId = @sessionId;";
                deleteCmd.Parameters.AddWithValue("@sessionId", sessionData.SessionId);
                await Task.Run(() => deleteCmd.ExecuteNonQuery(), cancellationToken);

                // Insert all messages
                await InsertMessagesAsync(connection, transaction, sessionData.SessionId, sessionData.Messages, 0, cancellationToken);
            }

            await Task.Run(() => transaction.Commit(), cancellationToken);
            _logger.LogDebug("Saved session {SessionId} to SQLite", sessionData.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save session {SessionId}", sessionData.SessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<PersistedSessionData?> LoadSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();

            // Load session metadata
            using var sessionCmd = connection.CreateCommand();
            sessionCmd.CommandText = "SELECT SessionId, CreatedAt, LastActivityAt, MessageCount, Summary, IsRemote, ConfigJson, CreatorUserId FROM Sessions WHERE SessionId = @sessionId;";
            sessionCmd.Parameters.AddWithValue("@sessionId", sessionId);

            PersistedSessionData? sessionData = null;
            using (var reader = await Task.Run(() => sessionCmd.ExecuteReader(), cancellationToken))
            {
                if (await Task.Run(() => reader.Read(), cancellationToken))
                {
                    sessionData = ReadSessionFromReader(reader);
                }
            }

            if (sessionData == null)
            {
                _logger.LogDebug("No session found for {SessionId}", sessionId);
                return null;
            }

            // Load messages
            sessionData.Messages = await LoadMessagesInternalAsync(connection, sessionId, cancellationToken);

            _logger.LogDebug("Loaded session {SessionId} from SQLite", sessionId);
            return sessionData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PersistedSessionData>> LoadAllSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = new List<PersistedSessionData>();

        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT SessionId, CreatedAt, LastActivityAt, MessageCount, Summary, IsRemote, ConfigJson, CreatorUserId FROM Sessions ORDER BY CreatedAt DESC;";

            using var reader = await Task.Run(() => cmd.ExecuteReader(), cancellationToken);
            while (await Task.Run(() => reader.Read(), cancellationToken))
            {
                var sessionData = ReadSessionFromReader(reader);
                sessions.Add(sessionData);
            }

            // Load messages for each session
            foreach (var session in sessions)
            {
                session.Messages = await LoadMessagesInternalAsync(connection, session.SessionId, cancellationToken);
            }

            _logger.LogInformation("Loaded {Count} sessions from SQLite", sessions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sessions");
        }

        return sessions;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();

            // Enable foreign keys for cascade delete
            using var pragmaCmd = connection.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
            pragmaCmd.ExecuteNonQuery();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Sessions WHERE SessionId = @sessionId;";
            cmd.Parameters.AddWithValue("@sessionId", sessionId);

            var rowsAffected = await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Deleted session {SessionId} from SQLite", sessionId);
                return true;
            }

            _logger.LogDebug("Session not found for deletion: {SessionId}", sessionId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool SessionExists(string sessionId)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(1) FROM Sessions WHERE SessionId = @sessionId;";
            cmd.Parameters.AddWithValue("@sessionId", sessionId);
            var count = (long)(cmd.ExecuteScalar() ?? 0L);
            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check session existence {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public List<string> GetPersistedSessionIds()
    {
        var sessionIds = new List<string>();
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT SessionId FROM Sessions ORDER BY CreatedAt DESC;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                sessionIds.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get persisted session IDs");
        }

        return sessionIds;
    }

    /// <inheritdoc/>
    public async Task<int> AssignOrphanedSessionsAsync(string creatorUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE Sessions 
                SET CreatorUserId = @creatorUserId 
                WHERE CreatorUserId IS NULL OR CreatorUserId = '';";
            cmd.Parameters.AddWithValue("@creatorUserId", creatorUserId);

            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken);
            if (rowsAffected > 0)
            {
                _logger.LogInformation("Assigned {Count} orphaned sessions to user {UserId}", rowsAffected, creatorUserId);
            }
            return rowsAffected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign orphaned sessions to user {UserId}", creatorUserId);
            return 0;
        }
    }

    #endregion

    #region Message Persistence

    /// <inheritdoc/>
    public async Task AppendMessagesAsync(string sessionId, IEnumerable<PersistedMessage> messages, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageList = messages.ToList();
            if (messageList.Count == 0) return;

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            // Get the current max sort order for this session
            using var maxCmd = connection.CreateCommand();
            maxCmd.Transaction = transaction;
            maxCmd.CommandText = "SELECT COALESCE(MAX(SortOrder), -1) FROM Messages WHERE SessionId = @sessionId;";
            maxCmd.Parameters.AddWithValue("@sessionId", sessionId);
            var maxSortOrder = (long)(await Task.Run(() => maxCmd.ExecuteScalar(), cancellationToken) ?? -1L);

            // Insert new messages
            await InsertMessagesAsync(connection, transaction, sessionId, messageList, (int)maxSortOrder + 1, cancellationToken);

            // Update session metadata
            using var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;
            updateCmd.CommandText = @"
                UPDATE Sessions
                SET MessageCount = (SELECT COUNT(*) FROM Messages WHERE SessionId = @sessionId),
                    LastActivityAt = @lastActivityAt,
                    UpdatedAt = @updatedAt
                WHERE SessionId = @sessionId;
            ";
            updateCmd.Parameters.AddWithValue("@sessionId", sessionId);
            updateCmd.Parameters.AddWithValue("@lastActivityAt", DateTime.UtcNow.ToString("O"));
            updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("O"));
            await Task.Run(() => updateCmd.ExecuteNonQuery(), cancellationToken);

            await Task.Run(() => transaction.Commit(), cancellationToken);
            _logger.LogDebug("Appended {Count} messages to session {SessionId}", messageList.Count, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append messages to session {SessionId}", sessionId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<PersistedMessage>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            return await LoadMessagesInternalAsync(connection, sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for session {SessionId}", sessionId);
            return new List<PersistedMessage>();
        }
    }

    #endregion

    #region Private Helpers

    private async Task InsertMessagesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sessionId,
        List<PersistedMessage> messages,
        int startSortOrder,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO Messages (Id, SessionId, Timestamp, Role, Content, TransformedContent,
                    MessageId, ToolCallId, ToolName, ToolResult, ToolError, ReasoningContent,
                    ParentToolCallId, Source, AttachmentsJson, ToolRequestsJson, SortOrder)
                VALUES (@id, @sessionId, @timestamp, @role, @content, @transformedContent,
                    @messageId, @toolCallId, @toolName, @toolResult, @toolError, @reasoningContent,
                    @parentToolCallId, @source, @attachmentsJson, @toolRequestsJson, @sortOrder);
            ";

            cmd.Parameters.AddWithValue("@id", msg.Id.ToString());
            cmd.Parameters.AddWithValue("@sessionId", sessionId);
            cmd.Parameters.AddWithValue("@timestamp", msg.Timestamp.ToString("O"));
            cmd.Parameters.AddWithValue("@role", msg.Role);
            cmd.Parameters.AddWithValue("@content", msg.Content ?? string.Empty);
            cmd.Parameters.AddWithValue("@transformedContent", (object?)msg.TransformedContent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@messageId", (object?)msg.MessageId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toolCallId", (object?)msg.ToolCallId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toolName", (object?)msg.ToolName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toolResult", (object?)msg.ToolResult ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@toolError", (object?)msg.ToolError ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@reasoningContent", (object?)msg.ReasoningContent ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@parentToolCallId", (object?)msg.ParentToolCallId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@source", (object?)msg.Source ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@attachmentsJson",
                msg.Attachments != null && msg.Attachments.Count > 0
                    ? JsonSerializer.Serialize(msg.Attachments, JsonOptions)
                    : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@toolRequestsJson",
                msg.ToolRequests != null && msg.ToolRequests.Count > 0
                    ? JsonSerializer.Serialize(msg.ToolRequests, JsonOptions)
                    : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sortOrder", startSortOrder + i);

            await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
        }
    }

    private async Task<List<PersistedMessage>> LoadMessagesInternalAsync(
        SqliteConnection connection,
        string sessionId,
        CancellationToken cancellationToken)
    {
        var messages = new List<PersistedMessage>();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, Timestamp, Role, Content, TransformedContent, MessageId,
                   ToolCallId, ToolName, ToolResult, ToolError, ReasoningContent,
                   ParentToolCallId, Source, AttachmentsJson, ToolRequestsJson
            FROM Messages
            WHERE SessionId = @sessionId
            ORDER BY SortOrder ASC;
        ";
        cmd.Parameters.AddWithValue("@sessionId", sessionId);

        using var reader = await Task.Run(() => cmd.ExecuteReader(), cancellationToken);
        while (await Task.Run(() => reader.Read(), cancellationToken))
        {
            messages.Add(ReadMessageFromReader(reader));
        }

        return messages;
    }

    private static PersistedSessionData ReadSessionFromReader(SqliteDataReader reader)
    {
        var sessionData = new PersistedSessionData
        {
            SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))).ToUniversalTime(),
            LastActivityAt = reader.IsDBNull(reader.GetOrdinal("LastActivityAt"))
                ? null
                : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastActivityAt"))).ToUniversalTime(),
            MessageCount = reader.GetInt32(reader.GetOrdinal("MessageCount")),
            Summary = reader.IsDBNull(reader.GetOrdinal("Summary")) ? null : reader.GetString(reader.GetOrdinal("Summary")),
            IsRemote = reader.GetInt32(reader.GetOrdinal("IsRemote")) == 1,
            CreatorUserId = reader.IsDBNull(reader.GetOrdinal("CreatorUserId")) ? null : reader.GetString(reader.GetOrdinal("CreatorUserId")),
            Messages = new List<PersistedMessage>()
        };

        if (!reader.IsDBNull(reader.GetOrdinal("ConfigJson")))
        {
            var configJson = reader.GetString(reader.GetOrdinal("ConfigJson"));
            sessionData.Config = JsonSerializer.Deserialize<PersistedSessionConfig>(configJson, JsonOptions);
        }

        return sessionData;
    }

    private static PersistedMessage ReadMessageFromReader(SqliteDataReader reader)
    {
        var message = new PersistedMessage
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
            Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("Timestamp"))).ToUniversalTime(),
            Role = reader.GetString(reader.GetOrdinal("Role")),
            Content = reader.GetString(reader.GetOrdinal("Content")),
            TransformedContent = reader.IsDBNull(reader.GetOrdinal("TransformedContent")) ? null : reader.GetString(reader.GetOrdinal("TransformedContent")),
            MessageId = reader.IsDBNull(reader.GetOrdinal("MessageId")) ? null : reader.GetString(reader.GetOrdinal("MessageId")),
            ToolCallId = reader.IsDBNull(reader.GetOrdinal("ToolCallId")) ? null : reader.GetString(reader.GetOrdinal("ToolCallId")),
            ToolName = reader.IsDBNull(reader.GetOrdinal("ToolName")) ? null : reader.GetString(reader.GetOrdinal("ToolName")),
            ToolResult = reader.IsDBNull(reader.GetOrdinal("ToolResult")) ? null : reader.GetString(reader.GetOrdinal("ToolResult")),
            ToolError = reader.IsDBNull(reader.GetOrdinal("ToolError")) ? null : reader.GetString(reader.GetOrdinal("ToolError")),
            ReasoningContent = reader.IsDBNull(reader.GetOrdinal("ReasoningContent")) ? null : reader.GetString(reader.GetOrdinal("ReasoningContent")),
            ParentToolCallId = reader.IsDBNull(reader.GetOrdinal("ParentToolCallId")) ? null : reader.GetString(reader.GetOrdinal("ParentToolCallId")),
            Source = reader.IsDBNull(reader.GetOrdinal("Source")) ? null : reader.GetString(reader.GetOrdinal("Source"))
        };

        if (!reader.IsDBNull(reader.GetOrdinal("AttachmentsJson")))
        {
            var attachmentsJson = reader.GetString(reader.GetOrdinal("AttachmentsJson"));
            message.Attachments = JsonSerializer.Deserialize<List<PersistedAttachment>>(attachmentsJson, JsonOptions);
        }

        if (!reader.IsDBNull(reader.GetOrdinal("ToolRequestsJson")))
        {
            var toolRequestsJson = reader.GetString(reader.GetOrdinal("ToolRequestsJson"));
            message.ToolRequests = JsonSerializer.Deserialize<List<PersistedToolRequest>>(toolRequestsJson, JsonOptions);
        }

        return message;
    }

    #endregion

    #region User Persistence

    /// <inheritdoc/>
    public async Task SaveUserAsync(Models.Domain.User user, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Users (Id, Username, Email, DisplayName, PasswordHash, PasswordSalt, Role, AvatarType, AvatarData, IsActive, CreatedAt, UpdatedAt, LastLoginAt)
                VALUES (@id, @username, @email, @displayName, @passwordHash, @passwordSalt, @role, @avatarType, @avatarData, @isActive, @createdAt, @updatedAt, @lastLoginAt)
                ON CONFLICT(Id) DO UPDATE SET
                    Username = @username,
                    Email = @email,
                    DisplayName = @displayName,
                    PasswordHash = @passwordHash,
                    PasswordSalt = @passwordSalt,
                    Role = @role,
                    AvatarType = @avatarType,
                    AvatarData = @avatarData,
                    IsActive = @isActive,
                    UpdatedAt = @updatedAt,
                    LastLoginAt = @lastLoginAt;
            ";

            cmd.Parameters.AddWithValue("@id", user.Id);
            cmd.Parameters.AddWithValue("@username", user.Username);
            cmd.Parameters.AddWithValue("@email", user.Email);
            cmd.Parameters.AddWithValue("@displayName", user.DisplayName);
            cmd.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@passwordSalt", user.PasswordSalt);
            cmd.Parameters.AddWithValue("@role", (int)user.Role);
            cmd.Parameters.AddWithValue("@avatarType", (int)user.AvatarType);
            cmd.Parameters.AddWithValue("@avatarData", (object?)user.AvatarData ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isActive", user.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdAt", user.CreatedAt.ToString("O"));
            cmd.Parameters.AddWithValue("@updatedAt", user.UpdatedAt.ToString("O"));
            cmd.Parameters.AddWithValue("@lastLoginAt", user.LastLoginAt.HasValue ? user.LastLoginAt.Value.ToString("O") : (object)DBNull.Value);

            await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
            _logger.LogDebug("Saved user {UserId} ({Username}) to SQLite", user.Id, user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user {UserId}", user.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Models.Domain.User?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", userId);

            return await Task.Run(() =>
            {
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? ReadUserFromReader(reader) : null;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by ID {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Models.Domain.User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Username = @username COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("@username", username);

            return await Task.Run(() =>
            {
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? ReadUserFromReader(reader) : null;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by username {Username}", username);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<Models.Domain.User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Email = @email COLLATE NOCASE;";
            cmd.Parameters.AddWithValue("@email", email);

            return await Task.Run(() =>
            {
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? ReadUserFromReader(reader) : null;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user by email {Email}", email);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Models.Domain.User>> GetAllUsersAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();

            if (activeOnly.HasValue)
            {
                cmd.CommandText = "SELECT * FROM Users WHERE IsActive = @isActive ORDER BY CreatedAt DESC;";
                cmd.Parameters.AddWithValue("@isActive", activeOnly.Value ? 1 : 0);
            }
            else
            {
                cmd.CommandText = "SELECT * FROM Users ORDER BY CreatedAt DESC;";
            }

            return await Task.Run(() =>
            {
                var users = new List<Models.Domain.User>();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(ReadUserFromReader(reader));
                }
                return users;
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all users");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", userId);

            var rowsAffected = await Task.Run(() => cmd.ExecuteNonQuery(), cancellationToken);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetUserCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = CreateConnection();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users;";

            var result = await Task.Run(() => cmd.ExecuteScalar(), cancellationToken);
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user count");
            throw;
        }
    }

    private static Models.Domain.User ReadUserFromReader(SqliteDataReader reader)
    {
        return new Models.Domain.User
        {
            Id = reader.GetString(reader.GetOrdinal("Id")),
            Username = reader.GetString(reader.GetOrdinal("Username")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            DisplayName = reader.GetString(reader.GetOrdinal("DisplayName")),
            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
            PasswordSalt = reader.GetString(reader.GetOrdinal("PasswordSalt")),
            Role = (Models.Domain.UserRole)reader.GetInt32(reader.GetOrdinal("Role")),
            AvatarType = (Models.Domain.AvatarType)reader.GetInt32(reader.GetOrdinal("AvatarType")),
            AvatarData = reader.IsDBNull(reader.GetOrdinal("AvatarData")) ? null : reader.GetString(reader.GetOrdinal("AvatarData")),
            IsActive = reader.GetInt32(reader.GetOrdinal("IsActive")) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))).ToUniversalTime(),
            UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt"))).ToUniversalTime(),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("LastLoginAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastLoginAt"))).ToUniversalTime()
        };
    }

    #endregion

    /// <summary>
    /// Disposes the service. SQLite connections are opened per-operation so no cleanup needed.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _logger.LogDebug("SqlitePersistenceService disposed");
        return ValueTask.CompletedTask;
    }
}
