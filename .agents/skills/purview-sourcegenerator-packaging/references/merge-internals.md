# The merge pass and the self-contained analyzer

## Where the merged artifact lives

`_PurviewMergeSourceGeneratorFramework` only ever writes to the component's **intermediate** output.
`GetSourceGeneratorAnalyzerFiles` returns that result by substituting the merged path into
`TargetPathWithTargetPlatformMoniker` immediately before its body runs, leaving `GetTargetPath` —
which resolves assembly references — pointing at the unmerged `bin`. That split is what keeps the
in-repo test harness working while shipped assemblies stay self-contained.

The artifact is **content-addressed**:

```text
$(IntermediateOutputPath)purview-merged/<contentTag>/$(TargetFileName)
```

`<contentTag>` is a SHA-256 over the component, the framework assembly and the merge tool (the tool
computes it in its `--tag` mode). The tag is in the **directory**, never the file name, because
ILRepack derives the merged assembly's simple name from the output file name and an analyzer must
keep `<AssemblyName>.dll`.

Two properties follow, and both matter:

- A rebuild with identical inputs reuses the existing file and **skips the merge**, so
  `_PurviewMergeSourceGeneratorFramework` is idempotent across the per-consumer — and potentially
  parallel — invocations of `GetSourceGeneratorAnalyzerFiles`.
- Changed inputs select a **new directory**, so the merge never overwrites an assembly a compiler
  host (csc, `VBCSCompiler`, Visual Studio) already has loaded. Overwriting such a file fails on
  Windows with a sharing violation, which surfaced as `MSB3073` (merge tool exit code
  `-532462766`) and left consumers loading a stale or partially written analyzer — the root cause of
  `CS8784 FileNotFoundException: Could not load file or assembly 'Purview.SourceGeneratorFramework'`.

The tool merges into a per-process `.staging-<pid>` directory and publishes it with a
non-overwriting directory rename. A concurrent invocation that loses the race simply observes the
published artifact rather than failing.

Packaging needs the stable `<AssemblyName>.dll` name, so the pack paths
(`GetPurviewMergedAnalyzerFile`, `GetPurviewMergedAnalyzerFileForPack`) copy the content-addressed
artifact into `$(IntermediateOutputPath)purview-pack/` and pack that plainly named staging copy.

## Framework type internalization

ILRepack's `Internalize` is best-effort, and two gaps matter:

- a framework type that reaches the merged component's public API surface stays public; and
- the types the framework's own generators emit **into** the component — the
  `Purview.SourceGeneratorFramework.Generators` attribute set, generated type libraries, marker
  attributes — are not part of the merged framework assembly at all, so ILRepack never sees them.

Either gap leaks framework types out of what must be a self-contained analyzer, and any project
loading that analyzer alongside the real framework assembly fails with `CS0433` ambiguity for every
leaked type.

So the merge tool runs a deterministic internalization pass (`FrameworkTypeInternalizer`) after
ILRepack finishes:

- Every type in a framework-owned namespace (`Purview.SourceGeneratorFramework` and its children,
  including the generated `Generators` attribute set) becomes non-public. Ownership is evaluated
  through the **declaring chain**, because Mono.Cecil reports an empty namespace for a nested type:
  the framework's nested containers — generated type-library namespace classes, nested operator/enum
  groups, `CodeWriter` scopes — are only reachable through their declaring type.
- Types the framework's generators emitted into the component, recognized by the tool name on their
  `System.CodeDom.Compiler.GeneratedCodeAttribute` (`TypeLibraryGenerator`,
  `AttributeDataModelGenerator`), become non-public too, so a generated type library can never leak a
  `TypeIdentity`/`TypeReference` member as public API.
- `Microsoft.CodeAnalysis.EmbeddedAttribute`, the framework-emitted marker, becomes non-public.
- **Roslyn component entry points are never internalized.** A generator, analyzer, code fix provider
  or refactoring provider reachable from the framework namespace stays public, because Roslyn only
  instantiates public components (`PSGFR27`). This is what keeps the framework's own bundled
  analyzers working after the pass.
