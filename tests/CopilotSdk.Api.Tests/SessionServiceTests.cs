using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for SessionService.
/// Note: These tests focus on the service logic. Integration with CopilotClientManager
/// requires a running Copilot CLI server, which is covered in integration tests.
/// </summary>
public class SessionServiceTests
{
    private readonly Mock<ILogger<SessionManager>> _sessionManagerLoggerMock;
    private readonly SessionManager _sessionManager;
    private readonly Mock<ILogger<SessionService>> _serviceLoggerMock;

    public SessionServiceTests()
    {
        _sessionManagerLoggerMock = new Mock<ILogger<SessionManager>>();
        _sessionManager = new SessionManager(_sessionManagerLoggerMock.Object);
        _serviceLoggerMock = new Mock<ILogger<SessionService>>();
    }

    #region CreateSessionAsync Request Mapping Tests

    [Fact]
    public void CreateSessionRequest_MapsToSessionConfig_Correctly()
    {
        // Arrange
        var request = new CreateSessionRequest
        {
            SessionId = "custom-session-id",
            Model = "gpt-4",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = "Append",
                Content = "You are a helpful assistant."
            },
            AvailableTools = new List<string> { "tool1", "tool2" },
            ExcludedTools = new List<string> { "tool3" },
            Provider = new ProviderConfig
            {
                Type = "openai",
                BaseUrl = "https://api.openai.com",
                ApiKey = "test-key"
            },
            Tools = new List<ToolDefinition>
            {
                new()
                {
                    Name = "test_tool",
                    Description = "A test tool",
                    Parameters = new List<ToolParameter>
                    {
                        new() { Name = "param1", Type = "string", Required = true }
                    }
                }
            }
        };

        // Act - Convert request to config
        var config = new SessionConfig
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

