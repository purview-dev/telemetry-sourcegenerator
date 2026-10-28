# Modernising a Roslyn test suite

A playbook for moving an existing test suite onto `CodeQuery` plus the TUnit assertion extensions,
and adding a stage-by-stage incremental cache test.

## Before you start, collect

- The test file(s) being changed.
- The Roslyn component under test — generator, analyzer, code fix or refactor (infer it from the
  base class if the task does not say).
- The component's type name.

## Requirements

### 1. The right base class and method

| Component | Base class | Method |
| --- | --- | --- |
| Generator | `TUnitSourceGeneratorTestBase<TGenerator>` | `GenerateAsync` |
| Analyzer | `TUnitDiagnosticAnalyzerTestBase<TAnalyzer>` | `AnalyzeAsync` |
| Code fix | `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` | `ApplyCodeFixAsync` / `ApplyFixAllAsync` |
| Refactor | `TUnitRefactoringTestBase<TRefactoring>` | `RefactorAsync` |

### 2. `CodeQuery` instead of string matching

Replace `GetGeneratedTree(...)` + `string.Contains(...)` with `CodeQuery`
(`result.Generated().Get/Has/TryGet…`) and the terminal assertion extensions —
`await Assert.That(result).HasGeneratedMethod/Class/Property/Field/SyntaxTree(…)` — which return the
node so a follow-up assertion has something to work with.

### 3. `TypeReference` instead of signature strings

Match parameter and return types structurally. A signature string breaks on a whitespace or
nullability change that means nothing.

### 4. Options in one place

Derive a `SourceGeneratorTestOptions` record that seeds the required namespaces and additional
assemblies, and remove the per-test duplication it replaces.

### 5. A stage-by-stage cache test

Use `RunIncrementalAsync`, or `GenerateIncrementalAsync` on the TUnit base, and assert the four
scenarios. `ServiceRegistrationCacheTests` and `IncrementalPipelineCacheTests` are the reference
pattern.

| Scenario | Expected |
| --- | --- |
| First run | every framework stage `New` |
| Identical rerun (`RunIncrementalAsync(sources, …)` runs the same source twice) | framework stages `Cached`/`Unchanged` |
| Source-only change | `ForAttribute_*` `Modified`; property/config stages stay `Cached` |
| Property-only change (`new IncrementalRunInput(sources, [("build_property.X", "value")])`) | `GetMSBuildPropertyValue_*`, `GetGenerationConfiguration`, `GetGenerationContext_*` `Modified`; `ForAttribute_*` stays `Cached` |

Use the `StepReasons(IncrementalCacheRun)` flattening helper. If the generator depends on its own
post-init output, assert on the framework-named stages rather than on every tracked step.

### 6. Minimal change

Keep behaviour equivalent, and do not reformat unrelated tests.

## Verification

Build the test project and run its suite before finishing.
