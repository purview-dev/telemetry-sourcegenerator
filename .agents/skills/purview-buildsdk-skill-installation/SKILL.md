---
name: purview-buildsdk-skill-installation
description: Install, list and diagnose the agent skills a Purview.BuildSdk package ships. Use for the InstallPurviewSkills, ListPurviewSkills and RecommendPurviewSkills MSBuild targets, for any PRSKL0001-PRSKL0014 diagnostic, for choosing a package runner (npx, pnpm dlx, bunx, yarn dlx, deno), for skills-lock.json, or when a package ships skills but no agent can see them.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Installing the skills a package ships

For *authoring* skills, use `purview-buildsdk-skill-tests`.

## The one thing to understand first

**The SDK writes nothing into an agent directory.** A package *ships* `.agents/skills/<name>/`, but
as metadata — it is what tells the SDK which skills the package offers and at which commit. Nothing
copies it out.

Skills are installed by the skills CLI and by nothing else. That is a deliberate single-writer rule.
Earlier versions of this SDK mirrored a package's whole `.agents/**` folder into the consuming
repository, and the CLI's own universal install location is `.agents/skills/` — the same paths. The
mirror would silently overwrite a pinned install on the next build, while `PRSKL0011` compared a
lockfile ref against content the mirror had already replaced. The mirror was removed outright rather
than fenced off, because the CLI covers the whole job.

Installing is therefore a separate, deliberately **explicit** step. It is never a side effect of a
restore, a build, a design-time build or a test run.

```bash
dotnet msbuild <project> -t:ListPurviewSkills      # diagnostics only, installs nothing
dotnet msbuild <project> -t:InstallPurviewSkills   # delegates to the skills CLI
```

Neither target has `BeforeTargets` or `AfterTargets`. If skills are appearing or changing on their
own, something has been wired up that should not be.

## How a skill is resolved, and why the version is always right

There is deliberately **no custom manifest**. A package that ships skills already carries both
halves of the mapping, and `ResolvePurviewSkillSources` reads them:

- `.agents/skills/<name>/` in the restored package says *which* skills it ships.
- The nuspec's `<repository url="..." commit="..." />`, written by SourceLink for every package this
  SDK builds, says which git repository and which **exact commit** produced them.

So the resolved ref is the commit the *restored package version* was built from. An older
`PackageReference` gets that version's skills, not whatever is on the default branch. Compatibility
is exact by construction rather than by convention.

## Installing from an explicit source

For a skill you want but have no `PackageReference` for:

```bash
dotnet msbuild <project> -t:InstallPurviewSkills -p:PurviewSkillsSource=purview-dev/build-sdk
```

The caller owns the ref here — append `#<ref>` to pin it. This path needs no restore, so
`PRSKL0001` does not apply.

## Diagnostics

| Code | Meaning | Fix |
| -- | -- | -- |
| `PRSKL0001` | `project.assets.json` is missing | Restore. Resolution reads the restore graph and deliberately does not trigger a restore of its own |
| `PRSKL0002` | The package's nuspec has no GitHub repository URL, or no commit | The package cannot be pinned. It is reported rather than installed from a guessed branch |
| `PRSKL0003` | No repository root resolved | Pass `-p:PurviewSkillsInstallRoot=<path>` |
| `PRSKL0004` | No skills CLI and no package runner found | Install Node, or set `PurviewSkillsCommand` |
| `PRSKL0005` | An authored skill is malformed | See `purview-buildsdk-skill-tests` |
| `PRSKL0010` | A skill a referenced package ships is missing | Warning. `npx skills add ...` |
| `PRSKL0011` | An installed skill is pinned to a different ref than the referenced package | Warning. Install the package's own commit |
| `PRSKL0012` | A skill cannot be verified | Message |
| `PRSKL0013` | An orphaned lockfile entry | Message |
| `PRSKL0014` | Recorded in the lockfile but never restored | Message |

## Recommendations run during an ordinary build

This is the **one** skill feature that does. `RecommendPurviewSkillsOnBuild` runs after `Build` and
delegates to `RecommendPurviewSkills`, which can also be invoked by name:

