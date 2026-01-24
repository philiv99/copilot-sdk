using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Domain;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using GitHub.Copilot.SDK;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for PromptRefinementService.
/// Tests focus on prompt building, response parsing, and error handling.
/// </summary>
public class PromptRefinementServiceTests
{
    private readonly CopilotClientManager _clientManager;
    private readonly Mock<ILogger<PromptRefinementService>> _loggerMock;
    private readonly PromptRefinementService _service;

    public PromptRefinementServiceTests()
    {
        // Create a real CopilotClientManager (not started, so Client will be null)
        var clientManagerLoggerMock = new Mock<ILogger<CopilotClientManager>>();
        _clientManager = new CopilotClientManager(clientManagerLoggerMock.Object, null);
        
        _loggerMock = new Mock<ILogger<PromptRefinementService>>();
        _service = new PromptRefinementService(_clientManager, _loggerMock.Object);
    }

    #region BuildRefinementPrompt Tests

    [Fact]
    public void BuildRefinementPrompt_WithContentOnly_ReturnsPromptWithContent()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build a task management app"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Build a task management app", prompt);
        Assert.Contains("ORIGINAL CONTENT:", prompt);
        Assert.Contains("prompt engineering expert", prompt);
        Assert.Contains("Respond ONLY with the improved system message content", prompt);
    }

    [Fact]
    public void BuildRefinementPrompt_WithContext_IncludesContextSection()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build an e-commerce site",
            Context = "Web application for selling handmade crafts"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Build an e-commerce site", prompt);
        Assert.Contains("ADDITIONAL CONTEXT:", prompt);
        Assert.Contains("Web application for selling handmade crafts", prompt);
    }

    [Fact]
    public void BuildRefinementPrompt_WithoutContext_ExcludesContextSection()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build an API"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Build an API", prompt);
        Assert.DoesNotContain("ADDITIONAL CONTEXT:", prompt);
    }

    [Theory]
    [InlineData("clarity", "making requirements crystal clear and unambiguous")]
    [InlineData("detail", "adding comprehensive details and specific examples")]
    [InlineData("constraints", "defining technical constraints, boundaries, and limitations")]
    [InlineData("all", "all aspects - clarity, detail, and constraints equally")]
    [InlineData("CLARITY", "making requirements crystal clear and unambiguous")]  // Case insensitive
    [InlineData("Detail", "adding comprehensive details and specific examples")]   // Mixed case
    public void BuildRefinementPrompt_WithValidFocus_IncludesFocusSection(string focus, string expectedFocusText)
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build something",
            RefinementFocus = focus
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Focus especially on:", prompt);
        Assert.Contains(expectedFocusText, prompt);
    }

    [Fact]
    public void BuildRefinementPrompt_WithInvalidFocus_ExcludesFocusSection()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build something",
            RefinementFocus = "invalid-focus"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.DoesNotContain("Focus especially on:", prompt);
    }

    [Fact]
    public void BuildRefinementPrompt_WithAllOptions_IncludesAllSections()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Create a REST API",
            Context = "Node.js backend with Express",
            RefinementFocus = "detail"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Create a REST API", prompt);
        Assert.Contains("ADDITIONAL CONTEXT:", prompt);
        Assert.Contains("Node.js backend with Express", prompt);
        Assert.Contains("Focus especially on:", prompt);
        Assert.Contains("adding comprehensive details", prompt);
    }

    [Fact]
    public void BuildRefinementPrompt_ContainsAllRefinementInstructions()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Test content"
        };

        // Act
        var prompt = _service.BuildRefinementPrompt(request);

        // Assert
        Assert.Contains("Clarifying any ambiguous requirements", prompt);
        Assert.Contains("Adding specific, actionable instructions", prompt);
        Assert.Contains("Including relevant technical constraints", prompt);
        Assert.Contains("Structuring the content logically", prompt);
        Assert.Contains("Adding helpful context about expected behaviors", prompt);
        Assert.Contains("Ensuring the tone is appropriate", prompt);
    }

    #endregion

    #region RefinePromptAsync - Client Not Connected Tests

    [Fact]
    public async Task RefinePromptAsync_WhenClientNotConnected_ReturnsErrorResponse()
    {
        // Arrange
        // The client manager is not started, so Client is null and it's not connected
        var request = new RefinePromptRequest
        {
            Content = "Test content"
        };

        // Act
        var response = await _service.RefinePromptAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Test content", response.OriginalContent);
        Assert.Contains("not connected", response.ErrorMessage!);
        Assert.Empty(response.RefinedContent);
    }

    [Fact]
    public async Task RefinePromptAsync_WhenClientDisconnected_ReturnsErrorResponse()
    {
        // Arrange
        // The client manager is not started, so it's in disconnected state
        var request = new RefinePromptRequest
        {
            Content = "Test content"
        };

        // Act
        var response = await _service.RefinePromptAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Contains("not connected", response.ErrorMessage!.ToLower());
    }

    #endregion

    #region Request Model Validation Tests

    [Fact]
    public void RefinePromptRequest_ContentRequired_ValidationFails()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "" // Empty content
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Content"));
    }

    [Fact]
    public void RefinePromptRequest_ContentWithValue_ValidationSucceeds()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Valid content"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        // MinLength(1) is satisfied
        Assert.DoesNotContain(validationResults, v => 
            v.MemberNames.Contains("Content") && v.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void RefinePromptRequest_OptionalFieldsAreOptional_ValidationSucceeds()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Some content",
            Context = null,
            RefinementFocus = null
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        Assert.Empty(validationResults);
    }

    #endregion

    #region Response Model Tests

    [Fact]
    public void RefinePromptResponse_SuccessCase_HasExpectedProperties()
    {
        // Arrange & Act
        var response = new RefinePromptResponse
        {
            Success = true,
            OriginalContent = "Original",
            RefinedContent = "Refined version",
            IterationCount = 1,
            ErrorMessage = null
        };

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Original", response.OriginalContent);
        Assert.Equal("Refined version", response.RefinedContent);
        Assert.Equal(1, response.IterationCount);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void RefinePromptResponse_ErrorCase_HasExpectedProperties()
    {
        // Arrange & Act
        var response = new RefinePromptResponse
        {
            Success = false,
            OriginalContent = "Original",
            RefinedContent = "",
            IterationCount = 0,
            ErrorMessage = "Something went wrong"
        };

        // Assert
        Assert.False(response.Success);
        Assert.Equal("Original", response.OriginalContent);
        Assert.Empty(response.RefinedContent);
        Assert.Equal(0, response.IterationCount);
        Assert.Equal("Something went wrong", response.ErrorMessage);
    }

    [Fact]
    public void RefinePromptResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new RefinePromptResponse();

        // Assert
        Assert.False(response.Success);
        Assert.Empty(response.RefinedContent);
        Assert.Empty(response.OriginalContent);
        Assert.Equal(0, response.IterationCount);
        Assert.Null(response.ErrorMessage);
    }

    #endregion

    #region Helper Methods

    private static List<System.ComponentModel.DataAnnotations.ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            model, validationContext, validationResults, validateAllProperties: true);
        return validationResults;
    }

    #endregion
}
