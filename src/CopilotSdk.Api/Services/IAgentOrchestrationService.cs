using Microsoft.Extensions.AI;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Coordinates real multi-agent orchestration: the user-facing (parent) session has a
/// single delegate-to-agent tool. Specialist child Copilot sessions are prepared before
/// the parent turn starts, then each delegation runs in the prepared child session with full Copilot CLI tools and a
/// progress-reporting tool available so it can actually execute work (shell, file ops,
/// git, etc.). Child events are relayed to the parent's SignalR group annotated with
/// the agent id.
/// </summary>
public interface IAgentOrchestrationService
{
    /// <summary>
    /// Builds an <see cref="AIFunction"/> that the orchestrator (parent) session can call to
    /// delegate work to a specialist agent. The function's identity (parent session id and
    /// allowed agent ids) is captured at build time.
    /// </summary>
    /// <param name="parentSessionId">The id of the user-facing session whose orchestrator will invoke the tool.</param>
    /// <param name="availableAgentIds">The agent ids the orchestrator may delegate to.</param>
    /// <param name="parentModel">The model used by the parent session; reused for child sessions.</param>
    AIFunction BuildDelegateTool(string parentSessionId, IReadOnlyList<string> availableAgentIds, string parentModel);

    /// <summary>
    /// Builds a tiny diagnostic tool that returns immediately if the host receives a
    /// custom tool callback. Used to isolate CLI/schema dispatch failures from child
    /// session or shell execution failures.
    /// </summary>
    AIFunction BuildDelegationProbeTool(string parentSessionId);

    /// <summary>
    /// Ensures the specialist child sessions for the parent are created before the parent
    /// model turn starts. This avoids creating sessions from inside a running tool callback.
    /// </summary>
    /// <param name="parentSessionId">The user-facing parent session id.</param>
    /// <param name="availableAgentIds">The specialist agent ids to prepare.</param>
    /// <param name="parentModel">The model used by the parent session; reused for child sessions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PrepareChildrenAsync(
        string parentSessionId,
        IReadOnlyList<string> availableAgentIds,
        string parentModel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aborts any in-flight delegations for the given parent session by aborting the
    /// associated child sessions. Used when the user aborts the parent session.
    /// </summary>
    Task AbortChildrenAsync(string parentSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes all child sessions associated with the parent. Used when the parent
    /// session is deleted.
    /// </summary>
    Task DisposeChildrenAsync(string parentSessionId, CancellationToken cancellationToken = default);
}
