---
name: purview-site-integration
description: Make a Purview repository (a NuGet library, an MSBuild SDK, an npm package, or a pure agent-skills repository such as pm-skills) fully integrated with purview.dev - what the site reads from the repository and when, the GitHub metadata it shows (topics as tags, description, releases, default branch), the documentation contract, published agent skills, mirrored assets, and the catalogue entry the site maintainers write for it. Use when preparing a repository to join purview.dev, when its project page or docs look wrong or stale, when adding skills, packages or releases a reader should see, or when asked "how does this show up on purview.dev?".
license: MIT
metadata:
  site: https://purview.dev
  repository: purview-dev/purview-dev
---

# Integrating a repository with purview.dev

purview.dev is a static site built from two inputs:

1. **The catalogue** — one YAML file per project in `purview-dev/purview-dev`
   (`src/src/data/projects/<id>.yml`): the curated words (description, origin, when to use it, use
   cases) and the pointers (which docs to read, which packages and skills to show).
2. **This repository** — read at build time through the GitHub API, raw file URLs and the package
   registries. Nothing is pushed to the site; a scheduled build and every site deploy pull it.

So integration has two halves: make this repository **readable** (this skill and
`purview-site-docs`), and give the site maintainers what they need to **write the entry**
([references/catalogue-contract.md](references/catalogue-contract.md)). Do not edit
`purview-dev/purview-dev` from this repository unless the user asks; propose the entry instead.

## What the site reads from this repository

| Site surface | Read from | Your responsibility |
| -- | -- | -- |
| Tag chips on cards, search, docs pages | GitHub **topics** | 6–15 lower-case, hyphenated topics (`csharp`, `source-generators`, `opentelemetry`). The first four are what a card shows. There is no other way to set tags |
| "From the repository" note on the project page | GitHub **description** | One sentence that agrees with the catalogue's `shortDescription` |
| Versions, release dates, "latest release" | GitHub **releases** + the NuGet / npm registry | Tag releases `vX.Y.Z`; prereleases `vX.Y.Z-prerelease.N`. Publish packages to the registry the entry names |
| Status checks (`stable`/`preview`/`archived`) | releases, registry versions, the archived flag | A stable registry version means `stable`; prerelease-only means `preview`; archive the repository rather than leave it idle |
| Documentation at `/docs/<id>/` | `docs/wiki/**/*.md` (or `docs/`, a wiki, or `README.md`) at the **default branch** (or a release tag) | Follow `purview-site-docs`: hierarchical folders, one H1, relative links, diagram pairs, `_redirects.yml` |
| Agent skills listed on the project page, served at `/skills/<id>/<name>/` | every `skills/<name>/SKILL.md` (discovered; the entry can opt out) and everything beside it | Front matter `name` (equal to the folder) and `description`. Skills reach users through `npx skills add purview-dev/<repo>`, never inside a package |
| Mirrored assets (JSON schemas and similar) | repository paths the entry lists under `assets:` | Keep the path stable; moving it breaks the site build until the entry follows |
| Build badge | the repository's GitHub Actions | A workflow that runs on the default branch |
| Discussions link | repository settings | Enable Discussions if you want readers to ask there |
| Star count | GitHub | — |

The default branch must be `main` unless the entry says otherwise; the docs, the edit links and the
"last reviewed" dates all follow it.

## The integration checklist

1. **Metadata** — description set, 6–15 topics, the repository public, Discussions decided:

   ```shell
   gh api repos/purview-dev/<repo> --jq '{description, topics, archived, default_branch, has_discussions}'
   ```

2. **Releases** — at least one GitHub release whose tag matches the packages' version, published to
   NuGet or npm. A project with no release shows no version anywhere.
3. **Documentation** — a `docs/wiki/` tree (or a deliberate one-page `README.md`), laid out per
   `purview-site-docs`. ASCII-art diagrams converted to LikeC4/Mermaid pairs. A `_redirects.yml` the
   moment a page moves.
4. **Skills** — one folder per skill under `skills/`, each with a `SKILL.md` whose front matter `name`
   matches the folder and whose `description` says what it does and when to use it.
5. **README** — the GitHub front door: what it is, install, a minimal example, a link into the docs.
   It points into the docs rather than duplicating them (a `readme`-sourced project is the exception:
   the README *is* the docs).
6. **The entry** — collect what [references/catalogue-contract.md](references/catalogue-contract.md)
   asks for, and hand it to the site maintainers (an issue or pull request on
   `purview-dev/purview-dev`). The site's own agents onboard projects from that information.
7. **Assess** — run the `purview-site-assessment` skill and fix every error before asking for the
   entry.

## Live examples and diagrams on the project page

A project page can link a live example: an `.mdx` page in the site repository
(`src/src/pages/examples/<id>.mdx`). That is authored on the site side; propose it with the entry when
the project has something visual to show. Diagrams inside your docs need nothing from the site — use
the diagram pairs from `purview-site-docs`.

## When something looks wrong on the site

| Symptom | Usual cause |
| -- | -- |
| No tags on the card | no GitHub topics |
| Version missing or old | no release, a tag that does not match the package version, or the registry has not indexed it yet |
| A docs page missing | not under the docs path, excluded by name, or a slug that collides with another |
| A link on a docs page goes to GitHub | it points outside the docs folder, or at a page that does not exist |
| A diagram shows Mermaid on the site | the `purview:likec4` model file or view was not found — the site's sync log says which |
| A bookmarked docs URL 404s | a page moved without a `_redirects.yml` line |
| Skills not listed | the skill is not at `skills/<name>/SKILL.md`, its front matter is missing a field, or the entry opts out with `skills: false` |

## Related skills

- `purview-site-docs` — how to write and organise the documentation itself.
- `purview-site-assessment` — check this repository against both skills and get a prioritised report.
