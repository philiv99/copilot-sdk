# App/Session Configuration Feature — Implementation Plan

## Overview

Expand the session creation system to support **composable features**, **persistence strategy selection**, and **tech stack options**. Each is defined as a markdown+JSON file pair in an organized folder structure, discovered at runtime, and presented in the Create Session UI.

**Depends on**: All existing phases (1–10) complete.

---

## Current Architecture Analysis

### What Exists Today

1. **System Prompt Templates** — discovered from `docs/system_prompts/` subfolders, each containing a `copilot-instructions.md`. Listed via `GET /api/copilot/system-prompt-templates` and selectable in the `SystemMessageEditor`.

2. **Session Creation** — `CreateSessionModal` has 4 tabs: Basic, System Message, Tools, Provider. The system message is a freeform textarea with optional template loading and AI refinement.

3. **Feature Embedding** — Auth and AI capabilities are currently **hardcoded** into the template-level `copilot-instructions.md` files (e.g., Section 2 of `app-dev-template` and `game_development`).

### What's Missing

- No way to **selectively include/exclude features** like Auth, AI, Analytics
- No way to **choose persistence strategies** independently of the template
- No way to **select tech stack options** (UI framework, state management, testing)
- Features are monolithically embedded in templates, not composable

---

## Proposed Folder Structure

```
docs/system_prompts/
├── app-dev-template/              # Existing app templates
│   └── copilot-instructions.md
├── game_development/              # Existing game template
│   ├── copilot-instructions.md
│   └── game_development.md
├── pipeline_development/          # Existing pipeline template
│   ├── copilot-instructions.md
│   └── pipeline_creation.md
│
├── features/                      # NEW: Composable feature modules
│   ├── _catalog.json              # Feature registry/metadata
│   ├── authentication/
│   │   ├── feature.json           # Metadata (name, description, tags, dependencies)
│   │   └── content.md             # System prompt fragment for this feature
│   ├── ai-integration/
│   │   ├── feature.json
│   │   └── content.md
│   ├── real-time/
│   │   ├── feature.json
│   │   └── content.md
│   ├── file-management/
│   │   ├── feature.json
│   │   └── content.md
│   ├── notifications/
│   │   ├── feature.json
│   │   └── content.md
│   ├── analytics-telemetry/
│   │   ├── feature.json
│   │   └── content.md
│   ├── internationalization/
│   │   ├── feature.json
│   │   └── content.md
│   ├── theming-branding/
│   │   ├── feature.json
│   │   └── content.md
│   ├── offline-support/
│   │   ├── feature.json
│   │   └── content.md
│   ├── search/
│   │   ├── feature.json
│   │   └── content.md
│   ├── export-import/
│   │   ├── feature.json
│   │   └── content.md
│   ├── accessibility/
│   │   ├── feature.json
│   │   └── content.md
│   ├── drag-and-drop/
│   │   ├── feature.json
│   │   └── content.md
│   ├── form-validation/
│   │   ├── feature.json
│   │   └── content.md
│   └── data-visualization/
│       ├── feature.json
│       └── content.md
│
├── persistence_options/           # NEW: Persistence strategy modules
│   ├── _catalog.json
│   ├── localstorage/
│   │   ├── option.json
│   │   └── content.md
│   ├── indexeddb/
│   │   ├── option.json
│   │   └── content.md
│   ├── sqlite-wasm/
│   │   ├── option.json
│   │   └── content.md
│   ├── rest-api-backend/
│   │   ├── option.json
│   │   └── content.md
│   ├── firebase/
│   │   ├── option.json
│   │   └── content.md
│   └── supabase/
│       ├── option.json
│       └── content.md
│
└── tech_stack/                    # NEW: Tech stack option modules
    ├── _catalog.json
    ├── ui_frameworks/
    │   ├── tailwind-css/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── material-ui/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── shadcn-ui/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── chakra-ui/
    │   │   ├── option.json
    │   │   └── content.md
    │   └── vanilla-css/
    │       ├── option.json
    │       └── content.md
    ├── state_management/
    │   ├── react-context/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── zustand/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── redux-toolkit/
    │   │   ├── option.json
    │   │   └── content.md
    │   └── jotai/
    │       ├── option.json
    │       └── content.md
    ├── testing/
    │   ├── vitest/
    │   │   ├── option.json
    │   │   └── content.md
    │   ├── jest/
    │   │   ├── option.json
    │   │   └── content.md
    │   └── playwright/
    │       ├── option.json
    │       └── content.md
    ├── routing/
    │   ├── react-router/
    │   │   ├── option.json
    │   │   └── content.md
    │   └── tanstack-router/
    │       ├── option.json
    │       └── content.md
    └── bundler/
        ├── vite/
        │   ├── option.json
        │   └── content.md
        └── next-js/
            ├── option.json
            └── content.md
```

