using CopilotSdk.Api.EventHandlers;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.AI;
using DomainSessionConfig = CopilotSdk.Api.Models.Domain.SessionConfig;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service implementation for session management operations.
/// </summary>
public class SessionService : ISessionService
{
    private readonly CopilotClientManager _clientManager;
    private readonly SessionManager _sessionManager;
    private readonly IToolExecutionService _toolExecutionService;
    private readonly IDevServerService _devServerService;
    private readonly IPersistenceService _persistenceService;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        CopilotClientManager clientManager,
        SessionManager sessionManager,
        IToolExecutionService toolExecutionService,
        IDevServerService devServerService,
        IPersistenceService persistenceService,
        ILogger<SessionService> logger)
    {
        _clientManager = clientManager;
        _sessionManager = sessionManager;
        _toolExecutionService = toolExecutionService;
        _devServerService = devServerService;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SessionInfoResponse> CreateSessionAsync(CreateSessionRequest request, string? creatorUserId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating session with model {Model} for user {UserId}", request.Model, creatorUserId ?? "anonymous");

        var config = new DomainSessionConfig
        {
            SessionId = request.SessionId,
            Model = request.Model,
            Streaming = request.Streaming,
            SystemMessage = request.SystemMessage,
            AvailableTools = request.AvailableTools,
            ExcludedTools = request.ExcludedTools,
            Provider = request.Provider,
            Tools = request.Tools
        };

        // Build AIFunction collection from tool definitions
        ICollection<AIFunction>? tools = null;
        if (request.Tools != null && request.Tools.Count > 0)
        {
            _logger.LogInformation("Building {ToolCount} custom tools for session", request.Tools.Count);
            tools = _toolExecutionService.BuildAIFunctions(request.Tools);
        }

        var session = await _clientManager.CreateSessionAsync(config, tools, cancellationToken);

        // Register the session in the SessionManager (persists to disk)
        await _sessionManager.RegisterSessionAsync(session.SessionId, session, config, creatorUserId, cancellationToken);

        // Store the app path if provided
        if (!string.IsNullOrWhiteSpace(request.AppPath))
        {
            var metadata2 = await _sessionManager.GetMetadataAsync(session.SessionId, cancellationToken);
            if (metadata2 != null)
            {
                metadata2.AppPath = request.AppPath;
                await _sessionManager.PersistSessionAsync(session.SessionId, metadata2, cancellationToken);
            }
        }

        var metadata = await _sessionManager.GetMetadataAsync(session.SessionId, cancellationToken);

        return new SessionInfoResponse
        {
            SessionId = session.SessionId,
            Model = config.Model,
            Streaming = config.Streaming,
            CreatedAt = metadata?.CreatedAt ?? DateTime.UtcNow,
            LastActivityAt = metadata?.LastActivityAt,
            Status = "Active",
            MessageCount = 0,
            Summary = metadata?.Summary,
            CreatorUserId = creatorUserId
        };
    }

    /// <inheritdoc/>
    public async Task<SessionInfoResponse> ResumeSessionAsync(string sessionId, ResumeSessionRequest? request = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming session {SessionId}", sessionId);

        // Build AIFunction collection from tool definitions if provided
        ICollection<AIFunction>? tools = null;
        if (request?.Tools != null && request.Tools.Count > 0)
        {
            _logger.LogInformation("Building {ToolCount} custom tools for resumed session", request.Tools.Count);
            tools = _toolExecutionService.BuildAIFunctions(request.Tools);
        }

        var session = await _clientManager.ResumeSessionAsync(
            sessionId,
            request?.Streaming ?? false,
            request?.Provider,
            tools,
            cancellationToken);

        // Update the session in the SessionManager (persists to disk)
        await _sessionManager.UpdateResumedSessionAsync(session.SessionId, session, cancellationToken);

        var metadata = await _sessionManager.GetMetadataAsync(session.SessionId, cancellationToken);

        return new SessionInfoResponse
        {
            SessionId = session.SessionId,
            Model = metadata?.Config?.Model ?? "unknown",
            Streaming = request?.Streaming ?? metadata?.Config?.Streaming ?? false,
            CreatedAt = metadata?.CreatedAt ?? DateTime.UtcNow,
            LastActivityAt = metadata?.LastActivityAt ?? DateTime.UtcNow,
            Status = "Active",
            MessageCount = metadata?.MessageCount ?? 0,
            Summary = metadata?.Summary,
            CreatorUserId = metadata?.CreatorUserId,
            CreatorDisplayName = await GetCreatorDisplayNameAsync(metadata?.CreatorUserId, cancellationToken)
        };
    }

    /// <inheritdoc/>
    public async Task<SessionListResponse> ListSessionsAsync(User? user = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing sessions for user {UserId} (role: {Role})", user?.Id ?? "anonymous", user?.Role.ToString() ?? "none");

        // Get all metadata from persistence (file system is the source of truth)
        var allMetadata = await _sessionManager.GetAllMetadataAsync(cancellationToken);

        // Apply role-based filtering
        IEnumerable<Models.Domain.SessionMetadata> filteredMetadata;
        if (user == null)
        {
            // No user context (unauthenticated) - return all for backwards compatibility
            filteredMetadata = allMetadata;
        }
        else if (user.Role == UserRole.Admin)
        {
            // Admins see all sessions
            filteredMetadata = allMetadata;
        }
        else if (user.Role == UserRole.Creator)
        {
            // Creators see only sessions they created
            filteredMetadata = allMetadata.Where(m => m.CreatorUserId == user.Id);
        }
        else
        {
            // Players see only sessions that have a dev server running (playable games)
            filteredMetadata = allMetadata.Where(m => m.IsDevServerRunning || m.DevServerPort.HasValue);
        }

        // Look up creator display names
        var creatorUserIds = filteredMetadata
            .Select(m => m.CreatorUserId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var creatorNames = new Dictionary<string, string>();
        foreach (var creatorId in creatorUserIds)
        {
            var creator = await _persistenceService.GetUserByIdAsync(creatorId!, cancellationToken);
            if (creator != null)
            {
                creatorNames[creatorId!] = creator.DisplayName;
            }
        }

        var sessions = filteredMetadata.Select(meta => new SessionInfoResponse
        {
            SessionId = meta.SessionId,
            Model = meta.Config?.Model ?? "unknown",
            Streaming = meta.Config?.Streaming ?? false,
            CreatedAt = meta.CreatedAt ?? DateTime.MinValue,
            LastActivityAt = meta.LastActivityAt,
            Status = _sessionManager.IsSessionActive(meta.SessionId) ? "Active" : "Inactive",
            MessageCount = meta.MessageCount,
            Summary = meta.Summary,
            CreatorUserId = meta.CreatorUserId,
            CreatorDisplayName = meta.CreatorUserId != null && creatorNames.TryGetValue(meta.CreatorUserId, out var name) ? name : null
        }).ToList();

        return new SessionListResponse
        {
            Sessions = sessions,
            TotalCount = sessions.Count
        };
    }

    /// <inheritdoc/>
    public async Task<SessionInfoResponse?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting session {SessionId}", sessionId);

        // First check persistence (source of truth)
        var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken);

        if (metadata == null)
        {
            // Try to get from SDK session list
            var sdkSessions = await _clientManager.ListSessionsAsync(cancellationToken);
            var sdkSession = sdkSessions.FirstOrDefault(s => s.SessionId == sessionId);

            if (sdkSession == null)
            {
                return null;
            }

            // Found in SDK but not in persistence
            return new SessionInfoResponse
            {
                SessionId = sdkSession.SessionId,
                Model = "unknown",
                Streaming = false,
                CreatedAt = sdkSession.StartTime,
                LastActivityAt = sdkSession.ModifiedTime,
                Status = "Inactive",
                MessageCount = 0,
                Summary = sdkSession.Summary
            };
        }

        return new SessionInfoResponse
        {
            SessionId = metadata.SessionId,
            Model = metadata.Config?.Model ?? "unknown",
            Streaming = metadata.Config?.Streaming ?? false,
            CreatedAt = metadata.CreatedAt ?? DateTime.MinValue,
            LastActivityAt = metadata.LastActivityAt,
            Status = _sessionManager.IsSessionActive(sessionId) ? "Active" : "Inactive",
            MessageCount = metadata.MessageCount,
            Summary = metadata.Summary,
            CreatorUserId = metadata.CreatorUserId,
            CreatorDisplayName = await GetCreatorDisplayNameAsync(metadata.CreatorUserId, cancellationToken)
        };
    }

    /// <summary>
    /// Looks up the display name for a creator user ID.
    /// </summary>
    private async Task<string?> GetCreatorDisplayNameAsync(string? creatorUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(creatorUserId)) return null;
        var creator = await _persistenceService.GetUserByIdAsync(creatorUserId, cancellationToken);
        return creator?.DisplayName;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting session {SessionId}", sessionId);

        try
        {
            // Delete from SDK
            await _clientManager.DeleteSessionAsync(sessionId, cancellationToken);

            // Remove from persistence and active tracking
            await _sessionManager.RemoveSessionAsync(sessionId, cancellationToken);

            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to delete"))
        {
            _logger.LogWarning("Session {SessionId} not found in SDK: {Message}", sessionId, ex.Message);

            // Still try to remove from persistence
            return await _sessionManager.RemoveSessionAsync(sessionId, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<SendMessageResponse> SendMessageAsync(string sessionId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending message to session {SessionId}", sessionId);

        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for sending message", sessionId);
            return new SendMessageResponse
            {
                SessionId = sessionId,
                Success = false,
                Error = $"Session '{sessionId}' not found or not active"
            };
        }

        try
        {
            // Convert attachments to SDK format
            List<UserMessageDataAttachmentsItem>? attachments = null;
            if (request.Attachments != null && request.Attachments.Count > 0)
            {
                attachments = request.Attachments.Select(a => new UserMessageDataAttachmentsItem
                {
                    Type = a.Type?.ToLowerInvariant() == "directory" 
                        ? UserMessageDataAttachmentsItemType.Directory 
                        : UserMessageDataAttachmentsItemType.File,
                    Path = a.Path ?? string.Empty,
                    DisplayName = a.DisplayName ?? a.Path ?? string.Empty
                }).ToList();
            }

            var messageOptions = new MessageOptions
            {
                Prompt = request.Prompt,
                Mode = request.Mode,
                Attachments = attachments
            };

            var messageId = await session.SendAsync(messageOptions, cancellationToken);

            // Update session metadata in persistence
            await _sessionManager.IncrementMessageCountAsync(sessionId, cancellationToken);

            // Persist the user message
            var persistedMessage = new PersistedMessage
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Role = "user",
                Content = request.Prompt,
                Attachments = request.Attachments?.Select(a => new PersistedAttachment
                {
                    Type = a.Type ?? "file",
                    Path = a.Path ?? string.Empty,
                    DisplayName = a.DisplayName
                }).ToList()
            };
            await _sessionManager.AppendMessagesAsync(sessionId, new[] { persistedMessage }, cancellationToken);

            return new SendMessageResponse
            {
                SessionId = sessionId,
                MessageId = messageId,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to session {SessionId}", sessionId);
            return new SendMessageResponse
            {
                SessionId = sessionId,
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<MessagesResponse> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting messages for session {SessionId}", sessionId);

        var session = _sessionManager.GetSession(sessionId);
        
        // If we have an active SDK session, get messages from it
        if (session != null)
        {
            try
            {
                var sdkEvents = await session.GetMessagesAsync(cancellationToken);
                var events = sdkEvents.Select(MapEventToDto).ToList();

                return new MessagesResponse
                {
                    SessionId = sessionId,
                    Events = events,
                    TotalCount = events.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages from SDK session {SessionId}, falling back to persisted messages", sessionId);
                // Fall through to try persisted messages
            }
        }

        // No active session or error - try to get persisted messages
        _logger.LogInformation("No active SDK session for {SessionId}, loading persisted messages", sessionId);
        var persistedMessages = await _sessionManager.GetPersistedMessagesAsync(sessionId, cancellationToken);

        if (persistedMessages.Count > 0)
        {
            _logger.LogInformation("Found {Count} persisted messages for session {SessionId}", persistedMessages.Count, sessionId);
            var events = persistedMessages.Select(MapPersistedMessageToDto).ToList();

            return new MessagesResponse
            {
                SessionId = sessionId,
                Events = events,
                TotalCount = events.Count
            };
        }

        _logger.LogWarning("No messages found for session {SessionId}", sessionId);
        return new MessagesResponse
        {
            SessionId = sessionId,
            Events = new List<SessionEventDto>(),
            TotalCount = 0
        };
    }

    /// <summary>
    /// Maps a persisted message to a session event DTO.
    /// </summary>
    private static SessionEventDto MapPersistedMessageToDto(PersistedMessage message)
    {
        var dto = new SessionEventDto
        {
            Id = message.Id,
            Timestamp = new DateTimeOffset(message.Timestamp, TimeSpan.Zero),
            Ephemeral = false
        };

        switch (message.Role.ToLowerInvariant())
        {
            case "user":
                dto.Type = "user.message";
                dto.Data = new UserMessageDataDto
                {
                    Content = message.Content,
                    TransformedContent = message.TransformedContent,
                    Source = message.Source,
                    Attachments = message.Attachments?.Select(a => new MessageAttachmentDto
                    {
                        Type = a.Type,
                        Path = a.Path,
                        DisplayName = a.DisplayName
                    }).ToList()
                };
                break;

            case "assistant":
                dto.Type = "assistant.message";
                dto.Data = new AssistantMessageDataDto
                {
                    MessageId = message.MessageId ?? message.Id.ToString(),
                    Content = message.Content,
                    ParentToolCallId = message.ParentToolCallId,
                    ToolRequests = message.ToolRequests?.Select(t => new ToolRequestDto
                    {
                        ToolCallId = t.ToolCallId,
                        ToolName = t.ToolName,
                        Arguments = t.Arguments
                    }).ToList()
                };
                break;

            case "tool":
                dto.Type = "tool.execution_complete";
                dto.Data = new ToolExecutionCompleteDataDto
                {
                    ToolCallId = message.ToolCallId ?? string.Empty,
                    ToolName = message.ToolName ?? string.Empty,
                    Result = message.ToolResult,
                    Error = message.ToolError
                };
                break;

            default:
                // For other types, use the role as-is
                dto.Type = message.Role;
                dto.Data = new { content = message.Content };
                break;
        }

        return dto;
    }

    /// <inheritdoc/>
    public async Task<bool> AbortAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Aborting session {SessionId}", sessionId);

        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session {SessionId} not found for abort", sessionId);
            return false;
        }

        try
        {
            await session.AbortAsync(cancellationToken);
            await _sessionManager.UpdateLastActivityAsync(sessionId, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error aborting session {SessionId}", sessionId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<PersistedMessagesResponse> GetPersistedHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting persisted history for session {SessionId}", sessionId);

        var messages = await _sessionManager.GetPersistedMessagesAsync(sessionId, cancellationToken);
        var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken);

        return new PersistedMessagesResponse
        {
            SessionId = sessionId,
            Messages = messages,
            TotalCount = messages.Count,
            CreatedAt = metadata?.CreatedAt,
            LastActivityAt = metadata?.LastActivityAt
        };
    }

    /// <summary>
    /// Maps an SDK session event to a DTO.
    /// </summary>
    private static SessionEventDto MapEventToDto(SessionEvent sdkEvent)
    {
        var dto = new SessionEventDto
        {
            Id = sdkEvent.Id,
            Type = sdkEvent.Type,
            Timestamp = sdkEvent.Timestamp,
            ParentId = sdkEvent.ParentId,
            Ephemeral = sdkEvent.Ephemeral
        };

        // Map specific event data based on type
        dto.Data = sdkEvent switch
        {
            UserMessageEvent e => new UserMessageDataDto
            {
                Content = e.Data.Content,
                TransformedContent = e.Data.TransformedContent,
                Source = e.Data.Source,
                Attachments = e.Data.Attachments?.Select(a => new MessageAttachmentDto
                {
                    Type = a.Type.ToString().ToLowerInvariant(),
                    Path = a.Path,
                    DisplayName = a.DisplayName
                }).ToList()
            },
            AssistantMessageEvent e => new AssistantMessageDataDto
            {
                MessageId = e.Data.MessageId,
                Content = e.Data.Content,
                ParentToolCallId = e.Data.ParentToolCallId,
                ToolRequests = e.Data.ToolRequests?.Select(tr => new ToolRequestDto
                {
                    ToolCallId = tr.ToolCallId,
                    ToolName = tr.Name,
                    Arguments = tr.Arguments
                }).ToList()
            },
            AssistantMessageDeltaEvent e => new AssistantMessageDeltaDataDto
            {
                MessageId = e.Data.MessageId,
                DeltaContent = e.Data.DeltaContent,
                TotalResponseSizeBytes = e.Data.TotalResponseSizeBytes,
                ParentToolCallId = e.Data.ParentToolCallId
            },
            AssistantReasoningEvent e => new AssistantReasoningDataDto
            {
                ReasoningId = e.Data.ReasoningId,
                Content = e.Data.Content
            },
            AssistantReasoningDeltaEvent e => new AssistantReasoningDeltaDataDto
            {
                ReasoningId = e.Data.ReasoningId,
                DeltaContent = e.Data.DeltaContent
            },
            SessionStartEvent e => new SessionStartDataDto
            {
                SessionId = e.Data.SessionId,
                Version = e.Data.Version,
                Producer = e.Data.Producer,
                CopilotVersion = e.Data.CopilotVersion,
                StartTime = e.Data.StartTime,
                SelectedModel = e.Data.SelectedModel
            },
            SessionErrorEvent e => new SessionErrorDataDto
            {
                ErrorType = e.Data.ErrorType,
                Message = e.Data.Message,
                Stack = e.Data.Stack
            },
            SessionIdleEvent => new SessionIdleDataDto(),
            ToolExecutionStartEvent e => new ToolExecutionStartDataDto
            {
                ToolCallId = e.Data.ToolCallId,
                ToolName = e.Data.ToolName,
                Arguments = e.Data.Arguments
            },
            ToolExecutionCompleteEvent e => new ToolExecutionCompleteDataDto
            {
                ToolCallId = e.Data.ToolCallId,
                ToolName = string.Empty,
                Result = e.Data.Result?.Content,
                Error = e.Data.Error?.Message
            },
            AssistantTurnStartEvent e => new AssistantTurnStartDataDto
            {
                TurnId = e.Data.TurnId
            },
            AssistantTurnEndEvent e => new AssistantTurnEndDataDto
            {
                TurnId = e.Data.TurnId
            },
            AssistantUsageEvent e => new AssistantUsageDataDto
            {
                Model = e.Data.Model,
                InputTokens = e.Data.InputTokens,
                OutputTokens = e.Data.OutputTokens,
                CacheReadTokens = e.Data.CacheReadTokens,
                CacheWriteTokens = e.Data.CacheWriteTokens,
                Cost = e.Data.Cost,
                Duration = e.Data.Duration
            },
            _ => null
        };

        return dto;
    }

    /// <inheritdoc/>
    public async Task<DevServerResponse> StartDevServerAsync(string sessionId, string? appPath, CancellationToken cancellationToken = default)
    {
        // Get session metadata (may be null for sessions that only exist as repos)
        var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken);

        // Use provided appPath, then session metadata, then auto-detect
        var targetPath = appPath ?? metadata?.AppPath ?? await FindAppPathAsync(sessionId, cancellationToken);
        _logger.LogInformation("Starting dev server for session {SessionId} with resolved path: {TargetPath}", sessionId, targetPath);

        // Persist the resolved path immediately so future attempts don't need to re-detect
        if (metadata != null && string.IsNullOrEmpty(metadata.AppPath) && targetPath != null)
        {
            metadata.AppPath = targetPath;
            await _sessionManager.PersistSessionAsync(sessionId, metadata, cancellationToken);
        }

        var result = await _devServerService.StartDevServerAsync(sessionId, targetPath, cancellationToken);
        
        if (result.success && metadata != null)
        {
            // Update session metadata with running server info
            metadata.AppPath = targetPath;
            metadata.DevServerPort = result.port;
            metadata.IsDevServerRunning = true;
            await _sessionManager.PersistSessionAsync(sessionId, metadata, cancellationToken);
        }

        return new DevServerResponse
        {
            Success = result.success,
            Pid = result.pid,
            Port = result.port,
            Url = result.success ? result.url : string.Empty,
            Message = result.message
        };
    }

    /// <inheritdoc/>
    public async Task<DevServerStopResponse> StopDevServerAsync(string sessionId, int pid, CancellationToken cancellationToken = default)
    {
        var (stopped, message) = await _devServerService.StopDevServerAsync(sessionId, pid, cancellationToken);
        
        if (stopped)
        {
            var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken);
            if (metadata != null)
            {
                metadata.IsDevServerRunning = false;
                metadata.DevServerPort = null;
                await _sessionManager.PersistSessionAsync(sessionId, metadata, cancellationToken);
            }
        }

        return new DevServerStopResponse
        {
            Stopped = stopped,
            Message = message
        };
    }

    /// <inheritdoc/>
    public async Task<DevServerStatusResponse> GetDevServerStatusAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var (isRunning, pid, port) = await _devServerService.GetDevServerStatusAsync(sessionId, cancellationToken);
        
        return new DevServerStatusResponse
        {
            IsRunning = isRunning,
            Pid = pid,
            Port = port,
            Url = isRunning && port.HasValue ? _devServerService.GetDevServerUrl(port.Value) : null
        };
    }

    /// <inheritdoc/>
    public async Task SetAppPathAsync(string sessionId, string appPath, CancellationToken cancellationToken = default)
    {
        var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken)
            ?? throw new InvalidOperationException($"Session {sessionId} not found");

        metadata.AppPath = appPath;
        await _sessionManager.PersistSessionAsync(sessionId, metadata, cancellationToken);
    }

    /// <summary>
    /// Attempts to find the app path for a session by checking common locations.
    /// Uses multiple strategies: exact name match, normalized name match, substring match,
    /// and finally a timestamp-based heuristic (repo created close to session creation time).
    /// </summary>
    private async Task<string> FindAppPathAsync(string sessionId, CancellationToken cancellationToken)
    {
        var reposDir = @"C:\development\repos";
        var normalizedSessionId = NormalizeName(sessionId);

        // Collect all candidate repos (directories with package.json) up front
        var candidateRepos = new List<(string Path, string DirName)>();
        if (Directory.Exists(reposDir))
        {
            foreach (var dir in Directory.GetDirectories(reposDir))
            {
                if (File.Exists(Path.Combine(dir, "package.json")))
                {
                    candidateRepos.Add((dir, Path.GetFileName(dir)));
                }
            }
        }

        // --- Strategy 1: Exact match by session ID ---
        var possiblePaths = new[]
        {
            Path.Combine(reposDir, sessionId),
            Path.Combine(reposDir, sessionId.ToLowerInvariant()),
            Path.Combine(reposDir, sessionId.Replace("_", "-")),
        };

        foreach (var path in possiblePaths)
        {
            if (candidateRepos.Any(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Found app for session {SessionId} at {Path} (exact)", sessionId, path);
                return path;
            }
        }

        // --- Strategy 2: Check inside the copilot-sdk project for embedded apps ---
        var projectRoot = Path.GetDirectoryName(AppContext.BaseDirectory);
        var currentDir = projectRoot;
        while (currentDir != null)
        {
            var apiDir = Path.Combine(currentDir, "src", "CopilotSdk.Api");
            if (Directory.Exists(apiDir))
            {
                var embeddedPath = Path.Combine(apiDir, sessionId);
                if (Directory.Exists(embeddedPath) && File.Exists(Path.Combine(embeddedPath, "package.json")))
                {
                    _logger.LogInformation("Found embedded app for session {SessionId} at {Path}", sessionId, embeddedPath);
                    return embeddedPath;
                }

                foreach (var dir in Directory.GetDirectories(apiDir))
                {
                    if (File.Exists(Path.Combine(dir, "package.json")) &&
                        NormalizeName(Path.GetFileName(dir)) == normalizedSessionId)
                    {
                        _logger.LogInformation("Found embedded app for session {SessionId} at {Path} (fuzzy)", sessionId, dir);
                        return dir;
                    }
                }

                break;
            }
            currentDir = Path.GetDirectoryName(currentDir);
        }

        // Also check directly from the known project structure path
        var knownProjectPath = Path.Combine(reposDir, "copilot-sdk", "src", "CopilotSdk.Api", sessionId);
        if (Directory.Exists(knownProjectPath) && File.Exists(Path.Combine(knownProjectPath, "package.json")))
        {
            _logger.LogInformation("Found app for session {SessionId} at {Path}", sessionId, knownProjectPath);
            return knownProjectPath;
        }

        // --- Strategy 3: Normalized name match across repos ---
        foreach (var (path, dirName) in candidateRepos)
        {
            if (NormalizeName(dirName) == normalizedSessionId)
            {
                _logger.LogInformation("Found app for session {SessionId} at {Path} (normalized match)", sessionId, path);
                return path;
            }
        }

        // --- Strategy 4: Bidirectional substring match ---
        foreach (var (path, dirName) in candidateRepos)
        {
            if (dirName.Contains(sessionId, StringComparison.OrdinalIgnoreCase) ||
                sessionId.Contains(dirName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Found app for session {SessionId} at {Path} (substring match)", sessionId, path);
                return path;
            }
        }

        // --- Strategy 5: Timestamp-based heuristic ---
        // When the session name has no string relation to the repo folder, try to match
        // by creation time: find the repo folder created closest to the session start.
        var metadata = await _sessionManager.GetMetadataAsync(sessionId, cancellationToken);
        if (metadata?.CreatedAt != null && candidateRepos.Count > 0)
        {
            var sessionCreated = metadata.CreatedAt.Value;
            // Gather already-mapped app paths from other sessions so we can exclude them
            var allMetadata = await _sessionManager.GetAllMetadataAsync(cancellationToken);
            var usedPaths = new HashSet<string>(
                allMetadata
                    .Where(m => !string.IsNullOrEmpty(m.AppPath) && m.SessionId != sessionId)
                    .Select(m => m.AppPath!),
                StringComparer.OrdinalIgnoreCase);

            // Score each candidate by how close its creation time is to the session creation
            var scored = candidateRepos
                .Where(c => !usedPaths.Contains(c.Path))                 // exclude repos already claimed by other sessions
                .Where(c => !c.DirName.Equals("copilot-sdk", StringComparison.OrdinalIgnoreCase)) // exclude this project
                .Select(c =>
                {
                    var dirInfo = new DirectoryInfo(c.Path);
                    // Use the earlier of CreationTime and LastWriteTime (some folders are moved/copied)
                    var folderTime = dirInfo.CreationTime < dirInfo.LastWriteTime
                        ? dirInfo.CreationTime
                        : dirInfo.LastWriteTime;
                    var diff = Math.Abs((folderTime.ToUniversalTime() - sessionCreated).TotalMinutes);
                    return new { c.Path, c.DirName, FolderTime = folderTime, DiffMinutes = diff };
                })
                .OrderBy(x => x.DiffMinutes)
                .ToList();

            if (scored.Count > 0)
            {
                var best = scored[0];
                // Only accept if the closest repo was created within 24 hours of the session
                if (best.DiffMinutes <= 24 * 60)
                {
                    _logger.LogInformation(
                        "Found app for session {SessionId} at {Path} (timestamp match, diff={DiffMin:F0} min, folder created {FolderTime})",
                        sessionId, best.Path, best.DiffMinutes, best.FolderTime);
                    return best.Path;
                }
                else
                {
                    _logger.LogWarning(
                        "Closest repo for session {SessionId} is {Path} but {DiffMin:F0} min away — too far to auto-match",
                        sessionId, best.Path, best.DiffMinutes);
                }
            }
        }

        // Default to the session ID path even if it doesn't exist (will fail with proper error)
        _logger.LogWarning("Could not auto-detect app path for session {SessionId}, defaulting to {Path}", sessionId, Path.Combine(reposDir, sessionId));
        return Path.Combine(reposDir, sessionId);
    }

    /// <summary>
    /// Normalizes a name by lowering, removing common suffixes/words, and stripping non-alphanumeric chars.
    /// </summary>
    private static string NormalizeName(string name)
    {
        var lower = name.ToLowerInvariant();
        // Remove common suffixes/words that vary between session IDs and repo names
        lower = lower.Replace("game", "").Replace("app", "").Replace("the", "")
                     .Replace("-", "").Replace("_", "").Replace(" ", "");
        return lower;
    }

    /// <inheritdoc/>
    public async Task<int> BackfillAppPathsAsync(CancellationToken cancellationToken = default)
    {
        var allMetadata = await _sessionManager.GetAllMetadataAsync(cancellationToken);

        // Process all sessions: those without AppPath, plus those whose AppPath may be incorrect
        _logger.LogInformation("Checking AppPath for {Count} sessions", allMetadata.Count);
        var updated = 0;

        foreach (var metadata in allMetadata)
        {
            try
            {
                var detectedFolder = await DetectRepoFolderFromMessagesAsync(metadata.SessionId, cancellationToken);
                if (detectedFolder == null)
                    continue;

                var reposDir = @"C:\development\repos";
                var fullPath = Path.Combine(reposDir, detectedFolder);

                if (!Directory.Exists(fullPath) || !File.Exists(Path.Combine(fullPath, "package.json")))
                {
                    _logger.LogDebug(
                        "Session {SessionId}: detected folder '{Folder}' but path does not exist or has no package.json",
                        metadata.SessionId, detectedFolder);
                    continue;
                }

                // Check if the existing AppPath already matches
                if (string.Equals(metadata.AppPath, fullPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var action = string.IsNullOrEmpty(metadata.AppPath) ? "Set" : $"Corrected (was: {metadata.AppPath})";
                metadata.AppPath = fullPath;
                await _sessionManager.PersistSessionAsync(metadata.SessionId, metadata, cancellationToken);
                updated++;
                _logger.LogInformation(
                    "{Action} AppPath for session {SessionId}: {AppPath}",
                    action, metadata.SessionId, fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error backfilling AppPath for session {SessionId}", metadata.SessionId);
            }
        }

        _logger.LogInformation("Updated AppPath for {Updated}/{Total} sessions", updated, allMetadata.Count);
        return updated;
    }

    /// <summary>
    /// Scans persisted messages for a session to detect the repo folder from tool results or assistant content.
    /// </summary>
    private async Task<string?> DetectRepoFolderFromMessagesAsync(string sessionId, CancellationToken cancellationToken)
    {
        var messages = await _sessionManager.GetPersistedMessagesAsync(sessionId, cancellationToken);

        // Scan tool results for repo path references (most reliable)
        foreach (var msg in messages.Where(m => m.Role == "tool" && !string.IsNullOrEmpty(m.ToolResult)))
        {
            var folder = SessionEventDispatcher.ExtractRepoFolder(msg.ToolResult!);
            if (folder != null && !folder.Equals("copilot-sdk", StringComparison.OrdinalIgnoreCase))
                return folder;
        }

        // Fall back to scanning assistant message content
        foreach (var msg in messages.Where(m => m.Role == "assistant" && !string.IsNullOrEmpty(m.Content)))
        {
            var folder = SessionEventDispatcher.ExtractRepoFolder(msg.Content!);
            if (folder != null && !folder.Equals("copilot-sdk", StringComparison.OrdinalIgnoreCase))
                return folder;
        }

        return null;
    }
}
