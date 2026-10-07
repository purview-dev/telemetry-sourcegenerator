---
name: source-generator-codewriter-modernization
description: "Use when implementing, reviewing, or refactoring C# source generators and analysers in Purview.SourceGeneratorFramework. Covers CodeWriter/XmlCommentWriter-style emission, incremental pipeline design, value equality, and Roslyn best practices."
---

# Source generator CodeWriter modernization

Use this skill for any work involving C# source generators, analysers, or generated output in `Purview.SourceGeneratorFramework`. It combines CodeWriter/XmlCommentWriter emission guidance with the incremental-source-generator and analyser best practices that ship with the framework.

## Required implementation pattern

When creating source output, favor this shape:

1. Build immutable, value-equatable pipeline values first.
2. Register source output.
3. Inside the callback: `var writer = generationContext.CreateCodeWriter();`
4. Write header/usings/namespace.
5. Write structured types and members using declaration options records.
6. Add source once per output artifact.

### CodeWriter lifetime: created in the output callback, never in pipeline state

The line between "fine" and "forbidden" is the **incremental-cache boundary**, not the act of
creating a writer or handing it to a helper.

**Fine — output-scoped use inside a `RegisterSourceOutput` callback.** Create the writer inside the
callback and pass it to any emitter/helper methods called from that same callback. It may be held in
local variables, passed as a parameter, or wrapped in a short-lived output context:

```csharp
context.RegisterSourceOutput(
    targets.CombineWithContext(contextProvider),
    static (spc, pair) =>
    {
        var (model, generationContext) = pair;
        var writer = generationContext.CreateCodeWriter();
        EmitHeader(writer, model);
        EmitType(writer, model);
        spc.AddSource($"{model.Name}.g.cs", writer.ToString());
    }
);
```

**Forbidden — persisting the writer across the incremental-cache boundary.** Do not create it earlier
in the pipeline and pass it down, and do not store it anywhere Roslyn caches or another callback can
observe it:

- Do not create a `CodeWriter` in a provider stage and carry it through the pipeline.
- Do not store a `CodeWriter` as a property or field on `GenerationContext` or a custom context.
- Do not return a `CodeWriter` (or an object holding one) from an incremental provider.
- Do not cache or reuse a writer for a later callback or a different output.

A cached writer can retain previously written source, mix output from concurrently running
callbacks, and defeat scope tracking. A writer created in the callback and used only within that
callback is safe and expected.

## Source generator & analyser best practices

Apply the following rules to every generator, analyser, and refactor.

### 1. Core principles

- **Analyser for validation; generator for generation.**
- **Syntax for syntax, symbols for declarations, operations for executable semantics.**
- **Use `ForAttributeWithMetadataName` for attribute-driven generators.**
- **Remove Roslyn objects from the incremental pipeline as early as possible.**
- **Every value crossing a pipeline boundary must have meaningful value equality.**
- **Prefer many small incremental stages over one large transform.**
- **Keep broad inputs such as `Compilation` away from downstream generation.**
- **Generate deterministic output.**
- **Compile against the oldest Roslyn API version containing the functionality you need.**
- **Test caching, not just generated text.**

The guiding principle for an incremental generator is:

> Extract semantic information once, convert it into a small value model, and make everything downstream operate only on that value model.

### 2. Analyser vs source generator

Use a `DiagnosticAnalyser` when the question is:

> Is the source code valid according to this library's rules?

Use an `IIncrementalGenerator` when the question is:

> Given valid source code, what source should be generated?

| Requirement | Prefer |
| --- | --- |
| Require a class to be `partial` | Analyser |
| Require an attribute on a declaration | Analyser |
| Validate a method signature | Analyser |
| Reject unsupported property types | Analyser |
| Detect invalid attribute arguments | Analyser |
| Detect unsupported API usage | Analyser |
| Offer an automatic fix | Analyser + `CodeFixProvider` |
| Generate members for a marked class | Incremental generator |
| Generate serializers/validators/mappers | Incremental generator |
| Generate a registry from discovered types | Incremental generator |
| Read a schema file and generate C# | Incremental generator |
| Internal generation failure | Generator diagnostic |

### 3. Choosing an analyser action

Use the narrowest API that represents the concept being analysed:

- `RegisterSyntaxNodeAction` — exact source syntax (e.g., modifier presence).
- `RegisterSymbolAction` — declaration semantics (e.g., attributes, interfaces, accessibility).
- `RegisterOperationAction` — executable behaviour (e.g., invocation, assignment, object creation).
- `RegisterOperationBlockStart/EndAction` — stateful method analysis.
- `RegisterSymbolStart/EndAction` — type-wide analysis across members.
- `RegisterCompilationStartAction` — resolve known framework symbols once.
- `RegisterAdditionalFileAction` — analyse `AdditionalFiles`.
- Avoid `RegisterSyntaxTreeAction`, `RegisterSemanticModelAction`, and compilation-end actions unless genuinely necessary.

