---
name: purview-sourcegenerator-packaging
description: >-
  Use when packaging or referencing a source generator built on Purview.SourceGeneratorFramework -
  the self-contained merged analyzer, GetSourceGeneratorAnalyzerFiles, embedding a generator in
  another package, PurviewGeneratorVisibleProperty, analyzer-closure validation, Roslyn version
  compatibility, and errors such as CS0433, CS8784 or MSB3073 from a loaded analyzer.
metadata:
  product: Purview.SourceGeneratorFramework
  repository: purview-dev/sourcegenerator-framework
---

# Packaging a source generator

The rule everything else follows from: **a shipped Roslyn component carries its own copy of the
framework and references no loose `Purview.SourceGeneratorFramework.dll`.** Two generators built
against different framework versions must be able to load into one compiler host. A shared DLL under
`analyzers/` cannot give you that — whichever version loads first wins, and the other fails with
`MissingMethodException` or `CS0433`.

The framework delivers this by IL-merging and internalizing its implementation into each component's
**intermediate** output, leaving the component's `bin` unmerged.

## Referencing the framework from a component

```xml
<PropertyGroup>
  <IsRoslynComponent>true</IsRoslynComponent>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Purview.SourceGeneratorFramework" Version="..." PrivateAssets="all" />
</ItemGroup>
```

And from a consuming project, as an analyzer:

```xml
<ProjectReference
  Include="..\MyGenerator\MyGenerator.csproj"
  PrivateAssets="all"
  OutputItemType="Analyzer"
  ReferenceOutputAssembly="false"
/>
```

`Purview.BuildSdk` invokes `GetSourceGeneratorAnalyzerFiles` automatically — naming
`Targets="GetSourceGeneratorAnalyzerFiles"` explicitly still works but is not required.

## The three consumption paths

| Path | Trigger | What you get |
| --- | --- | --- |
| `GetSourceGeneratorAnalyzerFiles` (default) | a consuming project references the component as an analyzer | the **merged** component, from the content-addressed `purview-merged/<contentTag>/`. `bin` stays unmerged, so an in-process test harness keeps shared framework type identity and `InternalsVisibleTo` access without `CS0433`. |
| `GetSourceGeneratorAnalyzerFiles` with `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=false` | opt-out | unmerged component **plus** the loose framework DLL from the package `lib/`. |
| `GetPurviewMergedAnalyzerFile` | the framework's own bundled-component pack, or a third-party package embedding the generator | the merged component; `bin` is never overwritten. |
| `GetPurviewMergedAnalyzerFileForPack` (pack-time, via `TargetsForTfmSpecificContentInPackage`) | packing a standalone packable generator | the merged DLL and PDB contributed straight to `analyzers/dotnet/cs`. `bin` is never mutated, so its shape does not depend on whether `build` or `pack` ran last. |

Two properties exist and both should normally be left alone:

- `PurviewEmbedSourceGeneratorFramework=false` — only for a project that *is* the framework
  compile-time library. A published generator package must never disable embedding.
- `PurviewMergeSourceGeneratorFrameworkForAnalyzerFiles=false` — only when the analyzer-files
  consumer must keep the unmerged + loose-DLL shape (a generator shipped into one compiler process
  alongside an incompatible framework version).

`PSGFR39` errors when a **non-packable** component opts out while embedding the framework, because
that shape, packed through the GASF path, forces the loose DLL back under `analyzers/`.

## A component that references another component

A code-fix or analyzer component can reference the generator component normally (`ProjectReference`,
`ReferenceOutputAssembly` **not** `false`) when it needs the generator's internal diagnostic
identity. Both are `IsRoslynComponent`; only the generator references the framework.

- The referenced component is returned as **its own merged assembly** and is never IL-merged into the
  dependent one — that would duplicate its types, including `InternalsVisibleTo`-visible internals,
  inside a second analyzer in the same host.
