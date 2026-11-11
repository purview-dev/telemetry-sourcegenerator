---
name: purview-buildsdk-skills-justfile
description: Add or update Justfile recipes that run Purview.BuildSdk's agent-skill targets - skills-update, skills-check, skills-fix, skills-install, skills-recommend and skills-verify. Use when a repository wants a just recipe to update, install, check or clean up its agent skills, when replacing hand-written skill scripts (update-agent-skills.ts, ensure-universal-skills.ts, package.json skills:* scripts) with the SDK targets, or when an existing skills recipe runs the wrong project, the wrong runner or the wrong agents.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Justfile recipes for the agent-skill targets

The recipes are thin: each one names one SDK target. The behaviour lives in the targets, which are
covered by `purview-buildsdk-skill-installation`; this skill is about wiring them into a `Justfile`
correctly.

## The rule that matters most

**Every skill recipe is run by a person, on purpose.** `InstallPurviewSkills`, `UpdatePurviewSkills`
and `FixPurviewSkills` change the repository, and the SDK refuses to run them inside a build, a
restore or a design-time build (`PRSKL0009`). Keep the Justfile consistent with that:

- never make a skills recipe a dependency of `build`, `test`, `default` or any other recipe;
- never call `skills-update`, `skills-install` or `skills-fix` from a git hook (`lefthook.yml`,
  `.husky/`) or a CI job that commits;
- `skills-verify` is the one recipe CI may call: it is read-only and fails on missing or drifted skills.

## Recipes

Put them in their own file beside the `Justfile`, and import it from the `Justfile`:

```just
# Justfile
import "Justfile.skills.just"
```

The only line that differs between repositories is `skills_project`. Work it out from the repository
as described in [Choosing `skills_project`](#choosing-skills_project) — do not ask for it, and do not
leave a placeholder:

```just
skills_project := solution_file    # or a path, when the Justfile has no such variable

# Update agent skills: re-add every skills-lock.json source, refreshing installed skills and adding new ones
[group('Agent skills')]
skills-update *args:
    dotnet msbuild "{{ skills_project }}" "-t:UpdatePurviewSkills;FixPurviewSkills" -v:n {{ args }}

# Check that only the skill directories of the agents in PurviewSkillsAgents exist
[group('Agent skills')]
skills-check *args:
    dotnet msbuild "{{ skills_project }}" -t:CheckPurviewSkills -v:n {{ args }}

# Remove the skills CLI's links from agent directories PurviewSkillsAgents does not select
[group('Agent skills')]
skills-fix *args:
    dotnet msbuild "{{ skills_project }}" -t:FixPurviewSkills -v:n {{ args }}

# Install the skills the referenced packages ship (pass -p:PurviewSkillsSource=owner/repo for others)
[group('Agent skills')]
skills-install *args:
    dotnet msbuild "{{ skills_project }}" -t:InstallPurviewSkills -v:n {{ args }}

# Report missing, drifted or unverifiable skills without changing anything
[group('Agent skills')]
skills-recommend *args:
    dotnet msbuild "{{ skills_project }}" -t:RecommendPurviewSkills -v:n {{ args }}

# Fail when a referenced package's skills are missing or drifted - the CI gate
[group('Agent skills')]
skills-verify *args:
    dotnet msbuild "{{ skills_project }}" -t:VerifyPurviewSkills -v:n {{ args }}
```

Only add the recipes the repository will use. `skills-update` and `skills-check` are the usual pair.

### Choosing `skills_project`

Take the first of these that applies:

1. **The `Justfile` already names the solution in a variable** — `solution_file`, `solution`, `sln`
   or similar, typically used by `build`, `test` and `restore`. Reference it:
   `skills_project := solution_file`. An imported file shares the importing file's variables, so the
   skill recipes then follow the solution if it is ever renamed or moved, with nothing to keep in step.
2. **An existing recipe runs `dotnet build`, `test` or `restore` on a literal solution path.** Use that
   path. If several recipes repeat it, offer to lift it into a `solution_file` variable and reference
   that, but do not restructure the `Justfile` unasked.
3. **No `Justfile`, or one that never names a solution.** Search for `*.slnx` and `*.sln`, ignoring
   `bin/`, `obj/`, `node_modules/` and `artifacts/`. If there is exactly one, use its path relative to
   the `Justfile`. With several, prefer the one the CI workflow or README builds; ask only if that
   still leaves more than one.
4. **No solution at all.** Name a project that imports the SDK and references every package that
   ships skills, and say in a comment that it sees only its own packages.

An existing `Justfile.skills.just` pointed at a hand-picked project gets the same treatment: replace
the project with the solution, chosen as above. Remove any comment explaining why that project was
picked, because the reason no longer holds.

### Details that break a recipe if missed

- **Name the solution.** Every skill target runs once for the whole solution, however many projects
  it holds: each project hands the work to the same elected leader, and MSBuild runs it once. Package
  resolution reads every project in the solution, so a package only a test project references is
  still pinned. There is no project to choose and nothing to keep in step as projects come and go.
  A single project also works, but sees only that project's packages. (SDK releases before this
  behaviour ran the target once per project; there, name one project that references every package
  that ships skills.)
