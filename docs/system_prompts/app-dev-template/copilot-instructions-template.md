# TEMPLATE: `.github/copilot-instructions.md` (Frontend-Only SPA)

> **Purpose:** This is a reusable template for generating domain-specific
> `copilot-instructions.md` files. When specializing for a particular domain
> (games, education, productivity, etc.), replace placeholder tokens
> (`<APP_NAME>`, `<APP_REPO>`, `<DOMAIN_TYPE>`) and add domain-specific
> sections where indicated by `<!-- DOMAIN: ... -->` comments.

---

## 0) Identity & Naming (Required First Step)

**Mandatory:** Before creating any files or repos:

1. Propose **3–7 names** suitable for the app concept.
2. Choose **one canonical name** and derive a **repo-safe slug**.

**Naming rules**
- Canonical name: `<APP_NAME>` (human-friendly, e.g., "LinkittyDo Word Safari")
- Repo slug: `<APP_REPO>` (lowercase, letters/numbers/hyphens only, no spaces, 3–32 chars, e.g., `linkittydo-word-safari`)

**Immutability**
- After selection, treat `<APP_NAME>` and `<APP_REPO>` as constants and use them everywhere:
  - repo folder + GitHub repo
  - README title
  - `docs/*` filenames
  - `package.json` name
  - app header/title screen
  - localStorage/indexedDB key prefixes
  - issue/PR titles and branch naming

**Required identity map**
Create `docs/<APP_REPO>_creation.md` section "App Identity" containing:
- App Name: `<APP_NAME>`
- Repo Slug: `<APP_REPO>`
- Storage Prefix: `<APP_REPO>:`
- Display Title: `<APP_NAME>`
- Short ID: `<APP_REPO>`
- Domain Type: `<DOMAIN_TYPE>` (e.g., "game", "education", "productivity", "utility")

**Hard rule:** No placeholders like `theapp`/`thegame`/`myproject` may remain after naming is chosen.

---

## 1) Non-Negotiables (SPA Constraints)

- **Frontend-only SPA**: no custom server-side code, no external databases, no custom backend APIs.
  - **One exception**: authentication calls to the **CopilotSdk.Api** (see Section 2).
- **React + Vite + TypeScript** required.
- All work stays **inside the repo folder `<APP_REPO>/`** (no external session folders).
- **Plan-driven execution**: `docs/<APP_REPO>_creation.md` is the single source of truth.
- Prefer **simple, auditable dependencies**; justify additions in the plan.
- The app must **not function for unauthenticated users** (see Section 2).

---

## 2) Authentication & Authorization (Mandatory)

Every app built from this template **must** require user authentication before granting access to any functionality. Authentication is provided by the **CopilotSdk.Api** backend — this is the **only** external API the app is permitted to call.

### 2.1) CopilotSdk.Api Auth Endpoints

The authentication backend runs at a configurable base URL (default: `http://localhost:5139`).

| Method | Endpoint | Purpose | Body |
|--------|----------|---------|------|
| `POST` | `/api/users/register` | Create a new account | `{ username, email, displayName, password, confirmPassword }` |
| `POST` | `/api/users/login` | Authenticate a user | `{ username, password }` |
| `POST` | `/api/users/logout` | Log out (client-side clear) | — |
| `GET`  | `/api/users/me` | Get current user profile | — (requires `X-User-Id` header) |

**Login response shape:**
```json
{
  "success": true,
  "message": "",
  "user": {
    "id": "string",
    "username": "string",
    "email": "string",
    "displayName": "string",
    "role": "Player | Creator | Admin",
    "avatarType": "Default | Preset | Custom",
    "avatarData": "string | null",
    "isActive": true,
    "createdAt": "ISO-string",
    "updatedAt": "ISO-string",
    "lastLoginAt": "ISO-string | null"
  }
}
```

### 2.2) User Roles

| Role | Value | Description |
|------|-------|-------------|
| `Player` | 0 | Can use the app. Default role for new registrations. |
| `Creator` | 1 | Can use the app + access creator/editor features (if the domain has them). |
| `Admin` | 2 | Full access including admin panels and user management. |

The app must respect these roles. At minimum, all three roles can use the app. If the domain warrants it (e.g., level editors, content creation), gate creator features behind `Creator` or `Admin` roles.

<!-- DOMAIN: Define role behavior for your domain. For example, in a game domain:
     "Players can play games. Creators can also design and publish levels. Admins can manage all content and users." -->

### 2.3) Required Auth Implementation

The app **must** implement the following:

**Auth module (`src/auth/`)**
- `authApi.ts` — API client for login, register, logout, validate session
- `AuthContext.tsx` — React context providing `{ user, login, register, logout, isAuthenticated, isLoading }`
- `ProtectedRoute.tsx` — Route wrapper that redirects to `/login` if unauthenticated
- `LoginScreen.tsx` — Login form (username + password)
- `RegisterScreen.tsx` — Registration form (username, email, display name, password, confirm)

