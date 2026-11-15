# The agent-skill recipes

`skills.just` gives each SDK skill target a recipe. The targets are a thin wrapper over the standard
skills CLI (`bun x skills` by default), which finds a repository's skills in its root `skills/`
folder and records what it installed in `skills-lock.json`. Their behaviour is covered by
`purview-buildsdk-skill-installation`; this page is about the recipes that run them.

## The rule that matters most

**Every skill recipe is run by a person, on purpose.** `InstallPurviewSkills`, `UpdatePurviewSkills`
and `FixPurviewSkills` change the repository, and the SDK refuses to run them inside a build, a
restore or a design-time build (`PRSKL0009`). Keep the `Justfile` consistent with that:

- never make a skills recipe a dependency of `build`, `test`, `default` or any other recipe;
- never call `skills-add`, `skills-install`, `skills-update` or `skills-fix` from a git hook
  (`lefthook.yml`, `.husky/`) or from a CI job that commits — `PRSKL0009` cannot see those, because they
  run the target on its own;
- `skills-check` is read-only and safe anywhere.

## The recipes

| Recipe | Target | Runs | Changes the repository |
| -- | -- | -- | -- |
| `skills-add <owner/repo[#ref]>` | `InstallPurviewSkills` with `PurviewSkillsSource` | `skills add <source> --skill "*"` | yes |
| `skills-install` | `InstallPurviewSkills` | `skills experimental_install` — restore `skills-lock.json` | yes |
| `skills-update` | `UpdatePurviewSkills;FixPurviewSkills` | `skills add <source> --skill "*"` once per lockfile source | yes |
| `skills-check` | `CheckPurviewSkills` | nothing | no |
| `skills-fix` | `FixPurviewSkills` | nothing; removes links itself | yes |

A repository that *authors* skills usually adds `skills-validate` to its root `Justfile`. It runs
`ValidatePurviewSkills` against the one project that packs, because that target runs once per
project it is invoked on:

```just
[group('Agent Skills')]
skills-validate *args:
    dotnet msbuild "src/src/Acme/Acme.csproj" -t:ValidatePurviewSkills -v:n {{ args }}
```

## Why the recipes name the solution

`skills_project` defaults to `solution_file`. Every skill target runs once for the whole solution,
however many projects it holds: each project hands the work to the same elected leader, and MSBuild
runs it once. A single project works too. An old `Justfile.skills.just` pointed at a hand-picked
"small" project gets the same treatment: delete it, and let `skills.just` name the solution.

## Details that break a recipe if missed

- **Quote a multi-target `-t:`.** `"-t:UpdatePurviewSkills;FixPurviewSkills"` — unquoted, `sh` ends the
  command at the `;` and runs `FixPurviewSkills` as a separate, failing command. The same holds under
  PowerShell.
- **Keep `*args` last** so a caller can pass properties: `just skills-update -p:PurviewSkillsDryRun=true`.
- `FixPurviewSkills` after `UpdatePurviewSkills` does nothing unless `PurviewSkillsAgents` is set, so
  it is safe in the recipe either way.
- `skills-install` with no lockfile fails with `PRSKL0007`: add a source first with `skills-add`.

## Runner and agents belong in MSBuild, not in the recipe

Set repository-wide choices once in `Directory.Build.props`, not as flags repeated in each recipe, so
a plain `dotnet msbuild -t:...` behaves the same as the recipe:

```xml
<PropertyGroup>
  <!-- Defaults: bun ('bun x skills'), and no agents (the skills CLI auto-detects). -->
  <PurviewSkillsRunner>bun</PurviewSkillsRunner>
  <!-- Optional. Setting it makes skills-check/skills-fix enforce that only these agents'
       skill directories exist. -->
  <PurviewSkillsAgents>universal</PurviewSkillsAgents>
</PropertyGroup>
```

One-off overrides go on the command line. `dotnet msbuild -p:` splits values on `,` and `;`, so escape
the separator: `just skills-update -p:PurviewSkillsAgents=claude-code%2Ccodex`.

| Property | Effect |
| -- | -- |
| `PurviewSkillsDryRun=true` | Print every CLI command; run none |
| `PurviewSkillsRunner=npx` | Launch the CLI through another runner (`bunx`, `pnpm`, `yarn`, `deno`) |
| `PurviewSkillsSource=owner/repo#ref` | What `skills-add` sets: the source to install from |

## Replacing hand-written skill scripts

| Hand-written | Replace with |
| -- | -- |
| `scripts/update-agent-skills.ts`, `"skills:update"` | `skills-update` (`UpdatePurviewSkills`) |
| `--update` / `--no-update` | Nothing: the re-add refreshes installed skills and adds new ones, so there is no separate `skills update` step to switch |
| `--dry-run` | `-p:PurviewSkillsDryRun=true` |
| `scripts/ensure-universal-skills.ts check`, `"skills:check"` | `skills-check` with `PurviewSkillsAgents=universal` |
| `scripts/ensure-universal-skills.ts fix`, `"skills:fix"` | `skills-fix` |
| a hard-coded `-a universal` | `PurviewSkillsAgents` — or leave it empty to auto-detect |

Update is `skills add`, not `skills update`, so it also installs skills a source has **gained**: each
source is re-added with `--skill "*"`. Mention that when migrating.

Before deleting the scripts, run `just skills-update -p:PurviewSkillsDryRun=true` and compare the
printed commands with what the old script ran. Then delete the scripts and their `package.json`
entries — but keep `package.json` itself, which Purview.BuildSdk reads the version from.

An existing `skills-recommend`, `skills-verify` or `skills-list` recipe has no target behind it any
more: delete it, and its callers.
