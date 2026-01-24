# System Message Refinement Feature Plan

## Overview

This document outlines the implementation plan for adding an interactive system message refinement feature to the Create New Session modal. The feature allows users to click a "Refine" button that sends the current system message content to an LLM with special instructions to expand and improve the content as a clearer requirements statement.

## Feature Description

**User Flow:**
1. User enters initial system message content in the "Content" textarea (`id="system-message-content"`)
2. User clicks a "Refine" help button next to the Content field
3. The current content is sent to a new backend API endpoint
4. The backend wraps the content with refinement instructions and sends to the Copilot LLM
5. The LLM responds with an improved, expanded version of the system message
6. The refined text replaces the current content in the textarea
7. User can iterate this process multiple times until satisfied

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Frontend                                   │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  SystemMessageEditor.tsx                                     │   │
│  │  ┌─────────────────────────────────────────────────────┐    │   │
│  │  │  Content Textarea (system-message-content)          │    │   │
│  │  └─────────────────────────────────────────────────────┘    │   │
│  │  [🔧 Refine] ← New button                                   │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│                              ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  copilotApi.ts - refineSystemMessage()                      │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                               │
                               ▼ POST /api/copilot/refine-prompt
┌─────────────────────────────────────────────────────────────────────┐
│                           Backend                                    │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  PromptRefinementController.cs                              │   │
│  │  POST /api/copilot/refine-prompt                            │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│                              ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  IPromptRefinementService / PromptRefinementService         │   │
│  │  - Wraps user content with refinement meta-prompt           │   │
│  │  - Sends to Copilot SDK session                             │   │
│  │  - Extracts and returns refined content                     │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                              │                                       │
│                              ▼                                       │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  CopilotClientManager / Ephemeral Session                   │   │
│  │  - Creates temporary session for refinement                 │   │
│  │  - Sends refinement prompt                                  │   │
│  │  - Cleans up session after response                         │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: Backend API Implementation ✅ COMPLETED 2026-01-24

**Goal:** Create the backend service and API endpoint for prompt refinement.

### Tasks

- [x] **1.1** Create request/response models in `Models/Requests/` and `Models/Responses/`:
  - `RefinePromptRequest.cs` - Contains the user's current system message content
    ```csharp
    public class RefinePromptRequest
    {
        public string Content { get; set; } = string.Empty;
        public string? Context { get; set; } // Optional: additional context about the app being built
        public string? RefinementFocus { get; set; } // Optional: "clarity", "detail", "constraints", etc.
    }
    ```
  - `RefinePromptResponse.cs` - Contains the refined content
    ```csharp
    public class RefinePromptResponse
    {
        public string RefinedContent { get; set; } = string.Empty;
        public string OriginalContent { get; set; } = string.Empty;
        public int IterationCount { get; set; } // Track refinement iterations
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
    ```

- [x] **1.2** Create `Services/IPromptRefinementService.cs` interface:
  ```csharp
  public interface IPromptRefinementService
  {
      Task<RefinePromptResponse> RefinePromptAsync(RefinePromptRequest request, CancellationToken cancellationToken = default);
  }
  ```

- [x] **1.3** Create `Services/PromptRefinementService.cs` implementation:
  - Inject `CopilotClientManager` for accessing the Copilot client
  - Define the meta-prompt template that instructs the LLM how to refine the content
  - Create an ephemeral session specifically for refinement
  - Send the wrapped prompt and collect the response
  - Parse and extract the refined content from the response
  - Clean up the ephemeral session

- [x] **1.4** Create the refinement meta-prompt template in `Services/PromptRefinementService.cs`:
  ```
  You are a prompt engineering expert. Your task is to improve and expand the following 
  system message content that will be used to instruct an AI assistant for building an application.

  ORIGINAL CONTENT:
  ---
  {user_content}
  ---

  Please refine this content by:
  1. Clarifying any ambiguous requirements
  2. Adding specific, actionable instructions
  3. Including relevant technical constraints or considerations
  4. Structuring the content logically with clear sections
  5. Adding helpful context about expected behaviors
  6. Ensuring the tone is appropriate for an AI system prompt

  Respond ONLY with the improved system message content. Do not include explanations, 
  preambles, or meta-commentary. The response should be ready to use directly as a system prompt.
  ```

- [x] **1.5** Create `Controllers/PromptRefinementController.cs`:
  - `POST /api/copilot/refine-prompt` endpoint
  - Validate request (content not empty, reasonable length)
  - Call `IPromptRefinementService.RefinePromptAsync`
  - Return the refined content or appropriate error response

- [x] **1.6** Register services in `Program.cs`:
  - Add `IPromptRefinementService` / `PromptRefinementService` to DI container

- [x] **1.7** Write unit tests in `tests/CopilotSdk.Api.Tests/`:
  - `PromptRefinementServiceTests.cs` - Test service logic, prompt wrapping, response parsing
  - `PromptRefinementControllerTests.cs` - Test API endpoint, validation, error handling

- [x] **1.8** Run all backend tests and verify they pass (254 tests passing)

---

