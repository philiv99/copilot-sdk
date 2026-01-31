
# Pipeline development assistant

You are building an application named **PipelineKit**.

## Operating Rules (Critical)

### Repo Scope (No External Files)

* **All files must be created/edited inside the current repository folder PipelineKit.**
* **Never** read/write from `$HOME$/.copilot/sessions` or any other external session folder.
* **Never** store temp files outside the repo. If you need scratch space, use:

  * `.\.tmp\` (create if missing)
* All paths must be **relative to the repo root** unless explicitly required.

### Primary Plan of Record

* The canonical plan is: **`docs/pipeline_creation.md`**
* Every dev session must:

  1. Read `docs/pipeline_creation.md`
  2. Execute the next incomplete tasks
  3. Update checkboxes and progress notes in `docs/pipeline_creation.md`
  4. Commit atomic changes

## Windows Tooling (Commands You May Use)

### Navigation & Inspection

* `cd`, `dir`, `tree`
* `type`, `more`
* `findstr` (search within files)

### File & Folder Management (Repo-only)

* `mkdir`, `rmdir /s /q`
* `del`
* `copy`, `xcopy`, `robocopy`
* `move`
* PowerShell equivalents allowed:

  * `Get-ChildItem`, `Get-Content`, `Set-Content`, `New-Item`, `Remove-Item`, `Copy-Item`, `Move-Item`

### Git / GitHub

* `git status`, `git diff`, `git add`, `git commit`, `git checkout -b`, `git push`
* `gh repo create`, `gh repo clone`
* `gh issue create`, `gh issue list`
* `gh pr create`, `gh pr merge`
* `gh release create`

### Node / React

* `npm install`, `npm run build`, `npm test`, `npm start`
* `npx` allowed when needed (prefer repo-local devDependencies)

## Architecture Principles

* **Frontend-only**: no server-side code, no external databases.
* Use browser APIs and `localStorage` for persistence.
* Use **React functional components + hooks**.
* Keep components small; isolate game logic in `src/utils/`.
* Prefer minimal dependencies; justify anything new.

## Standard Project Structure

```
/
├── .github/
│   └── copilot-instructions.md
├── docs/
│   └── plan.md
├── src/
│   ├── components/
│   ├── hooks/
│   ├── utils/
│   └── App.tsx
├── tests/
├── README.md
└── package.json
```

## Test & Quality Requirements

* Write tests for core game logic (rules, scoring, collisions, state transitions).
* **Run tests before every commit**: `npm test`
* Fix failing tests immediately.
* Keep changes small and reviewable.

## Documentation Requirements

### README.md Must Include

* App description and User Guide
* How to run locally

### docs/plan.md Rules

## Documentation Maintenance Rules

### Plan.md Maintenance
- **Track Progress**: 
	* Track Stages, Phases, and steps with checkboxes:
	  * `- [ ] Task`
	  * `- [x] Task`
	* Update after every session:
- **Document Decisions**: Record all significant technical decisions in the "Notes & Decisions" section, including:
  - Technology/library choices and rationale
  - Architecture decisions
  - Trade-offs considered
  - Implementation challenges and solutions
- **Update Dates**: Keep the Progress Tracking table current with actual start and completion dates

### Copilot-Instructions.md Maintenance
- **Keep Current**: Review and update this file whenever implementation details diverge from the original plan
- **Add Learnings**: Document important implementation patterns or conventions discovered during development
- **Technical Accuracy**: Ensure all technical stack details, project structure, and feature descriptions reflect the actual implementation

  * mark completed tasks
  * add brief notes on deviations or decisions

## First step

1. Create a local folder: `C:\development\repos\PipelineKit`
2. cd into `C:\development\repos\PipelineKit`
3. git init
2. Create a new repo at https://github.com/philiv99/PipelineKit
5. Be sure to create repo with a "main" branch (NOT master)
4. **IMPORTANT: make sure it has a README.md and an appropriate .gitignore File**
7. push commits to main branch
9. Begin execution using the session loop below

## “Execute the Plan” Session Loop (docs/pipeline_creation.md)

For each stage/phase/step, follow this exact loop:

2. **Read plan**

   * Open `docs/pipeline_creation.md` and identify the next `- [ ]` tasks.
  
3. **Implement**

   * Make changes only inside the repo.
   
4. **Test**

   * Run `npm test` (and `npm run build` if appropriate).
   
5. **Commit**

   * Atomic commit with a clear message:

     * `feat: ...`, `fix: ...`, `test: ...`, `docs: ...`, `refactor: ...
	 `
6. **Update plan**

   * Check off completed tasks and update documentation as needed
   
7. **Repeat**

   * Continue until the phase is completed

When completed make sure you 
1. push the committed changes to the current feature branch
2. Create a pull request to merge the current branch into main
3. send communication saying a Pull Request has been created. The puul request will need to be completed by hand


## Non-Negotiables

* No file operations outside the repo folder.
* No `$HOME$/.copilot/sessions`.
* `docs/pipeline_creation.md` is the single source of truth for what to do next.
