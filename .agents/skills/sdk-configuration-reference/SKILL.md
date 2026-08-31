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
- `VersionDetectionLogEnabled` — default `false`; set to `true` to log the detected package version

Behavior rules:

1. If `RootPackageJson` is set, the SDK uses that path.
2. Otherwise it tries to discover the repo root from CI variables, `.git`, or a nearby `package.json`.
3. When version detection succeeds, both `Version` and `PackageVersion` are set from the `version` field.
4. `UsePackageJsonVersion=Strict` should be treated as “fail if discovery/resolution cannot succeed”.

## Core identity and build settings

These are the most important configurable properties exposed by the SDK:

- `NamespacePrefix` — required unless `DisableNamespacePrefixCheck=true`
- `DisableNamespacePrefixCheck` — default `false`
- `TargetFramework` — defaults to `net10.0` when neither `TargetFramework` nor `TargetFrameworks` is set; projects explicitly declaring `IsRoslynComponent=true` default to `netstandard2.0`
- `IsRoslynComponent` — when explicitly `true`, applies source-generator defaults: a single `netstandard2.0` target, extended analyzer rules, disabled SourceLink and untracked-source embedding, no dependency file, compiler-generated output under the framework-specific intermediate directory, `symbols.nupkg`, `PackSourceGeneratorSymbols`, telemetry exclusion, and excluded normal build output
- `PackProjectReferencedSourceGenerators` — default `true`; packable projects automatically include analyzer `ProjectReference` outputs and runtime dependencies under `analyzers/dotnet/cs/`. Set it to `false` globally or use `Pack="false"` on one analyzer reference to opt out.
- `EnableAssemblyNameGeneration` — default `false`; when `true`, `AssemblyName` and default `PackageId` follow the logical project name
- `DisableProjectFileNamingConventionCheck` — default `false`; disables the directory-name/file-name match validation
- `DisableGenerateAssemblyInfoClass` — default `false`; disables generated `AssemblyInfo`
- `DisableAutoInternalsVisibleTo` — default `false`; disables automatic friend assembly generation
- `AutoIncludeUsings` — default `true`; controls SDK-added global usings
- `SourceLinkPackageName` — default `Microsoft.SourceLink.GitHub`
- `DisableSourceLink` — default `false`

## Telemetry and package-related settings

- `ExcludePurviewTelemetry` — default `false`; removes `Purview.Telemetry.SourceGenerator`
- `ExcludeMSTelemetryExtension` — default `false`; removes `Microsoft.Extensions.Telemetry.Abstractions`, only relevant if `ExcludePurviewTelemetry` is also `true`
- `IsPackable` — defaults to `false` if not set elsewhere
- `PackageTags`, `IncludeSource`, `IncludeSymbols`, `PublishRepositoryUrl`, `SymbolPackageFormat` — standard pack-related settings the SDK participates in for packable projects

## Test framework settings

The SDK supports opinionated testing defaults and validation.

Primary settings:

- `TestingFramework` — default `TUnit`; supported values: `TUnit`, `Xunit`, `None`
- `SubstituteFramework` — default `TUnitMocks`; supported values: `TUnitMocks`, `NSubstitute`, `None`
- `TestDataFramework` — default `Bogus`; supported values: `Bogus`, `None`

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
- `PurviewAutoSdkPack` — default `true`; when `true`, automatically packs the `Sdk/` folder contents into the NuGet package with the correct root-level paths
- `EnableAgentFolderInPackage` — default `true`; copies the bundled `.agents/**` folder from the SDK NuGet package into the consuming repo’s `.agents/`
- `AgentPackDestinationFolder` — default `.agents`; repo-relative destination folder that receives copied agent content as `$(AgentPackDestinationFolder)/**`

**Hard requirement:** This SDK must pack the contents of `Sdk/` into the NuGet package so that downstream consumers of `Purview.DotNetProjectSdk` receive the same `Sdk/**` files. The `PurviewAutoSdkPack` feature (default `true`) is the mechanism that delivers this for standard consuming projects. When a project is packable, the SDK automatically adds `Sdk/**/*` as package content with the correct root-level paths:

- `Sdk/.agents/**` → `.agents/**`
- `Sdk/.github/**` → `.github/**`
- `Sdk/build/**` → `build/**`
- `Sdk/buildTransitive/**` → `buildTransitive/**`
- `Sdk/buildMultiTargeting/**` → `buildMultiTargeting/**`
- `Sdk/*.md`, `Sdk/*.png`, `Sdk/*.jpg`, etc. → package root
- everything else under `Sdk/` → `Sdk/`

The SDK injects a `.gitignore` file into each second-level folder under `Sdk/.agents` during packaging with the following content:

```text[.gitignore]
# Ignore all files
*

# Don't ignore directories, so Git can traverse them
!*/

# Keep this file
!.gitignore
```

This lets consuming repos keep the agent folder structure discoverable while ignoring the copied content in Git.

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