- **Restore first.** The targets read `project.assets.json` and never restore on their own
  (`PRSKL0001`). A fresh clone needs `dotnet restore` (or `just restore`) before `skills-update`.
- **Quote a multi-target `-t:`.** `"-t:UpdatePurviewSkills;FixPurviewSkills"` — unquoted, `sh` treats
  the `;` as the end of the command and runs `FixPurviewSkills` as a separate, failing command.
- **Keep `*args` last** so a caller can pass properties: `just skills-update -p:PurviewSkillsDryRun=true`.
- `FixPurviewSkills` after `UpdatePurviewSkills` does nothing unless `PurviewSkillsAgents` is set, so
  it is safe in the recipe either way.

## Runner and agents belong in MSBuild, not in the recipe

Set repository-wide choices once in `Directory.Build.props`, not as flags repeated in each recipe,
so a plain `dotnet msbuild -t:...` behaves the same as the recipe:

```xml
<PropertyGroup>
  <!-- Defaults: bun ('bun x skills'), and no agents (the skills CLI auto-detects). -->
  <PurviewSkillsRunner>bun</PurviewSkillsRunner>
  <!-- Optional. Setting it makes skills-check/skills-fix enforce that only these agents'
       skill directories exist. -->
  <PurviewSkillsAgents>universal</PurviewSkillsAgents>
</PropertyGroup>
```

One-off overrides go on the command line. `dotnet msbuild -p:` splits values on `,` and `;`, so
escape the separator: `just skills-update -p:PurviewSkillsAgents=claude-code%2Ccodex`.

Useful properties to pass through `*args`:

| Property | Effect |
| -- | -- |
| `PurviewSkillsDryRun=true` | Print every CLI command; run none |
| `PurviewSkillsRunner=npx` | Launch the CLI through another runner (`bunx`, `pnpm`, `yarn`, `deno`) |
| `PurviewSkillsSource=owner/repo#ref` | `skills-install` from an explicit source with no package behind it |

## Replacing hand-written skill scripts

Repositories often carry their own scripts for this. The SDK targets replace them:

| Hand-written | Replace with |
| -- | -- |
| `scripts/update-agent-skills.ts`, `"skills:update"` | `skills-update` (`UpdatePurviewSkills`) |
| `--update` / `--no-update` | Nothing: the re-add refreshes installed skills and adds new ones, so there is no separate `skills update` step to switch |
| `--dry-run` | `-p:PurviewSkillsDryRun=true` |
| `scripts/ensure-universal-skills.ts check`, `"skills:check"` | `skills-check` with `PurviewSkillsAgents=universal` |
| `scripts/ensure-universal-skills.ts fix`, `"skills:fix"` | `skills-fix` |
| a hard-coded `-a universal` | `PurviewSkillsAgents` — or leave it empty to auto-detect |

Two behaviours differ, deliberately — mention them when migrating:

- A source a referenced package ships is re-added **at that package's commit**, not at latest, so its
  skills keep matching the package version. Other sources are re-added at latest, as before.
- Update is `skills add`, not `skills update`, so it also installs skills a source has **gained**: a
  package source gets every skill its referenced version ships, and any other source is re-added with
  `--skill "*"`.

After switching, delete the scripts and their `package.json` entries, and remove `package.json`
itself if nothing else uses it. Run `just skills-update -p:PurviewSkillsDryRun=true` and compare the
printed commands with what the old script ran before deleting anything.

## Updating an existing Justfile

1. Read the whole `Justfile` first: match its group names, comment style, and any `set shell`
   (a PowerShell shell still needs the `-t:` value quoted).
2. Keep recipe names the repository already uses; change only what they run. If `skills-check`
   already means "report", rename it to `skills-recommend` rather than silently changing its meaning,
   and say so. A recipe pointed at a hand-picked "small" project, to avoid running once per project,
   can point at the solution instead.
3. Check nothing depends on a changing recipe: `just --list`, then search the Justfile, CI workflows
   and hook configuration for the recipe name.
4. Verify: `just --list` shows the group, `just skills-update -p:PurviewSkillsDryRun=true` prints the
   commands, and `just skills-check` passes or names a real problem.
