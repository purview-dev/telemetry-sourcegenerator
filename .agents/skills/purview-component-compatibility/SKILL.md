---
name: purview-component-compatibility
description: >-
  Use when developing, reviewing or releasing a published component - a NuGet package, or an npm
  package - and choosing which dependency version to declare, when a consumer reports a version
  conflict or an unexpected upgrade, or when a component's supported target frameworks change. Applies
  the golden rule that a published component declares the LOWEST compatible version rather than the
  latest, walks the Purview component hierarchy to find each floor, and verifies it by building
  against it. Not for applications: they take the latest compatible version instead.
license: MIT
metadata:
  author: Purview-Dev
  version: "1.0"
  repository: purview-dev/engineering-playbook
---

# Purview component compatibility

A published component makes a promise with every version it declares: *this is the least I require of
you*. Raise that number and every consumer is pulled up with you, whether or not they wanted it.

**The golden rule: declare the lowest version that compiles, passes your tests, and is supported on
every target framework you claim.** The version you happen to have installed is never the answer, and
neither is the latest.

## Which direction?

| What you are publishing | Declare | Why |
| -- | -- | -- |
| an application, a test suite, a tool — it runs, and nobody installs it | the **latest** compatible version | you own the runtime, and a version you skip is a fix you did not take |
| a component — a NuGet or npm package somebody else installs | the **lowest** compatible version | dependency resolution takes the highest minimum in the graph, so your floor becomes theirs |

This skill is about the second row. If the repository only ever runs, stop here and take the latest.

## Why a floor matters

NuGet and npm resolve a graph to the **highest** minimum among all constraints. If a package declares
`Testcontainers` 4.16 because 4.16 is what its author had installed, every consumer is forced onto 4.16,
including the one whose own constraint is 4.0 and whose runtime is pinned to it. The consumer cannot
refuse an upgrade the package never needed to ask for, and the symptom appears in *their* build, not
yours.

## The workflow

### 1. Name the component and its frameworks

The floor is per target framework, not per package. Write down every `TargetFramework` the component
publishes; a multi-targeting component has a different floor on each, and the one that matters is the
oldest framework still supported, not the newest one you build against.

### 2. Read the hierarchy, do not remember it

The dependencies — and their minimums — are in the published `.nuspec`, not in anyone's memory:

```bash
node scripts/dependencies.mjs Purview.Containers
node scripts/dependencies.mjs Purview.Architecture.Testing --tfm net11.0
node scripts/dependencies.mjs Purview.Containers --walk
```

A bare version is a **minimum**; a bracketed one (`[1.2.3]`, `[1.0,2.0)`) is a range. The script prints
the distinction, because it is the whole rule: a minimum you raise is a minimum every consumer inherits.

`references/purview-components.md` records which Purview package depends on which, and which target
frameworks each family supports. Where the two disagree the registry wins — that file is the map, the
`.nuspec` is the territory.

### 3. Fix the floor, per framework

For each dependency, find the oldest version that:

- exposes every API the component calls (the compiler decides this, not the changelog);
- passes the component's own tests and its consumer-verification suite;
- is supported on every framework in that dependency group.

Then declare that version. Not `latest`, not floating (`1.*`), and never the version that resolved on
your machine by accident.

### 4. Prove the floor

A floor nobody builds against is a guess. Add a CI leg that restores the component with every dependency
held at its declared floor and runs the suite — the ordinary leg keeps resolving the latest, which is
what a consumer will actually get. `references/lowest-compatible.md` has the mechanics.

### 5. Keep the ceiling visible, and the floor written down

A component's README carries a badge for the current release and a sentence naming the floor it
supports. Badges are generated from the registry, so they cannot go stale; a version written into prose
is wrong within one release.

## Pitfalls that produce a floor nobody meant

| Pitfall | What it looks like | The fix |
| -- | -- | -- |
| Declaring what is installed | `dotnet add package X` writes the latest; right for an application, wrong for a component | edit the version down, then prove it in step 4 |
| Transitive uplift | a dependency pulls in a newer transient package than your floor allows | override it deliberately — the persistence sample does this for a security-patched package — and comment why |
| One framework's floor | a `net10.0-windows` group needs a higher floor than `net10.0` | read each group; never average them |
| Prerelease in the graph | a family that ships prereleases depends on them at the same version | pin the family to one property so the floor moves together |
| Analyzer and source-generator packages | compile-time only, so they rarely constrain a consumer | still declare a real floor; they appear in the graph and in transitive audits |
| Floating versions | `1.*` resolves differently on every restore | banned: a floor is a number |

## Checks before you answer

- Is each declared version the lowest that passes, or the one that happened to be installed?
- Does every declared target framework have a floor that compiles on that framework?
- Was the floor proved by a build, or only by your machine?
- Does any page state a version number? It should be a badge or a link to the registry.
- If a floor was raised, is that a breaking change the version number admits to?

## See also

`references/lowest-compatible.md` for the CI mechanics and the override patterns, and
`references/purview-components.md` for the Purview hierarchy. The rule itself, and the application-side
half of it, is stated in the Engineering Playbook's Dependency Versions page; the
`purview-engineering-principles` skill summarises it as one of the eleven conventions.