---

## Metadata Schemas

### `feature.json`

```json
{
  "id": "authentication",
  "name": "Authentication & Authorization",
  "description": "User login, registration, role-based access control via CopilotSdk.Api",
  "category": "feature",
  "tags": ["security", "users", "rbac"],
  "icon": "🔐",
  "isDefault": true,
  "dependencies": [],
  "conflicts": [],
  "compatibleWith": ["app-dev-template", "game_development"],
  "complexity": "medium",
  "estimatedImpact": "Adds ~8 files, auth context, protected routes, login/register screens"
}
```

### `option.json` (persistence / tech stack)

```json
{
  "id": "tailwind-css",
  "name": "Tailwind CSS",
  "description": "Utility-first CSS framework for rapid UI development",
  "category": "tech_stack",
  "subcategory": "ui_frameworks",
  "tags": ["css", "utility-first", "responsive"],
  "icon": "🎨",
  "isDefault": false,
  "dependencies": [],
  "conflicts": ["material-ui", "chakra-ui"],
  "npmPackages": ["tailwindcss", "@tailwindcss/forms", "autoprefixer", "postcss"],
  "complexity": "low"
}
```

### `_catalog.json` (per category)

```json
{
  "category": "features",
  "displayName": "Application Features",
  "description": "Composable feature modules to include in your application",
  "selectionMode": "multi",
  "items": ["authentication", "ai-integration", "real-time", "..."]
}
```

---

## Feature & Option Inventory

### Features (Multi-Select)

| ID | Name | Description | Default |
|----|------|-------------|---------|
| `authentication` | Authentication & Authorization | Login, registration, RBAC via CopilotSdk.Api | ✅ |
| `ai-integration` | AI Integration | LLM-powered features, prompt engineering patterns | ❌ |
| `real-time` | Real-Time Communication | WebSocket/SignalR/SSE for live updates | ❌ |
| `file-management` | File Management | Upload, download, preview, drag-and-drop | ❌ |
| `notifications` | Notifications | Toast notifications, push notifications, notification center | ❌ |
| `analytics-telemetry` | Analytics & Telemetry | Usage tracking, event logging, performance metrics | ❌ |
| `internationalization` | Internationalization (i18n) | Multi-language support, locale detection, RTL | ❌ |
| `theming-branding` | Theming & Branding | Custom theme system, dark/light mode, brand tokens | ❌ |
| `offline-support` | Offline Support | Service workers, cache-first strategies, sync queue | ❌ |
| `search` | Search & Filtering | Full-text search, faceted filters, debounced input | ❌ |
| `export-import` | Export / Import | CSV, JSON, PDF export; file import with validation | ❌ |
| `accessibility` | Accessibility (a11y) | WCAG 2.1 AA compliance, screen reader support, keyboard nav | ✅ |
| `drag-and-drop` | Drag and Drop | Sortable lists, kanban boards, drag-to-rearrange | ❌ |
| `form-validation` | Advanced Forms | Multi-step forms, validation schemas (Zod/Yup), dynamic fields | ❌ |
| `data-visualization` | Data Visualization | Charts, graphs, dashboards (Recharts/D3) | ❌ |

### Persistence Options (Single-Select)

