---
name: sdk-project-behavior-and-detection
description: "Use when explaining why Purview.DotNetProjectSdk classified a project as test, shared, CLI, web, Aspire host, or container, or when reasoning about auto-added packages, project references, namespaces, and naming conventions."
---

# Purview.DotNetProjectSdk project behavior and detection

Use this skill when a task asks **why** the SDK applied a behavior automatically, or when adding/moving projects in a repo that relies on Purview’s naming and project-type inference.

## Project-type detection rules

The SDK infers behavior from project names, project contents, and SDK declarations.

### Test detection

A project is treated as a test project when its name ends with `*Test` or `*Tests` and the suffix before `Test(s)` matches a supported testing type such as:

- `Unit`
- `Integration`
- `E2E`
- `EndToEnd`
- `Acceptance`
- `Functional`
- `Performance`
- `Load`
- `Smoke`
- `Stress`
- `Regression`
- `Security`
- `Chaos`
- `Scenario`
- `System`
- `Threat`
- `BlackBox`
- `WhiteBox`
- `Accessibility`
- `Interactive`
- `Environment`
- `Architecture`
- `Contract`

Derived properties:

- `IsTestProject=true`
- `TestingType=<detected suffix>`
- `PurviewTestType=<TestingType>Tests`
- `TargetProjectName=<project name with the test suffix removed>`

### Shared project detection

The SDK recognizes shared project names exactly. These are not generic substring matches.

Shared project names:

- `Shared`
- `SharedFramework`
- `SharedInfrastructure`
- `SharedInfra`
- `SharedUtilities`
- `SharedUtils`
- `SharedLibrary`
- `SharedLib`
- `SharedHelpers`

Shared testing project names:

- `SharedTestingFramework`
- `SharedTestingInfrastructure`
- `SharedTestingInfra`
- `SharedTestingUtilities`
- `SharedTestingUtils`
- `SharedTestingLibrary`
- `SharedTestingLib`
- `SharedTestingHelpers`

Derived flags:

- `IsSharedProject`
- `IsSharedTestingProject`

### SDK/content-based detection

- `IsSdkProject` / `SdkProjectName` come from parsing the project/import `Sdk="..."` declaration
- `IsWebSdkProject=true` for `Microsoft.NET.Sdk.Web`
- `IsWorkerSdkProject=true` for `Microsoft.NET.Sdk.Worker`
- `IsAspireHostProject=true` when the SDK starts with `Aspire.Sdk.Host` or `Aspire.AppHost.Sdk`
- `IsContainerProject=true` when `Dockerfile`, `dockerfile`, or `Dockerfile.dev` exists in the project directory
- `IsCLIProject=true` when the project name ends with `CLI`, `Console`, `CommandLine`, `QuickStart`, or `QuickStarts`

## Namespace and identity behavior

The SDK derives the project identity from `NamespacePrefix` and the project name.

Key behavior:

1. `PurviewLogicalProjectName` is built from `NamespacePrefix` plus the project name, with deduplication when the project name already starts with the namespace tail.
2. `RootNamespace` defaults to `PurviewLogicalProjectName`.
3. Known suffixes are stripped from `RootNamespace`, including shared/shared-testing names and common segments like `Core`, `EF`, `Shared`, `ClientShared`, and `ServiceDefaults`.
4. Test suffixes are removed from `RootNamespace`, so `Acme.Api.UnitTests` still maps back to `Acme.Api`.
5. `AssemblyName` and `PackageId` default to the fully evaluated `RootNamespace` (the canonical default public name). Test/shared-testing projects keep their detected suffix in `AssemblyName`/`PackageId` so test assemblies stay distinct. Explicit `AssemblyName`/`PackageId` values always win.
6. The naming defaults are applied during `Sdk.props` evaluation (before the Microsoft SDK computes `TargetName`), so the compiled output name always matches `AssemblyName`.

Do not hand-author alternate namespace conventions unless the repository explicitly opts out of the SDK defaults.

## Automatic project references

The SDK adds project references based on layout conventions.

### Non-test projects

For ordinary non-test, non-shared projects, it automatically looks for sibling shared projects:

- `../Shared*/Shared*.csproj`

It also removes accidental self/shared-testing matches.

### Test projects

For detected test projects, it attempts these target-project paths in order when they exist:

- `../$(TargetProjectName)/$(TargetProjectName).csproj`
- `../../$(TargetProjectName)/$(TargetProjectName).csproj`
- `../src/$(TargetProjectName)/$(TargetProjectName).csproj`
- `../../src/$(TargetProjectName)/$(TargetProjectName).csproj`

It also adds sibling shared-testing project references via:

- `../SharedTesting*/SharedTesting*.csproj`

This is why consistent naming and placement matter so much in repos that use the SDK.

## Automatic framework/package behavior

### For non-test C# projects

- Adds SourceLink unless `DisableSourceLink=true`
- Adds Purview telemetry packages unless `ExcludePurviewTelemetry=true`
- Generates documentation files (`GenerateDocumentationFile=true`) unless explicitly disabled
- Generates `InternalsVisibleTo` attributes unless `DisableAutoInternalsVisibleTo=true`

### For packable projects

- Defaults `GenerateDocumentationFile`, `IncludeSymbols`, `SymbolPackageFormat=snupkg`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, `IncludeSource`, and `DebugType=portable` — only when the consuming project has not supplied a value
- Delivers portable PDBs via the `.snupkg`; the normal `.nupkg` does not receive PDBs unless the project opts in explicitly
- Packs the repository-root `README.md` (registered via `PackageReadmeFile`) when the file exists and `PackageReadmeFile` is unset; skips when a README is already being packed
- Non-packable projects (including web apps) default `WarnOnPackingNonPackableProject=false` so solution-wide pack operations skip them silently

### For test and shared-testing projects

- Applies test-friendly `NoWarn` defaults
- Marks projects as not packable/publishable
- Adds substitute/test-data/testing packages based on `SubstituteFramework`, `TestDataFramework`, and `TestingFramework`
- For TUnit test projects, enables Microsoft.Testing.Platform integration properties automatically
- For shared-testing projects, skips the runnable test package and marks them with a skip/category pattern appropriate to the selected test framework

### For special project types

- CLI projects default to `OutputType=Exe` and include `appsettings*.json` as content
- Container projects enable `InvariantGlobalization`, `PublishAot`, Linux Docker defaults, and container tooling package references
- Web SDK projects get `Microsoft.AspNetCore.OpenApi.Generated` added to `InterceptorsNamespaces` unless marked as a separate web-project mode
- Aspire host projects default to `OutputType=Exe`

## How to reason about surprising behavior

If the SDK “did something unexpected”, inspect these values first:

- `MSBuildProjectName`
- `NamespacePrefix`
- `PurviewLogicalProjectName`
- `RootNamespace`
- `TestingType`
- `TargetProjectName`
- `SdkProjectName`
- `IsTestProject`
- `IsSharedProject`
- `IsSharedTestingProject`
- `IsContainerProject`
- `IsCLIProject`
- `IsWebSdkProject`
- `IsAspireHostProject`

Prefer explaining behavior from these computed properties rather than from assumptions about folder names alone.

## Guidance for structural changes

When adding or moving projects in a repo using this SDK:

1. Keep the `.csproj` filename equal to its containing directory name unless the repo explicitly disables that validation.
2. Preserve established `src/` and `tests/`-style layouts whenever possible.
3. Use test project suffixes intentionally so auto-detection and auto-references work.
4. Keep shared helpers in exact shared/shared-testing names if you want the corresponding SDK behavior.
5. If you change a naming rule in the SDK, update the README and the shipped skills together.
