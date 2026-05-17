using GitHub.Copilot.SDK;

namespace CopilotSdk.Api.Services;

/// <summary>
/// Handles SDK permission requests using the configured application policy.
/// </summary>
public interface IPermissionPolicyService
{
    Task<PermissionRequestResult> HandlePermissionRequestAsync(PermissionRequest request, PermissionInvocation invocation);
}
