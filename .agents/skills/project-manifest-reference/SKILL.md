---
name: project-manifest-reference
description: Field-by-field reference for the catalogue records under src/src/data/projects/ (schema vocabulary, defaults, loader-enforced rules, and the invariants the site build depends on). Use whenever authoring or reviewing a catalogue record.
category: purview-dev-website
roles:
    - catalogue
    - reference
tags:
    - projects-yml
    - zod
    - schema
---

# project-manifest-reference Skill

The catalogue is one file per project under `src/src/data/projects/` (plus
`src/src/data/external-projects.yml` for collaborations), typed by `src/src/lib/manifest/schema.ts` and validated
by `src/src/lib/manifest/load.ts`. `$schema: ../lib/manifest/schema.ts` is the first line of the file.

## Vocabularies (do not invent values)

| Field | Allowed values |
| --- | --- |
| `category` | `application-framework`, `validation`, `observability`, `source-generation`, `aspire`, `build-tooling`, `developer-tooling` |
| `status` | `stable`, `preview`, `archived` |
| `experimental` | boolean flag, default `false` — orthogonal to `status` (ADR 0004); invalid with `status: archived` |
| `install` | `nuget` (default), `msbuild-sdk`, `dotnet-tool` — NuGet packages only |
| `packages[].registry` | `nuget` (default), `npm` (ADR 0012) |
| `useCases[].audience` | `developer`, `team-lead`, `architect`, `contributor` (labels: Developer, Team lead, Architect, Contributor) |

Adding a value to any of these lists is a schema change and requires an ADR. `experimental` is a
**flag, not a status value**: it never replaces `status`, which stays the project's release channel.

## `projects[]` fields

| Field | Required | Notes |
| --- | --- | --- |
| `id` | yes | Lowercase `[a-z0-9-]` slug; also the URL segment and the LLM bundle name |
| `name` | yes | Display name; **must slugify to `id` when `docs` is set**; must not start with `Purview ` |
| `shortDescription` | yes | One sentence; falls back to the GitHub description if empty, then to `name` |
| `description` | yes | Long description (2–4 sentences) |
| `origin` | yes | Why the project became reusable tooling |
| `useWhen` | yes | When it is the right choice |
| `avoidWhen` | yes | When it is the wrong choice |
| `repository` | yes | `owner/repository`; **`owner` must be `purview-dev`** for `projects[]` |
| `category` | yes | See vocabulary |
| `status` | yes | See vocabulary |
| `experimental` | no | Default `false`; marks an exploratory project (unstable API, no support promise). Orthogonal to `status`; invalid with `status: archived`. Displayed as its own status via `displayStatus()` |
| `featured` | no | Default `false`; shown prominently on the home page |
| `order` | no | Default `1000`; projects are sorted ascending; keep values unique |
| `docs` | no | See below |
| `install` | no | Default `nuget`; how a **NuGet** package is consumed, so it has no meaning for an npm package |
| `targetFrameworks` | no | e.g. `net8.0`, `net9.0`, `net10.0`, `netstandard2.0`; `just check-projects` reports (advisory) when it does not match the union of the runtime packages' published NuGet TFMs |
| `packages` | no | `{ id, registry?, description?, primary?, targetFrameworks? }[]`; defaults to `[]`. `registry` is `nuget` (default) or `npm`; versions come from that registry and the install panel follows it |
| `skills` | no | `{ name, path?, description? }[]` — Agent Skills the project publishes from `skills/<name>/SKILL.md` (ADR 0010); mirrored to `/skills/<id>/<name>/` and rendered with the shared install block; defaults to `[]` |
| `assets` | no | `{ path, output, description? }[]`; repository files mirrored into `public/` at build time (`path` is repo-relative, `output` is site-relative); defaults to `[]` |
| `related` | no | Known project ids; rendered in the project page aside |
| `useCases` | no | Default `[]`; at least one required for non-archived projects |
| `acknowledgments` | no | `{ name, url, description? }[]`; upstream work the project credits |
| `supersedes` / `supersededBy` | no | Known project ids; used for archived/successor messaging |
| `discussions` | no | **Detected, not authored**: the page links repository Discussions when the repository has them enabled (`hasDiscussions` in the release cache). Set it only to override — `false` opts a repository out |

## `docs` (a discriminated union on `source`)

| `source` | Fields | Behaviour |
| --- | --- | --- |
| `github-path` | `path` (required), plus `rootPage`, `readmeAsIndex`, `order`, `exclude`, `channels`, `redirects` | Walks `<path>/**/*.md` at each channel's ref (default branch when no `channels`) |
| `wiki` | `rootPage`, `readmeAsIndex`, `order`, `exclude` | Shallow-clones `<repo>.wiki.git`; `_Sidebar.md` orders pages, `_Footer.md` is skipped. Single channel only |
| `readme` | none | Publishes `README.md` alone as `/docs/<id>/`. Single channel only |

