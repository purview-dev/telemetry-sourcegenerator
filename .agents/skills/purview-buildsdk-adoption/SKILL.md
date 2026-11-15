---
name: purview-buildsdk-adoption
description: Adopt Purview.BuildSdk in a repository, or troubleshoot an adoption that is not behaving. Use when wiring the SDK into Directory.Build.props for the first time, when a property seems to have no effect, when the resolved version, target framework or namespace is wrong, or when a consuming repository builds but the SDK's automatic behaviour is not appearing. Covers the import chain, pre-import property ordering, required settings and versions, target frameworks, and version detection from package.json.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Adopting Purview.BuildSdk

The goal is a working adoption that does not break existing build behaviour. Prefer minimal,
targeted property changes over rewriting a repository's `Directory.Build.props`.

For the full property list use `purview-buildsdk-configuration`; for auditing an existing
repository's shape, `purview-buildsdk-repository-review`.

## 1. The import chain

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <!-- Pre-import properties go HERE, above the import. See step 2. -->
    <NamespacePrefix>Acme.Sales</NamespacePrefix>
  </PropertyGroup>

  <Import Sdk="Purview.BuildSdk" Project="Sdk.props" />
</Project>
```

```xml
<!-- Directory.Build.targets -->
<Project>
  <Import Sdk="Purview.BuildSdk" Project="Sdk.targets" />
