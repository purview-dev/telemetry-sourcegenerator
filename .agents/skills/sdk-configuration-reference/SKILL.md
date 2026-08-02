---
name: sdk-configuration-reference
description: "Use when configuring Purview.DotNetProjectSdk through Directory.Build.props or a .csproj, especially for NamespacePrefix, version detection, testing framework selection, telemetry, repo bootstrapping, and embedded agent-skill settings."
---

# Purview.DotNetProjectSdk configuration reference

Use this skill when a task asks what can be configured in `Purview.DotNetProjectSdk`, where a property must be set, or which defaults the SDK applies automatically.

## First rule: know where a property must be set

Set repo-wide bootstrap properties **before** importing the SDK in `Directory.Build.props` when the value must affect `Sdk.props` evaluation.

Common pre-import properties:

- `NamespacePrefix`
- `UsePackageJsonVersion`
- `RootPackageJson`
- Repo-wide testing framework selection properties when you want every project to inherit them

If a property changes behavior in `Sdk.targets` instead, it can usually be set later (for example in a project file), but prefer repo-wide defaults in `Directory.Build.props` unless the scenario is intentionally project-specific.

## Version detection settings

These properties control package/app version resolution from `package.json`:

- `UsePackageJsonVersion` — default `true`; supported values: `true`, `false`, `Strict`
- `RootPackageJson` — explicit path to the `package.json` to read
- `EnableVersionDetectionCache` — default `true`; enables local caching of resolved version data
- `VersionDetectionCacheFile` — optional explicit cache file path
- `VersionDetectionLogSessionId` — optional session identifier for trace/log correlation
- `VersionDetectionLogStampFile` — optional explicit stamp/log marker path

Behavior rules:

1. If `RootPackageJson` is set, the SDK uses that path.
2. Otherwise it tries to discover the repo root from CI variables, `.git`, or a nearby `package.json`.
3. When version detection succeeds, both `Version` and `PackageVersion` are set from the `version` field.
4. `UsePackageJsonVersion=Strict` should be treated as “fail if discovery/resolution cannot succeed”.

## Core identity and build settings

These are the most important configurable properties exposed by the SDK:

- `NamespacePrefix` — required unless `DisableNamespacePrefixCheck=true`
- `DisableNamespacePrefixCheck` — default `false`
- `TargetFramework` — defaults to `net10.0` when neither `TargetFramework` nor `TargetFrameworks` is set
- `EnableAssemblyNameGeneration` — default `false`; when `true`, `AssemblyName` and default `PackageId` follow the logical project name
- `DisableProjectFileNamingConventionCheck` — default `false`; disables the directory-name/file-name match validation
- `DisableGenerateAssemblyInfoClass` — default `false`; disables generated `AssemblyInfo`
- `DisableAutoInternalsVisibleTo` — default `false`; disables automatic friend assembly generation
- `AutoIncludeUsings` — default `true`; controls SDK-added global usings
- `SourceLinkPackageName` — default `Microsoft.SourceLink.GitHub`
- `DisableSourceLink` — default `false`

## Telemetry and package-related settings

- `ExcludePurviewTelemetry` — default `false`; removes `Purview.Telemetry.SourceGenerator`
- `ExcludeMSTelemetryExtension` — default `false`; removes `Microsoft.Extensions.Telemetry.Abstractions`
- `IsPackable` — defaults to `false` if not set elsewhere
- `PackageTags`, `IncludeSource`, `IncludeSymbols`, `PublishRepositoryUrl`, `SymbolPackageFormat` — standard pack-related settings the SDK participates in for packable projects

## Test framework settings

The SDK supports opinionated testing defaults and validation.

Primary settings:

- `TestingFramework` — default `TUnit`; supported values: `TUnit`, `Xunit`, `None`
- `SubstituteFramework` — default `TUnitMocks`; supported values: `TUnitMocks`, `NSubstitute`, `None`
- `TestDataFramework` — default `Bogus`; supported values: `Bogus`, `None`
- `ProjectSdkTestFramework` — legacy alias retained for compatibility