        // Assert
        Assert.Equal("custom-session-id", config.SessionId);
        Assert.Equal("gpt-4", config.Model);
        Assert.True(config.Streaming);
        Assert.NotNull(config.SystemMessage);
        Assert.Equal("Append", config.SystemMessage.Mode);
        Assert.Equal("You are a helpful assistant.", config.SystemMessage.Content);
        Assert.NotNull(config.AvailableTools);
        Assert.Equal(2, config.AvailableTools.Count);
        Assert.NotNull(config.ExcludedTools);
        Assert.Single(config.ExcludedTools);
        Assert.NotNull(config.Provider);
        Assert.Equal("openai", config.Provider.Type);
        Assert.NotNull(config.Tools);
        Assert.Single(config.Tools);
        Assert.Equal("test_tool", config.Tools[0].Name);
    }

    #endregion

    #region SessionId Validation Tests

    [Theory]
    [InlineData("valid-session")]
    [InlineData("ValidSession123")]
    [InlineData("session_with_underscores")]
    [InlineData("test-session_123")]
    [InlineData("abc")]
    public void CreateSessionRequest_ValidSessionIds_PassValidation(string sessionId)
    {
        // Arrange
        var request = new CreateSessionRequest { SessionId = sessionId, Model = "gpt-4" };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("session with spaces")]
    [InlineData("session@name")]
    [InlineData("session!")]
    [InlineData("session#123")]
    [InlineData("session.name")]
    [InlineData("session/name")]
    public void CreateSessionRequest_InvalidSessionIds_FailValidation(string sessionId)
    {
        // Arrange
        var request = new CreateSessionRequest { SessionId = sessionId, Model = "gpt-4" };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        Assert.Single(results);
        Assert.Contains("Session ID can only contain letters, numbers, hyphens", results[0].ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateSessionRequest_NullOrEmptySessionId_PassValidation(string? sessionId)
    {
        // Arrange
        var request = new CreateSessionRequest { SessionId = sessionId, Model = "gpt-4" };
        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(request, context, results, validateAllProperties: true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    #endregion

    #region ResumeSessionRequest Tests

    [Fact]
    public void ResumeSessionRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new ResumeSessionRequest();

        // Assert
        Assert.False(request.Streaming);
        Assert.Null(request.Provider);
        Assert.Null(request.Tools);
    }

    [Fact]
    public void ResumeSessionRequest_CanSetAllProperties()
    {
        // Arrange & Act
        var request = new ResumeSessionRequest
        {
            Streaming = true,
            Provider = new ProviderConfig
            {
                Type = "azure",
                BaseUrl = "https://myazure.openai.azure.com"
            },
            Tools = new List<ToolDefinition>
            {
                new() { Name = "resume_tool", Description = "Tool for resumed session" }
            }
        };

        // Assert
        Assert.True(request.Streaming);
        Assert.NotNull(request.Provider);
        Assert.Equal("azure", request.Provider.Type);
        Assert.NotNull(request.Tools);
        Assert.Single(request.Tools);
    }

    #endregion

    #region SessionManager Integration Tests

    [Fact]
    public void SessionManager_CanRegisterAndRetrieveSession()
    {
        // Arrange
        var sessionId = "test-session";
        var config = new SessionConfig
        {
            Model = "gpt-4",
            Streaming = true
        };

        // Act
        _sessionManager.RegisterSession(sessionId, null!, config);
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(sessionId, metadata.SessionId);
        Assert.Equal("gpt-4", metadata.Config?.Model);
        Assert.True(metadata.Config?.Streaming);
    }

    [Fact]
    public void SessionManager_TracksMessageCount()
    {
        // Arrange
        var sessionId = "test-session";
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, null!, config);

        // Act
        _sessionManager.IncrementMessageCount(sessionId);
        _sessionManager.IncrementMessageCount(sessionId);
        var metadata = _sessionManager.GetMetadata(sessionId);

        // Assert
        Assert.Equal(2, metadata?.MessageCount);
    }

    [Fact]
    public void SessionManager_UpdatesLastActivity()
    {
        // Arrange
        var sessionId = "test-session";
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, null!, config);
        var initialTime = _sessionManager.GetMetadata(sessionId)?.LastActivityAt;

        // Wait to ensure time difference
        Thread.Sleep(50);

        // Act
        _sessionManager.UpdateLastActivity(sessionId);
        var updatedTime = _sessionManager.GetMetadata(sessionId)?.LastActivityAt;

        // Assert
        Assert.True(updatedTime > initialTime);
    }

    [Fact]
    public void SessionManager_RemoveSession_CleansUpProperly()
    {
        // Arrange
        var sessionId = "test-session";
        var config = new SessionConfig { Model = "gpt-4" };
        _sessionManager.RegisterSession(sessionId, null!, config);

        // Act
        var removed = _sessionManager.RemoveSession(sessionId);

        // Assert
        Assert.True(removed);
        Assert.Null(_sessionManager.GetMetadata(sessionId));
        Assert.Null(_sessionManager.GetSession(sessionId));
        Assert.False(_sessionManager.SessionExists(sessionId));
    }

    #endregion

    #region SessionInfo Response Mapping Tests

    [Fact]
    public void SessionInfoResponse_CanBeCreatedFromMetadata()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddMinutes(-10);
        var lastActivityAt = DateTime.UtcNow;
        var metadata = new SessionMetadata
        {
            SessionId = "test-session",
            CreatedAt = createdAt,
            LastActivityAt = lastActivityAt,
            MessageCount = 5,
            Summary = "Test conversation",
            Config = new SessionConfig
            {
                Model = "gpt-4",
                Streaming = true
            }
        };

        // Act
        var response = new Models.Responses.SessionInfoResponse
        {
            SessionId = metadata.SessionId,
            Model = metadata.Config?.Model ?? "unknown",
            Streaming = metadata.Config?.Streaming ?? false,
            CreatedAt = metadata.CreatedAt ?? DateTime.MinValue,
            LastActivityAt = metadata.LastActivityAt,
            Status = "Active",
            MessageCount = metadata.MessageCount,
            Summary = metadata.Summary
        };

        // Assert
        Assert.Equal("test-session", response.SessionId);
        Assert.Equal("gpt-4", response.Model);
        Assert.True(response.Streaming);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(lastActivityAt, response.LastActivityAt);
        Assert.Equal("Active", response.Status);
        Assert.Equal(5, response.MessageCount);
        Assert.Equal("Test conversation", response.Summary);
    }

    #endregion

    #region SessionListResponse Tests

    [Fact]
    public void SessionListResponse_CanContainMultipleSessions()
    {
        // Arrange & Act
        var response = new Models.Responses.SessionListResponse
        {
            Sessions = new List<Models.Responses.SessionInfoResponse>
            {
                new() { SessionId = "session-1", Model = "gpt-4" },
                new() { SessionId = "session-2", Model = "gpt-3.5-turbo" },
                new() { SessionId = "session-3", Model = "gpt-4-turbo" }
            },
            TotalCount = 3
        };

        // Assert
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.Sessions.Count);
        Assert.Contains(response.Sessions, s => s.SessionId == "session-1");
        Assert.Contains(response.Sessions, s => s.Model == "gpt-4-turbo");
    }

    #endregion

    #region Domain Model Tests

    [Fact]
    public void ToolDefinition_CanHaveMultipleParameters()
    {
        // Arrange & Act
        var tool = new ToolDefinition
        {
            Name = "complex_tool",
            Description = "A tool with multiple parameters",
            Parameters = new List<ToolParameter>
            {
                new() { Name = "input", Type = "string", Description = "Input text", Required = true },
                new() { Name = "format", Type = "string", Description = "Output format", Required = false },
                new() { Name = "count", Type = "number", Description = "Number of results", Required = false }
            }
        };

        // Assert
        Assert.Equal("complex_tool", tool.Name);
        Assert.Equal(3, tool.Parameters?.Count);
        Assert.Contains(tool.Parameters!, p => p.Name == "input" && p.Required);
        Assert.Contains(tool.Parameters!, p => p.Name == "format" && !p.Required);
    }

    [Fact]
    public void ProviderConfig_SupportsBYOKScenarios()
    {
        // Arrange & Act
        var provider = new ProviderConfig
        {
            Type = "azure",
            BaseUrl = "https://myresource.openai.azure.com",
            ApiKey = "my-api-key",
            BearerToken = "my-bearer-token",
            WireApi = "openai"
        };

        // Assert
        Assert.Equal("azure", provider.Type);
        Assert.Equal("https://myresource.openai.azure.com", provider.BaseUrl);
        Assert.Equal("my-api-key", provider.ApiKey);
        Assert.Equal("my-bearer-token", provider.BearerToken);
        Assert.Equal("openai", provider.WireApi);
    }

    [Fact]
    public void SystemMessageConfig_DefaultsToAppendMode()
    {
        // Arrange & Act
        var config = new SystemMessageConfig();

        // Assert
        Assert.Equal("Append", config.Mode);
        Assert.Equal(string.Empty, config.Content);
    }

    [Fact]
    public void SystemMessageConfig_SupportsReplaceMode()
    {
        // Arrange & Act
        var config = new SystemMessageConfig
        {
            Mode = "Replace",
            Content = "You are a custom assistant."
        };

        // Assert
        Assert.Equal("Replace", config.Mode);
        Assert.Equal("You are a custom assistant.", config.Content);
    }

    #endregion

    #region Messaging Model Tests

    [Fact]
    public void SendMessageRequest_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var request = new SendMessageRequest();

        // Assert
        Assert.Equal(string.Empty, request.Prompt);
        Assert.Null(request.Mode);
        Assert.Null(request.Attachments);
    }

    [Fact]
    public void SendMessageRequest_CanSetAllProperties()
    {
        // Arrange & Act
        var request = new SendMessageRequest
        {
            Prompt = "Test prompt",
            Mode = "immediate",
            Attachments = new List<MessageAttachmentDto>
            {
                new() { Type = "file", Path = "/path/to/file.txt", DisplayName = "file.txt" }
            }
        };

        // Assert
        Assert.Equal("Test prompt", request.Prompt);
        Assert.Equal("immediate", request.Mode);
        Assert.NotNull(request.Attachments);
        Assert.Single(request.Attachments);
        Assert.Equal("file", request.Attachments[0].Type);
    }

    [Fact]
    public void SendMessageResponse_CanIndicateSuccess()
    {
        // Arrange & Act
        var response = new SendMessageResponse
        {
            SessionId = "test-session",
            MessageId = "msg-123",
            Success = true
        };

        // Assert
        Assert.Equal("test-session", response.SessionId);
        Assert.Equal("msg-123", response.MessageId);
        Assert.True(response.Success);
        Assert.Null(response.Error);
    }

    [Fact]
    public void SendMessageResponse_CanIndicateFailure()
    {
        // Arrange & Act
        var response = new SendMessageResponse
        {
            SessionId = "test-session",
            Success = false,
            Error = "Session not found"
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Session not found", response.Error);
    }

    [Fact]
    public void MessagesResponse_CanContainMultipleEvents()
    {
        // Arrange & Act
        var response = new MessagesResponse
        {
            SessionId = "test-session",
            Events = new List<SessionEventDto>
            {
                new() { Id = Guid.NewGuid(), Type = "user.message", Timestamp = DateTimeOffset.UtcNow },
                new() { Id = Guid.NewGuid(), Type = "assistant.message", Timestamp = DateTimeOffset.UtcNow },
                new() { Id = Guid.NewGuid(), Type = "session.idle", Timestamp = DateTimeOffset.UtcNow }
            },
            TotalCount = 3
        };

        // Assert
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(3, response.Events.Count);
        Assert.Contains(response.Events, e => e.Type == "user.message");
        Assert.Contains(response.Events, e => e.Type == "assistant.message");
        Assert.Contains(response.Events, e => e.Type == "session.idle");
    }

    [Fact]
    public void SessionEventDto_CanHoldVariousEventTypes()
    {
        // Arrange & Act
        var userMessageEvent = new SessionEventDto
        {
            Id = Guid.NewGuid(),
            Type = "user.message",
            Timestamp = DateTimeOffset.UtcNow,
            Data = new UserMessageDataDto { Content = "Hello!" }
        };

        var assistantMessageEvent = new SessionEventDto
        {
            Id = Guid.NewGuid(),
            Type = "assistant.message",
            Timestamp = DateTimeOffset.UtcNow,
            Data = new AssistantMessageDataDto { MessageId = "msg-1", Content = "Hi there!" }
        };

        // Assert
        Assert.Equal("user.message", userMessageEvent.Type);
        Assert.IsType<UserMessageDataDto>(userMessageEvent.Data);
        Assert.Equal("Hello!", ((UserMessageDataDto)userMessageEvent.Data!).Content);

        Assert.Equal("assistant.message", assistantMessageEvent.Type);
        Assert.IsType<AssistantMessageDataDto>(assistantMessageEvent.Data);
        Assert.Equal("Hi there!", ((AssistantMessageDataDto)assistantMessageEvent.Data!).Content);
    }

    [Fact]
    public void MessageAttachmentDto_SupportsVariousTypes()
    {
        // Arrange & Act
        var fileAttachment = new MessageAttachmentDto
        {
            Type = "file",
            Path = "/path/to/file.cs",
            DisplayName = "file.cs",
            StartLine = 10,
            EndLine = 50,
            Language = "csharp"
        };

        var uriAttachment = new MessageAttachmentDto
        {
            Type = "uri",
            Uri = "https://example.com/doc",
            DisplayName = "Documentation"
        };

        // Assert
        Assert.Equal("file", fileAttachment.Type);
        Assert.Equal("/path/to/file.cs", fileAttachment.Path);
        Assert.Equal(10, fileAttachment.StartLine);
        Assert.Equal(50, fileAttachment.EndLine);
        Assert.Equal("csharp", fileAttachment.Language);

        Assert.Equal("uri", uriAttachment.Type);
        Assert.Equal("https://example.com/doc", uriAttachment.Uri);
    }

    #endregion

    #region Event Data DTO Tests

    [Fact]
    public void AssistantMessageDeltaDataDto_SupportsStreamingData()
    {
        // Arrange & Act
        var deltaData = new AssistantMessageDeltaDataDto
        {
            MessageId = "msg-123",
            DeltaContent = "Hello ",
            TotalResponseSizeBytes = 6,
            ParentToolCallId = "tool-call-1"
        };

        // Assert
        Assert.Equal("msg-123", deltaData.MessageId);
        Assert.Equal("Hello ", deltaData.DeltaContent);
        Assert.Equal(6, deltaData.TotalResponseSizeBytes);
        Assert.Equal("tool-call-1", deltaData.ParentToolCallId);
    }

    [Fact]
    public void ToolExecutionStartDataDto_ContainsToolInfo()
    {
        // Arrange & Act
        var toolStartData = new ToolExecutionStartDataDto
        {
            ToolCallId = "call-123",
            ToolName = "search_files",
            Arguments = new { query = "test", path = "/src" }
        };

        // Assert
        Assert.Equal("call-123", toolStartData.ToolCallId);
        Assert.Equal("search_files", toolStartData.ToolName);
        Assert.NotNull(toolStartData.Arguments);
    }

    [Fact]
    public void ToolExecutionCompleteDataDto_ContainsResult()
    {
        // Arrange & Act
        var toolCompleteData = new ToolExecutionCompleteDataDto
        {
            ToolCallId = "call-123",
            ToolName = "search_files",
            Result = "Found 5 matching files",
            Duration = 150.5
        };

        // Assert
        Assert.Equal("call-123", toolCompleteData.ToolCallId);
        Assert.Equal("Found 5 matching files", toolCompleteData.Result);
        Assert.Equal(150.5, toolCompleteData.Duration);
    }

    [Fact]
    public void ToolExecutionCompleteDataDto_ContainsError_OnFailure()
    {
        // Arrange & Act
        var toolCompleteData = new ToolExecutionCompleteDataDto
        {
            ToolCallId = "call-456",
            ToolName = "read_file",
            Error = "File not found",
            Result = null
        };

        // Assert
        Assert.Equal("File not found", toolCompleteData.Error);
        Assert.Null(toolCompleteData.Result);
    }

    #endregion
}