| ID | Name | Best For | Complexity |
|----|------|----------|------------|
| `localstorage` | localStorage | Small apps, settings, simple state | Low |
| `indexeddb` | IndexedDB | Structured data, offline-first, larger datasets | Medium |
| `sqlite-wasm` | SQLite (WASM) | Relational data in-browser, complex queries | Medium |
| `rest-api-backend` | REST API Backend | Server-persisted data, multi-user | High |
| `firebase` | Firebase/Firestore | Real-time sync, serverless, rapid prototyping | Medium |
| `supabase` | Supabase | Postgres-backed, open-source Firebase alternative | Medium |

### Tech Stack Options (Single-Select per Subcategory)

**UI Framework:**
| ID | Name | Default |
|----|------|---------|
| `vanilla-css` | Vanilla CSS / CSS Modules | ✅ |
| `tailwind-css` | Tailwind CSS | ❌ |
| `material-ui` | Material UI (MUI) | ❌ |
| `shadcn-ui` | shadcn/ui + Radix | ❌ |
| `chakra-ui` | Chakra UI | ❌ |

**State Management:**
| ID | Name | Default |
|----|------|---------|
| `react-context` | React Context + useReducer | ✅ |
| `zustand` | Zustand | ❌ |
| `redux-toolkit` | Redux Toolkit | ❌ |
| `jotai` | Jotai (Atomic) | ❌ |

**Testing:**
| ID | Name | Default |
|----|------|---------|
| `vitest` | Vitest | ✅ |
| `jest` | Jest + React Testing Library | ❌ |
| `playwright` | Playwright (E2E) | ❌ |

**Routing:**
| ID | Name | Default |
|----|------|---------|
| `react-router` | React Router v6+ | ✅ |
| `tanstack-router` | TanStack Router | ❌ |

**Bundler:**
| ID | Name | Default |
|----|------|---------|
| `vite` | Vite | ✅ |
| `next-js` | Next.js | ❌ |

---

## System Prompt Composition

When a session is created, the final system message is composed by:

1. **Base template** content (from the selected system prompt template)
2. **Selected features** — each `content.md` is appended as a labeled section
3. **Selected persistence option** — appended as a "Persistence" section
4. **Selected tech stack** — appended as a "Tech Stack" section
5. **User's custom system message** — appended last (with Append mode) or replaces all (with Replace mode)

```
[Base Template Content]

---
## Feature: Authentication & Authorization
[content.md from features/authentication/]

## Feature: Accessibility
[content.md from features/accessibility/]

---
## Persistence Strategy: IndexedDB
[content.md from persistence_options/indexeddb/]

---
## Tech Stack
### UI Framework: Tailwind CSS
[content.md from tech_stack/ui_frameworks/tailwind-css/]

### State Management: Zustand
[content.md from tech_stack/state_management/zustand/]

### Testing: Vitest
[content.md from tech_stack/testing/vitest/]

### Routing: React Router v6+
[content.md from tech_stack/routing/react-router/]

### Bundler: Vite
[content.md from tech_stack/bundler/vite/]

---
## Additional Instructions
[User's custom content]
```

---

## Backend Changes

### New Service: `ISessionConfigurationService`

```
Services/
├── ISessionConfigurationService.cs
├── SessionConfigurationService.cs
```

- `GetFeaturesAsync()` → scans `docs/system_prompts/features/`
- `GetPersistenceOptionsAsync()` → scans `docs/system_prompts/persistence_options/`
- `GetTechStackOptionsAsync()` → scans `docs/system_prompts/tech_stack/` (nested subcategories)
- `GetFeatureContentAsync(featureId)` → reads `content.md`
- `ComposeSystemMessageAsync(templateName, featureIds[], persistenceId, techStackIds[], customContent)` → builds final prompt