Related toggles and derived settings:

- `CollectCoverage` — defaults to `true` for detected test projects
- `EnableStaticNativeInstrumentation` — defaults to `false` for test projects
- `EnableDynamicNativeInstrumentation` — defaults to `false` for test projects
- `TestingPlatformDotnetTestSupport`, `UseMicrosoftTestingPlatformRunner`, `EnableMicrosoftTestingPlatform` — enabled automatically for TUnit test projects

## Repo bootstrap and developer-experience settings

These settings control the SDK’s repo-level helper file bootstrapping:

- `DisableAutoCopySdkFiles` — default `false`; master switch for SDK-managed repo file copying
- `BootstrapEditorConfigToRepoRoot` — default `true`
- `RepositoryEditorConfigFilePath` — optional override for the destination `.editorconfig`
- `BootstrapGlobalJsonToRepoRoot` — default `true`
- `RepositoryGlobalJsonFilePath` — optional override for the destination `global.json`
- `PurviewDotNetProjectSdkVersionForGlobalJson` — defaults to detected SDK package version, fallback `1.0.0`
- `EnableEmbeddedAgentSkills` — default `true`; copies bundled skills from `skills/**` into the consuming repo’s `.agents/skills/`
- `EnabledAgentFolderInPackage` — default `false`; when `true`, packs `$(ProjectAgentFolder)/skills/**` into the NuGet package as `skills/**`
- `ProjectAgentFolder` — default `ProjectAgent`; repo-relative root folder that contains a `skills/` subfolder
- `ProjectAgentDestinationFolder` — default `.agents`; repo-relative destination folder that receives copied skills as `$(ProjectAgentDestinationFolder)/skills/**`

## Important derived properties you can inspect

When explaining SDK behavior, prefer these derived values over guessing:

- `PurviewLogicalProjectName`
- `PurviewNamespacePrefix`
- `PurviewProjectShortName`
- `PurviewTestType`
- `RootNamespace`
- `AssemblyName`
- `PackageVersion`
- `TestingType`
- `TargetProjectName`
- `RepoRoot`
- `RootPackageJson`

## Compiler-visible properties

The SDK exports many properties for analyzers and source generators through `build_property.<PropertyName>`. When authoring analyzers or generators, prefer those exported properties instead of re-deriving SDK behavior manually.

Especially relevant exported properties include:

- `UsePackageJsonVersion`, `RootPackageJson`, `RepoRoot`, `Version`, `PackageVersion`
- `NamespacePrefix`, `DisableNamespacePrefixCheck`
- `TestingFramework`, `SubstituteFramework`, `TestDataFramework`
- `ExcludePurviewTelemetry`, `ExcludeMSTelemetryExtension`
- `EnableAssemblyNameGeneration`, `DisableAutoInternalsVisibleTo`, `DisableGenerateAssemblyInfoClass`
- `IsCSharpProject`, `IsTestProject`, `IsSharedTestingProject`, `IsSharedProject`
- `TestingType`, `TargetProjectName`
- `IsContainerProject`, `IsSdkProject`, `SdkProjectName`, `IsWebProject`, `IsWebSdkProject`, `IsWorkerSdkProject`, `IsAspireHostProject`, `IsCLIProject`
- `EditorConfigFilePath`, `RepositoryEditorConfigFilePath`, `BootstrapEditorConfigToRepoRoot`
- `RepositoryGlobalJsonFilePath`, `BootstrapGlobalJsonToRepoRoot`, `DisableAutoCopySdkFiles`
- `PurviewDotNetProjectSdkVersionForGlobalJson`, `CurrentYear`, `AutoGeneratedAssemblyInfoFile`

## Guidance for edits

When changing SDK configuration:

1. Preserve existing defaults unless the task explicitly changes product behavior.
2. Keep README, SDK property declarations, validation, and any shipped skills aligned.
3. If you add a new user-facing property, update both the configuration docs and the bundled skills.
4. If the property affects import-time behavior, document that it must be set before the SDK import.
