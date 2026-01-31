using System.Text;
using CopilotSdk.Api.Managers;
using CopilotSdk.Api.Models.Requests;
using CopilotSdk.Api.Models.Responses;
using GitHub.Copilot.SDK;
using SdkConnectionState = GitHub.Copilot.SDK.ConnectionState;
using DomainSessionConfig = CopilotSdk.Api.Models.Domain.SessionConfig;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Service implementation for refining system message prompts using the Copilot LLM.
/// Creates ephemeral sessions specifically for prompt refinement operations.
/// </summary>
public class PromptRefinementService : IPromptRefinementService
{
    private readonly CopilotClientManager _clientManager;
    private readonly ILogger<PromptRefinementService> _logger;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The meta-prompt template used to instruct the LLM on how to refine the content.
    /// </summary>
    internal const string DefaultMetaPromptTemplate = @"You are a prompt engineering expert. Your task is to improve and expand the following system message content that will be used to instruct an AI assistant for building an application.

ORIGINAL CONTENT:
---
{content}
---

{context_section}

{focus_section}

Please refine this content by:
1. Clarifying any ambiguous requirements
2. Adding specific, actionable instructions
3. Including relevant technical constraints or considerations
4. Structuring the content logically with clear sections
5. Adding helpful context about expected behaviors
6. Ensuring the tone is appropriate for an AI system prompt

Respond ONLY with the improved system message content. Do not include explanations, preambles, or meta-commentary. The response should be ready to use directly as a system prompt.";

    internal const string ContextSectionTemplate = @"ADDITIONAL CONTEXT:
---
{context}
---";

    internal const string FocusSectionTemplates = @"Focus especially on: {focus}";

