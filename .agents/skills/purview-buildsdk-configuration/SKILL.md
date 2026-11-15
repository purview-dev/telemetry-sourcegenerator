---
name: purview-buildsdk-configuration
description: Configure Purview.BuildSdk from Directory.Build.props or a .csproj. Use when setting NamespacePrefix, setting the target framework(s) or selecting a curated framework set, choosing a SourceLink provider for the repository host (GitHub, Azure DevOps, GitLab, Bitbucket), choosing a testing or mocking framework, turning telemetry or SourceLink off, controlling version detection from package.json, pinning the SDK version in global.json, tuning the repository bootstrap, making a library browser-safe for a Blazor WebAssembly client, configuring a BFF host, or working out whether a property must be set before the SDK import. Also use when the SDK "did something automatically" and you need the property that controls it.
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
- `PurviewTestContextNoWarn`, `DisablePurviewTestContextRuleSet`
- `DisablePurviewBlazorWebAssemblyDefaults`
- `TestingFramework`, `SubstituteFramework`, `TestDataFramework` — even for a single project

A target framework is a special case. An explicit `TargetFramework`/`TargetFrameworks` always wins,
wherever it is set except `Directory.Build.targets`. `PurviewTargetFrameworkSet` is read from the
**project XML**, so it can live in the project or in `Directory.Build.props` but must be declared
**unconditionally** — see [Setting the target framework(s)](#setting-the-target-frameworks).

Everything else is read in `Sdk.targets`, or only after the project body, and can be set in a project
file — `PurviewSharedTestingOutputType` and `AutoIncludeUsings` among them. Prefer repo-wide defaults
in `Directory.Build.props` unless the setting is deliberately project-specific.

## Required settings and versions

Four things must be right, and three of them are read during `Sdk.props` evaluation — set those in
`Directory.Build.props` **above** the `<Import>`:

| Setting | Where | Notes |
| -- | -- | -- |
| `NamespacePrefix` | `Directory.Build.props`, pre-import | Required. The build fails without it (`ValidateRootNamespacePrefixTarget`) unless `DisableNamespacePrefixCheck=true`. |
| SDK version | `global.json` → `msbuild-sdks.Purview.BuildSdk` | The pinned `Purview.BuildSdk` version. The SDK bootstraps `global.json` (with this value) when it is missing; `PurviewBuildSdkVersionForGlobalJson` decides what is written. |
| Product version | repo-root `package.json` → `version` | Drives both `Version` and `PackageVersion`. `UsePackageJsonVersion` (`true`/`false`/`Strict`) and `RootPackageJson` are pre-import; `Strict` fails the build when discovery cannot succeed instead of silently staying on `0.0.1`. |
| Package versions | `Directory.Packages.props` | CPM is on (`ManagePackageVersionsCentrally=true`). Each `PackageVersion` is a minimum, not an exact pin. |

## Setting the target framework(s)

A project that declares neither `TargetFramework` nor `TargetFrameworks` gets the SDK's default set,
`net10.0;net11.0` (`Current`). Pick one of three routes, narrowest first:

1. **Explicit declaration** — `<TargetFramework>net10.0</TargetFramework>` for one target,
   `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` for many. An explicit value **always wins**
   over every set, wherever it is set — the project (with or without a `Condition`), a file it
   imports, or `Directory.Build.props` before or after the import. Only `Directory.Build.targets` is
   too late, because the .NET SDK has already decided whether to multi-target. A project declaring
   `TargetFrameworks` keeps `TargetFramework` empty.
2. **A curated set** — `<PurviewTargetFrameworkSet>Supported</PurviewTargetFrameworkSet>` in
   `Directory.Build.props` (repository-wide) or the `.csproj`. Sets: `Current` (default),
   `Latest` (`net11.0`), `Supported` (`net10.0;net11.0`), `Broad` (`net8.0;net9.0;net10.0`), `All`
   (`net8.0;net9.0;net10.0;net11.0`). An unknown name fails with `PurviewInvalidTargetFrameworkSet`
   rather than silently falling back.
3. **Redefine a set** — set `PurviewTargetFrameworks<Set>` (for example `PurviewTargetFrameworksSupported`)
   **before the import** to pin the repository's meaning of a set across an SDK bump.

A Roslyn component (`IsRoslynComponent=true`) targets `netstandard2.0` and ignores the sets; selecting
one is reported as `PurviewTargetFrameworkSetIgnored`. The full table is in
[references/properties.md](references/properties.md#target-framework-and-language).

## The properties you will reach for most

| Property | Default | What it does |
| -- | -- | -- |
| `NamespacePrefix` | *(required)* | The root identity. Required unless `DisableNamespacePrefixCheck=true` |
| `TestingFramework` | `TUnit` | `TUnit`, `Xunit` or `None` |
| `SubstituteFramework` | `TUnitMocks` | `TUnitMocks`, `NSubstitute` or `None` |
| `TestDataFramework` | `Bogus` | `Bogus` or `None` |
| `TargetFramework` | `net10.0;net11.0` | `netstandard2.0` when `IsRoslynComponent=true` |
| `IsPackable` | `false` | Opts a project into the packable defaults |
| `UsePackageJsonVersion` | `true` | `true`, `false` or `Strict` — version from `package.json` |
| `ExcludePurviewTelemetry` | `false` | Drops the telemetry source generator |
| `DisableSourceLink` | `false` | Stops the SDK adding the SourceLink provider package to any project |
| `EnableAssemblyNameGeneration` | `true` | `AssemblyName`/`PackageId` follow the resolved `RootNamespace` |
| `PurviewBrowserSafe` | *(unset)* | On a class library a WebAssembly client downloads: trim, AOT and browser-unsupported APIs fail the build |
| `IsBffProject` | name ends `Bff` | Classifies a BFF host; `false` opts out |
| `PurviewBrowserSafeReferenceCheckAsError` | `false` | Makes `PRBLZ0001` (a client reference that is not browser-safe) an error |
| `MemorySafetyRules` | `0` | C# 15 memory-safety tier on `net11.0`+: `0` off (default), `1` preview syntax, `2` updated rules (`updated-memory-safety-rules`, opt-in — an experimental compiler feature) |
| `PurviewRuntimeAsync` | *(unset, treated as false)* | Opts a `net11.0`+ target into `runtime-async=on`; `UseRuntimeAsync=true` also opts in |
| `IsClientSideProject` | *(computed)* | Whether the project's assemblies can reach a browser; overrides the `ServerShared` classification |
| `PurviewAutoServerSharedReference` | `true` | Adds a sibling `ServerShared` reference to a non-client-side project |
| `PurviewMultiThreadedBuild` | `true` | Bootstraps a `Directory.Build.rsp` enabling MSBuild `-mt`; unset, a Blazor WebAssembly client writes `-mt:false` (`PRBLZ0002`) |

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

The SDK exports a large set of properties to Roslyn analyzers and source generators through
`CompilerVisibleProperty`, readable as `build_property.<PropertyName>`:

```csharp
context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.IsBffProject", out var isBff);
```

- **Consuming them** — read the exported property instead of re-deriving SDK behaviour; the export is
  the SDK's answer and stays correct when the rules change. The SDK's own analyzers use this
  (`build_property.IsBffProject` gates `PDS0008`/`PDS0009`).
- **Exposing a new one** — an analyzer can only see a property the SDK lists as a
  `CompilerVisibleProperty` item (with a `<Description>`) in `Sdk.props`. Add it there; there is no
  other mechanism, and an MSBuild property is invisible to the compiler until it is listed.

The exported set is listed in
[references/properties.md](references/properties.md#compiler-visible-properties).

## Version detection

1. If `RootPackageJson` is set, that file is used.
2. Otherwise the repo root is discovered from CI variables, `.git`, or a nearby `package.json`.
3. On success, both `Version` and `PackageVersion` come from the `version` field.
4. `UsePackageJsonVersion=Strict` turns a failed discovery into a build failure instead of a silent
   fallback.

## Repository bootstrap

The SDK bootstraps three repository-wide files from the package: `.editorconfig`, `global.json` and
`Directory.Build.rsp`.

- `PurviewRepoBootstrapMode` — `IfMissing` (default), `Always`, `WarnOnDrift`, `Never`
- `DisableAutoCopySdkFiles` — the master off switch
- `PurviewRepoBootstrapCopyRetries` / `...RetryDelayMilliseconds` / `...FailureAsError`
- `BootstrapMultiThreadedBuildRspToRepoRoot` / `PurviewMultiThreadedBuild` — the response file that
  enables MSBuild `-mt` (and `-nr:false`, which avoids a `RoslynCodeTaskFactory` cache left by a
  non-`-mt` build). A repository with a Blazor WebAssembly client gets `-mt:false` instead, because a
  `net11.0` client cannot build under `-mt`; a client built under `-mt` anyway fails with `PRBLZ0002`.
  The WebAssembly SDK's own error for this is `MSB4060` on `ReadWasmProps`, and it does not mention the
  response file

Every project in a solution writes those same destinations, so the write is race-tolerant by
design: an existing file is never overwritten unless asked, each write is staged into a temporary
file and renamed into place, retries are quiet, and only a failure that survives every retry is
reported.

**The SDK writes nothing into an agent directory.** It used to mirror a package's `.agents/**` into
the repository; that is gone, along with `EnableAgentFolderInPackage`, `AgentPackDestinationFolder`
and the `PurviewAgentFolder*` properties. Skills have exactly one installer, the skills CLI — see
`purview-buildsdk-skill-installation`.

The skill targets read their settings when they run, so these can go in `Directory.Build.props` or
on the command line, and a repository-wide choice belongs in `Directory.Build.props`:

| Property | Default | Controls |
| -- | -- | -- |
| `PurviewSkillsRunner` | `bun` (`bun x skills`) | Which package runner launches the skills CLI |
| `PurviewSkillsAgents` | empty — the CLI auto-detects | Agents to install to; when set, the only agent directories `CheckPurviewSkills` allows |
| `PurviewSkillsDryRun` | `false` | Install and update print commands instead of running them |

On a command line, `dotnet msbuild -p:` splits values on `,` and `;` — write
`-p:PurviewSkillsAgents=claude-code%2Ccodex`. For `just` recipes, see `purview-buildsdk-justfile`.

## SourceLink

Every non-test project gets a SourceLink provider package — `SourceLinkPackageName`, default
`Microsoft.SourceLink.GitHub`, added with `PrivateAssets="all"` so a debugger can step into a consumed
package's source and the `.nuspec` records the repository URL and commit. A repository on another host
selects the matching package in `Directory.Build.props`, so every project agrees:

| Host | Provider package |
| -- | -- |
| GitHub / GitHub Enterprise | `Microsoft.SourceLink.GitHub` *(default)* |
| Azure DevOps Services / Azure Repos | `Microsoft.SourceLink.AzureRepos.Git` |
| Azure DevOps Server / TFS (on-premises) | `Microsoft.SourceLink.AzureDevOpsServer.Git` (+ `SourceLinkAzureDevOpsServerGitHost`) |
| GitLab | `Microsoft.SourceLink.GitLab` |
| Bitbucket | `Microsoft.SourceLink.Bitbucket.Git` |

Because CPM is on, the chosen provider also needs a `PackageVersion` in `Directory.Packages.props`. Use
`DisableSourceLink=true` (in `Directory.Build.props`, or one project) to opt out. The host matrix, the on-premises Azure DevOps Server
host item, the CPM requirement and the self-hosted case are in
[references/sourcelink.md](references/sourcelink.md).

## Guidance when changing configuration

1. Preserve existing defaults unless the task explicitly changes behaviour, and do not restate an SDK
   default in a project file.
2. Confirm a change with `dotnet msbuild <project> -getProperty:<Name>` rather than reasoning about it.
3. For a package that ships its **own** MSBuild properties — adding one, renaming it, documenting it in
   a `.buildschema.json` sidecar, or exporting it to analyzers — use `purview-buildsdk-msbuild-properties`.
