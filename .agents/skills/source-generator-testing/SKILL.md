---
name: source-generator-testing
description: "Use when writing or fixing tests for source generators, diagnostic analyzers, code fixes, or refactorings in a Purview.SourceGeneratorFramework repository — picking the right runner/base, configuring options, querying produced code with CodeQuery, and asserting incremental caching."
---

# Testing source generators, analyzers, code fixes and refactorings

Use this skill whenever a task involves authoring, fixing, or modernising tests for Roslyn components
(generators, diagnostic analyzers, code fix providers, refactoring providers) built with
`Purview.SourceGeneratorFramework`. It covers the framework-agnostic test runner layer and the
`CodeQuery` syntax-lookup API. For the TUnit base classes and assertion extensions, also load the
`sdk` package's `tunit-test-authoring` skill.

## Picking the right runner

| Roslyn type | Runner |
|---|---|
| `IIncrementalGenerator` / `ISourceGenerator` | `SourceGeneratorTestRunner<TGenerator>` |
| `DiagnosticAnalyzer` | `DiagnosticAnalyzerTestRunner<TAnalyzer>` |
| `CodeFixProvider` | `CodeFixTestRunner<TAnalyzer, TCodeFix>` (single) |
| — | `CodeFixTestRunner<TAnalyzer, TCodeFix>.RunFixAllAsync` (project-wide) |
| `CodeRefactoringProvider` | `RefactoringTestRunner<TRefactoring>` |

TUnit projects should prefer the matching base class instead (see `tunit-test-authoring`):
`TUnitSourceGeneratorTestBase`, `TUnitDiagnosticAnalyzerTestBase`, `TUnitCodeFixTestBase`,
`TUnitRefactoringTestBase`.

## Result types

- `DriverRunResult` (generator) — `DriverResult`, `AllSyntaxTrees`/`PrimarySyntaxTrees`,
  `CompilationResult.Compilation`, `GetGeneratedTree`, `GetSource`, `GetTypeByMetadataName`, `LogEntries`.
- `AnalyzerTestResult` — `Diagnostics`, `Compilation`.
- `CodeFixTestResult` — `Diagnostics`, `CodeActions`, `FixedSource`, `Compilation`, `ChangedSolution`.
- `CodeFixFixAllResult` — `Diagnostics`, `CodeActions`, `FixedSources`, `ChangedSolution`.
- `RefactorTestResult` — `CodeActions`, `FixedSources`, `ChangedSolution`, `Compilation`.

Use `DriverRunResultExtensions` (`AssertNoCompilationErrors`, `AssertNoGenerationExceptions`,
`AssertSingleGeneratedSource`, `AssertGeneratedSourceContains`, …) for quick checks, but prefer
`CodeQuery` for structural assertions.

## Querying produced code with `CodeQuery`

Every result exposes a `CodeQuery` via extensions in `CodeQueryResultExtensions`:

```csharp
result.Generated()          // DriverRunResult: generated trees (default, generated-first)
result.Output()             // DriverRunResult: entire output compilation (user + generated)
analyzerResult.Code()       // AnalyzerTestResult: input compilation
codeFixResult.Code()        // CodeFixTestResult: input compilation
codeFixResult.FixedCode()   // CodeFixTestResult: parsed fixed source (or post-fix solution)
fixAllResult.FixedCode()    // CodeFixFixAllResult: post-fix documents
refactorResult.FixedCode()  // RefactorTestResult: post-refactor documents
```

Every `Get` has an accompanying `Has` (bool) and `TryGet` (out): `GetMethod`/`HasMethod`/`TryGetMethod`,
`GetClass`, `GetStruct`, `GetInterface`, `GetEnum`, `GetDelegate`, `GetRecord`, `GetProperty`,
`GetField`, `GetConstructor`, `GetNamespace`, `GetTypeDeclaration`, plus generic `Get<T>`/`Has<T>`
and `GetSyntaxTree`/`HasSyntaxTree`. `Get` throws `SyntaxNotFoundException` when nothing matches.

