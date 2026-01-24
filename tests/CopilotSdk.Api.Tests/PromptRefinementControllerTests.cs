using CopilotSdk.Api.Controllers;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using CopilotSdk.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CopilotSdk.Api.Tests;

/// <summary>
/// Unit tests for PromptRefinementController.
/// Tests focus on request validation, service invocation, and response handling.
/// </summary>
public class PromptRefinementControllerTests
{
    private readonly Mock<IPromptRefinementService> _serviceMock;
    private readonly Mock<ILogger<PromptRefinementController>> _loggerMock;
    private readonly PromptRefinementController _controller;

    public PromptRefinementControllerTests()
    {
        _serviceMock = new Mock<IPromptRefinementService>();
        _loggerMock = new Mock<ILogger<PromptRefinementController>>();
        _controller = new PromptRefinementController(_serviceMock.Object, _loggerMock.Object);
    }

    #region RefinePrompt - Success Tests

    [Fact]
    public async Task RefinePrompt_WithValidRequest_ReturnsOkWithRefinedContent()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build a task management app"
        };

        var expectedResponse = new RefinePromptResponse
        {
            Success = true,
            OriginalContent = "Build a task management app",
            RefinedContent = "You are an AI assistant helping to build a comprehensive task management application...",
            IterationCount = 1
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RefinePromptResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Build a task management app", response.OriginalContent);
        Assert.Contains("comprehensive task management", response.RefinedContent);
        Assert.Equal(1, response.IterationCount);
    }

    [Fact]
    public async Task RefinePrompt_WithContextAndFocus_PassesOptionsToService()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Build an API",
            Context = "Node.js backend",
            RefinementFocus = "detail"
        };

        var expectedResponse = new RefinePromptResponse
        {
            Success = true,
            OriginalContent = "Build an API",
            RefinedContent = "Detailed API specification...",
            IterationCount = 1
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(
            It.Is<RefinePromptRequest>(r => 
                r.Content == "Build an API" && 
                r.Context == "Node.js backend" && 
                r.RefinementFocus == "detail"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        _serviceMock.Verify(s => s.RefinePromptAsync(
            It.Is<RefinePromptRequest>(r => 
                r.Content == "Build an API" && 
                r.Context == "Node.js backend" && 
                r.RefinementFocus == "detail"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefinePrompt_ServiceReturnsSuccess_ReturnsOk()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test" };
        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefinePromptResponse { Success = true, RefinedContent = "Refined" });

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region RefinePrompt - Validation Tests

    [Fact]
    public async Task RefinePrompt_WithNullContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = null!
        };

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.Contains("content", problemDetails.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefinePrompt_WithEmptyContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = ""
        };

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.Contains("content", problemDetails.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefinePrompt_WithWhitespaceContent_ReturnsBadRequest()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "   "
        };

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.Contains("content", problemDetails.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("clarity")]
    [InlineData("detail")]
    [InlineData("constraints")]
    [InlineData("all")]
    [InlineData("CLARITY")]
    [InlineData("Detail")]
    public async Task RefinePrompt_WithValidRefinementFocus_AcceptsRequest(string focus)
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Test content",
            RefinementFocus = focus
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefinePromptResponse { Success = true, RefinedContent = "Refined" });

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("speed")]
    [InlineData("performance")]
    public async Task RefinePrompt_WithInvalidRefinementFocus_ReturnsBadRequest(string focus)
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Test content",
            RefinementFocus = focus
        };

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequestResult.Value);
        Assert.Contains("refinementFocus", problemDetails.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    #endregion

    #region RefinePrompt - Error Handling Tests

    [Fact]
    public async Task RefinePrompt_WhenClientNotConnected_Returns503ServiceUnavailable()
    {
        // Arrange
        var request = new RefinePromptRequest
        {
            Content = "Test content"
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefinePromptResponse
            {
                Success = false,
                OriginalContent = "Test content",
                ErrorMessage = "Copilot client is not connected. Please start the client first."
            });

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusResult.StatusCode);
        var problemDetails = Assert.IsType<ProblemDetails>(statusResult.Value);
        Assert.Contains("not connected", problemDetails.Detail);
    }

    [Fact]
    public async Task RefinePrompt_ServiceReturnsNonConnectionError_ReturnsOkWithError()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test" };
        var errorResponse = new RefinePromptResponse
        {
            Success = false,
            OriginalContent = "Test",
            ErrorMessage = "Something went wrong during refinement"
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errorResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RefinePromptResponse>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Something went wrong during refinement", response.ErrorMessage);
    }

    [Fact]
    public async Task RefinePrompt_ServiceReturnsTimeout_ReturnsOkWithError()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test" };
        var timeoutResponse = new RefinePromptResponse
        {
            Success = false,
            OriginalContent = "Test",
            ErrorMessage = "Request timed out"
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(timeoutResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RefinePromptResponse>(okResult.Value);
        Assert.False(response.Success);
        Assert.Contains("timed out", response.ErrorMessage);
    }

    #endregion

    #region RefinePrompt - Cancellation Tests

    [Fact]
    public async Task RefinePrompt_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test content" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _controller.RefinePrompt(request, cts.Token));
    }

    #endregion

    #region RefinePrompt - Service Invocation Tests

    [Fact]
    public async Task RefinePrompt_CallsServiceWithRequest()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test content" };
        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefinePromptResponse { Success = true });

        // Act
        await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        _serviceMock.Verify(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefinePrompt_PassesCancellationTokenToService()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Test content" };
        var cts = new CancellationTokenSource();
        
        _serviceMock.Setup(s => s.RefinePromptAsync(request, cts.Token))
            .ReturnsAsync(new RefinePromptResponse { Success = true });

        // Act
        await _controller.RefinePrompt(request, cts.Token);

        // Assert
        _serviceMock.Verify(s => s.RefinePromptAsync(request, cts.Token), Times.Once);
    }

    #endregion

    #region Response Structure Tests

    [Fact]
    public async Task RefinePrompt_SuccessResponse_ContainsAllExpectedFields()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Original" };
        var expectedResponse = new RefinePromptResponse
        {
            Success = true,
            OriginalContent = "Original",
            RefinedContent = "Refined content here",
            IterationCount = 1,
            ErrorMessage = null
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RefinePromptResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("Original", response.OriginalContent);
        Assert.Equal("Refined content here", response.RefinedContent);
        Assert.Equal(1, response.IterationCount);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public async Task RefinePrompt_ErrorResponse_ContainsAllExpectedFields()
    {
        // Arrange
        var request = new RefinePromptRequest { Content = "Original" };
        var expectedResponse = new RefinePromptResponse
        {
            Success = false,
            OriginalContent = "Original",
            RefinedContent = "",
            IterationCount = 0,
            ErrorMessage = "Error occurred"
        };

        _serviceMock.Setup(s => s.RefinePromptAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefinePrompt(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<RefinePromptResponse>(okResult.Value);
        Assert.False(response.Success);
        Assert.Equal("Original", response.OriginalContent);
        Assert.Empty(response.RefinedContent);
        Assert.Equal(0, response.IterationCount);
        Assert.Equal("Error occurred", response.ErrorMessage);
    }

    #endregion
}