## Phase 2: Frontend UI Components ✅ COMPLETED 2026-01-24

**Goal:** Add the Refine button and integrate with the API.

### Tasks

- [x] **2.1** Add TypeScript types in `types/refinement.types.ts`:
  ```typescript
  export interface RefinePromptRequest {
    content: string;
    context?: string;
    refinementFocus?: 'clarity' | 'detail' | 'constraints' | 'all';
  }

  export interface RefinePromptResponse {
    refinedContent: string;
    originalContent: string;
    iterationCount: number;
    success: boolean;
    errorMessage?: string;
  }
  ```

- [x] **2.2** Add API function in `api/copilotApi.ts`:
  ```typescript
  export async function refineSystemMessage(request: RefinePromptRequest): Promise<RefinePromptResponse> {
    const response = await fetch('/api/copilot/refine-prompt', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      throw new Error(`Refinement failed: ${response.statusText}`);
    }
    return response.json();
  }
  ```

- [x] **2.3** Create `hooks/usePromptRefinement.ts` custom hook:
  ```typescript
  export function usePromptRefinement() {
    const [isRefining, setIsRefining] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [iterationCount, setIterationCount] = useState(0);

    const refine = async (content: string, options?: Partial<RefinePromptRequest>) => {
      setIsRefining(true);
      setError(null);
      try {
        const response = await refineSystemMessage({ content, ...options });
        if (response.success) {
          setIterationCount(response.iterationCount);
          return response.refinedContent;
        } else {
          setError(response.errorMessage || 'Refinement failed');
          return null;
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
        return null;
      } finally {
        setIsRefining(false);
      }
    };

    return { refine, isRefining, error, iterationCount };
  }
  ```

- [x] **2.4** Update `SystemMessageEditor.tsx`:
  - Add "Refine" button next to or below the Content textarea
  - Import and use `usePromptRefinement` hook
  - Add loading state (spinner/disabled) while refinement is in progress
  - Show error message if refinement fails
  - Display iteration count to show how many times content has been refined
  - Add confirmation or undo capability (optional enhancement)

- [x] **2.5** Update `SystemMessageEditor.css`:
  - Style the Refine button (icon + text)
  - Add loading/spinner styles
  - Style error message display
  - Add visual feedback during refinement (pulsing border, etc.)

- [x] **2.6** Create `components/RefineButton.tsx` (optional, for reusability):
  ```typescript
  interface RefineButtonProps {
    onClick: () => void;
    isRefining: boolean;
    disabled?: boolean;
    iterationCount?: number;
  }
  ```
  - Created `RefineButton.tsx` with wand icon, spinner animation, and iteration badge
  - Created `RefineButton.css` with styling and animations

- [x] **2.7** Write component tests:
  - `SystemMessageEditor.test.tsx` - Add tests for Refine button behavior (20 new tests)
  - `RefineButton.test.tsx` - Test button states, loading, disabled (26 tests)
  - `usePromptRefinement.test.ts` - Test hook logic and error handling (14 tests)

- [x] **2.8** Run all frontend tests and verify they pass (483 tests total)

### Implementation Notes (Phase 2)
- Added Jest `transformIgnorePatterns` to handle axios ES modules in tests
- RefineButton includes wand icon SVG with smooth animations
- SystemMessageEditor shows overlay while refining
- Error messages are dismissible and clear when user types

---

## Phase 3: Integration, Polish, and Edge Cases ✅ COMPLETED 2026-01-24

**Goal:** End-to-end testing, error handling improvements, and UX polish.

### Tasks

- [x] **3.1** Integration testing:
  - Test full flow: UI → API → LLM → Response → UI update
  - Test with various input lengths (short, medium, long content)
  - Test with empty content (should show helpful error or default prompt)
  - Test rapid successive clicks (debouncing/request cancellation)
  - Test when Copilot client is not connected
  - Added 10 new integration tests to `IntegrationTests.cs`

- [x] **3.2** Error handling improvements:
  - Handle timeout scenarios (LLM taking too long) - 30 second default timeout
  - Handle network failures gracefully with user-friendly messages
  - Show user-friendly error messages (mapped from technical errors)
  - Add ability to cancel in-progress refinement via `cancel()` function
  - Added `AbortController` for request cancellation

- [x] **3.3** UX enhancements:
  - Add tooltip explaining what the Refine button does
  - Add keyboard shortcut (Ctrl+Shift+R) for power users
  - Add "Undo" button to revert to previous content (Ctrl+Shift+Z)
  - Add character count change indicator (+/- characters) showing expansion
  - Add Cancel button visible during refinement

- [x] **3.4** Accessibility improvements:
  - Ensure Refine button has proper ARIA labels
  - Announce refinement completion to screen readers via live region
  - Ensure focus management during and after refinement
  - Add loading announcement for assistive technologies
  - Added `sr-only` CSS class for screen reader announcements

- [x] **3.5** Performance considerations:
  - Add request debouncing (cancels previous request when new one starts)
  - Add request timeout (30 seconds default, configurable)
  - Implement request cancellation when user navigates away (useEffect cleanup)
  - Track mounted state to avoid setting state on unmounted components

