# Purview.BuildSdk property reference

Every user-facing property, grouped by what it changes. Defaults are what the SDK applies when the
consuming project supplies nothing.

## Identity and naming

| Property | Default | Behaviour |
| -- | -- | -- |
| `NamespacePrefix` | *(required)* | The root identity source. Required unless the check is disabled. Set before the SDK import |
| `DisableNamespacePrefixCheck` | `false` | Stops the build failing when `NamespacePrefix` is unset |
| `EnableAssemblyNameGeneration` | `true` | `AssemblyName` and the default `PackageId` follow the fully evaluated `RootNamespace`, or the full logical project name when suffix-stripping removed a segment (`Shared`, `ServiceDefaults`). Set `false` before the SDK import for standard project-name behaviour |
| `AssemblyIdentifierClassName` | *(derived)* | Renames the generated marker class |
| `DisableGenerateAssemblyIdentifierClass` | `false` (forced `true` for test and shared-testing projects) | Drops the empty marker class (`<project name minus NamespacePrefix, dots removed>Assembly`, global namespace) that architecture tests use to pin an assembly by `typeof` rather than by name |
| `DisableGenerateAssemblyInfoClass` | `false` (forced `true` for test and shared-testing projects) | Drops the generated `AssemblyInfo` |
| `DisableAutoInternalsVisibleTo` | `false` | Drops automatic friend-assembly generation |
| `DisableProjectFileNamingConventionCheck` | `false` | Stops validating that the `.csproj` filename matches its directory |
| `AutoIncludeUsings` | `true` | Controls the SDK-added global usings. Can be set in a project file |

## Target framework and language