Type lookups accept an optional generic arity — `GetClass(name, arity)` / `HasClass(name, arity)` — and the
`TypeReference`/`TypeIdentity` overloads match arity automatically from the identity, so
`new TypeIdentity("ResourceDefinition", ns, arity: 1)` finds `ResourceDefinition<T>` without matching the
non-generic `ResourceDefinition`.

Every `Get` returns a `CodeQueryResult<T>` — the matched syntax node (`Node`) plus a query scoped to it
(`Query`), with implicit conversions to both the node and the scoped query. Use `.Node` for direct syntax
access, or chain member queries (the originating query is carried by the result, so it is not passed again).

Types can be matched against `TypeReference`/`TypeIdentity`, resolved through the compilation's semantic
model (nullable value types are significant, so `int?` never matches `int`):

```csharp
result.Generated().HasMethod("DoWork", TypeReference.Create<int>(), TypeReference.Create<int>().Nullable(), complexType);
result.Generated().HasReturnType("Compute", TypeReference.Create<int>());
result.Generated().GetMethod("Format").HasParameters(TypeReference.Create<string>(), objectReference);
```

When a test expects a nullable annotation, prefer the test-only `query.MakeNullable(type)` extension (on a
`CodeQuery`, accepting a `TypeReference` or `TypeIdentity`). It resolves the annotation against the query's
compilation and, unlike `TypeReference.Nullable()`/`TypeIdentity.MakeNullable()`, does not trigger the
`PSGFR16` context-overload suggestion (tests have no generation context to pass).

Member chaining from a type declaration (`MemberQueryExtensions`):

```csharp
var service = result.Generated().GetClass("ServiceCollectionExtensions"); // or GetClass(name, "Namespace")
service.HasProperty("Count", intType);
service.HasIndexer(stringType, intType);
service.HasMethod("Add", intType, complexType);
service.HasMethodReturnType("Add", stringType);
service.HasConstructor(stringType);
service.HasAttribute("SomeAttribute");
```

Node-inspection checks on scoped results:

```csharp
result.Generated().GetClass("Service").HasAccessibility(Accessibility.Public);      // resolves C# defaults
result.Generated().GetClass("Service").GetProperty("Name").HasSetterAccessibility(Accessibility.Private);
result.Generated().GetClass("ResourceDefinition", 1).HasGenericTypeParameters("TResourceType");
result.Generated().GetClass("ResourceDefinition", 1).HasBaseType(new TypeIdentity("ResourceDefinition", ns));
result.Generated().GetClass("Service").HasNestedType("Builder");                    // GetNestedType(...) to fetch
result.Generated().GetClass("Service").IsInNamespace("Example.Models");             // IsInGlobalNamespace() for no namespace
result.Generated().IsInNamespace(cls.Node, "Example.Models");                       // or on the query directly
```

## Configuring options and a reusable starting point

`SourceGeneratorTestOptions` is the base record. Common knobs:

- `AdditionalNamespaces` / `IncludeDefaultNamespaces` — namespaces prepended to test source.
- `AdditionalAssemblyTypes` / `AdditionalReferences` — assemblies referenced by the test compilation
  (use `AdditionalAssemblyTypes = [typeof(SomeType)]` to pull in a whole assembly).
- `AdditionalSources` — extra source files added to every run.
- `AnalyzerConfigOptions` — `build_property.*` values; keys without the prefix are also exposed as MSBuild
  properties.
- `DisableSourceGeneratorPropertyName` / `DisableSourceGeneratorValue` — generator disable toggle.
- `NullableContextOptions`, `OutputKind`, `LanguageVersion`, `CompileToAssembly`.
- `ValidateCodeWriterScopes`, `EnableLogging`.
- `ExcludeGeneratedSourceHintNames` — hides generated marker trees from `PrimarySyntaxTrees`.

