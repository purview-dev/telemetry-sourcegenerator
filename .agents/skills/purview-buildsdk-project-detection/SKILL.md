---
name: purview-buildsdk-project-detection
description: Explain why Purview.BuildSdk classified a project as test, shared, shared-testing, CLI, web, worker, Aspire host, container, Roslyn component, Blazor WebAssembly client, Razor class library, Blazor host or BFF, and why it added a package, a project reference, a namespace or an analyzer automatically. Use when SDK behaviour looks surprising, when a test project gets no runner or no InternalsVisibleTo, or when reasoning about RootNamespace, AssemblyName and TargetProjectName.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Why Purview.BuildSdk did that

Use this skill when the question is **why** the SDK applied something automatically.

For where a project should live, use `purview-buildsdk-project-layout`. For the property that turns
a behaviour off, use `purview-buildsdk-configuration`.

## Diagnose from computed properties, never from the folder name

Read what the SDK actually computed:

```bash
dotnet msbuild <project> -getProperty:PurviewLogicalProjectName -getProperty:RootNamespace \
  -getProperty:TestingType -getProperty:TargetProjectName -getProperty:IsTestProject
```

The values worth inspecting first: `MSBuildProjectName`, `NamespacePrefix`,
`PurviewLogicalProjectName`, `RootNamespace`, `TestingType`, `TargetProjectName`, `SdkProjectName`,
`IsTestProject`, `IsSharedProject`, `IsSharedTestingProject`, `IsContainerProject`, `IsCLIProject`,
`IsWebSdkProject`, `IsAspireHostProject`, `IsBlazorWebAssemblyProject`, `IsRazorClassLibraryProject`,
`IsBlazorHostProject`, `IsBffProject`.

## Detection rules

### Test projects

Name matches `^.*\.([\w|_]\w*)Tests?$` — a target name, a dot, then any word before `Test`/`Tests`.
The captured word becomes `TestingType`.

This is a pattern, **not** a lookup. `MyProject.GoldenTests` is detected exactly as readily as
`MyProject.UnitTests`, with `TestingType=Golden` and `Category=Golden`, even though `Golden` is not a
name the SDK ships.

**The dot is required.** A project named plainly `UnitTests` does not match, and silently gets no
runner, no coverage, no `InternalsVisibleTo` and no category. This is the single most common
surprise.

Derived: `IsTestProject=true`, `TestingType=<suffix>`, `PurviewTestType=<TestingType>Tests`,
`TargetProjectName=<name with the suffix removed>`.

The SDK's explicit `TestType` item group governs **`InternalsVisibleTo` generation**, not detection:
a non-test project grants access to `{AssemblyName}.{TestType}Tests` for each name in the group. A
suffix outside it still builds, runs and gets a category — it just cannot see `internal` members.
The 53 names are in [references/test-types.md](references/test-types.md).

### Shared and shared-testing projects

Recognised by **exact name**, never by substring. The two lists are in
[references/detection.md](references/detection.md). Derived: `IsSharedProject`,
`IsSharedTestingProject`.

### SDK and content based

| Signal | Result |
| -- | -- |
| the project's `Sdk="..."` declaration | `IsSdkProject`, `SdkProjectName` |
| `Microsoft.NET.Sdk.Web` | `IsWebSdkProject=true` |
| `Microsoft.NET.Sdk.Worker` | `IsWorkerSdkProject=true` |
| SDK starts with `Aspire.Sdk.Host` or `Aspire.AppHost.Sdk` | `IsAspireHostProject=true` |
| `Dockerfile`, `dockerfile` or `Dockerfile.dev` in the project directory | `IsContainerProject=true` |
| name ends with `CLI`, `Console`, `CommandLine`, `QuickStart`, `QuickStarts` | `IsCLIProject=true` |
| `Microsoft.NET.Sdk.BlazorWebAssembly` | `IsBlazorWebAssemblyProject=true` |
| `Microsoft.NET.Sdk.Razor` | `IsRazorClassLibraryProject=true` |
| a web SDK project whose name ends with `Bff` (case-sensitive, like `CLI`), or that sets `IsBffProject=true` | `IsBffProject=true` |
| a web SDK project that references an `IsBlazorWebAssemblyProject` project | `IsBlazorHostProject=true` |

`IsBffProject=false` opts a `*Bff` project out. `IsBlazorHostProject` is **never** inferred into
`IsBffProject`: a host that keeps no tokens is not a BFF, and BFF rules do not describe it.

`IsBlazorHostProject` is known at evaluation for the `<Name>`/`<Name>.Client` pairing. A host
referencing a client under any other name is resolved during the build, so ask for it with the
target that resolves it:

