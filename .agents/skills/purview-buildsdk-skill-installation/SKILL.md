---
name: purview-buildsdk-skill-installation
description: Install, update, list, check and diagnose the agent skills a Purview.BuildSdk package ships. Use for the InstallPurviewSkills, UpdatePurviewSkills, CheckPurviewSkills, FixPurviewSkills, ListPurviewSkills, RecommendPurviewSkills and VerifyPurviewSkills MSBuild targets, for updating skills after a package upgrade, for choosing agents with PurviewSkillsAgents, for any PRSKL diagnostic, for choosing a package runner (bun x, npx, pnpm dlx, bunx, yarn dlx, deno), for skills-lock.json, or when a package ships skills but no agent can see them.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Installing the skills a package ships

For *authoring* skills, use `purview-buildsdk-skill-tests`; for `just` recipes that run these
targets, `purview-buildsdk-skills-justfile`.

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
dotnet msbuild <project> -t:UpdatePurviewSkills    # re-add every skills-lock.json source: refresh, and add new skills
dotnet msbuild <project> -t:CheckPurviewSkills     # only the PurviewSkillsAgents directories exist
dotnet msbuild <project> -t:FixPurviewSkills       # remove the CLI's links from any other agent directory
```

They run **only when named**. None has `BeforeTargets` or `AfterTargets`, nothing in the SDK depends
on them, and `InstallPurviewSkills`, `UpdatePurviewSkills` and `FixPurviewSkills` refuse to run when
something else pulls them into a build, a restore or a design-time build (`PRSKL0009`). Do not hook
them into a build, a `*DependsOn` property or a git hook. If skills are changing on their own,
something has been wired up that should not be.

Name **the solution**. Skills belong to the repository, so each target runs once however many
projects the solution holds: every project hands the work to the same elected leader, and MSBuild
runs it once. Package resolution reads every project in the solution, so a package only one project
references is still found. A single project works too, but sees only its own packages. For `just`
recipes that run these targets, see `purview-buildsdk-skills-justfile`.

## How a skill is resolved, and why the version is always right

There is deliberately **no custom manifest**. A package that ships skills already carries both
halves of the mapping, and `ResolvePurviewSkillSources` reads them:

- `.agents/skills/<name>/` in the restored package says *which* skills it ships.
- The nuspec's `<repository url="..." commit="..." />`, written by SourceLink for every package this
  SDK builds, says which git repository and which **exact commit** produced them.

So the resolved ref is the commit the *restored package version* was built from. An older
`PackageReference` gets that version's skills, not whatever is on the default branch. Compatibility
is exact by construction rather than by convention.

## Updating

```bash
# after bumping the PackageReference
dotnet restore
dotnet msbuild <project> -t:UpdatePurviewSkills
```

`UpdatePurviewSkills` re-adds every unique source recorded in `skills-lock.json` with `skills add`:

- a source a restored package ships is re-added at **that package's commit**, naming every skill
  that package version ships — the same pinned `add` `PRSKL0011` recommends, so the skills match the
  package version referenced;
- every other source is re-added at its latest with `--skill "*"`.

That is the whole update. `skills add` reinstalls every skill it selects, so one re-add both
refreshes the installed skills and adds any the source has gained; `skills update` would only
re-fetch what the lockfile already lists, and is never run. `-p:PurviewSkillsDryRun=true` prints every
command and runs none. Each failed source is named (`PRSKL0008`).

`InstallPurviewSkills` also works as an update for package skills alone: it re-adds each at its
package's commit. Do not reach for a bare `skills update` instead; see below.

## Choosing agents

`PurviewSkillsAgents` is **empty by default**, which passes no `--agent` and lets the CLI auto-detect
the agents on the machine. Set it to choose — in a project file `<PurviewSkillsAgents>universal</PurviewSkillsAgents>`,
or on a command line with the separator escaped or the value quoted, because `dotnet msbuild -p:`
itself splits on `,` and `;`:

```bash
dotnet msbuild <project> -t:UpdatePurviewSkills -p:PurviewSkillsAgents=claude-code%2Ccodex
```

Each agent becomes its own `--agent` flag; the CLI does not split a comma-separated value.

When `PurviewSkillsAgents` is set, `CheckPurviewSkills` fails (`PRSKL0015`) for any agent skill
directory at the repository root that belongs to an agent it does not name. `.agents/skills` is always
allowed — it is the CLI's canonical store — and the authored `skills/` folder is never touched.
`FixPurviewSkills` removes links (symlinks, junctions) from those directories, never their targets,
and then any folder left empty; a real file is kept and reported, because it is not provably the
CLI's. The agent-to-directory map is the `PurviewSkillsAgentDirectory` item group, taken from the
CLI's own agent table; an agent it does not know is `PRSKL0016`, fixed by adding an item:

```xml
<PurviewSkillsAgentDirectory Include=".new-agent/skills" Agents="new-agent" />
```

With `PurviewSkillsAgents` empty, both targets say so and do nothing: every agent directory is then a
legitimate destination.

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
| `PRSKL0004` | No skills CLI and no package runner found | Install Bun or Node, or set `PurviewSkillsCommand` |
| `PRSKL0005` | An authored skill is malformed | See `purview-buildsdk-skill-tests` |
| `PRSKL0006` | Retired, with `PurviewSkillsUpdateMode` | Remove the property; update no longer runs `skills update` |
| `PRSKL0007` | `UpdatePurviewSkills` found no `skills-lock.json` | Install first with `-t:InstallPurviewSkills` |
| `PRSKL0008` | Re-adding a lockfile source failed | Read the CLI output above it |
| `PRSKL0009` | Install, update or fix was pulled into a build, a restore or a design-time build | Remove the hook; run the target on its own |
| `PRSKL0010` | A skill a referenced package ships is missing | Warning. `bun x skills add ...` |
| `PRSKL0011` | An installed skill is pinned to a different ref than the referenced package | Warning. Install the package's own commit |
| `PRSKL0012` | A skill cannot be verified | Message |
| `PRSKL0013` | An orphaned lockfile entry | Message |
| `PRSKL0014` | Recorded in the lockfile but never restored | Message |
| `PRSKL0015` | An agent skill directory exists for an agent `PurviewSkillsAgents` does not name | `-t:FixPurviewSkills`, then remove anything it kept by hand |
| `PRSKL0016` | `PurviewSkillsAgents` names an agent with no known skills directory | Warning. Add a `PurviewSkillsAgentDirectory` item |
| `PRSKL0017` | Projects in the solution reference one skill-shipping package at different versions | Warning. Align the versions; only one version's skills can be installed |

## Nothing about skills runs during a build

This used to be the opposite: a `RecommendPurviewSkillsOnBuild` hook ran on `AfterTargets="Build"`.
It was removed after measurement — on a 39-project solution it read and regex-scanned every project's
`project.assets.json` (≈500 KB, 256 libraries each), made thousands of failing `Directory.Exists`
calls, read the lockfile and walked the repository root, on every build, to print advice that a
repository managing its own skills had already acted on. All of its inputs are repository-scoped, so
the per-project cost bought nothing.

The work is now paid only when somebody names a target:

```bash
dotnet msbuild <project> -t:RecommendPurviewSkills   # report, never fail
dotnet msbuild <project> -t:VerifyPurviewSkills      # report, and fail on PRSKL0010/0011
```

`EvaluatePurviewSkillState` joins the skills each restored package ships against the CLI's own
`skills-lock.json`. Both inputs are local files, so it stays offline and spawns no process.

`VerifyPurviewSkills` is the gate to call from CI. It escalates only `PRSKL0010` (missing) and
`PRSKL0011` (drifted) to errors: both are factual, both are fixed by one command, and neither can fire
without a referenced package that really ships skills. `PRSKL0012` stays a warning because a package
published without repository metadata is not the consumer's to fix, and `PRSKL0013`/`PRSKL0014` stay
messages. Failing over something the caller cannot clear is how a gate gets switched off.

Three things shape the output:

- Missing and drifted are **warnings** (fixable in one command); unverifiable and orphaned are
  **messages**. `VerifyPurviewSkills` turns missing and drifted into failures, and promotes
  unverifiable to a warning.
- A correctly installed set produces no output at all.
- There is no deduplication stamp any more. `EnablePurviewSkillRecommendations`,
  `PurviewSkillRecommendationsInCI` and `PurviewSkillRecommendationStampPath` were retired with the
  hook: a stamp existed so a per-build hook could report one state once per repository, and a target
  a person invokes should answer every time it is asked.

Orphan detection is **owner-scoped on purpose**: a lockfile cannot record who asked for an entry, so
only an entry under an owner the project references — but from a repository it no longer references
— is reported. A skill from any other owner is the developer's own, and is never mentioned.

### Drift never recommends a bare `skills update`

Re-adding a source without a ref installs the *newest* published skill, which is only correct when
the newest skill happens to describe the referenced package version. `PRSKL0011` names the referenced
package's own commit in a pinned `skills add` instead — which is what `-t:InstallPurviewSkills` and
`-t:UpdatePurviewSkills` run for package skills.

## Package runners

The CLI is published to npm, and every runner spells "run this npm package" differently.
`PurviewSkillsRunner` selects one, defaulting to `bun` (`bun x skills`).

| Runner | Invocation |
| -- | -- |
| `bun` *(default)* | `bun x skills@1` |
| `npx` | `npx -y skills@1` |
| `bunx` | `bunx skills@1` |
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
the same set later with `bun x skills experimental_install`.

The lockfile is read **directly** rather than by parsing `skills list --json`, because that
listing omits the `ref` — and the ref is the whole compatibility question. Reading a file also needs
no Node tooling, which is what keeps recommendations working on a machine that can build .NET but
has no Node. There is deliberately no competing lockfile.
