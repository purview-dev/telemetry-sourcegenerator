---
name: purview-buildsdk-skill-installation
description: Install, update, check and tidy agent skills in a repository that uses Purview.BuildSdk, through the standard skills CLI (bun x skills). Use for the InstallPurviewSkills, UpdatePurviewSkills, CheckPurviewSkills and FixPurviewSkills MSBuild targets, for installing skills from a repository's root skills/ folder (owner/repo#ref), for restoring skills-lock.json in a fresh clone, for choosing agents with PurviewSkillsAgents, for any PRSKL diagnostic, for choosing a package runner (bun x, npx, pnpm dlx, bunx, yarn dlx, deno), or when an agent cannot see a skill that should be installed.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Installing agent skills

For *authoring* skills, use `purview-buildsdk-skill-tests`; for `just` recipes that run these
targets, `purview-buildsdk-justfile`.

## The one thing to understand first

**The SDK is a thin wrapper over the standard skills CLI, and adds nothing bespoke.** A repository
publishes skills in its root `skills/` folder; the skills CLI installs them with
`bun x skills add owner/repo` and records each install in `skills-lock.json`. The SDK's targets only
run that CLI — from the repository root, once per solution, never during a build — and keep the
agent directories tidy. There is no package-to-skill mapping: a NuGet package carries .NET artifacts,
and skills come from git.

The SDK writes nothing into an agent directory itself. The CLI is the single writer, because its
universal install location is `.agents/skills/` and a second writer on the same paths would overwrite
a pinned install.

```bash
dotnet msbuild <solution> -t:InstallPurviewSkills -p:PurviewSkillsSource=purview-dev/build-sdk   # add a source
dotnet msbuild <solution> -t:InstallPurviewSkills     # restore skills-lock.json (a fresh clone)
dotnet msbuild <solution> -t:UpdatePurviewSkills      # re-add every lockfile source at its latest
dotnet msbuild <solution> -t:CheckPurviewSkills       # only the PurviewSkillsAgents directories exist
dotnet msbuild <solution> -t:FixPurviewSkills         # remove the CLI's links from any other agent directory
```

They run **only when named**. None has `BeforeTargets` or `AfterTargets`, nothing in the SDK depends
on them, and `InstallPurviewSkills`, `UpdatePurviewSkills` and `FixPurviewSkills` refuse to run when
something pulls them into a build, a restore or a design-time build (`PRSKL0009`). A git hook or a
`just` recipe dependency that runs one on its own is not caught by that — do not wire them up that way.

Name **the solution**. Skills belong to the repository, so each target runs once however many projects
the solution holds: every project hands the work to the same elected leader, and MSBuild runs it once.
A single project works too.

## Installing

| Command | Runs |
| -- | -- |
| `-t:InstallPurviewSkills -p:PurviewSkillsSource=owner/repo` | `skills add "owner/repo" --skill "*"` — every skill in that repository's root `skills/` |
| `-t:InstallPurviewSkills -p:PurviewSkillsSource=owner/repo#<ref>` | the same, pinned to a commit, tag or branch |
| `-t:InstallPurviewSkills` | `skills experimental_install` — restore exactly what `skills-lock.json` records |

The caller owns the ref: append `#<ref>` to pin it. With no source and no `skills-lock.json` there is
nothing to install, which is `PRSKL0007`. `bun x skills add` works just as well run directly; the
target exists so a repository uses one runner, one root and one agent selection everywhere.

## Updating

```bash
dotnet msbuild <solution> -t:UpdatePurviewSkills -p:PurviewSkillsDryRun=true   # see the commands first
dotnet msbuild <solution> -t:UpdatePurviewSkills
```

`UpdatePurviewSkills` re-adds every unique source recorded in `skills-lock.json` with
`skills add "<source>" --skill "*"`, at its latest. That is the whole update: `skills add` reinstalls
every skill it selects, so one re-add both refreshes the installed skills and adds any the source has
gained, whereas `skills update` would only re-fetch what the lockfile already lists, and is never run.
Every source is attempted even when one fails, and each failure is named (`PRSKL0008`). No lockfile is
`PRSKL0007`.

## Choosing agents

`PurviewSkillsAgents` is **empty by default**, which passes no `--agent` and lets the CLI auto-detect
the agents on the machine. Set it to choose — in `Directory.Build.props`
(`<PurviewSkillsAgents>universal</PurviewSkillsAgents>`), or on a command line with the separator
escaped, because `dotnet msbuild -p:` itself splits on `,` and `;`:

```bash
dotnet msbuild <solution> -t:UpdatePurviewSkills -p:PurviewSkillsAgents=claude-code%2Ccodex
```

Each agent becomes its own `--agent` flag; the CLI does not split a comma-separated value. A restore
(`experimental_install`) is passed no agent flag.

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

## Diagnostics

| Code | Meaning | Fix |
| -- | -- | -- |
| `PRSKL0003` | No repository root resolved | Pass `-p:PurviewSkillsInstallRoot=<path>`, or add an `AGENTS.md` at the root |
| `PRSKL0004` | No skills CLI and no package runner found | Install Bun or Node, or set `PurviewSkillsCommand` |
| `PRSKL0005` | An authored skill is malformed | See `purview-buildsdk-skill-tests` |
| `PRSKL0007` | Nothing to install or update: no `PurviewSkillsSource`, and no `skills-lock.json` | Install from a source first |
| `PRSKL0008` | Re-adding a lockfile source failed | Read the CLI output above it |
| `PRSKL0009` | Install, update or fix was pulled into a build, a restore or a design-time build | Remove the hook; run the target on its own |
| `PRSKL0015` | An agent skill directory exists for an agent `PurviewSkillsAgents` does not name | `-t:FixPurviewSkills`, then remove anything it kept by hand |
| `PRSKL0016` | `PurviewSkillsAgents` names an agent with no known skills directory | Warning. Add a `PurviewSkillsAgentDirectory` item |

`PRSKL0001`, `PRSKL0002`, `PRSKL0006`, `PRSKL0010`–`PRSKL0014` and `PRSKL0017` are retired: they
belonged to the package-to-skill mapping and recommendation targets, which are gone.

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
| `skills` | `skills` — already on `PATH`, no download |

An unknown runner falls through to `<runner> $(PurviewSkillsPackage)`, so it still works. The launcher
pins the CLI to a major (`PurviewSkillsPackage`, default `skills@1`).

`DetectPurviewSkillsCli` probes `PATH` for `skills` first, then `$(PurviewSkillsRunner)`, then
`$(PurviewSkillsRunnerCandidates)`, honouring `PATHEXT` on Windows where these are `.cmd` shims.
`PATH` is read directly rather than by spawning a version check: deterministic, no process, and it
cannot hang on a cold download. **The runner actually found wins over the configured preference.**
`PurviewSkillsCommand` overrides the launcher entirely and skips detection. A dry run with no runner
on `PATH` prints the configured runner's command rather than failing.

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
directory through the tree, one per project. An unresolved root is `PRSKL0003`.

The marker walk is described in [references/repository-root.md](references/repository-root.md); its
two-pass behaviour and its home-directory bound are both load-bearing.

## Why the CLI rather than a reimplementation

The CLI owns the agent-specific destinations and their discovery conventions, which differ per agent
and change independently of this SDK. It also owns `skills-lock.json`, which is what lets a fresh
clone restore the same set with `-t:InstallPurviewSkills` (`skills experimental_install`). There is
deliberately no competing lockfile and no competing metadata.
