---
name: tunit-test-authoring
description: "Use when writing TUnit tests for source generators, diagnostic analyzers, code fixes, or refactorings in a Purview.SourceGeneratorFramework repository — choosing the correct base class and method, customising options, using the assertion extensions, and modernising existing tests."
---

# TUnit test authoring for Roslyn components

Use this skill whenever a task involves authoring, fixing, or modernising **TUnit** tests for Roslyn
components built with `Purview.SourceGeneratorFramework`. It tells you which base class to derive from,
which method to call, how to customise options with an easy starting point, and how to use the TUnit
assertion extensions. For the framework-agnostic runner layer and the `CodeQuery` API, also load the
`sdk` package's `source-generator-testing` skill.

## Base class → method matrix

| Roslyn type | Base class | Method to call |
|---|---|---|
| `IIncrementalGenerator` / `ISourceGenerator` | `TUnitSourceGeneratorTestBase<TGenerator>` | `GenerateAsync(source, options, ct)` |
| `DiagnosticAnalyzer` | `TUnitDiagnosticAnalyzerTestBase<TAnalyzer>` | `AnalyzeAsync(source, options, ct)` |
| `CodeFixProvider` (single fix) | `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` | `ApplyCodeFixAsync(source, options, ct)` |
| `CodeFixProvider` (fix-all) | `TUnitCodeFixTestBase<TAnalyzer, TCodeFix>` | `ApplyFixAllAsync(sources, options, ct)` |
| `CodeRefactoringProvider` | `TUnitRefactoringTestBase<TRefactoring>` | `RefactorAsync(source, options, ct)` |

For cache tests, `TUnitSourceGeneratorTestBase` also exposes `GenerateIncrementalAsync(...)`.

Framework-agnostic equivalents (no TUnit): `SourceGeneratorTestRunner`, `DiagnosticAnalyzerTestRunner`,
`CodeFixTestRunner`, `RefactoringTestRunner`.

## Easy starting point: derive your options record

Create a test-options record that seeds the namespaces and assemblies your component needs, then pass it
to every test via `new MyTestOptions()`:

```csharp
public sealed record MyGeneratorTestOptions : SourceGeneratorTestOptions
{
    public MyGeneratorTestOptions()
    {
        AdditionalNamespaces = AdditionalNamespaces.Add("My.Namespace");
        AdditionalAssemblyTypes = AdditionalAssemblyTypes.AddRange(
            typeof(SomeDependencyType),
            typeof(TypeIdentity)                       // framework Shared assembly, when needed
        );
        DisableSourceGeneratorPropertyName = "DisableMyGenerator";
    }
}

public class MyGeneratorTests : TUnitSourceGeneratorTestBase<MyGenerator, MyGeneratorTestOptions>
{
    [Test]
    public async Task GeneratesExpectedSource(CancellationToken ct) =>
        await GenerateAsync("...source...", ct);
}
```

Use the base hooks to customise per-run: `OnBeforeRun`/`OnBeforeRunAsync` (mutate sources/options, e.g.
`options.WithAdditionalSources(markerAttributeSource)`) and `OnAfterRun`/`OnAfterRunAsync`. Use
`options.Compile()` to opt into `CompileToAssembly` while preserving the derived options type.

For code fixes/refactorings, select a specific registered action via `CodeFixTestOptions.EquivalenceKey`
or `CodeActionIndex`, and `RefactorTestOptions.Span`/`NodeSelector` (e.g.
`NodeSelector = query => query.GetMethod("M")`).

## TUnit assertion extensions