**Auth flow:**
1. On app load, check `localStorage` for stored user data (`<APP_REPO>:user`).
2. If found, validate with `GET /api/users/me` using the `X-User-Id` header.
3. If valid → proceed to app. If invalid → clear storage, show login screen.
4. Login: `POST /api/users/login` → on success, store user data + userId in `localStorage`.
5. All authenticated requests to CopilotSdk.Api include the `X-User-Id: <userId>` header.
6. Logout: clear `localStorage` auth keys, redirect to login screen.

**Route structure:**
- `/login` — Login screen (public)
- `/register` — Registration screen (public)
- `/*` — All other routes wrapped in `<ProtectedRoute>` (requires authentication)

**Auth API configuration:**
- Store the API base URL in an environment variable: `VITE_AUTH_API_URL`
- Default value: `http://localhost:5139`
- The `authApi.ts` client must read from `import.meta.env.VITE_AUTH_API_URL`

**CORS note:** The CopilotSdk.Api must have the generated app's origin (e.g., `http://localhost:5173`) added to its CORS policy. Document this requirement in the app's README.

### 2.4) Auth UI Requirements

- The login screen must display the `<APP_NAME>` branding/title.
- Show clear error messages for invalid credentials or registration failures.
- Provide a link/button to switch between login and registration.
- After successful login, navigate to the app's main view.
- Display the logged-in user's display name somewhere in the app header/nav.
- Provide a visible logout action in the app header/nav.
- Auth screens must use the app's theme (colors, fonts, layout style).

---

## 3) Repo Operating Rules (Scope + Workflow)