### New Controller: `SessionConfigurationController`

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/copilot/session-config/features` | List available features |
| GET | `/api/copilot/session-config/features/{id}` | Get feature details + content |
| GET | `/api/copilot/session-config/persistence` | List persistence options |
| GET | `/api/copilot/session-config/persistence/{id}` | Get persistence option details |
| GET | `/api/copilot/session-config/tech-stack` | List all tech stack categories + options |
| GET | `/api/copilot/session-config/tech-stack/{category}/{id}` | Get specific tech stack option |
| POST | `/api/copilot/session-config/compose` | Compose a full system message from selections |

### Extended `CreateSessionRequest`

Add new optional fields:

```csharp
public List<string>? SelectedFeatures { get; set; }
public string? SelectedPersistence { get; set; }
public Dictionary<string, string>? SelectedTechStack { get; set; }  // subcategory → optionId
```

---

## Frontend Changes

### Extended `CreateSessionModal` Tabs

Current: `Basic | System Message | Tools | Provider`

Proposed: `Basic | Features | Tech Stack | System Message | Tools | Provider`

**Features Tab:**
- Card grid with toggle switches per feature
- Shows icon, name, description, complexity badge
- Dependency warnings (e.g., "Offline Support requires IndexedDB persistence")
- Persistence dropdown (single-select)

**Tech Stack Tab:**
- Grouped by subcategory (UI Framework, State Management, Testing, Routing, Bundler)
- Radio-button groups within each subcategory
- Shows npm packages that will be included
- Conflict warnings

**System Message Tab (enhanced):**
- New "Preview Composed Message" button
- Shows the composed output of template + features + persistence + tech stack + custom content
- Read-only preview with expandable sections

### New Types

```typescript
interface FeatureDefinition {
  id: string;
  name: string;
  description: string;
  icon: string;
  tags: string[];
  isDefault: boolean;
  dependencies: string[];
  conflicts: string[];
  complexity: 'low' | 'medium' | 'high';
}

interface PersistenceOption {
  id: string;
  name: string;
  description: string;
  complexity: 'low' | 'medium' | 'high';
}

interface TechStackCategory {
  id: string;
  displayName: string;
  selectionMode: 'single';
  options: TechStackOption[];
}

