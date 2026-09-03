---
agent: ask
description: "Modernise a Roslyn test suite to use CodeQuery + TUnit assertion extensions, and add a stage-by-stage incremental cache test."
---

You are modernising tests in this repository. Apply the guidance from the `source-generator-testing` and
`tunit-test-authoring` skills for picking the right base class, querying generated code with `CodeQuery`,
and asserting incremental caching.

## Inputs

- Target test file(s): `${input:targetFiles:Path(s) to test file(s)}`
- Roslyn component under test: `${input:componentType:generator|analyzer|codefix|refactor}` (inferred if blank)
- Generator/analyzer/code-fix/refactor type name: `${input:componentName:Component type name}`

## Task

Modernise each test so it uses the framework's `CodeQuery` syntax-lookup API and the TUnit assertion
extensions, and add a stage-by-stage cache test proving each incremental pipeline layer caches correctly.

### Requirements

1. Choose the correct base class and method for the component type:
   - Generator → `TUnitSourceGeneratorTestBase<TGenerator>` → `GenerateAsync`.
   - Analyzer → `TUnitDiagnosticAnalyzerTestBase<TAnalyzer>` → `AnalyzeAsync`.
   - Code fix → `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` → `ApplyCodeFixAsync` / `ApplyFixAllAsync`.
   - Refactor → `TUnitRefactoringTestBase<TRefactoring>` → `RefactorAsync`.
2. Replace `GetGeneratedTree(...)` + `string.Contains(...)` assertions with `CodeQuery`
   (`result.Generated().Get/Has/TryGet…`) and the terminal assertion extensions
   (`await Assert.That(result).HasGeneratedMethod/Class/Property/Field/SyntaxTree(…)`) that return the node.
3. Replace signature string checks with `TypeReference` parameter/return-type matching.
4. Ensure options come from a derived `SourceGeneratorTestOptions` record seeding the required namespaces
   and additional assemblies; remove per-test duplication.
5. Add an incremental cache test using `RunIncrementalAsync` (or `GenerateIncrementalAsync` on the TUnit
   base) with the four scenarios from the skills' "Incremental cache testing" sections
   (`ServiceRegistrationCacheTests` / `IncrementalPipelineCacheTests` are the reference pattern):
   - first run → every framework stage `New`;
   - identical rerun (`RunIncrementalAsync(sources, …)` runs the same source twice) → framework stages
     `Cached`/`Unchanged`;
   - source-only change → `ForAttribute_*` `Modified`, property/config stages stay `Cached`;
   - property-only change (`new IncrementalRunInput(sources, [("build_property.X", "value")])`) →
     `GetMSBuildPropertyValue_*`/`GetGenerationConfiguration`/`GetGenerationContext_*` `Modified`,
     `ForAttribute_*` stays `Cached`.
   Use the `StepReasons(IncrementalCacheRun)` flattening helper; if the generator depends on its own
   post-init output, assert on the framework-named stages rather than every tracked step.
6. Keep changes minimal and behavior equivalent; do not reformat unrelated tests.

Verify by building the test project and running its suite before finishing.