```bash
dotnet msbuild <host> -t:ResolvePurviewBlazorHostProject -getProperty:IsBlazorHostProject
```

What each Blazor and BFF classification applies - and the browser-safe library check - is in
[references/blazor.md](references/blazor.md).

## Namespace and identity

1. `PurviewLogicalProjectName` is `NamespacePrefix` plus the project name, de-duplicated when the
   project name already starts with the prefix's tail.
2. `RootNamespace` defaults to `PurviewLogicalProjectName`.
3. Known suffixes are stripped from `RootNamespace` — shared and shared-testing names, and segments
   such as `Core`, `EF`, `Shared`, `ClientShared`, `ServiceDefaults`.
4. Test suffixes are stripped too, so `Acme.Api.UnitTests` maps back to `Acme.Api`.
5. `AssemblyName` and `PackageId` default to the fully evaluated `RootNamespace` — except where
   stripping removed a segment, where they use the full `PurviewLogicalProjectName` so the artifact
   stays distinct from its parent. Test and shared-testing projects keep their detected suffix.
   Explicit `AssemblyName`/`PackageId`/`RootNamespace` always win.
6. The naming defaults are applied during `Sdk.props`, before the Microsoft SDK computes
   `TargetName`, so the compiled output name always matches `AssemblyName`.

## Automatic packages and references

- **Non-test C# projects**: SourceLink (unless `DisableSourceLink`), the Purview telemetry packages
  (unless `ExcludePurviewTelemetry`), `GenerateDocumentationFile=true`, and `InternalsVisibleTo`
  generation (unless `DisableAutoInternalsVisibleTo`).
- **Test and shared-testing projects**: test-friendly `NoWarn` defaults, not packable or
  publishable, and the packages selected by `TestingFramework`, `SubstituteFramework` and
  `TestDataFramework`. TUnit test projects get Microsoft.Testing.Platform wiring automatically.
  Shared-testing projects skip the runnable test package.
- **Packable projects**, **Roslyn components** and **special project types** (CLI, container, web,
  Aspire host): see [references/detection.md](references/detection.md).
- **Blazor WebAssembly clients, Blazor hosts, BFFs and browser-safe libraries**: see
  [references/blazor.md](references/blazor.md).

Project-reference probing is layout-driven and covered by `purview-buildsdk-project-layout`.

## The shipped analyzers

The SDK adds `Purview.BuildSdk.Analyzers` as an `<Analyzer>` item to every C# project, so its rules
surface in command-line builds and in Visual Studio. The code-fix assembly ships beside it.

| Rule | What it enforces |
| -- | -- |
| `PDS0002` | Extensions classes live under `Extensions/<receiver namespace>/` |
| `PDS0003` | Explicit types with target-typed `new()` |
| `PDS0004` | Correct acronym capitalisation |
| `PDS0008` | A BFF's authentication cookie is not set `HttpOnly = false` |
| `PDS0009` | A BFF's authentication cookie is not set `SameSite = None` |

`PDS0004` follows .NET naming guidance: well-known framework spellings (`Sql`, `Guid`, `Uuid`,
`Url`, `Dns`, `Tcp`, `Http`, `Xml`, `Db`, …) are exempt — `Db` among them, so `DbContext`,
`DbConnection` and `DbSet` are never flagged. A small set is still uppercased: `Api`, `Ai`, `Ui`,
`Io`, `Os`, `Cpu`, `Gpu`, `Cli`, `Gui`, `Ram`, `Ssh`. Members mandated by a contract — interface
implementations and base-class overrides — are never renamed.

Customise through `dotnet_analyzer_configuration.pds0004.allowed_words` (exempt segments),
`.acronym_map` (segment renames, e.g. `Sql:SQL`, or `Db:DB` to re-enable it) and
`.allowed_identifiers` (brand names, exact or word-boundary prefix, first match wins — `CosmosDb`
covers `CosmosDbServer`). All three merge with and override the shipped defaults.

The shipped `.editorconfig` marks generated content (`Migrations/`, `*.g.cs`, `Generated/`,
`*.Designer.cs`, `obj/`, `bin/`) as `generated_code = true`, and suppresses namespace-conflict
diagnostics under `Extensions/`, so no `#pragma` suppressions are needed there.

Two Visual Studio refactorings ship with them, both described in
[references/detection.md](references/detection.md).

## When changing a detection rule

1. Keep the `.csproj` filename equal to its directory name.
2. Update `README.md` and the shipped skills in the same change — a rule true in code and false in
   prose is this repository's recurring failure.
3. Add or update the integration test in `src/tests/BuildSdk.IntegrationTests/` that pins it.