- The pass reports the result. Leftover public framework types fail the merge with **exit code 5**
  (`PSGFR43`). Public component members whose signature exposes a framework type are logged
  (`PSGFR41`) so the author can make them, or their declaring type, non-public. Visibility is
  followed through the declaring chain here too: a public nested type inside an internal type — for
  example the compiler-synthesised `<G>$`/`<M>$` extension containers emitted for an extension class
  — is not reachable from outside the component, so it is not part of the public surface and is not
  reported.

Ownership is seeded with the framework assembly's own type identities, so a framework type living
*outside* the framework namespace (`Microsoft.CodeAnalysis.*Extensions`, `System.StringExtensions`)
is internalized as well. The artifact's assembly-level `InternalsVisibleTo` /
`IgnoresAccessChecksTo` grants are removed, because a shipped analyzer is not the component's own
assembly.

## Tuning the pass

| Property | Default | Effect |
| --- | --- | --- |
| `PurviewMergeOwnedNamespaces` | empty | Extra namespace prefixes (semicolon-separated) treated as framework-owned. |
| `PurviewMergeOwnedTypeFullNames` | empty | Extra type full names (semicolon-separated) treated as framework-owned. |
| `PurviewMergePublicSurfaceSeverity` | `message` | How a finding is reported: `message` (plain log text), `warning`/`error` (MSBuild diagnostics with code `PSGFR41`), or `none`. |
| `PurviewMergePublicSurfaceValidation` | `true` | `false` sets the severity to `none`. |
| `PurviewMergePublicSurfaceOrigin` | the merged component assembly | The origin reported with an MSBuild finding; point it at a source or project path for IDE navigation. |

All five participate in the content tag, so changing one re-runs the merge — and re-emits its
findings — rather than reusing a cached artifact. The compiler analyzer with the same id (`PSGFR41`)
reports the same condition while you edit, so the surface is usually fixed before the merge runs.

## Generated accessibility: bin versus merged artifact

The component's own generated types — the type library, the attribute data models — keep their
generated accessibility in the component's **bin output**. `TLB0015` requires a hand-written partial
to be `public static partial` so it can merge with the generated library, and in-repo consumers such
as code fixers and sibling assemblies compile against it.

Only the **merged analyzer artifact** internalizes them. Self-containment is therefore enforced at
the merge boundary without rewriting the accessibility an in-repo consumer compiles against, and that
split is safe precisely because a merged artifact is never referenced as a compile-time dependency.

> A merged component is an analyzer artifact. Never reference it as a compile-time dependency. Tests
> that need to run a *packaged* generator load it out of band.

## How the framework package itself is assembled

`Purview.SourceGeneratorFramework` is dual-role: the framework assembly ships in `lib/` so generator
projects compile against it, and `analyzers/` carries self-contained generator, analyzer and
code-fixer assemblies. The bundled projects are:

- `SourceGeneratorFramework.Generators` — `AttributeDataModelGenerator`, `TypeLibraryGenerator`;
- `SourceGeneratorFramework.Analyzers` — the `PSGFR*` and `TLB*` analyzers;
- `SourceGeneratorFramework.CodeFixers` — the code fix providers.

Each has the framework implementation merged and internalized into it. The package deliberately does
**not** put `Purview.SourceGeneratorFramework.dll` under `analyzers/dotnet/cs`, so generators built
against different framework versions never ask Roslyn to load competing versions of a same-named
dependency.

The shared models and helpers that once shipped as a separate
`Purview.SourceGeneratorFramework.Shared.dll` are compiled directly into the framework assembly
(`SourceGeneratorShared.Link.targets` links the sources). Consumers get one
`Purview.SourceGeneratorFramework.dll`, which removes the version-skew hazard a separately bundled
Shared assembly caused — generator packages loading a different Shared version in-process failed with
binary-incompatibility errors, such as removed `PurviewTypeLibrary` fields.

Those projects are `IsRoslynComponent = true` and are **not** packable on their own. The
`SourceGeneratorFramework` project builds them via `GetPurviewMergedAnalyzerFile` and packs them
under `analyzers/dotnet/cs/` in `BuildAndPackBundledAnalyzerAssemblies`. They were previously
consumed as analyzer project references, but they now reference the framework assembly for the shared
types, which would form a project-reference cycle.