</Project>
```

Both halves are required. A repository with only `Sdk.props` imported gets the evaluation-time
defaults — naming, identity, framework selection — but none of the targets, so validation,
packaging behaviour and the generated files never run. That combination usually presents as "the
properties look right but nothing happens".

### Required settings and versions

Four values decide whether the SDK behaves as documented. Set the first three **before** the import
where noted:

| What | Where | Notes |
| -- | -- | -- |
| `NamespacePrefix` | `Directory.Build.props`, above the import | Required — the build fails without it unless `DisableNamespacePrefixCheck=true`. |
| SDK version | `global.json` → `msbuild-sdks.Purview.BuildSdk` | The pinned `Purview.BuildSdk` version, e.g. `2.0.1`. The SDK bootstraps `global.json` when it is missing and writes `PurviewBuildSdkVersionForGlobalJson` — the restored package version, else the version it was packed as. |
| Product version | repo-root `package.json` → `version` | Drives `Version` and `PackageVersion`. `UsePackageJsonVersion` / `RootPackageJson` are pre-import. |
| Package versions | `Directory.Packages.props` | CPM is on; a `PackageVersion` is a minimum, not an exact pin. |

```json
// global.json
{
  "test": { "runner": "Microsoft.Testing.Platform" },
  "msbuild-sdks": { "Purview.BuildSdk": "2.0.1" }
}
```

## 2. Pre-import ordering is the first thing to check

**A property that must affect `Sdk.props` evaluation has to be set before the import.** Setting it
in a `.csproj`, or below the import, is too late and silently does nothing — no error, no warning.
This is the single most common adoption failure.

Set before the import:

- `NamespacePrefix`
- `UsePackageJsonVersion`, `RootPackageJson`, `RepoRoot`
- `EnableAssemblyNameGeneration`
- `TestingFramework`, `SubstituteFramework`, `TestDataFramework` — even for a single project; a
  value in the `.csproj` is too late and the defaults (`TUnit`, `TUnitMocks`, `Bogus`) stay
- `PurviewTestContextNoWarn`, `DisablePurviewTestContextRuleSet`
- `DisablePurviewBlazorWebAssemblyDefaults`
- `PurviewTargetFrameworks<Set>`, when redefining a framework set

Most other properties are read in `Sdk.targets`, or only after the project body, and can be set in a
project file — `PurviewSharedTestingOutputType` and `AutoIncludeUsings` among them. `purview-buildsdk-configuration` lists
which is which.

When a property appears to have no effect, confirm what MSBuild actually resolved before changing
anything else:

```bash
dotnet msbuild <project> -getProperty:NamespacePrefix -getProperty:RootNamespace
```

## 3. Version detection

Versions come from `package.json`. Resolution order:

1. An explicit `RootPackageJson`.
2. An explicit `RepoRoot`, else CI workspace variables (`GITHUB_WORKSPACE`,
   `BUILD_SOURCESDIRECTORY`, `BUILD_REPOSITORY_LOCALPATH`, `CI_PROJECT_DIR`).
3. The nearest `.git` above the project.
4. The nearest `package.json` above the project.

The `package.json` at that root is read, and on success both `Version` and `PackageVersion` are set
from its `version` field. A root that is found but has no `package.json`, or a `package.json` with no
`version`, already fails the build.

If the version looks wrong, do not guess at the discovery — make it fail loudly instead:

```xml
<UsePackageJsonVersion>Strict</UsePackageJsonVersion>
```

`Strict` also fails the build when no repository root can be discovered at all, rather than silently
skipping detection and leaving the default `0.0.1`. Set `VersionDetectionLogEnabled=true` to see what it found.

## 4. Naming and placement

Most of the SDK's value is inferred from project names and locations, so an adoption that keeps a
bespoke structure gets correspondingly little. Confirm the repository follows the conventions rather
than introducing new structure to work around them — see `purview-buildsdk-project-layout`, and
`purview-buildsdk-repository-review` for auditing an existing layout.

The highest-value single check: every test project is named `<Target>.<Type>Tests`. **The dot is
required.** A project named plainly `UnitTests` is not detected as a test project at all, and
silently gets no runner, no coverage, no `InternalsVisibleTo` and no category.

## 5. Agent skills are a separate, explicit step

Nothing the SDK does during a restore or a build touches agent skills. Installing, updating and
cleaning them up are separate targets a person runs by name — `InstallPurviewSkills`,
`UpdatePurviewSkills`, `CheckPurviewSkills`, `FixPurviewSkills` — and the ones that change the
repository refuse to run inside a build (`PRSKL0009`). Do not hook them into one. The skills CLI does
the installing, through `bun x` by default; with `PurviewSkillsAgents` unset it auto-detects agents.

If the repository uses `just`, add recipes for them — see `purview-buildsdk-justfile`. For what each
target does and the `PRSKL*` diagnostics, see `purview-buildsdk-skill-installation`.

## 6. Target frameworks

A project that declares neither `TargetFramework` nor `TargetFrameworks` gets `net10.0;net11.0`.
Confirm what a project resolves before changing it:

```bash
dotnet msbuild <project> -getProperty:TargetFramework -getProperty:TargetFrameworks
```

To set it:

- **Declare it explicitly** — `TargetFramework` (one) or `TargetFrameworks` (many). An explicit value
  always wins over every set, wherever it is set (the project, with or without a `Condition`, a file it
  imports, or `Directory.Build.props`) — but not in `Directory.Build.targets`, which is too late.
- **Select a curated set** — `<PurviewTargetFrameworkSet>Supported</PurviewTargetFrameworkSet>` in
  `Directory.Build.props` or the `.csproj` (`Current`, `Latest`, `Supported`, `Broad`, `All`), so a
  lifecycle change is one SDK bump rather than an edit in every project.
- **Redefine a set** — set `PurviewTargetFrameworks<Set>` before the import to pin the repository's
  meaning of a set.

A Roslyn component targets `netstandard2.0` and ignores the sets. Full detail in
`purview-buildsdk-configuration` and [Configuration Reference](https://github.com/purview-dev/build-sdk/blob/main/docs/wiki/Configuration-Reference.md#target-framework-sets).

## 7. SourceLink provider

The SDK adds a SourceLink provider package (`SourceLinkPackageName`, default
`Microsoft.SourceLink.GitHub`) to every non-test project, and `PublishRepositoryUrl=true` writes the
repository URL and commit into the packed `.nuspec`. Match the repository host in
`Directory.Build.props`, so every project gets the same provider:

| Host | Package |
| -- | -- |
| GitHub / GitHub Enterprise | `Microsoft.SourceLink.GitHub` *(default)* |
| Azure DevOps Services / Azure Repos | `Microsoft.SourceLink.AzureRepos.Git` |
| Azure DevOps Server / TFS (on-premises) | `Microsoft.SourceLink.AzureDevOpsServer.Git`, plus a `SourceLinkAzureDevOpsServerGitHost` item |
| GitLab | `Microsoft.SourceLink.GitLab` |
| Bitbucket | `Microsoft.SourceLink.Bitbucket.Git` |

CPM is on, so add a `PackageVersion` for the chosen provider to `Directory.Packages.props` — the shipped
template already declares GitHub, Azure Repos and GitLab. `DisableSourceLink=true` opts out entirely.
Detail, including the on-premises host item, is in `purview-buildsdk-configuration`.

## Constraints

- Prefer minimal, targeted property changes over broad `Directory.Build.props` rewrites.
- Do not disable `PurviewAutoSdkPack` unless the consumer explicitly asks to opt out.
- Do not restate an SDK default in a project file. Hand-configuration that merely repeats a default
  is pure maintenance cost, and it hides the fact that the default later changed.
- Re-run the build — or the repository's canonical build command — after each change, and confirm
  the fix before moving to the next one.