interface TechStackOption {
  id: string;
  name: string;
  description: string;
  isDefault: boolean;
  npmPackages: string[];
  conflicts: string[];
}
```

---

## UI Flow

```
┌─────────────────────────────────────────────────────┐
│  Create New Session                                  │
│                                                      │
│  ┌─────┬──────────┬───────────┬────────┬─────┬─────┐│
│  │Basic│ Features │Tech Stack │Sys Msg │Tools│BYOK ││
│  └─────┴──────────┴───────────┴────────┴─────┴─────┘│
│                                                      │
│  ┌─ Features ──────────────────────────────────────┐ │
│  │                                                  │ │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐  │ │
│  │  │ 🔐 Auth    │ │ 🤖 AI     │ │ ⚡ Real-   │  │ │
│  │  │ ☑ Default  │ │ ☐         │ │    Time    │  │ │
│  │  │ medium     │ │           │ │ ☐         │  │ │
│  │  └────────────┘ └────────────┘ └────────────┘  │ │
│  │  ┌────────────┐ ┌────────────┐ ┌────────────┐  │ │
│  │  │ 📁 Files   │ │ 🔔 Notif  │ │ 📊 Charts  │  │ │
│  │  │ ☐         │ │ ☐         │ │ ☐         │  │ │
│  │  └────────────┘ └────────────┘ └────────────┘  │ │
│  │                                                  │ │
│  │  ── Persistence Strategy ────────────────────── │ │
│  │  ┌──────────────────────────────────────────┐   │ │
│  │  │ ▼ localStorage (default)                 │   │ │
│  │  └──────────────────────────────────────────┘   │ │
│  └──────────────────────────────────────────────────┘ │
│                                                      │
│                          [Cancel]  [Create Session]  │
└─────────────────────────────────────────────────────┘
```

---

## Phased Implementation Plan

### Phase 1: Folder Structure & Content Authoring

**Goal**: Create the file system layout, metadata schemas, and initial content for all features, persistence options, and tech stack options.

#### Tasks

- [ ] 1.1 Create `docs/system_prompts/features/` directory
- [ ] 1.2 Create `docs/system_prompts/persistence_options/` directory
- [ ] 1.3 Create `docs/system_prompts/tech_stack/` directory with subcategories:
  - `ui_frameworks/`, `state_management/`, `testing/`, `routing/`, `bundler/`
- [ ] 1.4 Define JSON metadata schema and document it in this plan file
  - `feature.json` schema
  - `option.json` schema
  - `_catalog.json` schema
- [ ] 1.5 Create `_catalog.json` for each top-level category
- [ ] 1.6 Create feature modules (feature.json + content.md for each):
  - `authentication` — extract from existing template Section 2
  - `ai-integration` — LLM patterns, prompt engineering, streaming
  - `real-time` — WebSocket/SignalR/SSE patterns
  - `file-management` — upload, download, drag-drop, preview
  - `notifications` — toast, push, notification center
  - `analytics-telemetry` — event tracking, performance metrics
  - `internationalization` — i18n with react-intl or i18next
  - `theming-branding` — theme tokens, dark/light mode, brand system
  - `offline-support` — service workers, cache strategies, sync
  - `search` — full-text, faceted filters, debounced input
  - `export-import` — CSV/JSON/PDF export, file import
  - `accessibility` — WCAG 2.1 AA, keyboard nav, screen readers
  - `drag-and-drop` — sortable lists, kanban, DnD Kit
  - `form-validation` — Zod/Yup schemas, multi-step, dynamic fields
  - `data-visualization` — Recharts/D3, dashboards, responsive charts
- [ ] 1.7 Create persistence option modules:
  - `localstorage` — simple key-value, size limits, serialization
  - `indexeddb` — structured storage, transactions, Dexie.js wrapper
  - `sqlite-wasm` — sql.js / wa-sqlite, relational queries
  - `rest-api-backend` — fetch/axios, REST conventions, error handling
  - `firebase` — Firestore, real-time listeners, auth integration
  - `supabase` — Postgres, Row Level Security, real-time subscriptions
- [ ] 1.8 Create tech stack option modules:
  - UI Frameworks: `vanilla-css`, `tailwind-css`, `material-ui`, `shadcn-ui`, `chakra-ui`
  - State Management: `react-context`, `zustand`, `redux-toolkit`, `jotai`
  - Testing: `vitest`, `jest`, `playwright`
  - Routing: `react-router`, `tanstack-router`
  - Bundler: `vite`, `next-js`
- [ ] 1.9 Review all content.md files for quality and consistency
- [ ] 1.10 Verify JSON files parse correctly (write a simple validation script or test)

---

### Phase 2: Backend — Discovery Service & API

**Goal**: Create the backend service that discovers and serves features, persistence options, and tech stack options. Add a composition endpoint.

#### Tasks

- [ ] 2.1 Create domain models in `Models/Domain/`:
  - `FeatureDefinition.cs` (id, name, description, icon, tags, isDefault, dependencies, conflicts, compatibleWith, complexity, estimatedImpact)
  - `PersistenceOptionDefinition.cs` (id, name, description, complexity, isDefault)
  - `TechStackCategory.cs` (id, displayName, selectionMode, subcategory)
  - `TechStackOptionDefinition.cs` (id, name, description, isDefault, npmPackages, conflicts, subcategory)
  - `SessionConfigurationCatalog.cs` (features, persistence, techStack)
- [ ] 2.2 Create request/response models:
  - `Models/Responses/FeatureListResponse.cs`
  - `Models/Responses/FeatureDetailResponse.cs`
  - `Models/Responses/PersistenceListResponse.cs`
  - `Models/Responses/TechStackListResponse.cs`
  - `Models/Requests/ComposeSystemMessageRequest.cs` (templateName, featureIds[], persistenceId, techStackSelections{}, customContent)
  - `Models/Responses/ComposeSystemMessageResponse.cs` (composedContent, sections[])
- [ ] 2.3 Create `Services/ISessionConfigurationService.cs` interface:
  - `GetFeaturesAsync()` → `List<FeatureDefinition>`
  - `GetFeatureDetailAsync(featureId)` → `FeatureDetailResponse` (includes content.md)
  - `GetPersistenceOptionsAsync()` → `List<PersistenceOptionDefinition>`
  - `GetPersistenceDetailAsync(optionId)` → detail + content
  - `GetTechStackCategoriesAsync()` → `List<TechStackCategory>` with nested options
  - `GetTechStackDetailAsync(category, optionId)` → detail + content
  - `ComposeSystemMessageAsync(request)` → `ComposeSystemMessageResponse`
- [ ] 2.4 Create `Services/SessionConfigurationService.cs`:
  - Scan `docs/system_prompts/features/` for `feature.json` files
  - Scan `docs/system_prompts/persistence_options/` for `option.json` files
  - Scan `docs/system_prompts/tech_stack/{subcategory}/` for `option.json` files
  - Parse JSON metadata, read `content.md` on detail requests
  - Cache discovered options in `IMemoryCache` (invalidate on file change or manual refresh)
  - Implement composition: concatenate template + feature contents + persistence + tech stack + custom
- [ ] 2.5 Create `Controllers/SessionConfigurationController.cs`:
  - `GET /api/copilot/session-config/features`
  - `GET /api/copilot/session-config/features/{id}`
  - `GET /api/copilot/session-config/persistence`
  - `GET /api/copilot/session-config/persistence/{id}`
  - `GET /api/copilot/session-config/tech-stack`
  - `GET /api/copilot/session-config/tech-stack/{category}/{id}`
  - `POST /api/copilot/session-config/compose`
  - `POST /api/copilot/session-config/refresh` (clear cache)
- [ ] 2.6 Register service in `Program.cs` DI container
- [ ] 2.7 Extend `CreateSessionRequest` with optional fields:
  - `SelectedFeatures` (List<string>?)
  - `SelectedPersistence` (string?)
  - `SelectedTechStack` (Dictionary<string, string>?)
- [ ] 2.8 Update `SessionService.CreateSessionAsync` to:
  - If configuration selections are present, call `ComposeSystemMessageAsync` to build the final system message
  - Merge composed message with any explicit `SystemMessage` from the request
- [ ] 2.9 Write unit tests for `SessionConfigurationService`:
  - Feature discovery (mock file system or use test fixtures)
  - Persistence option discovery
  - Tech stack discovery with subcategories
  - System message composition (multiple features + persistence + tech stack)
  - Dependency validation
  - Conflict detection
- [ ] 2.10 Write unit tests for `SessionConfigurationController`:
  - All endpoints return expected shapes
  - 404 for unknown feature/option IDs
  - Compose endpoint builds correct output
- [ ] 2.11 Run all backend tests, verify pass

---

### Phase 3: Frontend — Types, API Client, and State

**Goal**: Add TypeScript types, API client functions, and context/state for configuration options.

#### Tasks

- [ ] 3.1 Create `types/configuration.types.ts`:
  - `FeatureDefinition` interface
  - `PersistenceOption` interface
  - `TechStackCategory` interface
  - `TechStackOption` interface
  - `ComposeSystemMessageRequest` interface
  - `ComposeSystemMessageResponse` interface
  - `SessionConfigurationSelections` interface (features[], persistence, techStack{})
- [ ] 3.2 Extend `types/session.types.ts`:
  - Add `selectedFeatures`, `selectedPersistence`, `selectedTechStack` to `CreateSessionRequest`
- [ ] 3.3 Add API functions to `api/copilotApi.ts`:
  - `getFeatures()` → `FeatureDefinition[]`
  - `getFeatureDetail(id)` → `FeatureDetailResponse`
  - `getPersistenceOptions()` → `PersistenceOption[]`
  - `getPersistenceDetail(id)` → detail
  - `getTechStackCategories()` → `TechStackCategory[]`
  - `getTechStackDetail(category, id)` → detail
  - `composeSystemMessage(request)` → `ComposeSystemMessageResponse`
  - `refreshSessionConfig()` → void
- [ ] 3.4 Create `hooks/useSessionConfiguration.ts`:
  - Fetches features, persistence options, tech stack on mount
  - Manages selection state (selected features set, persistence choice, tech stack choices)
  - Provides toggle/select/deselect functions
  - Validates dependencies and conflicts
  - Exposes `composeMessage()` function
  - Loading and error states
- [ ] 3.5 Write tests for `useSessionConfiguration` hook
- [ ] 3.6 Run all frontend tests, verify pass

---

### Phase 4: Frontend — Feature Selection UI

**Goal**: Build the Features tab in the Create Session modal.

#### Tasks

- [ ] 4.1 Create `components/Configuration/FeatureCard.tsx`:
  - Toggle switch with icon, name, description
  - Complexity badge (low/medium/high with color coding)
  - Tags as small pills
  - Disabled state when dependency not met (tooltip explains why)
  - Conflict warning indicator
- [ ] 4.2 Create `components/Configuration/FeatureGrid.tsx`:
  - Responsive card grid layout
  - Search/filter bar (by name or tag)
  - "Select Defaults" button to reset to defaults
  - Selected count indicator
- [ ] 4.3 Create `components/Configuration/PersistenceSelector.tsx`:
  - Dropdown or radio card group
  - Each option shows name, description, complexity badge
  - Warning if conflicts with selected features (e.g., offline-support without indexeddb)
- [ ] 4.4 Create `components/Configuration/DependencyWarning.tsx`:
  - Inline warning component showing unmet dependencies
  - "Auto-add" button to include required dependencies
- [ ] 4.5 Add "Features" tab to `CreateSessionModal`:
  - Between "Basic" and current "System Message" tab
  - Contains `FeatureGrid` + `PersistenceSelector`
  - Selections stored in modal state, passed to create request
- [ ] 4.6 Wire up feature selections to the create session flow
- [ ] 4.7 Write component tests:
  - `FeatureCard` — toggle, disabled state, conflict display
  - `FeatureGrid` — filtering, default selection, count
  - `PersistenceSelector` — selection, conflict warnings
  - `DependencyWarning` — auto-add behavior
- [ ] 4.8 Run all frontend tests, verify pass

---

### Phase 5: Frontend — Tech Stack Selection UI

**Goal**: Build the Tech Stack tab in the Create Session modal.

#### Tasks

- [ ] 5.1 Create `components/Configuration/TechStackGroup.tsx`:
  - Radio button group for a single subcategory
  - Each option shows name, description, npm packages
  - Default option pre-selected
  - Conflict indicator with other selected tech stack options
- [ ] 5.2 Create `components/Configuration/TechStackPanel.tsx`:
  - Renders all subcategories as `TechStackGroup` components
  - Shows a "Selected Stack Summary" at the bottom
  - Lists all npm packages that will be included
- [ ] 5.3 Create `components/Configuration/StackSummary.tsx`:
  - Compact summary of all tech stack selections
  - Package count badge
  - "Reset to Defaults" button
- [ ] 5.4 Add "Tech Stack" tab to `CreateSessionModal`:
  - Between "Features" and "System Message" tabs
  - Contains `TechStackPanel`
- [ ] 5.5 Wire up tech stack selections to the create session flow
- [ ] 5.6 Write component tests:
  - `TechStackGroup` — selection, default, conflicts
  - `TechStackPanel` — all groups render, summary accurate
  - `StackSummary` — package list, reset
- [ ] 5.7 Run all frontend tests, verify pass

---

### Phase 6: System Message Composition & Preview

**Goal**: Integrate feature/tech stack selections into system message composition with a live preview.

#### Tasks

- [ ] 6.1 Create `components/Configuration/ComposedMessagePreview.tsx`:
  - Read-only view of the composed system message
  - Collapsible sections (template, each feature, persistence, tech stack, custom)
  - Section count and total character/token count
  - Copy-to-clipboard button
  - Syntax highlighting for markdown sections
- [ ] 6.2 Update `SystemMessageEditor` to include composition awareness:
  - Show a "Preview Composed" button when features/tech stack are selected
  - Indicate that custom content will be appended after composed sections
  - Show estimated total prompt size
- [ ] 6.3 Update `CreateSessionModal` submission logic:
  - Include `selectedFeatures`, `selectedPersistence`, `selectedTechStack` in request
  - If selections are present, the backend composes the final system message
  - Display preview before submission (optional confirmation step)
- [ ] 6.4 Add keyboard shortcut `Ctrl+Shift+P` for preview toggle
- [ ] 6.5 Write component tests:
  - `ComposedMessagePreview` — sections render, collapse/expand, copy
  - Modal submission includes all selections
- [ ] 6.6 Run all tests (frontend + backend), verify pass

---

### Phase 7: Persistence & Session History

**Goal**: Persist configuration selections with sessions so they can be viewed/reused later.

#### Tasks

- [ ] 7.1 Extend `PersistedSessionConfig` with:
  - `SelectedFeatures` (List<string>?)
  - `SelectedPersistence` (string?)
  - `SelectedTechStack` (Dictionary<string, string>?)
- [ ] 7.2 Update `SessionManager.RegisterSessionAsync` to persist selections
- [ ] 7.3 Update `SessionInfoResponse` to include configuration selections
- [ ] 7.4 Add "Configuration" section to session detail view:
  - Show selected features as badges
  - Show persistence choice
  - Show tech stack choices
- [ ] 7.5 Add "Clone Configuration" button to session list:
  - Opens Create Session modal pre-populated with that session's configuration
- [ ] 7.6 Write unit tests for persistence of configuration selections
- [ ] 7.7 Write component tests for configuration display in session detail
- [ ] 7.8 Run all tests, verify pass

---

### Phase 8: Validation, Polish & Documentation

**Goal**: Add validation rules, improve UX, handle edge cases, and document.

#### Tasks

- [ ] 8.1 Implement dependency validation in `SessionConfigurationService`:
  - When composing, verify all feature dependencies are satisfied
  - Return warnings for unmet dependencies (don't block, just warn)
  - Return errors for hard conflicts
- [ ] 8.2 Implement conflict detection:
  - Feature-to-feature conflicts (e.g., two competing auth systems)
  - Feature-to-persistence conflicts (e.g., offline-support needs indexeddb or sqlite-wasm)
  - Tech stack conflicts (e.g., next-js conflicts with react-router)
- [ ] 8.3 Add sorting and categorization:
  - Features sorted by: defaults first, then alphabetical
  - "Recommended" badge for common combinations
- [ ] 8.4 Add "Preset Configurations" (optional):
  - "Minimal SPA" — vanilla-css, react-context, localstorage, accessibility only
  - "Full-Stack App" — auth, real-time, rest-api-backend, tailwind, zustand
  - "Offline-First" — auth, offline-support, indexeddb, notifications
  - "Data Dashboard" — auth, data-visualization, analytics, rest-api-backend
- [ ] 8.5 Responsive / mobile layout for new tabs
- [ ] 8.6 Write integration tests:
  - Full flow: select features → select tech stack → compose → create session
  - Verify composed system message contains expected sections
  - Verify persistence round-trip (create → retrieve → verify selections)
- [ ] 8.7 Update documentation:
  - Update README.md with new session configuration features
  - Update technical-overview.md with new architecture
  - Update copilot-instructions.md to mark this feature complete
  - Create `docs/session-configuration-guide.md` for end users
- [ ] 8.8 Run full test suite (frontend + backend), verify all pass
- [ ] 8.9 Final review of all new files and documentation

---

## Implementation Progress

- [ ] Phase 1: Folder Structure & Content Authoring
- [ ] Phase 2: Backend — Discovery Service & API
- [ ] Phase 3: Frontend — Types, API Client, and State
- [ ] Phase 4: Frontend — Feature Selection UI
- [ ] Phase 5: Frontend — Tech Stack Selection UI
- [ ] Phase 6: System Message Composition & Preview
- [ ] Phase 7: Persistence & Session History
- [ ] Phase 8: Validation, Polish & Documentation

---

## Key Design Decisions

1. **File-system driven discovery** — No database table for features/options. The file system IS the registry. This makes it trivial to add a new feature: create a folder, add two files, done.

2. **JSON metadata + Markdown content** — Metadata is structured (for UI rendering, validation) while content is freeform markdown (for system prompt composition). This separation keeps things clean.

3. **Composition over inheritance** — Features are additive. The base template provides the foundation, features layer on capabilities. No feature overrides another; conflicts are detected and warned.

4. **Backend composition** — The backend composes the final system message, not the frontend. This ensures consistent output and allows the backend to validate dependencies/conflicts before creating the session.

5. **Backward compatibility** — All new fields are optional. Existing sessions and API calls work unchanged. The `CreateSessionRequest.SystemMessage` field can still be used standalone.

6. **Cache with manual refresh** — Feature/option discovery is cached in `IMemoryCache` for performance. A refresh endpoint allows reloading after adding new content files without restarting the server.