### 4. Syntax vs symbol vs operation

Decision tree:

1. Does exact source spelling/structure matter? → **Syntax**
2. Otherwise, is it a declaration? → **Symbol**
3. Otherwise, is it executable behaviour? → **Operation**

Use `SymbolEqualityComparer.Default.Equals(...)` when comparing symbols.

### 5. Analyser best practices

- Enable concurrent execution with `context.EnableConcurrentExecution()`.
- Explicitly configure generated-code analysis with `context.ConfigureGeneratedCodeAnalysis(...)`.
- Resolve known framework/library symbols once in a `RegisterCompilationStartAction`.
- Prefer narrow registrations over scanning entire syntax trees or compilations.
- Treat diagnostic IDs as public contracts and maintain release tracking files when publishing public diagnostics.

### 6. Incremental generator golden rules

Implement `IIncrementalGenerator`. Simply implementing it is not enough; the pipeline must be incremental.

> **Pipeline values must be immutable and value-equatable.**

Never keep these in persistent pipeline models:

| Type | Verdict |
| --- | --- |
| `ISymbol` / `INamedTypeSymbol` / `IMethodSymbol` / `IPropertySymbol` | Never retain |
| `Compilation` | Do not propagate |
| `SemanticModel` | Do not propagate |
| `IOperation` | Do not propagate |
| `SyntaxTree` | Do not propagate |
| `SyntaxNode` | Remove ASAP |
| `Location` | Remove ASAP |
| `AdditionalText` | Project immediately |
| `T[]` / `List<T>` | Avoid |
| `ImmutableArray<T>` | Wrap with sequence equality |

Use immutable records and `EquatableArray<T>` (sequence equality) for collection members.

### 7. Designing the pipeline

Pipeline shape:

```text
Roslyn Input
    ↓
Cheap discovery
    ↓
Semantic extraction
    ↓
Small equatable model
    ↓
Validation/transformation
    ↓
Generation model
    ↓
Source output
```

Guidelines:

- Project the semantic transform as the boundary where Roslyn objects disappear.
- Prefer `static` callbacks to avoid capturing generator state.
- Honour cancellation tokens.
- Split transformations into many small stages.
- Keep syntax predicates cheap.
- Avoid indirect discovery (every interface implementation, every subclass, entire-compilation scans).

### 8. Syntax discovery

- Prefer `context.SyntaxProvider.ForAttributeWithMetadataName(...)` for attribute-driven generators.
- Use `context.SyntaxProvider.CreateSyntaxProvider(...)` only when syntax itself is the trigger and there is no marker attribute.
- The predicate must be cheap; do not walk the tree or do semantic work in it.

### 9. `Collect`, `Combine`, and invalidation

- `Collect()` turns per-item outputs into one aggregate. Changing any item invalidates the aggregate.
- Use `Collect()` only for genuinely global output: registries, lookups, duplicate detection, aggregate switches.
- Prefer per-item `RegisterSourceOutput`.
- `Combine()` is correct when the output depends on two providers.
- Avoid `models.Combine(context.CompilationProvider)` — project the compilation to a tiny capability fact first.
- Use `.WithComparer(...)` only when logical equality differs from the default.

### 10. Diagnostics

- Prefer a separate `DiagnosticAnalyser` for normal user validation.
- Use generator diagnostics only for malformed additional files, generator-only configuration, conflicting output, or failures that cannot be expressed by an analyser.
- Report diagnostics on the most useful user-authored `Location`.
- Do not keep `Location` in long-lived pipeline models.

### 11. Output generation

- Output must be deterministic: no timestamps, random GUIDs, process IDs, machine paths, culture-dependent output, or unordered dictionary output.
- Hint names must be deterministic, unique, and stable.
- Prefer text generation or `CodeWriter` over building Roslyn syntax trees just to stringify them.
- Use `RegisterPostInitializationOutput` for constant source such as marker attributes.
- Add generated source once per output artifact.

### 12. Testing incrementally

- Snapshot-testing generated source is not enough.
- Test first execution, cached second execution, unrelated changes remaining cached, per-target invalidation, deletion, renaming, global options, additional files, and global registry invalidation.
- Use `GeneratorDriverOptions` with `trackIncrementalGeneratorSteps: true` and inspect reasons: `New`, `Modified`, `Unchanged`, `Cached`, `Removed`.

### 13. Roslyn version compatibility and packaging

