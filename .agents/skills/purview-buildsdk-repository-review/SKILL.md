---
name: purview-buildsdk-repository-review
description: Review a repository that uses Purview.BuildSdk for naming, placement and test-structure alignment, and report what the SDK can and cannot do automatically as a result. Use when asked to audit, rationalise or health-check a repo against the SDK's conventions, when onboarding an existing codebase onto Purview.BuildSdk, or when deciding whether a layout quirk is worth changing.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Reviewing a repository against the SDK's conventions

This is an audit, not a refactor. The deliverable is a prioritised list of findings and the cost of
each, so somebody else can decide what to change.

For the conventions themselves use `purview-buildsdk-project-layout`; for why one project was
classified as it was, `purview-buildsdk-project-detection`.

## The governing judgement

**Deviation is only a finding when it costs the repository something.** The SDK infers behaviour
from naming and placement, so a deviation is interesting precisely to the extent that it turns an
automatic behaviour off. A layout that differs from the canonical one but still gets every automatic
behaviour is not debt, and reporting it as such wastes the reader's attention.

Sort every finding into one of three buckets, and say which:

| Bucket | Meaning |
| -- | -- |
| **Acceptable variance** | Differs from the canonical layout, costs nothing. Preserve it. |
| **Structural debt** | Blocks an automatic behaviour the repository would benefit from. |
| **Incremental opportunity** | Low-risk tidying worth doing when the area is touched anyway. |

## 1. Read before concluding

Inspect, in this order, before forming any opinion:

- `Directory.Build.props` and `Directory.Build.targets` — where the SDK is imported, and what is set
  before the import
- the solution entry point, for what is actually in the build
- `Directory.Packages.props`, for Central Package Management
- the project layout on disk

## 2. Check the structural signals

- Source projects under `src/`, test projects under `tests/`.
- `.csproj` filename equals its containing directory name — unless
  `DisableProjectFileNamingConventionCheck=true`, which is itself a finding worth surfacing.
- Short project names, with `NamespacePrefix` carrying the repository identity.
- Test project names match `^.*\.([\w|_]\w*)Tests?$`. **The dot is required**; a project named
  plainly `UnitTests` is not detected at all and silently gets no runner, no coverage, no
  `InternalsVisibleTo` and no category. This is the single highest-value thing to check.
- Test suffixes come from the SDK's shipped `TestType` group where `internal` access is wanted — a
  suffix outside it still runs, it just cannot see `internal` members.
- Shared and shared-testing projects use the **exact** recognised names. A near miss gets none of
  the behaviour.

## 3. Verify with MSBuild, do not infer from folder names

A review that reasons from directory names will be wrong about at least one project. Ask:

```bash
dotnet msbuild <project> -getProperty:RootNamespace -getProperty:AssemblyName \
  -getProperty:IsTestProject -getProperty:TestingType -getProperty:TargetProjectName
```

Quote the computed values in the findings. "`Api.Tests` resolves `TestingType=` empty, so it has no
runner" is actionable; "`Api.Tests` looks like it might not be detected" is not.

## 4. Review test readability

- Subject-based suites: `{SubjectName}Tests`, with
  `{SubjectOrMemberUnderTest}_{Scenario}_{Expectation}` methods.
- Non-subject-based suites — build, generator, workflow, package, full-system — named clearly for
  their broader role. A forced `{Subject}Tests` on one of these is worse, not better.
- TUnit categories and display names used where a suite is large enough to need navigating.

See `purview-buildsdk-testing`.

## 5. Check the test dependencies are earned

The SDK already supplies `TUnit`, `TUnit.Mocks`, `Bogus` and the Microsoft.Testing.Platform wiring
to every detected test project. Anything beyond that was a deliberate choice, and deliberate choices
go stale:

- `TUnit.Aspire` and `Purview.Containers` are **not** universal defaults. Confirm each reference is
  genuinely needed by that project rather than copied from a sibling.
- Where a project references `Testcontainers` directly, check whether a `Purview.Containers` typed
  module now covers it — the module set grows, and a direct reference that was once necessary often
  is not any more.
- A package the SDK already adds, restated in a project file, is maintenance cost with no effect.

## 6. Explain consequences, not just mismatches

For each finding, name the specific automatic behaviour that is lost: `RootNamespace`,
`AssemblyName`, `PackageId`, `TargetProjectName`, the automatic target-project reference, the
sibling `Shared*` reference, `InternalsVisibleTo` generation, the test runner and coverage wiring.

"Project is not under `src/`" is not a finding. "Project is not under `src/`, so the test project's
four probe paths all miss and the `ProjectReference` has to be maintained by hand" is.

## Reporting the result

1. One sentence on whether the repository broadly fits the SDK's conventions.
2. The findings, ordered by impact, each with its bucket and the behaviour it costs.
3. The low-risk rationalisation steps, as concrete renames or moves.
4. An explicit split of what is **already automatic** versus what this repository has configured by
   hand — that list is usually the most useful part of the review, because hand-configuration that
   merely restates a default is pure maintenance cost.

Do not propose a cross-cutting rename as a first step. Name the one change with the best
cost-to-benefit ratio and say what it unlocks.
