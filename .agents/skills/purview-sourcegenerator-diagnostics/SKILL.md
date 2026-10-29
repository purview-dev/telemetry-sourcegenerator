---
name: purview-sourcegenerator-diagnostics
description: >-
  Use when a Purview.SourceGeneratorFramework build reports a PSGFR, PSGF, PRSGD or TLB diagnostic,
  when choosing which code fix applies, or when a source generator, diagnostic analyzer or code fix
  provider is silently not running at all.
metadata:
  product: Purview.SourceGeneratorFramework
  repository: purview-dev/sourcegenerator-framework
---

# Purview.SourceGeneratorFramework diagnostics

`Purview.SourceGeneratorFramework` ships `Purview.SourceGeneratorFramework.Analyzers` and
`Purview.SourceGeneratorFramework.CodeFixers` as analyzer assets inside the package, under
`analyzers/dotnet/cs/`. They are enabled automatically in any project that references the framework —
there is no separate package to add.

Two compiler-analyzer families, plus a set of MSBuild-time diagnostics:

- **Incremental generator best practice** — `PSGFR11`–`PSGFR33`: pipeline design, `CodeWriter` usage,
  Roslyn component discovery, extension-class conventions.
- **C# 14 extension-member conventions** — `PSGFR34`–`PSGFR38`, with
  `ReorganizeExtensionClassCodeFixProvider` and `ConvertToExtensionBlockCodeFixProvider`.
- **Merge and packaging** — `PSGFR39`–`PSGFR43`, where `PSGFR43` is the merge tool's blocked
  public-surface code.

## Start here when nothing runs

A compiler host loads a generator, analyzer or code fix provider only when **three** conditions hold.
Missing any one means the component is **silently ignored** — no error, no output, a green build:

1. **The type is public** (`PSGFR27`).
2. **The type is decorated** — `[Generator]` (`PSGFR26`), `[DiagnosticAnalyzer]` (`PSGFR25`), or
   `[ExportCodeFixProvider]` (`PSGFR24`).
3. **The assembly is loaded as an analyzer** — packed under `analyzers/dotnet/cs/` in a package, or
   referenced with `OutputItemType="Analyzer"` on a `ProjectReference`.

A code fix provider additionally appears only when a diagnostic ID in its `FixableDiagnosticIds` is
actually produced by an analyzer loaded alongside it (`PSGFR28`). Visual Studio MEF-composes fix
providers when the analyzer set loads, so after adding or updating a fixer assembly you must restart
Visual Studio or reload the project before the fixes show up.

## Build-time validation diagnostics

These are MSBuild diagnostics, not compiler analyzers, so they are **not** tracked in
`AnalyzerReleases.*.md`:

| Code | Raised by | Summary |
| --- | --- | --- |
| `PSGF0001` | `Purview.BuildSdk` | A Roslyn component did not produce (or declare) a source-generator analyzer file. |
| `PSGF0003` | `Purview.SourceGeneratorFramework` | A `PurviewGeneratorVisibleProperty` is not compiler-visible in the declaring project or its `Sdk/build`/`Sdk/buildTransitive` assets, so consumers cannot read `build_property.<Name>`. |
| `PRSGD0005` | `Purview.BuildSdk` | A file in the returned analyzer closure references an assembly that is neither in the closure nor a compiler-host assembly. |
| `PSGFR41` | merge tool | A component's public surface exposes framework types the merge internalizes. Reported by the analyzer at design time, and by the merge pass as `message` (default), `warning` or `error`. |
| `PSGFR43` | merge tool | The merged analyzer still exposes a public framework type, so it is not self-contained. Always an error; the merge fails with exit code 5. |

Opt out with `PurviewSourceGeneratorFrameworkAnalyzerValidation=false` (`PRSGD0005`) or
`PurviewSourceGeneratorFrameworkGeneratorPropertyValidation=false` (`PSGF0003`).

## Incremental pipeline rules

| Rule | Summary |
| --- | --- |
| `PSGFR11` | Prefer `SyntaxProvider.ForAttributeWithMetadataName` over `CreateSyntaxProvider` for attribute-based detection. |
| `PSGFR12` | Use `IIncrementalGenerator` / `RegisterSourceOutput` instead of `ISourceGenerator`. |
| `PSGFR14` | Avoid `RegisterImplementationSourceOutput` unless implementation-only output is required. |
| `PSGFR15` | Pipeline model collection members should use sequence equality (`EquatableArray<T>`). |
| `PSGFR30` | Prefer `static` lambdas in pipeline methods, so the compiler never allocates a closure on the per-item hot path. |
| `PSGFR31` | Prefer `GeneratorAttributeSyntaxContext.TargetSymbol` over `SemanticModel.GetDeclaredSymbol(ctx.TargetNode)`. |
| `PSGFR33` | Pipeline models must not retain Roslyn objects (`ISymbol`, `SyntaxNode`, `Location`, …); extract the information into value types. |

## CodeWriter rules

| Rule | Summary |
| --- | --- |
| `PSGFR16` | Prefer the nullable-context `Nullable()`/`MakeNullable()` overload, so annotations honour the target compilation. |
| `PSGFR17` | Consume scope-returning methods (`…Scope`, `IndentedScope`) with `using`. |
| `PSGFR18` | Prefer structured declaration APIs (`Class`, `Method`, `Property`, `Field`) over raw declaration text. |
| `PSGFR19` | Prefer structured statement APIs (`Return`, `MethodCall`, `Throw`, `Assignment`, `Using`, `Comment`) over raw statement text. |
| `PSGFR20` | Prefer the minimal `CodeWriter` overloads over hand-constructing `*DeclarationOptions` values. |
| `PSGFR21` | Prefer `HashDefines`/`HashDefinesScope` for `#if`/`#endif`. |
| `PSGFR22` | Prefer `PragmaDisable`/`OpenPragmasScope` for `#pragma warning`. |
| `PSGFR23` | Prefer structured `IfBlock`/`ElseIf`/`Else` over raw `if` block text. |
| `PSGFR29` | Do not embed a `CodeWriter` in a string; use `XmlCommentWriter.XmlInlineCode`. |
| `PSGFR32` | Avoid `NormalizeWhitespace` when generating source; use an indented text writer such as `CodeWriter`. |

