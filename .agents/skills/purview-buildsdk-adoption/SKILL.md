---
name: purview-buildsdk-adoption
description: Adopt Purview.BuildSdk in a repository, or troubleshoot an adoption that is not behaving. Use when wiring the SDK into Directory.Build.props for the first time, when a property seems to have no effect, when the resolved version or namespace is wrong, or when a consuming repository builds but the SDK's automatic behaviour is not appearing. Covers the import chain, pre-import property ordering, and version detection from package.json.
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

The SDK version comes from `global.json`, which the SDK itself bootstraps.

## 2. Pre-import ordering is the first thing to check

**A property that must affect `Sdk.props` evaluation has to be set before the import.** Setting it
in a `.csproj`, or below the import, is too late and silently does nothing — no error, no warning.
This is the single most common adoption failure.

Set before the import:

- `NamespacePrefix`
- `UsePackageJsonVersion`, `RootPackageJson`
- `EnableAssemblyNameGeneration`
- `AutoIncludeUsings`
- `PurviewSharedTestingOutputType`, `PurviewTestContextNoWarn`
- any repo-wide testing-framework selection you want every project to inherit

Everything else is read in `Sdk.targets` and can be set in a project file.

When a property appears to have no effect, confirm what MSBuild actually resolved before changing
anything else:

```bash
dotnet msbuild <project> -getProperty:NamespacePrefix -getProperty:RootNamespace
```

## 3. Version detection

Versions come from `package.json`. Resolution order:

1. An explicit `RootPackageJson`.
2. CI workspace variables.
3. The `.git` root.
4. A nearby `package.json`.

On success both `Version` and `PackageVersion` are set from the `version` field.

If the version looks wrong, do not guess at the discovery — make it fail loudly instead:

```xml
<UsePackageJsonVersion>Strict</UsePackageJsonVersion>
```

`Strict` fails the build when discovery or resolution cannot succeed, rather than silently skipping
it and leaving the default `1.0.0`. Set `VersionDetectionLogEnabled=true` to see what it found.

## 4. Naming and placement

Most of the SDK's value is inferred from project names and locations, so an adoption that keeps a
bespoke structure gets correspondingly little. Confirm the repository follows the conventions rather
than introducing new structure to work around them — see `purview-buildsdk-project-layout`, and
`purview-buildsdk-repository-review` for auditing an existing layout.

The highest-value single check: every test project is named `<Target>.<Type>Tests`. **The dot is
required.** A project named plainly `UnitTests` is not detected as a test project at all, and
silently gets no runner, no coverage, no `InternalsVisibleTo` and no category.

## 5. Agent skills are a separate, explicit step

The SDK does not install agent skills, and nothing it does during a build writes to an agent
directory. It reads which skills each referenced package ships and recommends the command that
installs them; the skills CLI does the installing. See `purview-buildsdk-skill-installation`.

## Constraints

- Prefer minimal, targeted property changes over broad `Directory.Build.props` rewrites.
- Do not disable `PurviewAutoSdkPack` unless the consumer explicitly asks to opt out.
- Do not restate an SDK default in a project file. Hand-configuration that merely repeats a default
  is pure maintenance cost, and it hides the fact that the default later changed.
- Re-run the build — or the repository's canonical build command — after each change, and confirm
  the fix before moving to the next one.