- [x] **3.6** Backend resilience:
  - Add rate limiting for refinement endpoint (10 requests per minute per client)
  - Created `RateLimitingMiddleware.cs` with configurable limits
  - Handle cases where Copilot client session creation fails (already in service)
  - Ensure ephemeral sessions are always cleaned up (finally block in service)
  - Add logging for debugging and monitoring (already comprehensive)

- [x] **3.7** Documentation:
  - Update README.md with new feature description
  - Add API documentation for `/api/copilot/refine-prompt` endpoint
  - Document the meta-prompt template for future customization
  - Add usage examples

- [x] **3.8** Final testing:
  - Run full test suite (frontend + backend) - 263 backend + 485 frontend = 748 tests passing
  - Updated hook tests for new error message mapping
  - Updated component tests for user-friendly error messages

### Implementation Notes (Phase 3)
- Added `usePromptRefinement` hook features: `cancel()`, `canUndo`, `previousContent`, timeout support
- Created `RateLimitingMiddleware` to prevent API abuse
- User-friendly error message mapping converts technical errors to helpful messages
- Keyboard shortcuts: Ctrl+Shift+R (refine), Ctrl+Shift+Z (undo refinement)
- Character count change indicator shows +/- characters after refinement

---

## API Specification

### POST /api/copilot/refine-prompt

**Request:**
```json
{
  "content": "Build a task management app",
  "context": "Web application for small teams",
  "refinementFocus": "detail"
}
```

**Response (Success):**
```json
{
  "refinedContent": "You are an AI assistant helping to build a task management web application for small teams...",
  "originalContent": "Build a task management app",
  "iterationCount": 1,
  "success": true,
  "errorMessage": null
}
```

**Response (Error):**
```json
{
  "refinedContent": "",
  "originalContent": "Build a task management app",
  "iterationCount": 0,
  "success": false,
  "errorMessage": "Copilot client is not connected. Please start the client first."
}
```

**HTTP Status Codes:**
- `200 OK` - Refinement successful
- `400 Bad Request` - Invalid request (empty content, content too long)
- `503 Service Unavailable` - Copilot client not connected
- `504 Gateway Timeout` - LLM request timed out

---

## File Changes Summary

### New Files to Create:

**Backend:**
- `src/CopilotSdk.Api/Models/Requests/RefinePromptRequest.cs`
- `src/CopilotSdk.Api/Models/Responses/RefinePromptResponse.cs`
- `src/CopilotSdk.Api/Services/IPromptRefinementService.cs`
- `src/CopilotSdk.Api/Services/PromptRefinementService.cs`
- `src/CopilotSdk.Api/Controllers/PromptRefinementController.cs`
- `tests/CopilotSdk.Api.Tests/PromptRefinementServiceTests.cs`
- `tests/CopilotSdk.Api.Tests/PromptRefinementControllerTests.cs`

**Frontend:**
- `src/CopilotSdk.Web/src/types/refinement.types.ts`
- `src/CopilotSdk.Web/src/hooks/usePromptRefinement.ts`
- `src/CopilotSdk.Web/src/components/RefineButton.tsx`
- `src/CopilotSdk.Web/src/components/RefineButton.css`
- `src/CopilotSdk.Web/src/components/RefineButton.test.tsx`
- `src/CopilotSdk.Web/src/hooks/usePromptRefinement.test.ts`

### Files to Modify:

**Backend:**
- `src/CopilotSdk.Api/Program.cs` - Register new services

**Frontend:**
- `src/CopilotSdk.Web/src/api/copilotApi.ts` - Add `refineSystemMessage` function
- `src/CopilotSdk.Web/src/components/SystemMessageEditor.tsx` - Add Refine button
- `src/CopilotSdk.Web/src/components/SystemMessageEditor.css` - Style updates
- `src/CopilotSdk.Web/src/components/SystemMessageEditor.test.tsx` - Add tests

---

## Success Criteria

1. ✅ User can click Refine button with system message content
2. ✅ Content is sent to backend API
3. ✅ Backend wraps content with refinement instructions
4. ✅ LLM processes and returns refined content
5. ✅ Refined content replaces original in textarea
6. ✅ User can iterate refinement multiple times
7. ✅ Proper loading states shown during refinement
8. ✅ Errors are handled gracefully with user-friendly messages
9. ✅ All tests pass (unit + integration)
10. ✅ Feature is accessible and keyboard-navigable

---

## Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1: Backend | 8 tasks | 3-4 hours |
| Phase 2: Frontend | 8 tasks | 3-4 hours |
| Phase 3: Polish | 8 tasks | 2-3 hours |
| **Total** | **24 tasks** | **8-11 hours** |

---

## Dependencies

- Copilot client must be connected and running
- GitHub Copilot SDK must support session creation for refinement
- Backend must have access to create ephemeral sessions

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| LLM response takes too long | Add timeout and cancellation support |
| Refinement quality varies | Allow multiple iterations; consider adding quality presets |
| Session cleanup fails | Use try/finally; add session cleanup job |
| High API usage | Add rate limiting; consider caching |