**Easy starting point recipe.** Derive an options record that seeds the namespaces and assemblies your
generator needs, so every test gets a working compilation with no boilerplate:

```csharp
public sealed record MyGeneratorTestOptions : SourceGeneratorTestOptions
{
    public MyGeneratorTestOptions()
    {
        AdditionalNamespaces = AdditionalNamespaces.Add("My.Namespace");
        AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
            typeof(SomeDependencyType),
            typeof(TypeIdentity)   // the framework's Shared assembly, when needed
        );
        DisableSourceGeneratorPropertyName = PropertyLibrary.DisableMyGenerator;
    }
}
```

`Compile()` returns a copy with `CompileToAssembly = true`, preserving the derived options type. Use the
base class hooks `OnBeforeRun`/`OnBeforeRunAsync`/`OnAfterRun` to mutate sources/options per run (for
example to append a marker attribute source via `WithAdditionalSources`).

When `CompileToAssembly` is enabled, emission is fully in-memory. On .NET 8+ the assembly loads into a
collectible `AssemblyLoadContext`, so the result is `IDisposable` — `using var result = ...` unloads it and
keeps repeated runs from polluting the default context. `result.CompilationResult.Assembly` is the runnable
assembly (generated code can execute); `result.CompilationResult.Metadata` / `.MetadataAssembly` give a
metadata-only reflection view (types, members, attributes) over the emitted assembly without executing code.

## Best practices

- **Deterministic output**: `WriteAutoGeneratedHeader` is timestamp-free; assert with
  `ContainsGeneratedCode`/`GeneratesCode` (whitespace-flattened) or `CodeQuery`, never with timestamps.
- **Generator references**: to use a generated type in the test project AND pass the generator type to a
  runner, reference the generator project twice — once `OutputItemType="Analyzer"` and once as a normal
  reference.
- **Multi-target**: build generators against the Roslyn version that supports the test matrix; the
  framework is built against Roslyn 5.0 (net8.0/net9.0 assets keep a .NET 8–10 matrix loading); keep
  `System.Collections.Immutable` version pinned to the shared one.
- **Scope validation**: keep `PurviewSourceGeneratorFrameworkValidateCodeWriterScopes` enabled; it makes
  undisposed `CodeWriter` scopes fail tests.
- **Prefer `CodeQuery` over string matching** for structural assertions (members, signatures, namespaces).

## Incremental cache testing (`RunIncrementalAsync`)

To prove the pipeline caches correctly stage-by-stage, use `SourceGeneratorTestRunner.RunIncrementalAsync`
(or `GenerateIncrementalAsync` on the TUnit base). It runs a sequence of source sets over a **single shared
`GeneratorDriver`** and captures each run's `TrackedSteps`, keyed by tracking name. The canonical reference
implementation is `IncrementalPipelineCacheTests` in `SourceGeneratorShared.UnitTests`; the end-to-end
generator variant is `ServiceRegistrationCacheTests` in
`SourceGeneratorFramework.ExampleGenerator.UnitTests`. Copy the pattern into your own test project — do
not expect the source repo's files locally.

### What is being asserted and why

Roslyn reports one `IncrementalStepRunReason` per step output on each run:

- `New` — the step ran for the first time.
- `Modified` — the step ran and produced a different value than the previous run.
- `Unchanged` — the step ran but produced the same value.
- `Cached` — the step was skipped and its previous result reused from the incremental cache.

A pipeline is "caching correctly" when an unchanged input keeps every stage `Cached`/`Unchanged`, and a
targeted change marks **only** the stages whose inputs actually changed `Modified` while unrelated stages
stay `Cached`. If a generator accidentally leaks `Compilation`, `SemanticModel`, `ISymbol`,
`SyntaxNode`, or `Location` into a pipeline model, unrelated stages will report `Modified`/`New` on rerun —
these tests fail the build and catch the regression.

### The four scenarios every cache test should cover

1. **First run → all `New`.** Nothing can be cached on the first run; this confirms every stage is tracked
   under the expected name.