### Repo Scope (No External Files)
- All files must be created/edited inside `<APP_REPO>`.
- Never read/write from `$HOME$/.copilot/sessions` or any external folder.
- If scratch space is needed, use `.\.tmp\` (create if missing).
- Use **repo-relative paths** unless explicitly required.

### Plan of Record
Canonical plan: `docs/<APP_REPO>_creation.md`

Every dev session must:
1. Read the plan and identify next incomplete tasks (`- [ ]`).
2. Implement the smallest coherent unit.
3. Run required checks (tests/build).
4. Commit atomic changes.
5. Update plan checkboxes + notes.

---

## 4) Required Output Format When Starting a New App

When asked to create a new app under this instruction set, respond with:

### Level 1: Executive Summary
1–2 paragraphs: what the SPA does, the target domain, and the user value.

### Level 2: Stage Roadmap (Exactly 3 stages)
1. **Stage 1: MVP** — Core functionality + authentication + basic UI
2. **Stage 2: Feature expansion** — Domain-specific features, polish, role-based features
3. **Stage 3: Extensibility + polish** — Architecture-ready, accessibility, advanced features

Each stage must include **phases**, and each phase must include **numbered steps**.

**Stage 1 must always include:**
- Project scaffolding (Vite + React + TypeScript)
- Authentication module (login, register, protected routes)
- Core domain logic
- Basic UI with theme

### Level 3: Implementation Plan
Must include:
- repository layout
- major modules/components
- state model + persistence approach
- routing map (must include `/login`, `/register`, and protected routes)
- authentication integration details
- test strategy (what is unit-tested vs interaction-tested)
- accessibility commitments

After planning, generate **Stage 1 scaffolding only**.

---

## 5) UI Theme & Design System (Optional-by-Type, Required-if-Branded)

If this app type requires branding or a consistent theme, define it explicitly.

**Theme definition must exist**
- `src/theme/theme.ts` (tokens: colors, spacing, typography, radii, shadows)
- `src/theme/global.css` (CSS custom properties / variables)

**Document theme**
- `docs/theme.md` describing palette, typography, component styles, and any additions.

**Rule**
- No new colors/fonts/components without documenting in `docs/theme.md`.
- The theme must apply consistently to auth screens (login/register) as well as the main app.

<!-- DOMAIN: Define your brand here. For example, a game brand would specify
     palette, headline fonts, UI style (chunky/flat/minimal), etc. -->

---

## 6) SPA Architecture Rules (Client-Side)

### State management
- Prefer local component state for small apps.
- For medium/large apps, choose one:
  - React Context + reducer
  - Zustand
  - Redux Toolkit
- Document the choice in the plan.
- `AuthContext` is always required (see Section 2) regardless of other state choices.

### Logic isolation
- Pure domain logic in `src/core/` (deterministic functions, validators, generators)
- Auth module in `src/auth/` (API client, context, protected route, screens)
- UI components in `src/ui/`
- App shell/routes in `src/app/`

### Routing
- Use `react-router-dom` (v6+).
- Public routes: `/login`, `/register`
- Protected routes: everything else, wrapped in `<ProtectedRoute>`
- Define all routes in `src/app/routes.tsx` or `src/app/App.tsx`.

### Rendering/performance
- Do not create unnecessary high-frequency rerenders.
- For animation-heavy experiences:
  - isolate loops outside React state (use refs, requestAnimationFrame, or canvas modules)
  - React controls shell/menus/overlays

### Persistence (client-only)
- Use `localStorage` for settings/preferences/small state.
- Use `indexedDB` when you need structured or large data.
- Prefix **all** keys with `<APP_REPO>:`.
- Auth keys: `<APP_REPO>:user`, `<APP_REPO>:userId`

### External API calls
- **Only** the CopilotSdk.Api auth endpoints are permitted (see Section 2).
- No other backend APIs, third-party data APIs, or server calls.
- All domain data must be generated, stored, and managed client-side.

---

## 7) Standard Project Structure (Required)

```
/
├── .github/
│   └── copilot-instructions.md
├── docs/
│   ├── <APP_REPO>_creation.md       # plan of record
│   └── theme.md                      # if themed/branded
├── src/
│   ├── app/                          # routing, app shell, layout
│   │   ├── App.tsx
│   │   ├── routes.tsx
│   │   └── Layout.tsx
│   ├── auth/                         # authentication (MANDATORY)
│   │   ├── authApi.ts                # CopilotSdk.Api auth client
│   │   ├── AuthContext.tsx            # auth state provider
│   │   ├── ProtectedRoute.tsx        # route guard
│   │   ├── LoginScreen.tsx           # login form
│   │   └── RegisterScreen.tsx        # registration form
│   ├── core/                         # pure domain logic: validators, generators, models
│   ├── ui/
│   │   ├── components/               # reusable UI components
│   │   └── screens/                  # page-level views (domain-specific)
│   ├── theme/                        # theme tokens + CSS vars (if branded)
│   ├── types/                        # shared TypeScript interfaces
│   ├── utils/                        # general-purpose utilities
│   └── main.tsx                      # entry point
├── tests/
├── public/
├── .env                              # VITE_AUTH_API_URL=http://localhost:5139
├── .env.example                      # checked in, documents required env vars
├── README.md
└── package.json
```

<!-- DOMAIN: Add domain-specific directories here. For example, a game app might add:
     src/engine/       — game loop, canvas renderer, audio
     src/game/logic/   — rules, scoring, generators
     src/game/scenes/  — title, playing, results
-->

---

## 8) Testing & Quality Gates (Required)

### Minimum standard
- Unit tests for pure logic in `src/core/`.
- Unit tests for auth logic (API client mocking, context behavior).
- Interaction tests where UI behavior is central.

Preferred tooling:
- Vitest
- @testing-library/react

### Auth-specific tests (required)
- Login success → stores user data, navigates to app
- Login failure → displays error, remains on login screen
- Protected route → redirects to login when unauthenticated
- Logout → clears stored data, redirects to login
- Session validation → revalidates stored user on app load

### Required before every commit
- `npm test`
- `npm run build` when assets/build pipeline changed

### Dependency hygiene
- Avoid large libraries unless they clearly reduce complexity.
- Document dependency decisions in plan notes.

---

## 9) Accessibility & UX (Required)

- All core actions must work with:
  - keyboard
  - mouse/pointer (touch-friendly by default)
- Visible focus states.
- Respect `prefers-reduced-motion`.
- Maintain readable contrast and legible typography.
- Provide clear reset/undo/restart patterns where applicable.
- Auth forms must:
  - Have proper `<label>` associations
  - Show validation errors inline with `aria-describedby`
  - Support form submission via Enter key
  - Announce errors to screen readers via `aria-live` regions

---

## 10) Tooling Commands (Windows + Git + Node)

### Windows navigation/inspection
- `cd`, `dir`, `tree`
- `type`, `more`
- `findstr`

### File/folder management (repo-only)
- `mkdir`, `rmdir /s /q`, `del`, `copy`, `xcopy`, `robocopy`, `move`
- PowerShell equivalents allowed:
  - `Get-ChildItem`, `Get-Content`, `Set-Content`, `New-Item`, `Remove-Item`, `Copy-Item`, `Move-Item`

### Git/GitHub
- `git status`, `git diff`, `git add`, `git commit`, `git checkout -b`, `git push`, `git pull`
- `gh repo create/clone`, `gh issue create/list/view`, `gh pr create/list/view`

### Node/Vite
- `npm install`, `npm run dev`, `npm run build`, `npm run preview`, `npm test`
- `npx` allowed when necessary (prefer devDependencies)

---

## 11) Documentation Requirements (Required)

### README.md must include
- What the app does (purpose + key workflows)
- **Prerequisites:**
  - Node.js 18+
  - CopilotSdk.Api running at the configured URL (for authentication)
- How to run locally:
  - `cp .env.example .env` (configure `VITE_AUTH_API_URL` if needed)
  - `npm install`
  - `npm run dev`
- How to test/build:
  - `npm test`
  - `npm run build`
- **Authentication:**
  - Note that users must log in before using the app
  - Link to CopilotSdk.Api for user management / registration
  - Document required CORS configuration on CopilotSdk.Api
- App Identity:
  - `<APP_NAME>`, `<APP_REPO>`
- If branded: theme summary + link to `docs/theme.md`

### `docs/<APP_REPO>_creation.md` must
- Track tasks with checkboxes
- Maintain "Notes & Decisions":
  - state management choice
  - persistence approach
  - routing approach (including auth routes)
  - authentication integration notes
  - major dependency adds + why
  - any deviations from template

---

## 12) Repo Initialization (Name-Driven)

Only after `<APP_NAME>` + `<APP_REPO>` are chosen:

1. Create local folder: `C:\development\repos\<APP_REPO>`
2. `cd C:\development\repos\<APP_REPO>`
3. `git init`
4. Create GitHub repo: `https://github.com/<YOUR_GH_USER>/<APP_REPO>`
5. Ensure branch is `main`
6. Add `README.md` + `.gitignore` + `.env.example`
7. Initial push:
   - `git add .`
   - `git commit -m "chore: initial commit"`
   - `git branch -M main`
   - `git push -u origin main`