- `rootPage` names the markdown file that becomes the landing page (`index`). It may only select a
  non-index page when the conventional root page is removed via `exclude` (the aggregation otherwise
  raises `DocsValidationError`). A product wiki mirrored from `docs/wiki` uses `rootPage: Home.md`: the
  wiki is plain Markdown with `Home.md` as its hub and `_Sidebar.md` as the whole navigation, and it
  carries no site configuration — a repository that still ships one is a deviation the portal ignores,
  not a requirement.
- `exclude` matches the **base file name** (not the repository path) and supports `*` wildcards. It is a
  filter over the files the repository ships, so a pattern naming a file that has since been deleted is a
  no-op, never a warning or a failure.
- `readmeAsIndex` uses the repository `README.md` as the landing page and cannot be combined with
  `rootPage`; it aliases `readme` to `index`.
- `order` is an explicit list of page slugs, applied before the `_Sidebar.md` order.
- `redirects` maps moved slugs (`previous: current`) to keep old URLs alive on every channel that
  publishes the tree. Prefer the repository's own `_redirects.yml` in its docs root (ADR 0013), which
  lands with the move and is read per channel; the catalogue's pairs win when both name a slug.
- If `docs` is set but the aggregation yields no pages, `just data-sync` fails loudly.

## `docs.channels` — live and prerelease documentation (ADR 0009)

A project whose default branch is ahead of its latest release can publish two
views of its documentation. Channels are optional; omitting them keeps one
`live` channel on the default branch.

```yaml
docs:
  source: github-path
  path: docs/wiki
  rootPage: Getting-Started.md
  channels:
    live:
      ref: latest-stable   # newest non-prerelease release tag
    prerelease:
      ref: default         # the repository's default branch
```

| Field | Meaning |
| --- | --- |
| `channels.live` / `channels.prerelease` | At most these two; unknown keys are rejected |
| `ref` | `default` (default branch), `latest-stable`, `latest-prerelease`, or any branch/tag/commit used verbatim. Defaults: `live` → `latest-stable`, `prerelease` → `default` |
| `path`, `rootPage`, `readmeAsIndex`, `order`, `exclude` | Optional per-channel overrides of the shared block (a tag's docs tree can differ from `main`) |

- **`live` is the default channel** and owns `/docs/<id>/…`; `prerelease` is
  served at `/docs/<id>/prerelease/…`. A prerelease-only project is the default
  channel and serves its docs at the canonical URLs.
- `latest-stable` with no stable release → live follows the default branch
  (warning). `latest-prerelease` with no tag → the channel is skipped. Two
  channels on the same ref → the non-default one is dropped.
- **Promotion is automatic.** The prerelease channel is published only while a
  prerelease version outranks the newest stable release, comparing GitHub
  release tags with NuGet versions. Publishing `3.0.0` retires
  `3.0.0-prerelease.N` docs on the next build; the channel returns with the next
  prerelease. No manifest edit or PR is needed to stop publishing it.
- `wiki` and `readme` sources reject `channels`: they have a single version.
- Use-case `docsPage` deep links resolve against the **default** channel.

## Loader-enforced rules (`loadProjects()`)

1. Schema validation with remediation text (unknown keys are rejected).
2. `projects[].repository.split('/')[0]` must equal `purview-dev`.
3. `related`, `supersedes`, and `supersededBy` must all reference existing project ids.
4. A package id may be declared by **exactly one** project for its registry (the key is
   `registry:id`; an absent `registry` means `nuget`).
5. `experimental: true` is rejected when `status` is `archived` (schema refinement).
6. An asset `output` path may be declared by **exactly one** project, must be relative, and must
   not contain a `..` segment.
7. The returned list is sorted by `order` ascending.

## Defaults applied by the loader

`featured: false`, `order: 1000`, `packages: []`, `related: []`, `useCases: []`,
`acknowledgments: []`, `install: 'nuget'`, `experimental: false`. Derived values (`repoOwner`,
`repoName`, `sourceUrl`, `issuesUrl`, `discussions`, `discussionsUrl`, `changelogUrl`, `releasesUrl`) are computed,
never authored.

## `externalProjects[]` fields

Required: `id` (slug), `name`, `shortDescription`, `description`, `url` (valid URL), `category`.
Optional: `repository` (`owner/repo`, link target), `status`, `featured`, `order`.
Loader-enforced: a collaboration entry must **not** use the `purview-dev` owner (the manifest test
asserts that no `externalProjects` id starts with `purview-`).
