# Purview Telemetry Source Generator

Incremental .NET source generator producing [`ActivitySource`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics) instrumentation from interface method definitions.

Use this file as first resort. Only search the repo or external docs when behaviour diverges from here.

---

## 1. Overview

Core outputs (per annotated interface method):

- Activity instrumentation (start/stop + tags)
- Structured logging (templated message + state)
- Metrics (counter/histogram etc.)

Generator targets: `netstandard2.0` (broad IDE/MSBuild compatibility). Integration tests: `net9.0`. Sample: .NET Aspire app.

---

## 2. Quick Start (≈90 seconds)

From repo root:

```
dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
dotnet test  ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
cd samples/SampleApp
dotnet build -c Release   # (~20s – do NOT cancel; incremental engine needs full pass)
dotnet test  -c Release
```

Inspect generated code (path pattern – do not rely on exact TFMs):

```
samples/SampleApp/SampleApp.Host/obj/**/generated*/**/*.g.cs
```

If nothing generated: confirm `EmitCompilerGeneratedFiles` is still enabled in project file(s).

Windows without `make`? Map:

```
make build  => dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
make test   => dotnet test  ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
make format => dotnet format ./src
```

---

## 3. Development Workflow

### 3.1 Pre-Change Checklist

- Rebuild + test generator solutions (ensure green baseline)
- Run sample app build + tests (baseline generation OK)
- Open a fresh shell (avoid stale env vars)

### 3.2 During Implementation

- Keep edits incremental; prefer new emitter/helper over editing many existing concerns simultaneously
- Add/update integration test **before** finalizing emitter shape (snapshot will drive iteration)
- When generation output looks stale: `dotnet clean` then rebuild (incremental cache occasionally sticks)

### 3.3 Pre-Commit

- `make build && make test`
- Sample app: build + test
- Review changed snapshot `.received.*` vs `.verified.*`; promote only intentional diffs
- `make format`

### 3.4 Pre-Push / PR Readiness

- No unexpected snapshot churn
- All new diagnostics covered by at least one test
- README/examples updated if feature-facing

---

## 4. Architecture Primer

High level flow:

1. Syntax discovery (interfaces + attributes)
2. Semantic model binding (method symbols classified into telemetry facets)
3. Validation (multi-target guardrails & diagnostics)
4. Emission (templated partials / helper types / DI registration)
5. Incremental caching (hash inputs → selective regeneration)

Generated assets (conceptual):

- Implementation class per interface
- Activity + logging + metrics instrumentation blocks
- Registration helpers / initialisers

Never hand-edit generated `.g.cs` – modify templates or emitters.

---

## 5. Multi-Target Guardrails

Terminology: A method is "multi-target" when it produces >1 telemetry modality (e.g., Activity + Metrics). Some combinations are disallowed to avoid ambiguous scoping or duplicated semantics.

| Combination                                                    | Status     | Notes                                          |
| -------------------------------------------------------------- | ---------- | ---------------------------------------------- |
| Activity + Logging (basic events)                              | Allowed    | Common case                                    |
| Activity + Metrics                                             | Allowed    | Ensure tags stable & low cardinality           |
| Logging + Metrics                                              | Allowed    | Prefer structured state object reuse           |
| Activity + Logging Scopes                                      | Disallowed | Scope nesting conflicts with activity lifetime |
| Activities + Events + Context (triple)                         | Disallowed | Overlapping context emission rules             |
| Non-multi-target + multi-target mixed generation (same method) | Disallowed | Pick one model                                 |

All supported combinations must retain feature parity with single-modality generation (naming, DI, diagnostics).

Add tests for each newly supported pair to prevent regression.

---

## 6. Testing & Snapshots

Primary test suite: `src/Purview.Telemetry.SourceGenerator.IntegrationTests` (Verify snapshots).

Key scenarios to exercise when altering emit logic:

- Interface → Implementation generation
- Activity attribute coverage (tags / status / exceptions)
- Logging templates (message placeholders, state objects, structured args ordering)
- Metrics (counters, histograms, naming conventions)
- Multi-target combinations & guardrails

Snapshot workflow:

1. Run tests → failing test produces `.received.*` beside existing `.verified.*`
2. Inspect each diff (never blanket-accept)
3. Promote intended change: rename `.received.*` → `.verified.*` (or use Verify tooling if integrated)
4. Re-run tests to confirm clean state

Never modify generated code directly to “fix” a snapshot – adjust generator logic.

Regenerate only when: you intentionally changed templates, emitter logic, attribute interpretation, or diagnostics wording.

---

## 7. Diagnostics & Troubleshooting

### 7.1 Common Failure Modes

- Empty generation: Attribute removed / item excluded / `EmitCompilerGeneratedFiles` disabled
- Stale output: Incremental driver cached; run `dotnet clean` or touch an interface file
- Snapshot drift: Forgot to update `.verified.*` after intentional template change
- Multi-target rejection: Disallowed combination (see section 5) – expect explicit diagnostic
- Performance anomaly: Large interface set with high attribute diversity; inspect binlog

### 7.2 Enabling Generator Traces

Create a build log for inspection:

```
dotnet build -c Release -bl:build.binlog ./samples/SampleApp/SampleApp.slnx
```

Open `build.binlog` in an msbuild log viewer to inspect generator timings.

### 7.3 Diagnostic IDs (Add as features grow)

| ID (placeholder) | Meaning                                       | Action                     |
| ---------------- | --------------------------------------------- | -------------------------- |
| PX0001           | Disallowed combination                        | Adjust attributes          |
| PX0002           | Duplicate method name after normalization     | Rename or adjust signature |
| PX0003           | Unsupported return type for telemetry pattern | Change method return type  |

Keep table updated when adding new diagnostics; each must be test-covered.

---

## 8. Extending the Generator

### 8.1 Adding a New Telemetry Attribute / Facet

1. Define attribute (naming: PascalCase, suffix with clear intent, e.g. `TelemetryCounterAttribute`)
2. Add corresponding record / model in `Records/`
3. Extend parser / classification logic (keep cohesive, avoid leaking semantic concerns into emitters)
4. Update emitters (new template or extend existing) – ensure idempotent output
5. Add integration test producing snapshot(s)
6. Document in README + this file if it changes guardrails
7. Add diagnostic(s) for invalid usage patterns

### 8.2 Naming / Style Conventions

- File-scoped namespaces; nullable enabled
- Consistent indentation & formatting (`dotnet format` gate)
- Deterministic member ordering improves diff clarity

---

## 9. Release & Versioning

Version source of truth: `package.json`.

Sync versions:

```
bun .build/update-version.ts
git diff   # ensure propagated
```

Release types:

- `make release-pre` (prerelease / pre tag) – for feature validation
- `make release-final` (stable) – after green CI, no pending snapshot changes

Pre-release checklist:

- All tests green (generator + sample app)
- No uncommitted changes
- Conventional commits present since last tag

---

## 10. PR Review Checklist

Reviewer verifies:

- Generator + integration tests build & pass
- Sample app builds & tests pass
- No unexplained snapshot churn
- New diagnostics documented + tested
- Multi-target combinations obey guardrails
- README and examples updated if feature-facing
- Version untouched unless intentionally part of release

---

## 11. FAQ

Q: No generated files appear – why?  
A: Ensure attributes present, interface public/internal as expected, `EmitCompilerGeneratedFiles` enabled, and run a clean build.

Q: Tests failing with many snapshot diffs.  
A: You likely changed template logic. Review each `.received.*`, promote only intentional changes, re-run.

Q: How do I disable a telemetry modality for a method?  
A: Remove or adjust the modality attribute; generator only emits requested facets.

Q: Cached behavior after reverting code?  
A: Run `dotnet clean`, delete `obj/` for impacted projects, rebuild.

Q: Add new combination support?  
A: Update guardrails (section 5), add tests (single + multi interface), update diagnostics table.

---

## 12. Reference Commands (Copy/Paste)

```
# Core
dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
dotnet test  ./src/Purview.Telemetry.SourceGenerator.slnx -c Release
dotnet format ./src

# Sample App
cd samples/SampleApp
dotnet build -c Release
dotnet test  -c Release

# Clean & Re-run
dotnet clean ./samples/SampleApp/SampleApp.slnx
dotnet build ./samples/SampleApp/SampleApp.slnx -c Release
```