- The framework assembly is declared as copy-to-output content beside the generator's `bin`, and
  content with `CopyToOutputDirectory` flows transitively through `ProjectReference`. That is what
  lets Visual Studio load the dependent code-fix provider from its own `bin` without a
  `FileNotFoundException` for the framework assembly.
- Visual Studio's project system resolves an `OutputItemType=Analyzer` project reference to the
  referenced project's **unmerged** `bin` assembly and adds it to the compiler's analyzers — the
  command-line build does not. `Purview.BuildSdk` removes that item before adding the resolved
  closure, so Roslyn only ever receives the merged artifact. Without that removal the unmerged copy
  drags `Purview.SourceGeneratorFramework` into the compiler host
  (`CS8784 FileNotFoundException: Could not load file or assembly 'Purview.SourceGeneratorFramework'`)
  and every generator registers twice.

## Analyzer-closure validation

`Purview.BuildSdk` validates the whole closure `GetSourceGeneratorAnalyzerFiles` returns: every
file's PE `AssemblyRef` must resolve to another file in the returned set, or to a compiler-host
assembly (`Microsoft.CodeAnalysis*`, `System.Composition.*`, `System.*`, `netstandard`, …). A missing
component artifact fails the build with `PRSGD0005`.

Extend the permitted set with `<PurviewAnalyzerClosurePermittedReference Include="..." />` or the
`PurviewAnalyzerClosurePermittedReferences` property. Opt out with
`PurviewSourceGeneratorFrameworkAnalyzerValidation=false`.

## Generator-read MSBuild properties

A generator reading `build_property.<Name>` is only correct if `<Name>` is a
`CompilerVisibleProperty` **wherever the generator runs** — including in a consumer's project.
Declare what the component reads:

```xml
<ItemGroup>
  <PurviewGeneratorVisibleProperty Include="MyGenerator_Disable" />
</ItemGroup>
```

`PSGF0003` fails the build when a declared property is neither a `CompilerVisibleProperty` in the
project nor declared by the project's own `Sdk/build` or `Sdk/buildTransitive` assets — which is what
consumers actually receive. Opt out with
`PurviewSourceGeneratorFrameworkGeneratorPropertyValidation=false`.

NuGet imports `buildTransitive` assets for **PackageReference** consumers only. Inside the package
repository every project uses `ProjectReference`, so `Purview.BuildSdk`'s
`ImportProjectReferencedBuildTransitiveAssets` registers the referenced project's
`Sdk/buildTransitive/*.props|*.targets` `CompilerVisibleProperty` items for in-repo consumers. Opt
out with `PurviewImportProjectReferenceBuildTransitive=false`.

## Embedding a generator in another package

Pack the **merged** assembly, so the outer package ships no loose framework DLL:

```xml
<PropertyGroup>
  <PackProjectReferencedSourceGenerators>false</PackProjectReferencedSourceGenerators>
  <TargetsForTfmSpecificContentInPackage>$(TargetsForTfmSpecificContentInPackage);PackMyGenerator</TargetsForTfmSpecificContentInPackage>
</PropertyGroup>

<Target Name="PackMyGenerator">
  <MSBuild
    Projects="../MyGenerator/MyGenerator.csproj"
    Targets="GetPurviewMergedAnalyzerFile"
    Properties="Configuration=$(Configuration)"
    RemoveProperties="TargetFramework;TargetFrameworks;RuntimeIdentifier;SelfContained"
  >
    <Output TaskParameter="TargetOutputs" ItemName="_MyGeneratorMerged" />
  </MSBuild>
  <ItemGroup>
    <TfmSpecificPackageFile Include="@(_MyGeneratorMerged)">
      <PackagePath>analyzers/dotnet/cs/</PackagePath>
    </TfmSpecificPackageFile>
  </ItemGroup>
</Target>
```