2. **Identical rerun → all `Cached`/`Unchanged`.** `RunIncrementalAsync(sources, ...)` runs the same source
   set twice for exactly this case. This is the strongest "it caches" proof.
3. **Source-only change → only the source/attribute stage `Modified`.** Changing an attributed class must
   mark `ForAttribute_*` (and downstream output) `Modified` while property/config stages stay `Cached`.
4. **Property-only change → only the property/configuration stage `Modified`.** Toggling an MSBuild
   property (via `IncrementalRunInput.AnalyzerConfig`) must mark `GetMSBuildPropertyValue_*` /
   `GetGenerationConfiguration` / `GetGenerationContext_*` `Modified` while `ForAttribute_*` stays `Cached`.

### How the runner makes this possible

`RunIncrementalAsync` creates **one** driver, enables incremental step tracking, and **reuses the same
`Compilation` instance for identical source sets** (keyed by prepared source text). Without that reuse,
Roslyn would see a fresh compilation on the second run and report stages `Modified`/`New` even though the
sources are byte-identical — the "cached" assertion would fail.

### The `StepReasons` helper

`IncrementalCacheRun.Steps` is `ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>`
keyed by tracking name. Flatten each step's `Outputs` into the reasons list so assertions read cleanly:

```csharp
static ImmutableDictionary<string, ImmutableArray<IncrementalStepRunReason>> StepReasons(IncrementalCacheRun run)
{
    var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<IncrementalStepRunReason>>();
    foreach (var pair in run.Steps)
        builder[pair.Key] = [.. pair.Value.SelectMany(step => step.Outputs.Select(static output => output.Reason))];
    return builder.ToImmutable();
}
```

### Framework pipeline stage names

- `GetMSBuildPropertyValue_{Property}` — `IncrementalPipeline.PropertyValueProvider`.
- `GetGenerationConfiguration` — `IncrementalPipeline.GenerationContextValueProvider`.
- `GetGenerationContext_{Capabilities}` — e.g. `GetGenerationContext_EmptyCapabilities`.
- `ForAttribute_{AttributeType}` — `IncrementalPipeline.ForAttributeWithMetadataName`.

The framework reference (`IncrementalPipelineCacheTests`, framework-agnostic runner) shows all four
scenarios against `TestGenerator`/`DiagnosticTestGenerator`:

```csharp
using System.Collections.Immutable;
using Purview.SourceGeneratorFramework.TestGenerators;
using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;

public class IncrementalPipelineCacheTests
{
    const string AttributedSource = """
        [TestAttribute]
        public partial class MyClass { }
        """;
    const string ChangedAttributedSource = """
        [TestAttribute]
        public partial class AnotherClass { }
        """;
    const string TestAttributeSource = """
        [System.AttributeUsage(System.AttributeTargets.Class)]
        public sealed class TestAttribute : System.Attribute { }
        """;

    static SourceGeneratorTestOptions CreateOptions() =>
        new SourceGeneratorTestOptions()
            .WithAdditionalSources(TestAttributeSource)
            .WithExcludeGeneratedSourceHintNames("TestAttribute");

    static ImmutableDictionary<string, ImmutableArray<StepReason>> StepReasons(IncrementalCacheRun run) { /* as above */ }

    [Test]
    public async Task FirstRun_AllStagesAreNew(CancellationToken cancellationToken)
    {
        var result = await new SourceGeneratorTestRunner<TestGenerator>().RunIncrementalAsync(
            [new IncrementalRunInput([AttributedSource])],
            CreateOptions(),
            cancellationToken);

        var reasons = StepReasons(result.Runs[0]);
        await Assert.That(reasons).IsNotEmpty();
        await Assert.That(reasons.Values.SelectMany(r => r).All(r => r == StepReason.New)).IsTrue();
    }

    [Test]
    public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
    {
        var result = await new SourceGeneratorTestRunner<TestGenerator>()
            .RunIncrementalAsync([AttributedSource], CreateOptions(), cancellationToken);

        var second = StepReasons(result.Runs[1]);
        await Assert.That(second.Values.SelectMany(r => r).All(r => r is StepReason.Cached or StepReason.Unchanged)).IsTrue();
    }

    [Test]
    public async Task SourceChange_MarksAttributeStageModified_PropertyStagesStayCached(CancellationToken cancellationToken)
    {
        var result = await new SourceGeneratorTestRunner<TestGenerator>().RunIncrementalAsync(
            [new IncrementalRunInput([AttributedSource]), new IncrementalRunInput([ChangedAttributedSource])],
            CreateOptions(),
            cancellationToken);

        var second = StepReasons(result.Runs[1]);
        await Assert.That(second["ForAttribute_TestAttribute"]).Contains(StepReason.Modified);
        await Assert.That(second["GetMSBuildPropertyValue_DisableTestGenerator"].All(r => r == StepReason.Cached)).IsTrue();
    }

    [Test]
    public async Task PropertyChange_MarksPropertyStageModified_AttributeStageStaysCached(CancellationToken cancellationToken)
    {
        var result = await new SourceGeneratorTestRunner<TestGenerator>().RunIncrementalAsync(
            [
                new IncrementalRunInput([AttributedSource]),
                new IncrementalRunInput([AttributedSource], [("build_property.DisableTestGenerator", "true")]),
            ],
            CreateOptions(),
            cancellationToken);

        var second = StepReasons(result.Runs[1]);
        await Assert.That(second["GetMSBuildPropertyValue_DisableTestGenerator"]).Contains(StepReason.Modified);
        await Assert.That(second["ForAttribute_TestAttribute"].All(r => r == StepReason.Cached)).IsTrue();
    }
}
```

### End-to-end generator variant (`GenerateIncrementalAsync`)

TUnit tests derive from the base class and call `GenerateIncrementalAsync` (which wires the
`OnBeforeRun`/`OnBeforeRunAsync` hooks and the derived options). `ServiceRegistrationCacheTests` in
`SourceGeneratorFramework.ExampleGenerator.UnitTests` is the reference:

```csharp
public class ServiceRegistrationCacheTests
    : TUnitSourceGeneratorTestBase<ServiceRegistrationGenerator, ServiceRegistrationTestOptions>
{
    const string Source = """
        namespace Test;

        [GenerateService]
        public class MyService { }
        """;

    [Test]
    public async Task IdenticalRerun_AllStagesCached(CancellationToken cancellationToken)
    {
        var result = await GenerateIncrementalAsync([Source], cancellationToken: cancellationToken);

        var second = StepReasons(result.Runs[1]);
        string[] frameworkStages =
        [
            "GetMSBuildPropertyValue_EmitServiceRegistrationInfo",
            "GetGenerationConfiguration",
            "GetGenerationContext_EmptyCapabilities",
            "ForAttribute_GenerateServiceAttribute",
        ];
        await Assert.That(
            frameworkStages.All(stage =>
                second.TryGetValue(stage, out var reasons)
                && reasons.All(r => r is StepReason.Cached or StepReason.Unchanged))).IsTrue();
    }
}
```

**Why the example filters to `frameworkStages`:** `ServiceRegistrationGenerator` emits its own
`GenerateServiceAttribute` via post-initialization output, which is regenerated as a new `SyntaxTree` each
run. That makes Roslyn's *internal* `ForAttributeWithMetadataName` `Compilation` step legitimately report
`Modified` on an identical rerun. Asserting on the framework-named stages (which stay `Cached`/`Unchanged`)
is the meaningful check. If your generator does not depend on its own post-init output, the stricter
"every tracked step is `Cached`/`Unchanged`" assertion (as in the framework `IncrementalPipelineCacheTests`)
is correct.

Per-run MSBuild-property changes are supplied with `new IncrementalRunInput(sources, [("build_property.X", "value")])`.

## License

This project is licensed under the MIT license.