## Component, extension and merge rules

| Rule | Summary |
| --- | --- |
| `PSGFR24` | `CodeFixProvider` is not marked `[ExportCodeFixProvider]`; Visual Studio will never discover it. |
| `PSGFR25` | `DiagnosticAnalyzer` is not marked `[DiagnosticAnalyzer]`; it will never run. |
| `PSGFR26` | A generator type is not marked `[Generator]`; it will never run. |
| `PSGFR27` | A Roslyn component type is not public; the compiler host cannot instantiate it. |
| `PSGFR28` | `FixableDiagnosticIds` names a diagnostic no analyzer in the compilation produces; the fix will never be shown. |
| `PSGFR34` | Prefer C# 14 `extension(Receiver)` blocks over classic static `this`-parameter extension methods. |
| `PSGFR35` | Extension class name must match the extended type (`{Receiver}Extensions`). |
| `PSGFR36` | Extension classes belong in the extended type's namespace, under an `Extensions` folder. |
| `PSGFR37` | One extension class per receiver type; split a class that extends several. |
| `PSGFR38` | Extension classes should carry `[EditorBrowsable(EditorBrowsableState.Never)]`. |
| `PSGFR39` | A non-packable Roslyn component opted out of the default self-contained analyzer output (`PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=false`) while embedding the framework; otherwise the package embeds the loose framework DLL under `analyzers/`. |
| `PSGFR40` | In a Roslyn component (`IsRoslynComponent=true`), reference SGF types as inline code (`<c>Type</c>`) rather than a `cref`: copied documentation must not depend on cref resolution. |
| `PSGFR41` | A public member (or generic constraint) whose signature references an SGF type in a component whose framework implementation is merged. The merge internalizes every SGF type, so the signature ends up referring to an internal type. Make the member, or its declaring type, non-public. |
| `PSGFR42` | A disposable Testing-harness result (`DriverRunResult`) is produced and never disposed, leaking the collectible `AssemblyLoadContext` and `MetadataLoadContext` of a compiled run. `warning` when the run syntactically compiles to an assembly (a `Compile` options member, or `CompileToAssembly = true`), otherwise `info`. Awaiting an assertion, or calling a helper handed the result — an extension on it, or a method taking it as a parameter — is not a production: both hand the same instance back, so only a task-like await or a genuine producer is reported. |

## Type-library and attribute-model diagnostics

The bundled generators carry their own families: `TypeLibraryValidationAnalyzer`
(`TLB0001`–`TLB0019`, plus `TLB0021`) and the attribute-data-model validation analyzers. They are
documented on their own wiki pages, `Type-Library.md` and `Attribute-Data-Models.md`.

## Code fixes

Most rules above have a fix in `Purview.SourceGeneratorFramework.CodeFixers`:

| Fix provider | Rule |
| --- | --- |
| `AddGeneratorAttributeCodeFixProvider` | `PSGFR26` |
| `AddDiagnosticAnalyzerAttributeCodeFixProvider` | `PSGFR25` |
| `AddExportCodeFixProviderAttributeCodeFixProvider` | `PSGFR24` |
| `MakeRoslynComponentPublicCodeFixProvider` | `PSGFR27` |
| `RemoveOrphanedFixableDiagnosticIdCodeFixProvider` | `PSGFR28` |
| `PreferTargetSymbolCodeFixProvider` | `PSGFR31` |
| `PreferStaticLambdaCodeFixProvider` | `PSGFR30` |
| `PreferNullableContextOverloadCodeFixProvider` (project-wide Fix All) | `PSGFR16` |
| `PipelineModelReferenceEqualityCollectionCodeFixProvider` | `PSGFR15` |
| `PreferStructuredCodeWriterIfBlockCodeFixProvider` | `PSGFR23` |
| `CodeWriterToStringCodeFixProvider` | `PSGFR29` |
| `PreferInlineCodeForFrameworkCrefCodeFixProvider` | `PSGFR40` |
| `MakeComponentSurfaceNonPublicCodeFixProvider` | `PSGFR41` |
| `DisposeTestResultCodeFixProvider` | `PSGFR42` |
| `ReorganizeExtensionClassCodeFixProvider` (renames, splits, moves, updates referencing files) | `PSGFR35`, `PSGFR36`, `PSGFR37` |
| `ConvertToExtensionBlockCodeFixProvider` | `PSGFR34` |
| `AddExtensionClassMetadataCodeFixProvider` | `PSGFR38` |
| `MakeTypeLibrarySpecNonPublicCodeFixProvider` | `TLB0021` |
| `AttributeDataModelSymbolPropertyCodeFixProvider` | attribute-data-model symbol properties |
| `TypeLibraryMemberAccessibilityCodeFixProvider`, `TypeLibraryMarkerDefaultInitializerCodeFixProvider`, `MakeTypeLibrarySpecPartialCodeFixProvider`, `RenameTypeLibrarySpecCodeFixProvider`, `TypeLibraryMemberTypeCodeFixProvider` | type-library rules |

## Related skills

`purview-sourcegenerator-codewriter` for the emission patterns the `CodeWriter` rules push you
towards, and `purview-sourcegenerator-testing` for proving a pipeline actually caches.
