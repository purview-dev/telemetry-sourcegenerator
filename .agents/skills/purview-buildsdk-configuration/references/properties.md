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
| `DisableGenerateAssemblyInfoClass` | `false` | Drops the generated `AssemblyInfo` |
| `DisableAutoInternalsVisibleTo` | `false` | Drops automatic friend-assembly generation |
| `DisableProjectFileNamingConventionCheck` | `false` | Stops validating that the `.csproj` filename matches its directory |
| `AutoIncludeUsings` | `true` | Controls the SDK-added global usings. Set before the SDK import |

## Target framework and language

| Property | Default | Behaviour |
| -- | -- | -- |
| `TargetFramework` | `net10.0` | Applied when neither `TargetFramework` nor `TargetFrameworks` is set. A project that explicitly declares `IsRoslynComponent=true` defaults to `netstandard2.0` |
| `IsRoslynComponent` | *(unset)* | When explicitly `true`: a single `netstandard2.0` target, `LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=true`, `Deterministic=true`, extended analyzer rules, SourceLink with `EmbedUntrackedSources=true`, no dependency file, telemetry exclusion, and `PrivateAssets=all` on `Microsoft.CodeAnalysis.*` references |
| `IsRoslynComponentOnly` | `true` for Roslyn components | Analyzer-only package: `IncludeBuildOutput=false`, `IncludeSymbols=false`, portable PDB packed beside the analyzer under `analyzers/dotnet/cs/`. Set `false` for a dual-role component using normal library symbol packaging |
| `DisableRoslynCompilerDefaultsValidation` | `false` | Opts out of `ValidateRoslynComponentCompilerSettings`, which fails the pack of a packable Roslyn component missing `LangVersion`, `Nullable`, `TreatWarningsAsErrors` or `EnforceExtendedAnalyzerRules` |
| `PackProjectReferencedSourceGenerators` | `true` | Packable projects include analyzer `ProjectReference` outputs under `analyzers/dotnet/cs/`. Use `Pack="false"` on one reference to opt that one out |

## Style policy

| Property | Default | Behaviour |
| -- | -- | -- |
| `DisablePurviewStylePolicyValidation` | `false` | Stops the build failing (`PRSGD0006`–`PRSGD0009`) when the repository `.editorconfig` overrides the modifier policy — `dotnet_style_require_accessibility_modifiers` other than `omit_if_default`, hiding `IDE0040`/`IDE1006`, disabling the Style category in bulk, weakening the `_camelCase` private-instance-field rule, or adding the accessibility rules to `NoWarn`. Entries the SDK injects itself are ignored |
| `PurviewTestContextNoWarn` | `CA1002;CA1012;CA1034;CA1047;CA1050;CA1051;CA1062;CA1064;CA1515;CA1707` | The production API-surface rules exempted in test and shared-testing projects. Test projects keep the strict style contract (`IDE0040`, field naming, formatting, `IDE1006`) but are context aware: public test classes, `Method_Scenario_Expectation` names, exposed fields and unvalidated helper parameters are allowed. Set before the SDK import |
| `DisablePurviewTestContextRuleSet` | `false` | Makes test and shared-testing projects enforce the production API-surface rules too |
| `PurviewPolicyExemptNoWarn` | *(derived)* | The SDK-injected `NoWarn` entries `ValidatePurviewStylePolicy` accepts |

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
| `PurviewSharedTestingOutputType` | `Library` | Forced onto `IsSharedTestingProject` projects, with `IsTestProject`/`IsTestingPlatformApplication` cleared, because the test packages otherwise flip them into an executable test host. Set `Exe` before the SDK import to keep the package-driven shape |

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

When the repository root is discoverable, the root `README.md` is packed automatically and
registered as `PackageReadmeFile`, unless one is configured already.

Non-packable projects, including web apps, default `WarnOnPackingNonPackableProject=false` so a
solution-wide pack skips them silently.

`ContinuousIntegrationBuild` is set only by real CI environment variables. Packability alone never
forces SourceLink's CI-mode dirty-repository checks.

## Telemetry and SourceLink