If the generator is also embedded at compile time for the outer package's consumers, the outer
package must make the framework's compiler-visible properties visible to them — build assets from
`Purview.SourceGeneratorFramework` are **not** copied into the outer package. Ship a `.props` that
declares the property and its `CompilerVisibleProperty` entry, packed under the outer package's own
ID so NuGet imports it:

```xml
<None Include="Sdk\Sdk.props" Pack="true" PackagePath="buildTransitive\$(PackageId).props" Visible="false" />
```

## `IsExternalInit`

The framework assembly defines `System.Runtime.CompilerServices.IsExternalInit` **publicly**, so the
bundled generators can emit `init`-based attribute types into any consumer compilation and the merge
has a single marker definition to internalize.

**Do not declare your own.** A generator-local marker gives calls to the framework's `init` setters a
different required-custom-modifier identity from the setter definitions. The merge tool normalizes
those modifiers in a temporary copy for compatibility with legacy tooling, but removing the redundant
marker is the preferred configuration.

## Roslyn version compatibility

> The version of `Microsoft.CodeAnalysis.*` you compile the component against sets a **minimum
> compiler-host API requirement**. The consumer's `<TargetFramework>` has nothing to do with it —
> analyzer code runs inside the compiler/IDE host.

| Roslyn package | Minimum Visual Studio | Language / .NET generation |
| ---: | --- | --- |
| 4.8 | VS 2022 17.8 | C# 12 / .NET 8 |
| 4.12 | VS 2022 17.12 | C# 13 / .NET 9 |
| 5.0 | VS 2026 18.0 | C# 14 / .NET 10 |

**This framework is built against Roslyn 5.0**, so a host loading it must be Roslyn 5.0 or later
(.NET 10 SDK / Visual Studio 2026 18.0). The testing packages multi-target `net8.0`–`net10.0`, and
Roslyn 5.x ships `net8.0`/`net9.0` assets, so those test targets still load the runner.

## A generator project to start from

```xml
<Project Sdk="Microsoft.NET.Sdk">
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
    <PackageReference Include="Purview.SourceGeneratorFramework" Version="$(SourceGeneratorFrameworkVersion)" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <None Include="$(TargetPath)" Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
  </ItemGroup>
</Project>
```

`Purview.BuildSdk` already defaults most of this for a project it classifies as a Roslyn component,
so in an SDK repository the project file is usually far shorter. The exact Roslyn baseline is a
product-support decision.

## Release gates

Acceptance criteria for self-contained packaging — check metadata, never "the sample compiled":

1. **No framework reference, no public framework types.** Every `analyzers/dotnet/cs/*.dll` has no
   assembly reference to `Purview.SourceGeneratorFramework` and exposes no public type in a
   framework-owned namespace (including the generated `Generators` attribute set and the
   framework-emitted `Microsoft.CodeAnalysis.EmbeddedAttribute`). The one exception is the
   component's own Roslyn entry points, which must stay public — Roslyn only instantiates public
   components (`PSGFR27`).
2. **No loose framework DLL in any package**, and no `*.pdb` in the `.nupkg` — symbols ship only
   through the `.snupkg`.
3. **Merged entry points survive**: each merged DLL still exposes its `IIncrementalGenerator`,
   `DiagnosticAnalyzer` or `CodeFixProvider`.
4. **Installed-package consumer test**: a generator project referencing the framework by
   `PackageReference` packs one self-contained DLL under `analyzers/dotnet/cs`, and a consumer that
   installs it builds with the generator producing output.
5. **Two-version coexistence test**: build Generator A against the current framework and Generator B
   against an intentionally binary-incompatible version, install both into one consumer, and build
   with both package-reference orders. Both must run against their own embedded copy.

## Going deeper

The merge pass, the content-addressed artifact, framework type internalization and the properties
that tune it are in [references/merge-internals.md](references/merge-internals.md) — read it when a
merge fails, when a type leaks, or when you hit `MSB3073` / `CS0433` / `CS8784` from a loaded
analyzer.