| Property | Default | Behaviour |
| -- | -- | -- |
| `TargetFramework` | `net10.0;net11.0` | Applied when neither `TargetFramework` nor `TargetFrameworks` is set. An explicit value (or `TargetFrameworks`) always wins over every set, wherever it is set — with or without a `Condition` — except `Directory.Build.targets`, which is too late. A project that explicitly declares `IsRoslynComponent=true` defaults to `netstandard2.0` |
| `PurviewTargetFrameworkSet` | `Current` | Named set resolved when the project declares no TFM: `Current`, `Latest`, `Supported`, `Broad` or `All`. An unknown name fails with `PurviewInvalidTargetFrameworkSet`. A single-entry set resolves to `TargetFramework` rather than paying for an outer build. Declare unconditionally |
| `PurviewTargetFrameworksCurrent` | `net10.0;net11.0` | Redefines the `Current` set (the default). Set before the SDK import |
| `PurviewTargetFrameworksLatest` | `net11.0` | Redefines the `Latest` set |
| `PurviewTargetFrameworksSupported` | `net10.0;net11.0` | Redefines the `Supported` set |
| `PurviewTargetFrameworksBroad` | `net8.0;net9.0;net10.0` | Redefines the `Broad` set |
| `PurviewTargetFrameworksAll` | `net8.0;net9.0;net10.0;net11.0` | Redefines the `All` set |
| `TreatWarningsAsErrors` | `true` | Every project: compiler, analyzer and NuGet warnings fail the build (MSBuild `<Warning>` tasks do not). `false` in a project or `Directory.Build.props` opts out. A packable Roslyn component still fails its pack without it (`PRSGD0003`) |
| `IsRoslynComponent` | *(unset)* | When explicitly `true`: a single `netstandard2.0` target, `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `Deterministic=true`, extended analyzer rules, SourceLink with `EmbedUntrackedSources=true`, no dependency file, telemetry exclusion, and `PrivateAssets=all` on `Microsoft.CodeAnalysis.*` references |
| `IsRoslynComponentOnly` | `true` for Roslyn components | Analyzer-only package: `IncludeBuildOutput=false`, `IncludeSymbols=false`, portable PDB packed beside the analyzer under `analyzers/dotnet/cs/`. Set `false` for a dual-role component using normal library symbol packaging |
| `DisableRoslynCompilerDefaultsValidation` | `false` | Opts out of `ValidateRoslynComponentCompilerSettings`, which fails the pack of a packable Roslyn component missing `LangVersion`, `Nullable`, `TreatWarningsAsErrors` or `EnforceExtendedAnalyzerRules` |
| `PackProjectReferencedSourceGenerators` | `true` | Packable projects include analyzer `ProjectReference` outputs under `analyzers/dotnet/cs/`. Use `Pack="false"` on one reference to opt that one out |

Overrides are layered, narrowest first: an explicit `TargetFramework`/`TargetFrameworks` in the project
always wins, then a `PurviewTargetFrameworkSet` in the project, then a set the repository selected, then
a `PurviewTargetFrameworks<Set>` redefinition, then the SDK default. Roslyn components are not covered —
they target `netstandard2.0`; selecting a set on one is reported as `PurviewTargetFrameworkSetIgnored`.

## Style policy

| Property | Default | Behaviour |
| -- | -- | -- |
| `DisablePurviewStylePolicyValidation` | `false` | Stops the build failing (`PRSGD0006`–`PRSGD0009`) when the repository `.editorconfig` overrides the modifier policy — `dotnet_style_require_accessibility_modifiers` other than `omit_if_default`, hiding `IDE0040`/`IDE1006`, disabling the Style category in bulk, weakening the `_camelCase` private-instance-field rule, or adding the accessibility rules to `NoWarn`. Entries the SDK injects itself are ignored |
| `PurviewTestContextNoWarn` | `CA1002;CA1012;CA1034;CA1047;CA1050;CA1051;CA1062;CA1064;CA1515;CA1707` | The production API-surface rules exempted in test and shared-testing projects. Test projects keep the strict style contract (`IDE0040`, field naming, formatting, `IDE1006`) but are context aware: public test classes, `Method_Scenario_Expectation` names, exposed fields and unvalidated helper parameters are allowed. Set before the SDK import |
| `DisablePurviewTestContextRuleSet` | `false` | Makes test and shared-testing projects enforce the production API-surface rules too |
| `PurviewPolicyExemptNoWarn` | *(derived)* | The SDK-injected `NoWarn` entries `ValidatePurviewStylePolicy` accepts |

## .NET 11 features and ServerShared

| Property | Default | Behaviour |
| -- | -- | -- |
| `MemorySafetyRules` | `0` | C# 15 memory-safety tier on `net11.0`+ targets: `0` off (default), `1` preview pointer syntax (sets `LangVersion=preview` when none was chosen), `2` `updated-memory-safety-rules`. Tier `2` is opt-in — the updated rules are an experimental compiler feature. Unknown values fail with `PRCFG0001`. Never touches `AllowUnsafeBlocks` |
| `PurviewRuntimeAsync` | *(unset, treated as false)* | Set to `true` for `runtime-async=on` on `net11.0`+ targets. `UseRuntimeAsync=true` also opts in; an explicit `false` under either name keeps it off |
| `PurviewMultiThreadedBuild` | `true` | Bootstraps a managed `Directory.Build.rsp` with `-mt`/`-nr:false`; `false` writes `-mt:false`. Unset, a Blazor WebAssembly client writes `-mt:false` for the repository, and a client built under `-mt` anyway fails with `PRBLZ0002`; an explicit value always wins |
| `BootstrapMultiThreadedBuildRspToRepoRoot` | `true` | Whether that bootstrapped response file is written at all |
| `IsClientSideProject` | *(computed)* | `true` for a WebAssembly client, `PurviewBrowserSafe` library, Razor class library, or `*.Client`/`*.ClientShared`/`*.Browser`/`*.Wasm`/`*.WebAssembly`/`*.Components` (or `ClientShared`/`Components`) name. Explicit value wins |
| `PurviewAutoServerSharedReference` | `true` | Adds `../ServerShared*/ServerShared*.csproj` to a non-client-side project |
| `PurviewServerSharedReferenceCheckAsError` | `false` | Makes `PRSHR0001`/`PRSHR0002` errors |
| `DisablePurviewServerSharedReferenceCheck` | `false` | Turns `PRSHR0001`/`PRSHR0002` off |

## Testing

| Property | Default | Behaviour |
| -- | -- | -- |
| `TestingFramework` | `TUnit` | `TUnit`, `Xunit`, `None` |
| `SubstituteFramework` | `TUnitMocks` | `TUnitMocks`, `NSubstitute`, `None` |
| `TestDataFramework` | `Bogus` | `Bogus`, `None` |
| `CollectCoverage` | `true` for detected test projects | |
| `EnableStaticNativeInstrumentation` | `false` for test projects | |
| `EnableDynamicNativeInstrumentation` | `false` for test projects | |
| `TestingPlatformDotnetTestSupport`, `UseMicrosoftTestingPlatformRunner`, `EnableMicrosoftTestingPlatform` | enabled for TUnit test projects | Microsoft.Testing.Platform wiring |
| `PurviewSharedTestingOutputType` | `Library` | Forced onto `IsSharedTestingProject` projects, with `IsTestProject`/`IsTestingPlatformApplication` cleared, because the test packages otherwise flip them into an executable test host. Set `Exe` (in the project or `Directory.Build.props`) to keep the package-driven shape |

## Packaging

| Property | Default | Behaviour |
| -- | -- | -- |
| `IsPackable` | `false` | |
| `PurviewAutoSdkPack` | `true` | Packs `Sdk/**/*` into the package at the right root-level paths |
| `PurviewSkillsSourceFolder` | `<repo root>/skills` | Where authored skills live |

Packable-project defaults, applied only when the project supplied nothing:
`GenerateDocumentationFile=true`, `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`,
`PublishRepositoryUrl=true`, `EmbedUntrackedSources=true`, `DebugType=portable`. Portable PDBs ship
in the `.snupkg`; the `.nupkg` gets none unless the project opts in. Roslyn-component-only packages
instead default `IncludeSymbols=false` and ship their PDB inside `analyzers/dotnet/cs/`.

Unless `PackageReadmeFile` is configured, a README is packed and registered automatically: the project's
own `README.md` (beside its `.csproj`) wins, else the repository-root one. A README-named file already
being packed, such as an `Sdk/README.md`, is used instead.

Non-packable projects, including web apps, default `WarnOnPackingNonPackableProject=false` so a
solution-wide pack skips them silently.

`ContinuousIntegrationBuild` is set only by real CI environment variables. Packability alone never
forces SourceLink's CI-mode dirty-repository checks.

## Telemetry and SourceLink

| Property | Default | Behaviour |
| -- | -- | -- |
| `ExcludePurviewTelemetry` | `false` | Removes `Purview.Telemetry.SourceGenerator` |
| `ExcludeMSTelemetryExtension` | `false` | Removes `Microsoft.Extensions.Telemetry.Abstractions`. Only relevant when `ExcludePurviewTelemetry` is also `false` — the whole group is skipped otherwise |
| `SourceLinkPackageName` | `Microsoft.SourceLink.GitHub` | The provider package, added with `PrivateAssets=all` to every non-test project. Set it in `Directory.Build.props` to match the host — `Microsoft.SourceLink.AzureRepos.Git` (Azure DevOps Services), `Microsoft.SourceLink.AzureDevOpsServer.Git` (on-premises ADO Server / TFS), `Microsoft.SourceLink.GitLab`, `Microsoft.SourceLink.Bitbucket.Git` |
| `DisableSourceLink` | `false` | `true` stops the SDK adding the package at all. An empty `SourceLinkPackageName` does **not** opt out — the GitHub default fills it |
| `EnableSourceLink` | `true` (set by the provider) | The provider package's own switch; `false` keeps the package but skips the source mapping |

Both are read only on the package reference the SDK adds, which MSBuild evaluates after the project body,
so a `.csproj` value works too; `Directory.Build.props` keeps every project on one provider. See
[sourcelink.md](sourcelink.md) for the host matrix, Azure DevOps specifics, the CPM `PackageVersion`
requirement and self-hosted hosts.

## Blazor WebAssembly, browser-safe libraries and BFF hosts

| Property | Default | Behaviour |
| -- | -- | -- |
| `IsBffProject` | `true` for a web SDK project named `*Bff`, else `false` | Classifies a BFF host. `false` opts a `*Bff` project out; `true` opts another web project in |
| `IsBlazorHostProject` | *(computed)* | `true` for a web SDK project that references a WebAssembly client. Set `false` to opt out. Never implies `IsBffProject` |
| `PurviewAutoBlazorClientReference` | `true` | A host named `<Name>` references its `<Name>.Client` WebAssembly client automatically. Skipped when the reference is already declared |
| `DisablePurviewBlazorWebAssemblyDefaults` | `false` | Keeps the generic defaults on a WebAssembly client: packable when asked, Dockerfile container detection, the automatic `Shared` reference. Set before the SDK import |
| `PurviewBrowserSafe` | *(unset)* | `true` on a class library applies `IsAotCompatible`, `SupportedPlatform browser` and the browser-safe `WarningsAsErrors`, each only where the project set nothing |
| `PurviewBrowserSafeWarningsAsErrors` | `true` on a `PurviewBrowserSafe` library, otherwise unset | `false` hands the trim and AOT diagnostics back to `TreatWarningsAsErrors` (`true` by default); `CA1416` then falls back to the repository's severity (`suggestion` in the shipped `.editorconfig`) |
| `PurviewBrowserSafeWarningCodes` | `CA1416`, `IL2001`-`IL2128`, `IL3000`-`IL3005`, `IL3050`-`IL3059` | The ids a browser-safe library adds to `WarningsAsErrors` |
| `DisablePurviewBrowserSafeReferenceCheck` | `false` | Turns off `PRBLZ0001`, the check that every project a client (or browser-safe library) references sets `PurviewBrowserSafe` |
| `PurviewBrowserSafeReferenceCheckAsError` | `false` | Reports `PRBLZ0001` as an error |
| `PurviewBffExposesApi` | `true` when the BFF references `Microsoft.AspNetCore.OpenApi`, otherwise unset | Whether a BFF keeps the `Microsoft.AspNetCore.OpenApi.Generated` interceptor namespace |

`PurviewBrowserSafe="true"` is also accepted as `ProjectReference` metadata, recording that a
reference was judged browser-safe when it cannot say so itself.

## Version detection

| Property | Default | Behaviour |
| -- | -- | -- |
| `UsePackageJsonVersion` | `true` | `true`, `false`, `Strict` |
| `RootPackageJson` | *(discovered)* | Explicit path to the `package.json` to read. A relative path resolves from the project directory; in `Directory.Build.props` use `$(MSBuildThisFileDirectory)package.json` |
| `EnableVersionDetectionCache` | `true` | |
| `VersionDetectionCacheFile` | *(derived)* | |
| `VersionDetectionLogEnabled` | `false` | Logs the detected version |

## Repository bootstrap

| Property | Default | Behaviour |
| -- | -- | -- |
| `DisableAutoCopySdkFiles` | `false` | Master switch for SDK-managed repository file writes |
| `PurviewRepoBootstrapMode` | `IfMissing` | Also `Always`, `WarnOnDrift`, `Never` |
| `BootstrapEditorConfigToRepoRoot` | `true` | |
| `RepositoryEditorConfigFilePath` | *(derived)* | Overrides the destination `.editorconfig` |
| `BootstrapGlobalJsonToRepoRoot` | `true` | |
| `RepositoryGlobalJsonFilePath` | *(derived)* | Overrides the destination `global.json` |
| `BootstrapMultiThreadedBuildRspToRepoRoot` | `true` | Creates a managed `Directory.Build.rsp` enabling `-mt` |
| `RepositoryDirectoryBuildRspFilePath` | *(derived)* | Overrides the destination `Directory.Build.rsp` |
| `PurviewMultiThreadedBuild` | `true` | `false` rewrites a managed rsp to `-mt:false`; unset, a WebAssembly client writes `-mt:false` |
| `PurviewBuildSdkVersionForGlobalJson` | restored package version, else the version it was packed as | |
| `PurviewRepoBootstrapCopyRetries` | `3` | |
| `PurviewRepoBootstrapCopyRetryDelayMilliseconds` | `500` | |
| `PurviewRepoBootstrapCopyFailureAsError` | `true` | `false` downgrades a persistent write failure to a warning |
| `PurviewSuppressCopyRetryWarnings` | `true` | Demotes the built-in copy task's `MSB3026` retry notice to a message |
| `PurviewRepositoryRootMarkers` | `.git/;.github/;.gitattributes;LICENSE.md;AGENTS.md;.agents/;CLAUDE.md;.claude/;package.json` | Ordered markers identifying a repository root, most authoritative first |

There is no agent-folder mirror. `EnableAgentFolderInPackage`, `AgentPackDestinationFolder`,
`PurviewAgentFolderSourcePath`, `PurviewAgentFolderCopy*`, `PurviewAgentSyncManifestPath` and
`DisableMirroredFileTrackingCheck` were removed: skills are installed by the skills CLI, and
nothing else was left for the mirror to deliver.

## Skills

Covered in full by `purview-buildsdk-skill-installation`: `PurviewSkillsRunner`,
`PurviewSkillsRunnerCandidates`, `PurviewSkillsCommand`, `PurviewSkillsPackage`,
`PurviewSkillsAgents`, `PurviewSkillsSource`, `PurviewSkillsInstallRoot`, `PurviewSkillsRepoRoot`,
`PurviewSkillsDryRun`, and the `PurviewSkillsAgentDirectory` item group.

## Compiler-visible properties

The SDK lists these as `CompilerVisibleProperty` items, so Roslyn analyzers and source generators read
them as `build_property.<PropertyName>`:

```csharp
context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.IsBffProject", out var isBff);
```

Read the exported property rather than re-deriving SDK behaviour — it is the SDK's own answer and stays
correct when the rules change. To make a new property visible, add a `CompilerVisibleProperty` item
(with a `<Description>`) in `Sdk.props`; nothing else exposes an MSBuild property to the compiler.

Among the exported properties (the full list, with descriptions, is the *Compiler-visible SDK properties*
table in the [Configuration Reference](https://github.com/purview-dev/build-sdk/blob/main/docs/wiki/Configuration-Reference.md#compiler-visible-sdk-properties)):

- `UsePackageJsonVersion`, `RootPackageJson`, `RepoRoot`, `Version`, `PackageVersion`
- `NamespacePrefix`, `DisableNamespacePrefixCheck`
- `TestingFramework`, `SubstituteFramework`, `TestDataFramework`
- `ExcludePurviewTelemetry`, `ExcludeMSTelemetryExtension`
- `EnableAssemblyNameGeneration`, `DisableAutoInternalsVisibleTo`, `DisableGenerateAssemblyInfoClass`,
  `DisableGenerateAssemblyIdentifierClass`, `AssemblyIdentifierClassName`
- `IsCSharpProject`, `IsTestProject`, `IsSharedTestingProject`, `IsSharedProject`,
  `IsServerSharedProject`, `IsClientSideProject`
- `PurviewAutoServerSharedReference`, `DisablePurviewServerSharedReferenceCheck`,
  `PurviewServerSharedReferenceCheckAsError`
- `MemorySafetyRules`, `PurviewRuntimeAsync`, `PurviewMultiThreadedBuild`
- `TestingType`, `TargetProjectName`
- `IsContainerProject`, `IsSdkProject`, `SdkProjectName`, `IsWebProject`, `IsWebSdkProject`,
  `IsWorkerSdkProject`, `IsAspireHostProject`, `IsCLIProject`
- `IsBlazorWebAssemblyProject`, `IsRazorClassLibraryProject`, `IsBlazorHostProject`, `IsBffProject`
- `PurviewBrowserSafe`, `PurviewBrowserSafeWarningsAsErrors`, `DisablePurviewBrowserSafeReferenceCheck`,
  `PurviewBrowserSafeReferenceCheckAsError`, `DisablePurviewBlazorWebAssemblyDefaults`,
  `PurviewAutoBlazorClientReference`, `PurviewBffExposesApi`
- `EditorConfigFilePath`, `RepositoryEditorConfigFilePath`, `BootstrapEditorConfigToRepoRoot`
- `RepositoryGlobalJsonFilePath`, `BootstrapGlobalJsonToRepoRoot`, `DisableAutoCopySdkFiles`
- `PurviewRepoBootstrapMode`, `PurviewRepoBootstrapCopyRetries`,
  `PurviewRepoBootstrapCopyRetryDelayMilliseconds`, `PurviewRepoBootstrapCopyFailureAsError`
- `PurviewSuppressCopyRetryWarnings`
- `PurviewSkillsSourceFolder`,
  `PurviewSkillsAgents`, `PurviewSkillsInstallRoot`, `PurviewRepositoryRootMarkers`,
  `PurviewSkillsRunner`, `PurviewSkillsDryRun`
- `PurviewBuildSdkVersionForGlobalJson`, `CurrentYear`, `AutoGeneratedAssemblyInfoFile`,
  `AutoGeneratedAssemblyIdentifierFile`