| Property | Default | Behaviour |
| -- | -- | -- |
| `ExcludePurviewTelemetry` | `false` | Removes `Purview.Telemetry.SourceGenerator` |
| `ExcludeMSTelemetryExtension` | `false` | Removes `Microsoft.Extensions.Telemetry.Abstractions`. Only relevant when `ExcludePurviewTelemetry` is also `false` — the whole group is skipped otherwise |
| `SourceLinkPackageName` | `Microsoft.SourceLink.GitHub` | |
| `DisableSourceLink` | `false` | |

## Blazor WebAssembly, browser-safe libraries and BFF hosts

| Property | Default | Behaviour |
| -- | -- | -- |
| `IsBffProject` | `true` for a web SDK project named `*Bff`, else `false` | Classifies a BFF host. `false` opts a `*Bff` project out; `true` opts another web project in |
| `IsBlazorHostProject` | *(computed)* | `true` for a web SDK project that references a WebAssembly client. Set `false` to opt out. Never implies `IsBffProject` |
| `PurviewAutoBlazorClientReference` | `true` | A host named `<Name>` references its `<Name>.Client` WebAssembly client automatically. Skipped when the reference is already declared |
| `DisablePurviewBlazorWebAssemblyDefaults` | `false` | Keeps the generic defaults on a WebAssembly client: packable when asked, Dockerfile container detection, the automatic `Shared` reference. Set before the SDK import |
| `PurviewBrowserSafe` | *(unset)* | `true` on a class library applies `IsAotCompatible`, `SupportedPlatform browser` and the browser-safe `WarningsAsErrors`, each only where the project set nothing |
| `PurviewBrowserSafeWarningsAsErrors` | `true` | `false` keeps the trim and AOT diagnostics as warnings; `CA1416` then falls back to the repository's severity (`suggestion` in the shipped `.editorconfig`) |
| `PurviewBrowserSafeWarningCodes` | `CA1416`, `IL2001`-`IL2128`, `IL3000`-`IL3005`, `IL3050`-`IL3059` | The ids a browser-safe library adds to `WarningsAsErrors` |
| `DisablePurviewBrowserSafeReferenceCheck` | `false` | Turns off `PRBLZ0001`, the check that every project a client (or browser-safe library) references sets `PurviewBrowserSafe` |
| `PurviewBrowserSafeReferenceCheckAsError` | `false` | Reports `PRBLZ0001` as an error |
| `PurviewBffExposesApi` | `true` when the BFF references `Microsoft.AspNetCore.OpenApi`, else `false` | Whether a BFF keeps the `Microsoft.AspNetCore.OpenApi.Generated` interceptor namespace |

`PurviewBrowserSafe="true"` is also accepted as `ProjectReference` metadata, recording that a
reference was judged browser-safe when it cannot say so itself.

## Version detection

| Property | Default | Behaviour |
| -- | -- | -- |
| `UsePackageJsonVersion` | `true` | `true`, `false`, `Strict` |
| `RootPackageJson` | *(discovered)* | Explicit path to the `package.json` to read |
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
`EnablePurviewSkillRecommendations`, `PurviewSkillRecommendationsInCI`.

## Compiler-visible properties

Exported to analyzers and generators as `build_property.<PropertyName>`:

- `UsePackageJsonVersion`, `RootPackageJson`, `RepoRoot`, `Version`, `PackageVersion`
- `NamespacePrefix`, `DisableNamespacePrefixCheck`
- `TestingFramework`, `SubstituteFramework`, `TestDataFramework`
- `ExcludePurviewTelemetry`, `ExcludeMSTelemetryExtension`
- `EnableAssemblyNameGeneration`, `DisableAutoInternalsVisibleTo`, `DisableGenerateAssemblyInfoClass`,
  `DisableGenerateAssemblyIdentifierClass`, `AssemblyIdentifierClassName`
- `IsCSharpProject`, `IsTestProject`, `IsSharedTestingProject`, `IsSharedProject`
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
  `PurviewSkillsRunner`
- `PurviewBuildSdkVersionForGlobalJson`, `CurrentYear`, `AutoGeneratedAssemblyInfoFile`,
  `AutoGeneratedAssemblyIdentifierFile`
