# TEMPLATE: `.github/copilot-instructions.md` (Frontend-Only SPA)

## 0) Identity & Naming (Required First Step)

**Mandatory:** Before creating any files or repos:

1. Propose **3–7 names** suitable for the app concept.
2. Choose **one canonical name** and derive a **repo-safe slug**.

**Naming rules**
- Canonical name: `<APP_NAME>` (human-friendly)
- Repo slug: `<APP_REPO>` (lowercase, letters/numbers/hyphens only, no spaces, 3–32 chars)

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
Create `docs/<APP_REPO>_creation.md` section “App Identity” containing:
- App Name: `<APP_NAME>`
- Repo Slug: `<APP_REPO>`
- Storage Prefix: `<APP_REPO>:`
- Display Title: `<APP_NAME>`
- Short ID: `<APP_REPO>`

**Hard rule:** No placeholders like `theapp`/`thegame` may remain after naming is chosen.

---

## 1) Non-Negotiables (SPA Constraints)

- **Frontend-only SPA**: no server-side code, no external databases.
- **React + Vite + TypeScript** required.
- All work stays **inside the repo folder `<APP_REPO>/`** (no external session folders).
- **Plan-driven execution**: `docs/<APP_REPO>_creation.md` is the single source of truth.
- Prefer **simple, auditable dependencies**; justify additions in the plan.

---

## 2) Repo Operating Rules (Scope + Workflow)

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

## 3) Required Output Format When Starting a New App Type

When asked to create a new app under this instruction set, respond with:

### Level 1: Executive Summary
1–2 paragraphs: what the SPA does and why.

### Level 2: Stage Roadmap (Exactly 3 stages)
1. Stage 1: MVP
2. Stage 2: Feature expansion
3. Stage 3: Extensibility + polish (architecture-ready)

Each stage must include **phases**, and each phase must include **numbered steps**.

### Level 3: Implementation Plan
Must include:
- repository layout
- major modules/components
- state model + persistence approach
- routing map (if any)
- test strategy (what is unit-tested vs interaction-tested)
- accessibility commitments

After planning, generate **Stage 1 scaffolding only**.

---

## 4) UI Theme & Design System (Optional-by-Type, Required-if-Branded)

If this app type requires branding or a consistent theme, define it explicitly.

**Theme definition must exist**
- `src/theme/theme.ts` (tokens)
- `src/theme/global.css` (CSS vars)

**Document theme**
- `docs/theme.md` describing palette, typography, component styles, and any additions.

**Rule**
- No new colors/fonts/components without documenting in `docs/theme.md`.

*(If you have a “house brand” like LinkittyDo, this is where those tokens and UI rules go.)*

---

## 5) SPA Architecture Rules (Client-Only)

### State management
- Prefer local component state for small apps.
- For medium/large apps, choose one:
  - React Context + reducer
  - Zustand
  - Redux Toolkit  
Document the choice in the plan.

### Logic isolation
- Pure logic in `src/core/` (deterministic functions, validators, generators)
- UI in `src/ui/`
- App shell/routes in `src/app/`

### Rendering/performance
- Do not create unnecessary high-frequency rerenders.
- For animation-heavy experiences:
  - isolate loops outside React state (use refs, requestAnimationFrame, or canvas modules)
  - React controls shell/menus/overlays

### Persistence (client-only)
- Use `localStorage` for settings/preferences/small state.
- Use `indexedDB` when you need structured or large data.
- Prefix all keys with `<APP_REPO>:`.

### Offline-first (optional)
- If offline behavior matters, document caching strategy and limits.

---

## 6) Standard Project Structure (Recommended)

```
/
├── .github/
│   └── copilot-instructions.md
├── docs/
│   ├── <APP_REPO>_creation.md
│   └── theme.md                  # if themed/branded
├── src/
│   ├── app/                      # routing, app shell
│   ├── core/                     # pure logic: validators, generators, domain model
│   ├── ui/
│   │   ├── components/
│   │   └── screens/
│   ├── theme/                    # theme tokens + css vars (optional/required if branded)
│   ├── utils/
│   ├── App.tsx
│   └── main.tsx
├── tests/
├── public/
├── README.md
└── package.json
```

---

## 7) Testing & Quality Gates (Required)

### Minimum standard
- Unit tests for pure logic in `src/core/`.
- Interaction tests where UI behavior is central.

Preferred tooling:
- Vitest
- @testing-library/react

### Required before every commit
- `npm test`
- `npm run build` when assets/build pipeline changed

### Dependency hygiene
- Avoid large libraries unless they clearly reduce complexity.
- Document dependency decisions in plan notes.

---

## 8) Accessibility & UX (Required)

- All core actions must work with:
  - keyboard
  - mouse/pointer (touch-friendly by default)
- Visible focus states.
- Respect `prefers-reduced-motion`.
- Maintain readable contrast and legible typography.
- Provide clear reset/undo/restart patterns where applicable.

---

## 9) Tooling Commands (Windows + Git + Node)

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

## 10) Documentation Requirements (Required)

### README.md must include
- What the app does (purpose + key workflows)
- How to run locally:
  - `npm install`
  - `npm run dev`
- How to test/build:
  - `npm test`
  - `npm run build`
- App Identity:
  - `<APP_NAME>`, `<APP_REPO>`
- If branded: theme summary + link to `docs/theme.md`

### `docs/<APP_REPO>_creation.md` must
- Track tasks with checkboxes
- Maintain “Notes & Decisions”:
  - state management choice
  - persistence approach
  - routing approach
  - major dependency adds + why
  - any deviations from template

---

## 11) Repo Initialization (Name-Driven)

Only after `<APP_NAME>` + `<APP_REPO>` are chosen:

1. Create local folder: `C:\development\repos\<APP_REPO>`
2. `cd C:\development\repos\<APP_REPO>`
3. `git init`
4. Create GitHub repo: `https://github.com/<YOUR_GH_USER>/<APP_REPO>`
5. Ensure branch is `main`
6. Add `README.md` + `.gitignore`
7. Initial push:
   - `git add .`
   - `git commit -m "chore: initial commit"`
   - `git branch -M main`
   - `git push -u origin main`

---

## 12) Execution Loop (Required)

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

## 13) Enforcement Checklist (Must Pass)

- No placeholder names remain.
- Repo folder == `<APP_REPO>`.
- `docs/<APP_REPO>_creation.md` exists and is referenced.
- `package.json` name matches `<APP_REPO>`.
- UI displays `<APP_NAME>`.
- Storage keys prefixed with `<APP_REPO>:`.
