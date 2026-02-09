
You are building an application named **PipelineKit Web**.

The ultimate goal is a **web app** where users can **create, edit, version, run, and monitor generic pipelines**. Pipelines standardize how information moves between stages (input → stage behaviors → output), while keeping the data formats **generic but consistently handled**.

You must use:

* **Frontend:** ReactJS + Vite + TypeScript
* **Backend:** .NET C# Web API
* **Database:** MySQL (required)
* **Pipeline configuration storage:** **ALL pipeline configuration must live in MySQL** (no config files required at runtime)
* **Config format:** JSON documents (stored in DB)
* **Secrets:** `.env` for sensitive values (DB connection strings, keys)
* **Architecture requirement:** favor shared modules/patterns and extensibility, because the app will later support adding new pipeline behaviors via “vibe coding” updates (do not implement vibe-coding itself now; only design the system so it is compatible and modular)

---

# 0) Output Requirements (Very Important)

Your response must be **three levels of planning**:

## Level 1: Executive Summary

* 1–2 paragraphs describing what will be built and why

## Level 2: Stage Roadmap (3 stages)

Create **exactly three stages**:

1. **Stage 1: MVP** (minimum viable product)
2. **Stage 2: Extend pipeline I/O and stage behaviors**
3. **Stage 3: UI control over adding features in real-time** (architecture-compatible, not fully implemented)

Each stage must contain **phases**, and each phase must contain **numbered steps**.

## Level 3: Implementation Plan

Provide concrete deliverables:

* repository layout
* key modules/classes/components
* database schema (MySQL DDL)
* API endpoint list
* UI routes/components list
* pipeline runtime model and contracts

After the plan, generate the initial scaffolding code for **Stage 1 MVP only**.

---

# 1) Core Product Concept

## 1.1 Generic Pipeline Model (DAG Required)

The pipeline is not only a simple list. It must support:

* **fan-out:** output of one stage can feed multiple downstream stages (parallel branches)
* **fan-in / reduce:** outputs of multiple upstream stages can be merged into one downstream stage input

Therefore, the pipeline definition must be a **Directed Acyclic Graph (DAG)** composed of nodes and edges.

Terminology:

* **Node** = a stage instance configured within a pipeline version
* **Edge** = a connection from upstream node output port → downstream node input port

Support these node types:

* **StageNode**: a normal processing stage
* **ReduceNode**: a specialized node that merges multiple upstream envelopes into one envelope using a declared reduce strategy

The system must validate:

* no cycles
* edge compatibility based on envelope contentType and declared stage capabilities
* required inputs for nodes

## 1.2 Standardization

All information moving between nodes is always in a standard envelope format (defined below).

---

# 2) Standard Envelope Contract (Canonical Payload)

Define a canonical **PipelineEnvelope** used for all stage input/output.

Minimum required fields:

* `envelopeId: string`
* `runId: string`
* `timestamp: string (ISO)`
* `contentType: string` (MIME-like)
* `data: unknown` (generic payload)
* `dataRef?: { kind: "inline" | "file" | "blob"; locator: string }` (optional pointer for large payloads)
* `metadata: Record<string, string | number | boolean>`
* `provenance: Array<{ stageNodeId: string; stageName: string; startedAt: string; endedAt: string; status: "ok"|"warn"|"error"; notes?: string }>`
* `errors: Array<{ code: string; message: string; stageNodeId?: string; severity: "warn"|"error"; details?: unknown }>`
* `metrics: Record<string, number>`

Rules:

* Nodes MUST NOT rely on concrete types beyond this contract.
* Nodes interpret `data` using `contentType` and `metadata`.
* Nodes append provenance entries and errors; do not overwrite history.
* Fan-out: the same envelope can be cloned and sent downstream with branch metadata.

---

# 3) Side-Effects as First-Class, Enforced Behavior (New Requirement)

Pipeline stage behaviors may have **side-effects** (writes to DB, files, caches, intermediate materialization, external API calls, etc.). The system must force these side-effects to be **declared, documented, and constrained**.

## 3.1 Side-Effect Declaration Contract

Every stage type must declare a **StageManifest** that includes:

* `typeKey: string`
* `displayName: string`
* `description: string`
* `inputs: { acceptedContentTypes: string[]; requiredMetadataKeys?: string[] }`
* `outputs: { emittedContentTypes: string[] }`

And critically:

* `sideEffects: Array<{
    effectType: "none"|"fileWrite"|"dbWrite"|"cacheWrite"|"networkCall"|"queuePublish"|"custom";
    description: string;
    riskLevel: "low"|"medium"|"high";
    idempotency: "idempotent"|"nonIdempotent"|"bestEffort";
    rollback: "notSupported"|"compensatingAction"|"transactional";
    constraints: string[];   // rules the runner must enforce
    outputsProduced?: Array<{ artifactType: string; locatorHint?: string }>;
  }>`

Stages with side-effects MUST include at least one entry in `sideEffects` with `effectType != "none"`.

## 3.2 Enforcement Requirements

The pipeline engine must enforce:

* Stages must register a manifest with explicit side-effect declarations (cannot be missing).
* Stage execution must emit `SideEffectEvent` records into run logs describing:

  * what effect occurred
  * where artifacts were written (paths, table names, etc.)
  * success/failure status
* If a stage is `nonIdempotent`, the runner must require an explicit config flag such as:

  * `"allowNonIdempotent": true`
    otherwise validation fails.
* Provide a mechanism for “dry-run” mode:

  * where side-effect stages are either blocked or run in simulation mode (MVP can block).
* Side-effect constraints must be checked during pipeline validation and again at runtime.
* Intermediate results must be persistable:

  * allow stages to write intermediate artifacts via a controlled **ArtifactStore** interface, not arbitrary file system writes.

## 3.3 Artifact Store Abstraction (For Intermediates)

Provide an **ArtifactStore** abstraction with:

* `Put(runId, nodeId, label, contentType, bytes|object) -> artifactRef`
* `Get(artifactRef) -> payload`
* `List(runId, nodeId) -> refs`

In MVP, implement ArtifactStore as a server-side folder + DB records (safe path, no traversal).

---

# 4) Reduce / Merge Semantics (New Requirement)

Because downstream nodes may depend on multiple upstream outputs, implement a standard reduce pattern.

## 4.1 ReduceNode Contract

A ReduceNode accepts N input envelopes and emits 1 output envelope.
The reduce strategy must be declared via config and validated.

Example strategies:

* `merge.metadata` (merge metadata maps, conflicts resolved by rule)
* `concat.text` (concatenate text payloads in order)
* `merge.json` (deep merge JSON objects)
* `collect.array` (collect multiple payloads into an array)
* `custom.keyedJoin` (join on metadata key)

ReduceNode must:

* define deterministic ordering rules (e.g., by edge priority, upstream node order)
* document its behavior in provenance
* emit warnings if incompatible contentTypes are merged

## 4.2 Runtime Scheduling

The runner must support:

* parallel execution of independent nodes (Stage 2 can implement concurrency)
* fan-out: send output to multiple ready downstream nodes
* fan-in: a ReduceNode becomes runnable only when all declared upstream inputs are available (or a configured quorum policy if supported later)

MVP can execute sequentially but must preserve the DAG structure and readiness rules.

---

# 5) Backend Architecture (.NET Web API)

Backend responsibilities:

* CRUD for pipelines and DAG definitions
* pipeline versioning
* run execution
* run history + node-level logs/status
* stage registry (StageManifest discovery and validation hooks)
* artifact store records (intermediates + outputs)

Execution Model:

* Stage 1 MVP: synchronous run engine with DAG validation and correct execution order
* Stage 2: async job execution + parallelism (planned)
* Stage 3: UI-driven feature management architecture (planned)

Stage plugin pattern (future-ready):

* `IPipelineNodeExecutor` for StageNode
* `IReduceExecutor` for ReduceNode strategies
* `StageRegistry` mapping typeKey → manifest + executor

---

# 6) Persistence Model (MySQL Required)

All pipeline definitions and DAG wiring stored in MySQL:

* Pipelines
* PipelineVersions
* PipelineNodes (nodeId, nodeType, stageTypeKey, config JSON)
* PipelineEdges (fromNodeId, toNodeId, fromPort, toPort, priority)
* StageTypes (registry metadata + manifest JSON)
* PipelineRuns
* PipelineRunNodeStatus
* PipelineRunEvents (logs, provenance, side-effect events)
* Artifacts (artifact refs and metadata)

Use JSON columns for configs/manifests.

---

# 7) Frontend Architecture (React + Vite + TypeScript)

Provide management UI:

* list/create/show pipelines
* DAG editor (MVP can be simplified: list nodes + edge table; later graphical)
* stage config editor (JSON text area w/ basic validation)
* run pipeline input editor
* view run history + node statuses
* inspect artifacts and side-effect events

---

# 8) Configuration & Secrets

Use `.env`:

* backend: DB settings, artifact folder root, environment mode
* frontend: API base URL

No committed secrets.

---

# 9) Stage Roadmap Requirements (MUST FOLLOW)

You must propose exactly three stages, each with phases → steps:

## Stage 1: MVP

Goal: CRUD pipelines as DAGs, register a few stage types, run DAG sequentially with correct readiness rules, side-effect declaration enforcement, artifact storage, run history.

## Stage 2: Extend pipeline I/O and stage behaviors

Goal: more contentTypes, more stages, real adapters, async/parallel runs, more reduce strategies, improved validation.

## Stage 3: UI control over adding features in real-time

Goal: architectural patterns enabling new stage behaviors to be added with minimal code changes (registry + manifests + feature flags + capability discovery). No dynamic compilation required. Focus on boundaries, discoverability, and safe enablement.

---

# 10) Deliverables Required in Your Response

After the planning levels, generate Stage 1 MVP scaffolding including:

### Backend (C# .NET)

* Web API project (.NET 8 default)
* EF Core MySQL integration (Pomelo default)
* entities + DbContext
* migrations-ready schema
* controllers:

  * pipelines, versions
  * nodes, edges
  * stage types/manifests registry
  * runs, run events
  * artifacts
* pipeline DAG runner service:

  * validates DAG
  * executes nodes in readiness order
  * logs provenance + side-effect events
* built-in stage types (at least 3) with manifests including side-effects:

  1. `LoadTextStage` (sideEffects: none)
  2. `ConvertStage` (sideEffects: none)
  3. `PersistArtifactStage` (sideEffects: fileWrite via ArtifactStore)
* reduce strategies (at least 2):

  * `collect.array`
  * `concat.text`

### Frontend (React + Vite + TS)

* routes:

  * `/pipelines`
  * `/pipelines/:id`
  * `/pipelines/:id/edit`
  * `/runs/:runId`
* components:

  * PipelineList
  * PipelineDetail
  * PipelineDagEditor (nodes + edges, MVP table-based)
  * NodeConfigEditor (JSON)
  * RunLauncher
  * RunViewer (node statuses + side-effect events + artifacts)

### Database (MySQL)

* provide DDL for MVP tables
* JSON columns for configs and manifests
* minimal indexes

### Documentation

* README:

  * local run instructions
  * `.env` setup
  * example DAG pipeline:

    * load text → fan-out to two transforms → reduce → persist artifact

### Testing

* minimal unit tests:

  * DAG validation
  * side-effect enforcement rules
  * one reduce strategy

---

# 11) Strict Engineering Constraints

* enforce declared side-effects for every stage type
* no hidden side-effects: all side-effect actions must emit events
* stage IO must use the envelope contract
* DAG semantics must be respected (fan-out + reduce/fan-in)
* modular design anticipating new stages

---

# 12) Clarifying Questions (Ask Only If Needed)

Before coding, ask only these if required:

1. Preferred .NET version (default: .NET 8)
2. Preferred MySQL provider (default: Pomelo.EntityFrameworkCore.MySql)
3. Frontend UI kit preference (default: minimal custom CSS)

If no response, proceed with defaults.

---

### Now proceed:

1. Produce the **3-level plan** exactly as specified.
2. Then generate **Stage 1 MVP code scaffolding** and file structure for both frontend and backend.
3. Include MySQL DDL.
4. Include `.env.example` files for frontend and backend.