```bash
dotnet msbuild <project> -t:RecommendPurviewSkills
```

`EvaluatePurviewSkillState` joins the skills each restored package ships against the CLI's own
`skills-lock.json`. Both inputs are local files, so it stays offline and spawns no process.

Four things keep it quiet, and all of them are load-bearing:

- Design-time builds, CI builds, non-default TFM passes and
  `EnablePurviewSkillRecommendations=false` are skipped outright.
  `PurviewSkillRecommendationsInCI=true` re-enables CI.
- A fingerprint stamp at `<repo root>/.purview/skill-recommendations.cache` means a given state is
  reported once per repository, not once per project and again on every rebuild. The explicit target
  ignores the stamp.
- Missing and drifted are **warnings** (fixable in one command); unverifiable and orphaned are
  **messages**. No amount of skill advice can break a build using `TreatWarningsAsErrors`.
- A correctly installed set produces no output at all.

Orphan detection is **owner-scoped on purpose**: a lockfile cannot record who asked for an entry, so
only an entry under an owner the project references — but from a repository it no longer references
— is reported. A skill from any other owner is the developer's own, and is never mentioned.

### Drift never recommends `npx skills update`

`update` installs the *newest* published skill, which is only correct when the newest skill happens
to describe the referenced package version. `PRSKL0011` names the referenced package's own commit in
a pinned `npx skills add` instead.

## Package runners

The CLI is published to npm, and every runner spells "run this npm package" differently.
`PurviewSkillsRunner` selects one, defaulting to `npx` because npx ships with Node itself.

| Runner | Invocation |
| -- | -- |
| `npx` | `npx -y skills@1` |
| `bunx` | `bunx skills@1` |
| `bun` | `bun x skills@1` |
| `pnpm` | `pnpm dlx skills@1` |
| `yarn` | `yarn dlx skills@1` — Yarn 2+ only |
| `deno` | `deno run -A npm:skills@1` |

An unknown runner falls through to `<runner> $(PurviewSkillsPackage)`, so it still works.

`DetectPurviewSkillsCli` probes `PATH` for `skills` first, then `$(PurviewSkillsRunner)`, then
`$(PurviewSkillsRunnerCandidates)`, honouring `PATHEXT` on Windows where these are `.cmd` shims.
`PATH` is read directly rather than by spawning a version check: deterministic, no process, and it
cannot hang on a cold download. **The runner actually found wins over the configured preference.**
`PurviewSkillsCommand` overrides the launcher entirely and skips detection.

`ListPurviewSkills` reports runner availability but **never** errors on its absence — resolving and
reporting needs no Node tooling at all.

## Always the repository root

`skills-lock.json` and the agent directories belong to the repository, not to a project, so every
skill target runs from one resolved root:

1. `PurviewSkillsInstallRoot` — an explicit override
2. `PurviewSkillsRepoRoot` — only when the **user** set it
3. the `PurviewRepositoryRootMarkers` walk
4. `PurviewSkillsRepoRoot` / `RepoRoot` as the SDK guessed them
5. CI workspace variables
6. SourceLink's git `SourceRoot`

There is deliberately **no project-directory fallback**. That would scatter a lockfile and an agent
directory through the tree, one per project. An unresolved root is `PRSKL0003` on install, and
silence for recommendations — a build never fails over advice.

The marker walk is described in [references/repository-root.md](references/repository-root.md); its
two-pass behaviour and its home-directory bound are both load-bearing.

## Why the CLI rather than a reimplementation

The CLI owns the agent-specific destinations and their discovery conventions, which differ per agent
and change independently of this SDK. It also owns `skills-lock.json`, so a repository can restore
the same set later with `npx skills experimental_install`.

`PurviewSkillsAgents` (default `*`) narrows the target agents.

The lockfile is read **directly** rather than by parsing `npx skills list --json`, because that
listing omits the `ref` — and the ref is the whole compatibility question. Reading a file also needs
no Node tooling, which is what keeps recommendations working on a machine that can build .NET but
has no Node. There is deliberately no competing lockfile.