- The `Microsoft.CodeAnalysis.*` version used to compile the analyser/generator sets the minimum compiler-host requirement.
- The consumer's `TargetFramework` does not determine analyser compatibility.
- Choose the oldest Roslyn version that contains the APIs you need.
- Common baselines: Roslyn 4.8 for VS 17.8 / .NET 8, 4.12 for VS 17.12 / .NET 9, 5.0 for VS 2026 18.0 / .NET 10.
- Ship one `netstandard2.0` analyser/generator binary unless you have a deliberate multi-version strategy.
- Do not mistake multi-targeting for automatic analyser asset selection.
- Use `PrivateAssets="all"` for Roslyn development dependencies.
- Enable `EnforceExtendedAnalyzerRules` and investigate `RSxxxx` diagnostics before suppressing them.

### 14. Recommended project configuration

A generator project should normally include:

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <IsPackable>true</IsPackable>
  <IncludeBuildOutput>false</IncludeBuildOutput>
  <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="$(RoslynVersion)" PrivateAssets="all" />
  <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="$(RoslynAnalyserVersion)" PrivateAssets="all" />
</ItemGroup>

<ItemGroup>
  <None Include="$(TargetPath)" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

## Preferred API map for CodeWriter

### File and namespace

- `AutoGeneratedHeader(...)`
- `Using(...)`
- `FileScopedNamespace(...)` or `BlockNamespace(...)`
- `OpenPragmasScope(...)` for warning suppression scopes

### Types and members

- Types: `Class`, `Struct`, `RecordClass`, `RecordStruct`, `Interface`, `Enum`, `Delegate`
- Members: `Method`, `MethodScope`, `Property`, `Field`, `Constructor`
- Attributes: `AttributeDeclarationOptions`, `AttributeArgumentOptions`
- Type syntax: `TypeReference` (nullable/generic/array/pointer-safe composition)

### XML documentation

Use `XmlCommentWriter` extension methods on `CodeWriter`:

- `XmlSummary(...)`, `XmlParam(...)`, `XmlTypeParam(...)`, `XmlReturn(...)`, `XmlRemarks(...)`, `XmlExample(...)`
- `XmlCode(...)` / `XmlCodeBlock(...)`
- `XmlList(...)`, `XmlSeeAlso(...)`, `XmlException(...)`

Static helpers: `CodeWriter.XmlInlineCode(...)`, `CodeWriter.XmlSee(...)`, `CodeWriter.XmlParamRef(...)`, `CodeWriter.XmlText(...)`.

## Refactoring guide: string/StringBuilder -> CodeWriter

Apply this checklist in order:

1. **Move emission boundaries** — replace giant string assembly with phases: header, namespace, type, members.
2. **Replace manual braces/indentation** — use `using` scopes (`ClassScope`, `MethodScope`, `OpenBlockScope`, `IndentedScope`).
3. **Replace handwritten signatures** — use declaration option records.
4. **Replace raw XML lines** — use XML extension methods (`XmlSummary`, `XmlParam`, etc.).
5. **Normalize type strings** — use `TypeReference`.
6. **Preserve semantics and ordering** — generated members and diagnostics must remain equivalent.
7. **Validate scope safety** — keep or enable `PurviewSourceGeneratorFrameworkValidateCodeWriterScopes` for tests/dev.

## Anti-patterns to remove during refactors

- `StringBuilder.AppendLine("public class ...")` for declarations that can be structured.
- Manually writing `{` / `}` around methods and types where scope APIs exist.
- Hard-coded nullable type suffixes and generic syntax in arbitrary strings when `TypeReference` is available.
- Raw XML tag string composition when XML extension methods can enforce consistency.
- Creating a `CodeWriter` before the output callback and passing it through the pipeline.
- Storing a `CodeWriter` on `GenerationContext`, a custom context, or any incremental pipeline model.
- Sharing one `CodeWriter` across multiple generated outputs or callbacks.
- Keeping Roslyn objects, `CodeWriter`, or mutable state in incremental pipeline models.

## Review checklist for pull requests

- Generated declarations use structured APIs for types and members.
- XML docs use XML extension methods rather than raw `///` fragments.
- `CodeWriter` is created inside each output callback and never persists in pipeline state; passing it
  to helper methods within that callback is expected.
- Header and generated attributes are deterministic and consistent.
- Existing diagnostics, generated member names, and public behavior are preserved.
- Roslyn objects are removed from pipeline models; `EquatableArray<T>` is used for collections.
- Per-target output is preferred over collected global output unless global knowledge is required.
- `CompilationProvider` is not casually combined into output.
- Deterministic hint names and source text are used.
- Incremental caching behavior is tested, not just generated text.

## See also

- `agents/source-generator-framework-writer.agent.md` — specialist agent for `Purview.SourceGeneratorFramework` emitter authoring.
- `prompts/refactor-source-generator-to-codewriter.prompt.md` — prompt template for legacy-emitter refactor tasks.
