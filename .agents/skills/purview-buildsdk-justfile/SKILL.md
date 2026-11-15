---
name: purview-buildsdk-justfile
description: Set up or update the Justfile of a repository that uses Purview.BuildSdk - a root Justfile that imports one barrel, .build/just/index.just, over shared modules for .NET (restore, build, test, pack, ensure-tools), version display and `just bump major|minor|patch`, local Purview.Build pipeline runs (pipeline-pr, pipeline-local-release), the agent-skill targets (skills-add, skills-install, skills-update, skills-check, skills-fix), lint-check/lint-fix, vs, scrub, an optional pm-docs-site Docs group, and exported telemetry opt-outs and .NET CLI speed-ups. Use when adding a Justfile, splitting or de-duplicating one, adding a just recipe, bumping the version from just, replacing hand-written skill scripts (update-agent-skills.ts, package.json skills:* scripts) with the SDK targets, or when a recipe runs the wrong project, runner or agents.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# The Justfile

A repository's recipes are mostly the same everywhere: restore, build, test, pack, lint, the
pipeline, the skill targets. Written into each `Justfile` by hand they drift, and every repository
fixes the same quoting bug separately. So the shared recipes are **modules**, copied verbatim from
this skill's [`assets/just/`](assets/just/index.just) into the repository, and the root `Justfile`
holds only what is specific to that repository.

```text
Justfile                  settings, one import, the repository's variables and own recipes
.build/just/
  index.just              the barrel: imports every module, optional ones with import?
  env.just                exported telemetry opt-outs and .NET CLI speed-ups
  version.just            version, bump
  bump-version.ts         what `just bump` runs (Bun)
  dotnet.just             ensure-tools, restore, build, clean, test, pack
  utilities.just          lint-check, lint-fix, vs, scrub
  purview-build.just      pipeline-pr, pipeline-build, pipeline-tests
  skills.just             skills-add, -install, -update, -check, -fix
  purview-build-nuget.just  optional: pipeline-pack-validate, pipeline-local-release, pipeline-release
  docs.just               optional: docs, docs-install, docs-check
```

`.build/` is not matched by the stock `.gitignore` templates — `[Bb]uild/` does not match a leading
dot — but confirm it with `git check-ignore -v .build/just/index.just` (no output, exit 1, means
tracked). If a rule matches, add `!/.build/` after it.

## The root Justfile

```just
# Recipes for <repository>. Run `just` for the list.
#
# The shared recipes come from .build/just/, copied verbatim from the purview-buildsdk-justfile
# skill. Change them there, not here; this file holds what belongs to this repository.

set quiet

# Let a definition here override a module's default variable or no-op hook.
set allow-duplicate-variables
set allow-duplicate-recipes

import ".build/just/index.just"

solution_file := "src/Acme.slnx"

[private]
default:
    just --list
```

**Import the barrel and nothing else.** Adding or removing a module never touches the root file.

Both `allow-duplicate-*` settings are required. Every module gives its variables a default, and a
definition in the root `Justfile` replaces it — regardless of where it sits relative to the import.
Without the settings, a duplicate is an error.

## Variables the modules read

Override only what differs from the default. `solution_file` is the one nearly every repository sets.

| Variable | Module | Default | Notes |
| -- | -- | -- | -- |
| `solution_file` | `dotnet.just` | `.` | the `.slnx`/`.sln` every recipe builds; `.` lets dotnet find the only one in the root |
| `build_configuration` | `dotnet.just` | `Debug` | |
| `artifacts_folder` | `dotnet.just` | `./artifacts` | `pack` output; `scrub` deletes it |
| `test_filter` | `dotnet.just` | `/*/*/*/*/` | MTP tree-node filter, `just test` with no argument |
| `version_file` | `version.just` | `package.json` | read by just itself, no Bun needed |
| `current_version` | `version.just` | from `version_file` | shown by `build`, `pack`, `version` |
| `csharpier` | `utilities.just` | `dotnet csharpier` | |
| `pipeline_tool` | `purview-build.just` | `dotnet purview-build` | |
| `skills_project` | `skills.just` | `solution_file` | always the solution — see [the skill recipes](references/skills-recipes.md) |
| `docs_site` | `docs.just` | `src/docs-site` | pm-docs-site's `SITE_ROOT` |

To find `solution_file`: an existing `Justfile` variable or the path its `build`/`test` recipes use;
otherwise the only `*.slnx`/`*.sln` outside `bin/`, `obj/`, `node_modules/` and `artifacts/`; with
several, the one CI builds. Ask only if that still leaves more than one.

## Hooks

`_after-bump` runs after a successful `just bump` and does nothing by default. A repository that
pins its version anywhere else — docs, templates — overrides it in the root `Justfile`:

```just
_after-bump: version-sync
```

## Optional modules

The barrel imports these with `import?`, so **the file's presence is the switch**: copy it into
`.build/just/` to turn it on, delete it to turn it off. Never edit `index.just` for this.