---

## 13) Execution Loop (Required)

For each next task in `docs/<APP_REPO>_creation.md`:

1. Read plan → identify next `- [ ]`
2. Implement (repo-only)
3. Test/build
4. Commit atomically using conventional prefixes:
   - `feat:`, `fix:`, `test:`, `docs:`, `refactor:`, `chore:`
5. Update plan checkboxes + decision notes
6. Repeat

When feature branch is ready:
- `git push -u origin <branch>`
- `gh pr create --base main --head <branch> --title "..." --body "..."`

---

## 14) Enforcement Checklist (Must Pass)

### Identity
- [ ] No placeholder names remain (`theapp`, `thegame`, `myproject`, etc.).
- [ ] Repo folder == `<APP_REPO>`.
- [ ] `docs/<APP_REPO>_creation.md` exists and is referenced.
- [ ] `package.json` name matches `<APP_REPO>`.
- [ ] UI displays `<APP_NAME>`.
- [ ] Storage keys prefixed with `<APP_REPO>:`.

### Authentication
- [ ] `src/auth/` directory exists with all required files.
- [ ] Login screen is the entry point for unauthenticated users.
- [ ] `<ProtectedRoute>` wraps all non-auth routes.
- [ ] `X-User-Id` header sent on authenticated requests to CopilotSdk.Api.
- [ ] User data stored in `localStorage` under `<APP_REPO>:user` and `<APP_REPO>:userId`.
- [ ] Logout clears auth storage and redirects to login.
- [ ] Auth API base URL is configurable via `VITE_AUTH_API_URL`.
- [ ] `.env.example` documents the `VITE_AUTH_API_URL` variable.
- [ ] No external API calls other than CopilotSdk.Api auth endpoints.

### Quality
- [ ] Auth tests exist and pass (login, register, protected routes, logout).
- [ ] `npm test` passes.
- [ ] `npm run build` succeeds.
- [ ] README documents authentication prerequisites.

---

## Appendix: Domain Customization Guide

When creating a domain-specific `copilot-instructions.md` from this template:

1. **Replace all tokens**: `<APP_NAME>`, `<APP_REPO>`, `<DOMAIN_TYPE>`, `<YOUR_GH_USER>`
2. **Fill in `<!-- DOMAIN: ... -->` comment blocks** with domain-specific content
3. **Add domain-specific sections** as needed (e.g., game architecture, education pedagogy rules)
4. **Customize the project structure** in Section 7 to include domain directories
5. **Define role behavior** for Player/Creator/Admin in the context of your domain
6. **Add domain-specific testing requirements** to Section 8
7. **Add a theme/brand section** (Section 5) if the domain has visual identity requirements
8. **Remove this appendix** and the `<!-- DOMAIN: ... -->` comments from the final file

### Common domain patterns

| Domain | Extra directories | Role differentiation | Theme |
|--------|------------------|---------------------|-------|
| Game | `src/engine/`, `src/game/logic/`, `src/game/scenes/` | Player=play, Creator=design levels, Admin=manage | Often branded |
| Education | `src/lessons/`, `src/progress/` | Player=learn, Creator=author content, Admin=manage | Usually branded |
| Productivity | `src/features/`, `src/workflows/` | Player=use, Creator=configure, Admin=manage orgs | Optional |
| Utility | `src/tools/` | Minimal role distinction | Minimal |
