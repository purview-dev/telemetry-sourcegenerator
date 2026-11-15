---
name: purview-buildsdk-settings-docs-sync
description: Change or document a Purview.BuildSdk setting without leaving part of the surface behind. Use when flipping a .NET 11 compiler feature default (Runtime Async, memory-safety rules), adding or renaming a property, or making sure every skill and doc explains how to set required settings and versions, the target framework(s), and the compiler-visible (build_property.*) settings. Repository-local guidance for purview-dev/build-sdk.
license: MIT
metadata:
  scope: repository-local
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Keeping settings, docs and skills in step

> **Repository-local.** This skill lives in `.agents/skills/` and is tracked by name (the rest of that
> folder is gitignored); it is not packed and not published. It is deliberately **not** linked from
> `AGENTS.md`: `RepositorySkillsSelfTests` fails any AGENTS.md link that points into `.agents/skills/`.
> The published skills are authored under `skills/` and are a different thing entirely.

## The general sweep is a published skill

This skill is the **settings** sweep for this repository. Any other documentation change — a renamed
recipe, a new wiki page, a moved page, a skill whose scope grew, an audit of whether the docs are
still true — follows the published
[`purview-buildsdk-docs-sync`](../../../skills/purview-buildsdk-docs-sync/SKILL.md) skill: every
claim checked against the code, and the wiki kept hierarchical. In this repository the wiki's root
pages are exactly `Getting-Started.md` (the site's `docs.rootPage`), `Configuration-Reference.md` and
`Repository-Bootstrap.md` (the site catalogue's `docsPage` deep links); every other page lives in one
topic folder, and `DocumentationWikiTests` holds the folders, `_Sidebar.md` and `Home.md` to that.

## Start from the schema

`src/src/BuildSdk/Sdk/Sdk.props.buildschema.json` is the canonical description of every SDK property —
description, type and default — and `PropertySchemaTests` holds the code, the `CompilerVisibleProperty`
list, `README.md` and `docs/wiki/Configuration-Reference.md` to it. The generic method (where a
sidecar goes, when a property is pre-import, when to export it, how to rename or retire one) is the
published [`purview-buildsdk-msbuild-properties`](../../../skills/purview-buildsdk-msbuild-properties/SKILL.md)
skill. In this repository, adding a property means:

1. `Sdk/Props/Defaults.props` — the default, guarded with `'$(X)' == ''`.
2. `Sdk/Sdk.props.buildschema.json` — the entry. `defaultValue` is what a plain class library evaluates
   to; without one, end the description with `No default: <why>.`; when `Sdk.props` (or a file it
   imports) reads the property, say `Set it before the \`Sdk.props\` import.` The test computes both
   from the XML and fails if the description disagrees.
3. `Sdk/Sdk.props` — a `CompilerVisibleProperty` with a `<Description>`, only if an analyzer reads it.
4. `README.md` and `docs/wiki/Configuration-Reference.md` — a row in the table for its area (the
   Default cell opens with the backticked default, or italics when there is none), plus a row in each
   *Compiler-visible SDK properties* table when exported.
5. `CHANGELOG.md` — under the `package.json` version's heading (see the Changelog row below).
6. `just properties-check` — every failure names the file and the row to add.

A property that must not appear in IntelliSense goes in
`src/tests/BuildSdk.IntegrationTests/PropertySchema/SdkPropertyClassification.cs` with a reason:
`ExternallyOwned` (MSBuild/.NET/NuGet), `Internal` (unprefixed implementation detail),
`ComputedDefaults` (machine- or project-dependent value). Prefer a `_` prefix for anything new and
internal — then no list is needed.

## The rule this exists for

A property is written down in several places, and only some of them are checked by a test. The failure
mode this repository keeps hitting is a change that lands in one place and stops there — the property
declaration, its validation, `README.md`, the wiki page and the shipped skills drift apart, and the
only evidence is a consumer following a doc that still describes last release's default.

So: **treat a settings change as a sweep, not an edit.** Work the surfaces below, then prove it with
the validation commands at the end.

## When to use this

- Flipping a .NET 11 compiler feature default (`MemorySafetyRules`, `PurviewRuntimeAsync`).
- Adding, renaming, re-defaulting or removing a user-facing property.
- Making required settings, versions, target frameworks or compiler-visible settings clearer in the
  docs and the shipped skills.
- Auditing whether a property is documented everywhere before you change it.

For auditing a *consumer* repository, use `purview-buildsdk-repository-review`. For what a project
detection rule does, use `purview-buildsdk-project-detection`.

## The surfaces — one property, many homes

Find the row and make sure every file agrees.

| Surface | File | What changes |
| -- | -- | -- |
| Runtime default (all projects) | `src/src/BuildSdk/Sdk/Props/Defaults.props` | The default when unset, plus the explanatory comment. Sets the value in `Sdk.props`, which is also what makes it compiler-visible. |
| Per-TFM default | `src/src/BuildSdk/Sdk/Targets/Modern.targets` | The `net11.0`+ feature selection. Must agree with `Defaults.props`. |
| Property schema | `src/src/BuildSdk/Sdk/Sdk.props.buildschema.json` | Description, type and `defaultValue` — what csproj IntelliSense shows. `PropertySchemaTests` fails when it disagrees with the code or the docs. |
| Compiler-visible description | `src/src/BuildSdk/Sdk/Sdk.props` | The `CompilerVisibleProperty` `<Description>`, which is what an analyzer author reads. |
| Pinning test | `src/tests/BuildSdk.IntegrationTests/Dotnet11FeaturesTests.cs` | The test that fails when the default changes silently. |
| Changelog | `CHANGELOG.md` | An entry under `## [<package.json version>] - <date>`: `### Changed` for behaviour, `### Documentation` for docs-only. Never `[Unreleased]` during active development — a merge to `main` publishes the version, so the entry would read as unreleased until it was already live. `[Unreleased]` is only for a pre-release version (`-alpha`, `-beta`, `-rc`). |
| README | `README.md` | The feature table row, the property table(s), the quick start, and the `.NET 11 features` section + tiers paragraph. |
| Wiki | `docs/wiki/Configuration-Reference.md`, `Getting-Started.md`, `configuration/Version-Detection.md`, `concepts/Project-Type-Detection.md` | The property tables, the required-settings and target-framework sections, and the compiler-visible section. |
| Shipped skills | `skills/purview-buildsdk-*/SKILL.md` and `references/*.md` | The property tables and the sections below. |
| AGENTS.md | `AGENTS.md` | The "authored set, and what each one answers" table, when a skill's scope grows. |

Two traps worth naming:

- **The default lives in two places.** `Defaults.props` defaults the property for every project (which
  is what exposes it to the compiler); `Modern.targets` re-defaults it per-TFM. Change one and the
  other silently disagrees.
- **`Sdk.props` vs `Sdk.targets` decides *where* a value may be set.** A property read during
  `Sdk.props` evaluation must be set before the import; everything else can be set in the project. Say
  which it is, in the docs, next to the property.

A **feature** bigger than a single property gets its own guide as well. SourceLink is the worked
example, because the decision is per repository host: add a wiki page in its topic folder
(`docs/wiki/packaging/SourceLink.md`) and register it in `_Sidebar.md` **and** that section's table in
`Home.md` — `DocumentationWikiTests` fails until both list it — add a `## <Feature>` section to `README.md`, and — when
it is configuration — a `references/<feature>.md` under
`skills/purview-buildsdk-configuration/` that the `SKILL.md` links. Fix any wrong package or property
name you find while documenting: the SourceLink pages used to name `Microsoft.SourceLink.AzureDevOps.Git`,
which does not exist.

## Worked example — flip a .NET 11 compiler feature default

1. `Defaults.props` — change the default and the comment. Say **why** (an experimental compiler feature
   is opt-in), not just what.
2. `Modern.targets` — change the same default in the per-TFM block and its header comment.
3. `Sdk.props` — the `CompilerVisibleProperty` `<Description>` ("0 off (default), …").
4. `Dotnet11FeaturesTests.cs` — make the default test assert the switch is **absent**, rename it to say
   so, and add an explicit opt-in test. De-couple any other test that asserted the switch was on by
   default (for example, the project-features-survive test).
5. `README.md` — the `.NET 11 opt-ins` row, the `.NET 11 features` table row and the tiers paragraph,
   and the property table row.
6. `docs/wiki/Configuration-Reference.md` — the General row **and** the `.NET 11 features` table.
7. Skills — the `MemorySafetyRules`/`PurviewRuntimeAsync` rows in
   `skills/purview-buildsdk-configuration/SKILL.md` and `references/properties.md`.
8. `CHANGELOG.md` — a `### Changed` entry under the `package.json` version's heading that names the old default, the new one, how to
   opt in, and what is unchanged.

Keep `AllowUnsafeBlocks` (and anything else) at its compiler default: a feature flip must not quietly
change a second setting.

## Required settings and versions — where it must be said

Four values, stated as a table wherever a reader lands first:

| Setting | Home |
| -- | -- |
| `NamespacePrefix` (required, pre-import) | `Directory.Build.props`, above the `<Import>` |
| SDK version | `global.json` → `msbuild-sdks.Purview.BuildSdk` (`PurviewBuildSdkVersionForGlobalJson` decides what the bootstrap writes) |
| Product version | repo-root `package.json` → `version` (`UsePackageJsonVersion`, `RootPackageJson`) |
| Package versions | `Directory.Packages.props` (CPM; a `PackageVersion` is a minimum) |

Keep it identical in all four homes: `README.md` quick start, `docs/wiki/Configuration-Reference.md`,
`docs/wiki/Getting-Started.md`, and the `purview-buildsdk-adoption` and
`purview-buildsdk-configuration` skills.

## Target framework(s) — where it must be said

Every skill and doc that talks about configuring a project must explain how to set the target
framework(s), consistently:

- the default set is `net10.0;net11.0` (`Current`);
- an explicit `TargetFramework`/`TargetFrameworks` always wins over every set, wherever it is set
  except `Directory.Build.targets` (`Targets/TargetFrameworkOwnership.targets` reconciles it);
- a curated `PurviewTargetFrameworkSet` (`Current`, `Latest`, `Supported`, `Broad`, `All`) or a
  redefined `PurviewTargetFrameworks<Set>` (pre-import, in `Directory.Build.props`);
- a Roslyn component keeps `netstandard2.0` and ignores the sets (`PurviewTargetFrameworkSetIgnored`).

The canonical home is `skills/purview-buildsdk-configuration/SKILL.md` and its
`references/properties.md`; the other skills point at it rather than restating the whole thing.

## Compiler-visible settings — where it must be said

The `CompilerVisibleProperty` → `build_property.*` mechanism, plus a one-line
`AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.X", …)` example, belongs in:

- `skills/purview-buildsdk-configuration/SKILL.md` and `references/properties.md`,
- `skills/purview-buildsdk-project-detection/SKILL.md` (the analyzers section),
- `README.md`, `docs/wiki/Configuration-Reference.md` and `docs/wiki/concepts/Project-Type-Detection.md`.

Say both halves: **consume** the exported property instead of re-deriving SDK behaviour, and **add a
new one** by listing it as a `CompilerVisibleProperty` item (with a `<Description>`) in `Sdk.props` —
nothing else exposes an MSBuild property to the compiler.

## Skills sweep

Each published skill that touches project configuration gets the change; a skill that is only about
skills tooling does not.

| Skill | What to update |
| -- | -- |
| `purview-buildsdk-configuration` | required settings, target framework(s), compiler-visible properties, the property table, `references/properties.md` |
| `purview-buildsdk-adoption` | the required-settings table and `global.json` example, the target-framework step |
| `purview-buildsdk-project-layout` | the target-framework section |
| `purview-buildsdk-project-detection` | the target-framework subsection, the `build_property.*` note |
| `purview-buildsdk-repository-review` | the read list (`global.json`, `package.json`), the target-framework finding |
| `purview-buildsdk-testing` | the test-project target-framework paragraph |
| `purview-buildsdk-skill-tests`, `-skill-installation`, `-justfile` | **not** project configuration — leave them alone |

Also update each skill's frontmatter `description` so the change makes the skill **fire** on the right
question ("setting the target framework(s)", "pinning the SDK version in global.json").

## Validate

```bash
just build
just skills-validate          # PRSKL0005, the pack-time check
just version-sync-check       # the version pins must still match package.json
just lint-check               # CSharpier over the repo
just properties-check         # schema vs props, defaults, compiler-visible list, README and wiki tables

# Targeted and fast (MTP: --treenode-filter, never --filter):
dotnet test --project src/BuildSdk.slnx --configuration Debug \
  --treenode-filter "/*/*/Dotnet11FeaturesTests/*" --ignore-exit-code 8
dotnet test --project src/BuildSdk.slnx --configuration Debug \
  --treenode-filter "/*/*/(TargetFrameworkSetTests)|(PropertySchemaTests)|(CoreDefaultsTests)/*" --ignore-exit-code 8
dotnet test --project src/BuildSdk.slnx --configuration Debug \
  --treenode-filter "/*/*/(RepositorySkillTests)|(RepositorySkillsSelfTests)|(PurviewSkillValidationTests)/*" --ignore-exit-code 8
```

State the commands you ran before declaring the change done.

## Guardrails

- **Do not publish repository-local advice.** Anything under `skills/` ships to every consumer through
  `npx skills add purview-dev/build-sdk`; guidance about working *in this repository* stays here, in
  `.agents/skills/`, or in `AGENTS.md`.
- **Do not add a link into `.agents/skills/` from `AGENTS.md`** — it is gitignored, so the link is
  broken in a fresh clone and `RepositorySkillsSelfTests` fails the build.
- **Do not give a local skill an authored skill's name.**
  `AuthoredSkillsAreNotInstalledIntoTheAgentDirectories` fails when a folder here collides with one
  under `skills/`.
- **Preserve a default unless the task explicitly changes product behaviour.** A docs task is not
  licence to re-default anything.
- **Do not restate a default in a consumer project file** — recommend deleting the repetition instead.
- **Never force-add `.agents/skills/`.** Everything the CLI writes is reproducible from
  `skills-lock.json`; `InstalledSkillsAreNotTracked` fails if any of it is committed.
- Keep changes small, use Conventional Commits, and do not mix an unrelated refactor into the sweep.

