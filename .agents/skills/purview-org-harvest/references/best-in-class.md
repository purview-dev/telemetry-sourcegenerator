# Best-in-class, by concern

One owner per concern. Read the owner, not the nearest repository that happens to have a wiki.

Every row was read from the organisation with `gh repo list purview-dev` and
`gh api repos/<owner>/git/trees/main?recursive=1`. The page counts and skill counts are the shape at
the time of writing, not a promise: they are what the snapshot records, and the snapshot is
regenerated rather than trusted.

## The matrix

| Concern | Owner | Read it at |
| -- | -- | -- |
| Repository shape, MSBuild defaults, analyzers, skill authoring | `build-sdk` | `docs/wiki`, `templates/`, `skills/purview-buildsdk-*` |
| Pipeline, release, pack validation | `build` | `docs/wiki`, `skills/purview-build-*` |
| Architecture policy: dependency direction, naming rules | `architests` | `docs` (nine pages; `Policy-Reference.md` is the reference, `Extending.md` the how-to), `skills/purview-architests-*` |
| Local dependencies in tests | `containers` | `docs/wiki`, `skills/purview-containers-*` |
| TypeScript contracts generated from C# | `zodsharp` | `docs/wiki`, `skills/purview-zodsharp-*` |
| Logging, tracing, metrics | `telemetry-sourcegenerator` | `docs/wiki`, `skills/purview-telemetry-*` |
| Event streams and event storage | `event-sourcing` | `docs/wiki` — **no skills folder** |
| Operation outcomes without exceptions | `results` | `docs/wiki`, `skills/purview-results-*` |
| Validated identity types | `value-objects` | `docs` — **no `docs/wiki`** |
| Aspire hosting resources | `aspire-resourcekit` | `docs/wiki`, `skills/purview-resourcekit-*` |
| Architecture diagrams generated from source | `aspirec4` | `docs/wiki`, `skills/purview-aspirec4-*` |
| LikeC4 diagrams inside an Astro or Starlight site | `astroc4` | `docs/wiki`, `skills/purview-astroc4-*` — an npm package, not .NET |
| Source generator authoring | `sourcegenerator-framework` | `docs/wiki`, `skills/purview-sourcegenerator-*` |
| Skill-set design, and the dated-snapshot pattern | `pm-skills` | `skills/pm-*` — **no documentation folder at all** |
| OpenAPI generated from XML comments | `openapi-xmlcomments` | `docs/wiki`, `skills/purview-openapi-xmlcomments-*` |
| The reusable conventions themselves | `engineering-playbook` | `docs/wiki`, `skills/purview-*` |
| The public project catalogue | `purview-dev.github.io` | `src/src/data/projects/*.yml` |

`purview-dev.github.io` is the catalogue, and a catalogue is an index rather than a membership list:
an entry is added by hand, so a repository missing from it is silent. `gh repo list` is the authority.

## The overlaps worth knowing about

**`build-sdk` and `engineering-playbook` both answer "review this repository".** They are not
duplicates, and the split is worth stating because a harvest will hit it immediately:

- `build-sdk` owns *what the tooling does* — project detection, the props and targets, the analyzer
  set, the packaged test-support rule sets.
- `engineering-playbook` owns *why a repository is shaped that way, and what it costs* — the rule,
  the failure it prevents, and what a smaller repository does instead.

A question about a property name goes to `build-sdk`. A question about whether a project should exist
goes here.

**`containers` and `aspire-resourcekit` both look like "local dependencies".** `containers` owns the
test-time resource — a real SQL Server, PostgreSQL or queue — and `aspire-resourcekit` owns the
development-time orchestration. A test that needs a database is `containers`; an AppHost that needs
one is `aspire-resourcekit`.

**`aspirec4` and `astroc4` both draw LikeC4 diagrams.** `aspirec4` owns the diagram generated from a
running Aspire resource graph; `astroc4` owns putting a `.c4` model inside an Astro or Starlight site —
the fence transform and the LikeC4 syntax highlighting. A question about the .NET host is `aspirec4`; a
question about the documentation site is `astroc4`, which is the one owner here that is not a .NET
package, so it is read for its TypeScript conventions rather than for its MSBuild ones.

## Reading a repository you have not cloned

Tier 1 first — the shape, for everything:

```bash
gh api repos/purview-dev/<repo>/git/trees/main?recursive=1 --jq '.tree[].path'
```

Then Tier 2 — the content, for the one owner:

```bash
gh api -H 'Accept: application/vnd.github.raw' \
  repos/purview-dev/<owner>/contents/docs/wiki/_Sidebar.md
```

`_Sidebar.md` is the fastest index of a wiki, because the repository's own documentation rule says a
page that is not in it does not exist. Where a repository publishes no wiki, its `AGENTS.md` is the
next best entry point, and where it has neither, read `skills/*/SKILL.md` — the trigger in each
`description` says what the repository thinks it is for.