- **`purview-build-nuget.just`** — copy it when the repository packs NuGet packages: any project is
  packable, or `.config/purview-build.json` has a `PackValidation` section. Pack validation reads
  `PackValidation.RequiredContent`, which lists every file each package must contain. A packed file
  that it does not list fails the run (`contains undeclared content ... not defined in
  RequiredContent`), so a new file under a package's `Sdk/`, `build/` or `buildTransitive/` needs an
  entry there as well.
- **`docs.just`** — copy it when the repository has a pm-docs-site project site: a `pm-docs-site.json`
  somewhere, usually `src/docs-site/`. Set `docs_site` to that folder when it is not `src/docs-site`.
  This module **is** the `Docs` group pm-docs-site's spec asks for, so do not add a second one to the
  root `Justfile`.

## The environment

`env.just` exports, to every process a recipe starts:

- **Telemetry opt-outs**: the .NET CLI, MSBuild, Aspire, the Azure SDKs and Azure MCP, PowerShell,
  Git Credential Manager, Next.js, Astro, Yarn, Claude Code, plus `DO_NOT_TRACK`.
- **.NET CLI speed and noise**: `DOTNET_CLI_ENABLEAOT` (the .NET 11 NativeAOT CLI host) and
  `DOTNET_NOLOGO`.

The MSBuild server (`DOTNET_CLI_USE_MSBUILD_SERVER`) is deliberately **not** forced on. One shared,
long-lived MSBuild process keeps the NuGet SDK resolver's settings from the first build. A later build
under a different `nuget.config` then resolves with the wrong sources and fallback folders, and fails
with `MSB4242`. Purview.BuildSdk's own package-consumption tests do exactly that, and fail. Leave it to
the SDK default unless every build on the machine shares one NuGet configuration.

Each is `env(NAME, default)`, so a value already in the environment — a developer who opted back in, a
CI job — wins. Multithreaded MSBuild (`-mt`) is not set here: Purview.BuildSdk bootstraps it into
`Directory.Build.rsp`, which a plain `dotnet build` and an IDE also read.

## Version

`just version` prints the version from `package.json`. `just bump major|minor|patch` increments that
part and resets the parts after it — `2.0.1` → `3.0.0` / `2.1.0` / `2.0.2` — dropping any
pre-release suffix. It rewrites only the `"version"` value, so the file keeps its formatting.
`just bump patch --dry-run` prints the result and writes nothing. Any other part is rejected before
anything runs. It needs Bun.

## Adopting it, or migrating an existing Justfile

1. **Read the whole existing `Justfile`**, any `*.just` it imports, CI workflows, and hook config
   (`.config/lefthook.yml`, `lefthook.yml`, `.husky/`). Write down every recipe name something outside
   the `Justfile` calls.
2. **Copy the modules** from [`assets/just/`](assets/just/index.just) into `.build/just/`, byte for
   byte, plus the optional ones that apply. Do not edit them in place.
3. **Write the root `Justfile`** as above, with the variables the repository needs.
4. **Delete every recipe a module now provides.** A copy in the root silently *replaces* the module's
   recipe (that is what `allow-duplicate-recipes` does), so a leftover duplicate is not harmless: it
   keeps the old behaviour and hides the fix. Keep a root recipe only when it does something
   different on purpose, and comment why.
5. **Keep what is the repository's own**: recipes that run its own scripts, apps or migrations. Put
   them in the root `Justfile` under their own `[group(...)]`.
6. **Map renamed recipes** and update every caller found in step 1:

   | Old | Now |
   | -- | -- |
   | `ensure-pipeline-tool`, `restore-tools` | `ensure-tools` |
   | `current_version` (recipe) | `version` |
   | `import "Justfile.skills.just"` | `skills.just`, through the barrel — delete the old file |
   | a hand-written `Docs` group | `docs.just` |

7. **Do not add `set shell` or `set windows-shell`.** The modules are written for just's default `sh`
   — Git Bash on Windows — so they run unchanged on every OS.

## Changing a module

Change it here, in the skill's `assets/just/`, then copy the file into each repository. A repository
that edits its copy drifts from every other, and the next copy overwrites the edit. If one repository
needs different behaviour, override the recipe or variable in its root `Justfile`.

Recipe comments: just shows a recipe's **last** comment line in `just --list`. A recipe with a
multi-line comment carries `[doc('...')]` with its one-line summary.

## Verify

```bash
git check-ignore -v .build/just/index.just   # no output: tracked
just --list                                  # every group, one-line descriptions
just --dry-run build                         # the solution path is the right one
just bump patch --dry-run                    # prints the next version, writes nothing
just skills-update -p:PurviewSkillsDryRun=true
```

For the skill recipes in detail, replacing hand-written skill scripts, and the runner and agent
settings, see [references/skills-recipes.md](references/skills-recipes.md).