    private static readonly Dictionary<string, string> FocusDescriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["clarity"] = "making requirements crystal clear and unambiguous",
        ["detail"] = "adding comprehensive details and specific examples",
        ["constraints"] = "defining technical constraints, boundaries, and limitations",
        ["all"] = "all aspects - clarity, detail, and constraints equally"
    };

    public PromptRefinementService(
        CopilotClientManager clientManager,
        ILogger<PromptRefinementService> logger)
    {
        _clientManager = clientManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<RefinePromptResponse> RefinePromptAsync(RefinePromptRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== REFINE START === Content length: {Length}, Focus: {Focus}", 
            request.Content?.Length ?? 0, request.RefinementFocus ?? "none");

        // Validate client is connected
        var clientState = _clientManager.Status.ConnectionState;
        _logger.LogInformation("Client state check: Client={HasClient}, State={State}", 
            _clientManager.Client != null, clientState);
            
        if (_clientManager.Client == null || clientState != SdkConnectionState.Connected.ToString())
        {
            _logger.LogWarning("Cannot refine prompt - Copilot client is not connected. State: {State}", clientState);
            return new RefinePromptResponse
            {
                Success = false,
                OriginalContent = request.Content,
                ErrorMessage = "Copilot client is not connected. Please start the client first."
            };
        }

        CopilotSession? ephemeralSession = null;
        string ephemeralSessionId = $"refinement-{Guid.NewGuid():N}";

        IDisposable? eventSubscription = null;

        try
        {
            // Build the refinement prompt
            var refinementPrompt = BuildRefinementPrompt(request);

            _logger.LogDebug("Creating ephemeral session {SessionId} for prompt refinement", ephemeralSessionId);

            // Create an ephemeral session for refinement
            // NOTE: Using streaming=false to get complete response in single message
            var sessionConfig = new DomainSessionConfig
            {
                SessionId = ephemeralSessionId,
                Model = "claude-opus-4.5",
                Streaming = false // Non-streaming for simpler single-response handling
            };

            _logger.LogInformation("[REFINE] Creating ephemeral session with Streaming={Streaming}", sessionConfig.Streaming);
            ephemeralSession = await _clientManager.CreateSessionAsync(sessionConfig, null, cancellationToken);
            _logger.LogInformation("[REFINE] Ephemeral session created: {SessionId}", ephemeralSession.SessionId);

            // Set up completion tracking
            var completionSource = new TaskCompletionSource<string?>();
            var responseBuilder = new StringBuilder();
            var receivedContent = false;

            // Set up event handler to capture the response
            SessionEventHandler eventHandler = (SessionEvent evt) =>
            {
                try
                {
                    // Log ALL events received for debugging
                    _logger.LogInformation("[REFINE EVENT] Type={Type}, Id={Id}, Class={Class}", 
                        evt.Type, evt.Id, evt.GetType().Name);
                    
                    if (evt is AssistantMessageDeltaEvent deltaEvent)
                    {
                        var delta = deltaEvent.Data?.DeltaContent;
                        if (!string.IsNullOrEmpty(delta))
                        {
                            responseBuilder.Append(delta);
                            receivedContent = true;
                            _logger.LogDebug("[REFINE] Delta received, total length now: {Length}", responseBuilder.Length);
                        }
                    }
                    else if (evt is AssistantMessageEvent messageEvent)
                    {
                        // Complete message received (non-streaming fallback)
                        var content = messageEvent.Data?.Content;
                        _logger.LogInformation("[REFINE] AssistantMessageEvent - Content length: {Length}", content?.Length ?? 0);
                        if (!string.IsNullOrEmpty(content))
                        {
                            responseBuilder.Clear();
                            responseBuilder.Append(content);
                            receivedContent = true;
                        }
                    }
                    else if (evt is AssistantTurnEndEvent turnEndEvent)
                    {
                        // This is the correct completion event
                        _logger.LogInformation("[REFINE] Turn ended! Received content: {HasContent}, Length: {Length}", 
                            receivedContent, responseBuilder.Length);
                        completionSource.TrySetResult(receivedContent ? responseBuilder.ToString() : null);
                    }
                    else if (evt.Type == "session.idle")
                    {
                        // Session idle also indicates completion
                        _logger.LogInformation("[REFINE] Session idle - completing. Content: {HasContent}, Length: {Length}", 
                            receivedContent, responseBuilder.Length);
                        completionSource.TrySetResult(receivedContent ? responseBuilder.ToString() : null);
                    }
                    else if (evt is SessionErrorEvent errorEvent)
                    {
                        _logger.LogWarning("[REFINE] Error event: {Message}", errorEvent.Data?.Message);
                        completionSource.TrySetException(new Exception($"Error from LLM: {errorEvent.Data?.Message}"));
                    }
                    else if (evt.Type == "error")
                    {
                        _logger.LogWarning("[REFINE] Generic error event during refinement");
                        completionSource.TrySetException(new Exception("Error event received from LLM"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event in refinement handler");
                }
            };

            // Subscribe to events
            eventSubscription = ephemeralSession.On(eventHandler);

            _logger.LogInformation("[REFINE] Sending prompt to session {SessionId}, prompt length: {Length}", 
                ephemeralSessionId, refinementPrompt.Length);
            _logger.LogDebug("[REFINE] Prompt preview (first 200 chars): {Preview}", 
                refinementPrompt.Substring(0, Math.Min(200, refinementPrompt.Length)));

            // Send the refinement prompt
            var messageOptions = new MessageOptions
            {
                Prompt = refinementPrompt,
                Mode = "edit" // Use edit mode for refinement
            };

            _logger.LogInformation("[REFINE] Calling SendAsync...");
            await ephemeralSession.SendAsync(messageOptions, cancellationToken);
            _logger.LogInformation("[REFINE] SendAsync returned, now waiting for completion events...");

            // Wait for the response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(DefaultTimeout);

            try
            {
                // Register cancellation to complete the task
                await using var registration = timeoutCts.Token.Register(() =>
                {
                    if (receivedContent)
                    {
                        // If we received some content, return what we have
                        completionSource.TrySetResult(responseBuilder.ToString());
                    }
                    else
                    {
                        completionSource.TrySetCanceled(timeoutCts.Token);
                    }
                });

                _logger.LogInformation("[REFINE] Awaiting completion task...");
                var assistantResponse = await completionSource.Task;
                _logger.LogInformation("[REFINE] Completion task finished. Response length: {Length}", assistantResponse?.Length ?? 0);

                if (string.IsNullOrWhiteSpace(assistantResponse))
                {
                    _logger.LogWarning("[REFINE] No valid assistant response received for refinement");
                    return new RefinePromptResponse
                    {
                        Success = false,
                        OriginalContent = request.Content,
                        ErrorMessage = "No response received from the LLM. Please try again."
                    };
                }

                _logger.LogInformation("[REFINE] === SUCCESS === Original: {OriginalLength}, Refined: {RefinedLength}",
                    request.Content.Length, assistantResponse.Length);

                return new RefinePromptResponse
                {
                    Success = true,
                    OriginalContent = request.Content,
                    RefinedContent = assistantResponse.Trim(),
                    IterationCount = 1
                };
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("[REFINE] === TIMEOUT === after {Timeout}. Received any content: {HasContent}, Length: {Length}", 
                    DefaultTimeout, receivedContent, responseBuilder.Length);
                return new RefinePromptResponse
                {
                    Success = false,
                    OriginalContent = request.Content,
                    ErrorMessage = $"Refinement timed out after {DefaultTimeout.TotalSeconds} seconds. Please try again."
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[REFINE] === CANCELLED ===");
            return new RefinePromptResponse
            {
                Success = false,
                OriginalContent = request.Content,
                ErrorMessage = "Refinement was cancelled."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REFINE] === ERROR === during prompt refinement");
            return new RefinePromptResponse
            {
                Success = false,
                OriginalContent = request.Content,
                ErrorMessage = $"An error occurred during refinement: {ex.Message}"
            };
        }
        finally
        {
            // Clean up the event subscription
            eventSubscription?.Dispose();

            // Clean up the ephemeral session
            if (ephemeralSession != null)
            {
                try
                {
                    _logger.LogDebug("Cleaning up ephemeral session {SessionId}", ephemeralSessionId);
                    await _clientManager.DeleteSessionAsync(ephemeralSessionId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up ephemeral session {SessionId}", ephemeralSessionId);
                }
            }
        }
    }

    /// <summary>
    /// Builds the full refinement prompt from the request.
    /// </summary>
    internal string BuildRefinementPrompt(RefinePromptRequest request)
    {
        var prompt = DefaultMetaPromptTemplate.Replace("{content}", request.Content);

        // Add context section if provided
        if (!string.IsNullOrWhiteSpace(request.Context))
        {
            var contextSection = ContextSectionTemplate.Replace("{context}", request.Context);
            prompt = prompt.Replace("{context_section}", contextSection);
        }
        else
        {
            prompt = prompt.Replace("{context_section}", string.Empty);
        }

        // Add focus section if provided
        if (!string.IsNullOrWhiteSpace(request.RefinementFocus) &&
            FocusDescriptions.TryGetValue(request.RefinementFocus, out var focusDescription))
        {
            var focusSection = FocusSectionTemplates.Replace("{focus}", focusDescription);
            prompt = prompt.Replace("{focus_section}", focusSection);
        }
        else
        {
            prompt = prompt.Replace("{focus_section}", string.Empty);
        }

        // Clean up any extra newlines
        prompt = prompt.Replace("\n\n\n", "\n\n");

        return prompt;
    }
}
