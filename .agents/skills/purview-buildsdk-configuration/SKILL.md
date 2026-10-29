---
name: purview-buildsdk-configuration
description: Configure Purview.BuildSdk from Directory.Build.props or a .csproj. Use when setting NamespacePrefix, choosing a testing or mocking framework, turning telemetry or SourceLink off, controlling version detection from package.json, tuning the repository bootstrap, making a library browser-safe for a Blazor WebAssembly client, configuring a BFF host, or working out whether a property must be set before the SDK import. Also use when the SDK "did something automatically" and you need the property that controls it.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Purview.BuildSdk configuration

Use this skill when the question is "what property controls this, and where do I set it?"

For *why* the SDK classified a project a particular way, use `purview-buildsdk-project-detection`
instead. For where projects and tests should live, use `purview-buildsdk-project-layout`.

## First rule: know where a property must be set

A property that changes `Sdk.props` evaluation must be set **before** the SDK import — in
`Directory.Build.props`, above the import. Setting it in a `.csproj` is too late and silently has no
effect.

Pre-import properties include:

- `NamespacePrefix`
- `UsePackageJsonVersion`, `RootPackageJson`
- `EnableAssemblyNameGeneration`
- `PurviewSharedTestingOutputType`, `PurviewTestContextNoWarn`
- `AutoIncludeUsings`
- `DisablePurviewBlazorWebAssemblyDefaults`
- repo-wide testing-framework selection you want every project to inherit

Everything else is read in `Sdk.targets` and can be set in a project file. Prefer repo-wide defaults
in `Directory.Build.props` unless the setting is deliberately project-specific.

## The properties you will reach for most

| Property | Default | What it does |
| -- | -- | -- |
| `NamespacePrefix` | *(required)* | The root identity. Required unless `DisableNamespacePrefixCheck=true` |
| `TestingFramework` | `TUnit` | `TUnit`, `Xunit` or `None` |
| `SubstituteFramework` | `TUnitMocks` | `TUnitMocks`, `NSubstitute` or `None` |
| `TestDataFramework` | `Bogus` | `Bogus` or `None` |
| `TargetFramework` | `net10.0` | `netstandard2.0` when `IsRoslynComponent=true` |
| `IsPackable` | `false` | Opts a project into the packable defaults |
| `UsePackageJsonVersion` | `true` | `true`, `false` or `Strict` — version from `package.json` |
| `ExcludePurviewTelemetry` | `false` | Drops the telemetry source generator |
| `DisableSourceLink` | `false` | Drops SourceLink from packable projects |
| `EnableAssemblyNameGeneration` | `true` | `AssemblyName`/`PackageId` follow the resolved `RootNamespace` |
| `PurviewBrowserSafe` | *(unset)* | On a class library a WebAssembly client downloads: trim, AOT and browser-unsupported APIs fail the build |
| `IsBffProject` | name ends `Bff` | Classifies a BFF host; `false` opts out |
| `PurviewBrowserSafeReferenceCheckAsError` | `false` | Makes `PRBLZ0001` (a client reference that is not browser-safe) an error |

The full set, grouped by area and with the behaviour each one changes, is in
[references/properties.md](references/properties.md). Read it rather than guessing a property name —
several of these have near-miss names that do nothing.

## Derived properties to inspect, not re-derive

When explaining what the SDK did, read the value it computed rather than reasoning from the folder
name:

`PurviewLogicalProjectName`, `PurviewNamespacePrefix`, `PurviewProjectShortName`, `PurviewTestType`,
`RootNamespace`, `AssemblyName`, `PackageVersion`, `TestingType`, `TargetProjectName`, `RepoRoot`,
`RootPackageJson`, `IsBlazorHostProject`, `IsBffProject`, `PurviewBffExposesApi`, `IsAotCompatible`,
`WarningsAsErrors`.

Ask MSBuild for one directly:

```bash
dotnet msbuild <project> -getProperty:RootNamespace
dotnet msbuild <project> -getProperty:TestingType -getProperty:TargetProjectName
```

## Compiler-visible properties

The SDK exports its properties to analyzers and generators as `build_property.<PropertyName>`. When
authoring an analyzer, read the exported property instead of re-deriving SDK behaviour — the export
is the SDK's answer, and it stays correct when the rules change. The exported set is listed in
[references/properties.md](references/properties.md).

## Version detection

1. If `RootPackageJson` is set, that file is used.
2. Otherwise the repo root is discovered from CI variables, `.git`, or a nearby `package.json`.
3. On success, both `Version` and `PackageVersion` come from the `version` field.
4. `UsePackageJsonVersion=Strict` turns a failed discovery into a build failure instead of a silent
   fallback.

## Repository bootstrap

The SDK bootstraps two repository-wide files from the package: `.editorconfig` and `global.json`.

- `PurviewRepoBootstrapMode` — `IfMissing` (default), `Always`, `WarnOnDrift`, `Never`
- `DisableAutoCopySdkFiles` — the master off switch
- `PurviewRepoBootstrapCopyRetries` / `...RetryDelayMilliseconds` / `...FailureAsError`

Every project in a solution writes those same destinations, so the write is race-tolerant by
design: an existing file is never overwritten unless asked, each write is staged into a temporary
file and renamed into place, retries are quiet, and only a failure that survives every retry is
reported.

**The SDK writes nothing into an agent directory.** It used to mirror a package's `.agents/**` into
the repository; that is gone, along with `EnableAgentFolderInPackage`, `AgentPackDestinationFolder`
and the `PurviewAgentFolder*` properties. Skills have exactly one installer, the skills CLI — see
`purview-buildsdk-skill-installation`.

## Guidance when changing SDK configuration

1. Preserve existing defaults unless the task explicitly changes product behaviour.
2. Keep `README.md`, the property declaration, its validation and the shipped skills aligned — a
   property documented in one place and absent from another is the failure mode this repository
   keeps hitting.
3. A new property that affects import-time behaviour must say so in its documentation.
4. Add the property to the `CompilerVisibleProperty` list if an analyzer would benefit from it.
