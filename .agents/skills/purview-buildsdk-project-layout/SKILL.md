---
name: purview-buildsdk-project-layout
description: Decide where a project goes and what to call it in a repository that uses Purview.BuildSdk. Use when creating, moving, splitting or renaming a project or test project, choosing a test suffix, aligning namespaces and assembly identity with NamespacePrefix, laying out a Blazor Web App host, WebAssembly client and browser-safe Contracts library, or relying on the SDK's automatic project references. Also use when rationalising a repository's shape so the SDK's conventions work without overrides.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Project layout and naming for Purview.BuildSdk

Use this skill when physically adding, moving, splitting or renaming a project.

For *why* the SDK classified an existing project a particular way, use
`purview-buildsdk-project-detection`. For the property that changes a behaviour, use
`purview-buildsdk-configuration`.

## First principle

**Treat naming and placement as configuration, not decoration.**

The SDK infers namespaces, assembly identity, test categories, package wiring and project references
from a small set of conventions. The closer a repository sits to them, the fewer overrides it needs.

The corollary matters just as much: **preserve the host repository's existing layout first.** Only
introduce new structure when no established pattern exists.

## Canonical layout

```text
<repo root>/
  package.json              version source
  .editorconfig             bootstrapped by the SDK
  skills/                   authored agent skills, if any
  src/
    <Solution>.slnx
    src/<Project>/<Project>.csproj
    tests/<Target>.<Type>Tests/<Target>.<Type>Tests.csproj
```

- The `.csproj` filename must equal its containing directory name, unless
  `DisableProjectFileNamingConventionCheck=true`.
- Source projects under `src/`, test projects under `tests/`.
- Place a new project beside similar ones — same language, layer and test type.
- One test type per project by default.

A repository that already uses a different shape keeps it; add to it rather than converting it.

## Naming and identity

`NamespacePrefix` is the root identity source. Keep project names short and let the SDK apply the
prefix:

| `NamespacePrefix` | Project | `RootNamespace` |
| -- | -- | -- |
| `Aspire` | `Hosting` | `Aspire.Hosting` |
| `Acme.Sales.RegionalPipeline` | `Identity.API` | `Acme.Sales.RegionalPipeline.Identity.API` |
| `Acme.Sales.RegionalPipeline` | `Identity.Core` | `Acme.Sales.RegionalPipeline.Identity` |

`AssemblyName` and `PackageId` follow the resolved `RootNamespace` — except where suffix-stripping
removed a segment (`Core`, `Shared`, `ServiceDefaults`), in which case they keep the fuller logical
name so the artifacts stay distinct from their parent. Explicit values always win, and
`EnableAssemblyNameGeneration=false` (before the SDK import) restores standard behaviour.

Do not hand-author an alternate namespace scheme when one is already in use.

## Test project naming

Test detection is a **pattern**, not a list: `^.*\.([\w|_]\w*)Tests?$`. A target name, a dot, then
any word before `Test` or `Tests`. The captured word becomes `TestingType` and the baseline category.

**The dot is required.** A project named plainly `UnitTests`, with no target in front of it, is not
detected as a test project at all — no runner, no coverage, no `InternalsVisibleTo`, no category.
Always name a test project `<Target>.<Type>Tests`.

Prefer these by default: `UnitTests`, `IntegrationTests`, `E2ETests`, `FunctionalTests`,
`ContractTests`. Use another suffix only when the test type itself carries operational meaning.

A suffix outside the SDK's shipped `TestType` group still builds, runs and gets its category — it
just cannot reach the target project's `internal` members, because that group drives
`InternalsVisibleTo` generation. The full list is in `purview-buildsdk-project-detection`.

## Shared projects: exact names only

The SDK recognises these names exactly — a near miss gets none of the behaviour.

Shared: `Shared`, `SharedFramework`, `SharedInfrastructure`, `SharedInfra`, `SharedUtilities`,
`SharedUtils`, `SharedLibrary`, `SharedLib`, `SharedHelpers`.

Shared testing: `SharedTestingFramework`, `SharedTestingInfrastructure`, `SharedTestingInfra`,
`SharedTestingUtilities`, `SharedTestingUtils`, `SharedTestingLibrary`, `SharedTestingLib`,
`SharedTestingHelpers`.

## Automatic project references to preserve

Layout is what makes these work. Move a project off the convention and you own the reference.

Test projects probe for their target, in order:

- `../$(TargetProjectName)/$(TargetProjectName).csproj`
- `../../$(TargetProjectName)/$(TargetProjectName).csproj`
- `../src/$(TargetProjectName)/$(TargetProjectName).csproj`
- `../../src/$(TargetProjectName)/$(TargetProjectName).csproj`

Non-test projects pick up sibling shared projects via `../Shared*/Shared*.csproj`; test projects
pick up `../SharedTesting*/SharedTesting*.csproj`. A Blazor WebAssembly client is the exception: it
does not pick up `Shared*`, because everything it references is downloaded to the browser.

## Blazor Web App: host and client

```text
src/src/
  Shop/Shop.csproj                        Microsoft.NET.Sdk.Web - the host
  Shop.Client/Shop.Client.csproj          Microsoft.NET.Sdk.BlazorWebAssembly - the client
  Shop.ClientShared/Shop.ClientShared.csproj   a PurviewBrowserSafe library both use
  Shop.Bff/Shop.Bff.csproj                a BFF, when tokens are kept server-side in a separate host
src/tests/
  Shop.Client.UnitTests/                  TargetProjectName = Shop.Client
```

- A web host named `<Name>` beside a WebAssembly client named `<Name>.Client` is the pairing the SDK
  recognises: the host gets `IsBlazorHostProject=true` and a reference to the client automatically
  (skipped when declared; `PurviewAutoBlazorClientReference=false` opts out).
- Identities with `NamespacePrefix=Acme`: `Shop.Client` is `Acme.Shop.Client`; `Shop.Bff` is
  `Acme.Shop.Bff`; `ClientShared` is a stripped segment, so `Shop.ClientShared` has namespace
  `Acme.Shop` and assembly `Acme.Shop.ClientShared`.
- Every library the client references must set `<PurviewBrowserSafe>true</PurviewBrowserSafe>`, or the
  build reports `PRBLZ0001`. Keep server-only code (file, process, database access) out of anything
  the client references.
- Name a BFF host `*Bff` (case-sensitive) for `IsBffProject=true`. Being the client's host does not
  make a project a BFF.

## Creating a new project

1. Match the SDK and style of sibling projects.
2. Use Central Package Management if the repository has it.
3. Add only the dependencies the project's scope needs — the SDK supplies the testing stack,
   SourceLink and telemetry by default.
4. Add the project to the solution.
5. Keep target frameworks, nullability, analyzers and warning settings consistent with neighbours.

## Moving or splitting

1. Create the destination using the established layout.
2. Move the files.
3. Update namespaces, usings and references for the destination.
4. Verify the destination still produces the intended `TestingType`, `TargetProjectName` and
   `RootNamespace` — ask MSBuild rather than assuming:
   `dotnet msbuild <project> -getProperty:RootNamespace -getProperty:TargetProjectName`.
5. Remove dependencies the source no longer needs.
6. Update solution membership and project references.
7. Build, then run the affected tests.

## Guardrails

- Prefer minimal, targeted diffs; avoid cross-cutting renames unrelated to the move.
- Keep test intent unchanged while relocating.
- When the structure is ambiguous, infer from the nearest sibling projects and say which assumption
  you made.
- When in doubt, preserve compatibility with the SDK's automatic naming, namespace and
  project-reference behaviour.