All assertion extensions live under `Purview.SourceGeneratorFramework.Testing.TUnit.Assertions`
(globally imported by the package's props). `Assert.That(...)` calls are terminal and **return the value**
when awaited.

- **`CodeQueryAssertions`** — return `CodeQueryResult<T>` (the matched node via `.Node`, plus a query
  scoped to it via `.Query`): `HasGeneratedMethod` (optionally with `TypeReference[]` parameter types),
  `HasGeneratedMethodReturnType`, `HasGeneratedClass` (by name or `TypeReference`/`TypeIdentity` identity),
  `HasGeneratedProperty`, `HasGeneratedField`, `HasGeneratedSyntaxTree`; `HasFixedMethod` for code-fix and
  refactor results. The assertions operate on a `CodeQuery` directly, so pass any query — `result.Generated()`
  (generated trees), `result.Output()` (whole compilation), or `result.FixedCode()` — or use the convenience
  overloads on the test result types, which query the relevant code for you.
- **Scoped member chaining** — `HasPropertyOfType(name, type)`, `HasFieldOfType(name, type)`,
  `HasMethodOfType(name, TypeReference[])`, `HasConstructorOfType(TypeReference[])`, and
  `HasAttributeOfType(name)` chain from a scoped `CodeQueryResult<T>` (e.g. the result of `HasGeneratedClass`)
  and return the matched member. They are named to avoid colliding with the bool `Has*` predicates in
  `MemberQueryExtensions`.
- **Fluent `.And` chains** — the node-producing assertions (`HasGeneratedClass`, `HasPropertyOfType`,
  `HasMethodOfType`, `HasNestedType`) move the chain onto the matched node, so further assertions can be
  appended with `.And`. Node-inspection assertions — `WithAccessibility`, `WithGetterAccessibility`,
  `WithSetterAccessibility`, `WithBaseType`, `WithGenericTypeParameter(s)`, `IsInNamespace`,
  `IsInGlobalNamespace` — keep the node on the chain:
  ```csharp
  var method = await Assert.That(query)
      .HasGeneratedClass("Service")
      .And.HasNestedType("Builder")
      .And.WithAccessibility(Accessibility.Private)
      .And.HasMethodOfType("Build", []);
  ```
  Generic types are matched by arity — `HasGeneratedClass(name, arity)` or a `TypeIdentity` with arity —
  so `new TypeIdentity("ResourceDefinition", ns, arity: 1)` finds `ResourceDefinition<TResourceType>`
  without matching the non-generic `ResourceDefinition`.
- **Nullable expected types in tests** — use the test-only `query.MakeNullable(type)` extension (on a
  `CodeQuery`). It resolves the annotation against the query's compilation and, unlike
  `TypeReference.Nullable()`/`TypeIdentity.MakeNullable()`, does not trip the `PSGFR16` context-overload
  suggestion, since tests have no generation context to pass.
  ```csharp
  CodeQueryResult<MethodDeclarationSyntax> method = await Assert.That(result.Generated()).HasGeneratedMethod("DoWork", [intType, nullableInt]);
  CodeQueryResult<ClassDeclarationSyntax> cls = await Assert.That(result.Generated()).HasGeneratedClass("Service");
  await Assert.That(cls.HasProperty("Count", intType)).IsTrue();           // chained member query
  await Assert.That(cls.Node.Identifier.ValueText).IsEqualTo("Service");   // direct syntax access
  await Assert.That(result.FixedCode()).HasFixedMethod("DoWork");          // code-fix / refactor results

  var query = result.Generated();
  var attributeClass = await Assert.That(query).HasGeneratedClass(hostKitAttribute);
  await Assert.That(attributeClass).HasPropertyOfType("Name", query.MakeNullable(TypeLibrary.System.String));
  ```
- **`DiagnosticAssertions`** — `HasDiagnostic(descriptor|id)`, `HasDiagnostics(count)`,
  `DoesNotHaveDiagnostic`, `HasNoDiagnostics`, `HasNoErrorDiagnostics` on generator/analyzer/code-fix results.
- **`TypeIdentityAssertions`** — `HasSymbol(TypeIdentity)` / `HasSymbol("Namespace.Type")`.
- **`GeneratedCodeAssertionsExtensions`** — `GeneratesCode(expected)`, `ContainsGeneratedCode(expected)`
  (whitespace-flattened string comparison).

For structural assertions (members, signatures, namespaces) prefer `result.Generated()` +
`Get/Has/TryGet` from `CodeQuery` (see `source-generator-testing`).

## Incremental cache tests (`GenerateIncrementalAsync`)

`TUnitSourceGeneratorTestBase` exposes `GenerateIncrementalAsync`, which mirrors `RunIncrementalAsync` but
also wires the base class hooks (`OnBeforeRun`/`OnBeforeRunAsync`) and your derived options record. Use it to
prove the pipeline caches stage-by-stage. The reference is `ServiceRegistrationCacheTests` in
`SourceGeneratorFramework.ExampleGenerator.UnitTests`; the framework-agnostic twin with a full walkthrough is
in the `source-generator-testing` skill.

Why the tests look the way they do:

- `GenerateIncrementalAsync([Source])` runs the **same source twice** on a single shared driver. The first
  run reports every stage `New`; the second must report `Cached`/`Unchanged` for unchanged stages — that is
  the core "it caches" proof.
- `GenerateIncrementalAsync([new IncrementalRunInput([Source]), new IncrementalRunInput([changed])])` runs two
  **different** source sets, so a source-only change must mark `ForAttribute_*` `Modified` while
  property/configuration stages stay `Cached`.
- `new IncrementalRunInput([Source], [("build_property.X", "value")])` toggles an MSBuild property for one
  run only, so a property-only change must mark `GetMSBuildPropertyValue_*`/`GetGenerationConfiguration`/
  `GetGenerationContext_*` `Modified` while `ForAttribute_*` stays `Cached`.
- The `StepReasons(IncrementalCacheRun)` helper flattens each tracked step's `Outputs` into a
  `ImmutableDictionary<string, ImmutableArray<IncrementalStepRunReason>>` so assertions can address a stage
  by name (see `source-generator-testing` for the helper body).
- If the generator's pipeline depends on its own post-initialization output (a self-referencing generated
  attribute), Roslyn's internal `ForAttributeWithMetadataName` `Compilation` step is legitimately `Modified`
  on rerun; assert on the framework-named stages (e.g. `ForAttribute_GenerateServiceAttribute`,
  `GetGenerationConfiguration`, `GetGenerationContext_EmptyCapabilities`) rather than every tracked step.

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

    [Test]
    public async Task PropertyChange_MarksPropertyStageModified_AttributeStageStaysCached(CancellationToken cancellationToken)
    {
        var result = await GenerateIncrementalAsync(
            [
                new IncrementalRunInput([Source]),
                new IncrementalRunInput([Source], [(PropertyLibrary.EmitServiceRegistrationInfo, "true")]),
            ],
            cancellationToken: cancellationToken);

        var second = StepReasons(result.Runs[1]);
        await Assert.That(second["GetMSBuildPropertyValue_EmitServiceRegistrationInfo"]).Contains(StepReason.Modified);
        await Assert.That(second["ForAttribute_GenerateServiceAttribute"].All(r => r is StepReason.Cached or StepReason.Unchanged)).IsTrue();
    }
}
```

Use `using StepReason = Microsoft.CodeAnalysis.IncrementalStepRunReason;` and your own stage names.

## Modernising existing tests

When converting legacy tests that do `result.GetGeneratedTree(...)` + `string.Contains(...)`:

1. Replace tree-lookup + string matching with `result.Generated().GetClass/GetMethod/GetProperty(...)` and
   the `Has*`/`TryGet*` family.
2. Replace signature string checks with `TypeReference` parameter/return-type matching.
3. Replace `Assert.That(text).Contains("...")` with the terminal assertion extensions that return nodes.
4. Verify options use a derived record (namespaces + assemblies) rather than repeating `AdditionalNamespaces`
   per test.
5. Add a stage-by-stage cache test if the component has an incremental pipeline — first run `New`,
   identical rerun `Cached`/`Unchanged`, and targeted changes mark only the affected stage `Modified`.
   Use the inlined examples in this skill and in `source-generator-testing`'s "Incremental cache testing"
   section, swapping in your own generator and stage names.

## License

This project is licensed under the MIT license.