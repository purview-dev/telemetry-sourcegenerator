---
name: docs-aggregation-rules
description: How the website aggregates documentation from product repositories into /docs/<project>/ — sources, root-page and exclusion rules, ordering, injected front matter, tags from repository topics, staleness, and the failure modes with their remediations.
category: purview-dev-website
roles:
    - docs
    - reference
tags:
    - starlight
    - documentation
    - aggregation
---

# docs-aggregation-rules Skill

`just data-sync` runs `src/scripts/sync-docs.ts` → `aggregateDocs()` in
`src/src/lib/docs/aggregate.ts`. It reads each catalogue project's `docs` configuration, fetches
the markdown from the product repository, writes the mirror to `src/src/content/docs/docs/<id>/`
and records the page list in `src/.cache/docs/index.json`.

**Never edit the mirror by hand** — it is gitignored and regenerated.

## Where content comes from

| `docs.source` | Fetch mechanism | Notes |
| --- | --- | --- |
| `github-path` | Repository tree (`git/trees/<ref>?recursive=1`) filtered to `<path>/**/*.md` | Each file fetched from `raw.githubusercontent.com` at the channel's ref (the repository's default branch when no `channels` are declared) |
| `wiki` | `git clone --depth 1 https://github.com/<repo>.wiki.git` | `_Sidebar.md` and `_Footer.md` are never rendered. A wiki has one HEAD, so it supports a single channel |
| `readme` | Raw `README.md` | Exactly one page (`index`), single channel |

The ref is the repository's **default branch** (from the release cache, falling
back to `main`) unless the manifest declares `docs.channels` (ADR 0009).
`lastReviewed` is the last commit date for the file (Atom feed for
`github-path`/`readme`, wiki HEAD commit for `wiki`); a channel pinned to a
release tag uses that release's date instead, because the content cannot move.

## Channels (`live` and `prerelease`)

A project may publish two views of its documentation:

```yaml
    docs:
      source: github-path
      path: docs/wiki
      rootPage: Getting-Started.md
      channels:
        live:
          ref: latest-stable
        prerelease:
          ref: default
```

- The **default** channel (`live`, or the only declared channel) is written to
  `src/src/content/docs/docs/<id>/<page>.md` and served at `/docs/<id>/<page>/`.
- Any other channel is written to
  `src/src/content/docs/docs/<id>/<channel>/<page>.md` and served at
  `/docs/<id>/<channel>/<page>/`.
- `docs/<id>/<channel>` is reserved: a source page whose slug equals a channel id
  raises `DocsValidationError`.
- `src/.cache/docs/index.json` is schema 2 and stores
  `projects[].channels[] = { id, ref, refKind, version, label, default, pages[] }`.
  A cache from an older schema reads as absent, so `just data-sync` regenerates it.
- The Starlight sidebar groups a two-channel project per channel (`Live 2.4.2`,
  `Prerelease main`); a one-channel project keeps a flat list. The page title
  component renders the switcher and a prerelease notice.
- **Promotion is automatic.** The prerelease channel is only aggregated while a
  prerelease version outranks the newest stable release, computed from GitHub
  release tags and NuGet versions. A stable release that catches up retires the
  channel on the next sync (a `docs <id>: prerelease documentation is not
  published: …` line, not an error) and the next prerelease restores it. Never
  remove the declaration just to stop publishing it.
- **Retired URLs redirect to live.** For each withheld channel URL the build
  writes a `noindex` meta-refresh page (canonical + `data-pagefind-ignore`)
  pointing at the live page with the same slug — GitHub Pages serves files only,
  so `_redirects` is not an option. These files are written at
  `astro:build:done` (`config/docs-redirects-integration.ts`), outside Astro's
  route list, so they stay out of the sitemap, `discover.json` and the llms
  bundles.
- Per-project `llms-full.txt` publishes the default channel only; `llms.txt`
  also links the prerelease channel when one exists.


## Root page selection (the most common onboarding failure)

1. A conventional index file wins by default: `index.md`, `Home.md`, or `readme.md` (case-insensitive
   basename).
2. `docs.rootPage` may override it **only** when that conventional file is removed with
   `docs.exclude`. Otherwise aggregation raises `DocsValidationError`:
   *"selects a non-index page while a conventional root page already exists"*.
3. If no index file and no `rootPage` exist, aggregation raises
   *"docs: no root page exists"* — add `index.md`/`Home.md`/`readme.md` or set `rootPage`.
4. If `rootPage` names a file that does not exist, aggregation raises
   *"docs.rootPage: ... does not exist"*.

The product wikis are mirrored into `docs/wiki` and are **plain Markdown**: `Home.md` is the hub,
`_Sidebar.md` is the whole navigation, and the repository carries no site configuration at all — the
engineering playbook deleted its `mkdocs.yml` and its TechDocs `index.md`, and no repository needs to
add them back. A repository that still ships a site file (`mkdocs.yml`) or a generated `index.md` is a
**deviation this portal ignores**: aggregation reads `docs.path` and this entry's `exclude`, never the
site configuration. So the pattern for a wiki is:

```yaml
    docs:
      source: github-path
      path: docs/wiki
      rootPage: Home.md
      exclude: [ _Sidebar.md ]
```

`Home.md` is itself a conventional root page, so `rootPage` restates the default rather than
overriding it, and nothing has to be excluded for it to win. Exclude only the files the repository
actually ships.

While a repository still carries a site file, `just data-sync` reports it once and **never fails**:

    docs containers: mkdocs.yml is a repository site configuration and the portal ignores it — the
    catalogue entry alone decides what is published, so the repository can drop the file.

That is an advisory (`findLegacySiteConfig` in `src/lib/docs/aggregate.ts`), not a gate, and it is
printed on stdout so it survives into the build log; on GitHub Actions the same text is repeated as a
`::warning` annotation on the run. The aggregation keeps stripping the Material markup such a file
stands for (`./mkdocs`), so the file can be deleted upstream in its own time and on its own pull
request; when it is gone the advisory simply stops appearing.

## Exclusions and ordering

- `exclude` matches the **base file name** (`index.md`, `_Sidebar.md`) with `*` wildcards; it is
  applied **before** the root page is selected (that is what makes rule 2 work).
- A pattern that matches nothing is a **no-op**: excluding `index.md` from a wiki that has since
  replaced it with `Home.md` neither warns nor fails, so a stale pattern cannot block the build while
  the upstream repository is mid-reorganisation. Remove it when you notice, but it is not drift the
  guard reports.
- Page order comes from, in priority: `docs.order` (explicit slug list) → a `_Sidebar.md` link order
  (both for `wiki` and for a mirrored wiki under `github-path`) → `1000`.
- The landing page is always `order: -1`, so it heads the sidebar as "Overview".
- A page's slug is its path from the docs root (ADR 0011): a flat corpus keeps the single-segment
  slug (`Getting-Started.md` → `getting-started`, served at `/docs/<id>/getting-started/`), and a
  topic folder stays a path segment (`schemas/Custom-Rules.md` →
  `/docs/<id>/schemas/custom-rules/`). A nested `README.md` is the folder's index, so
  `guide/README.md` publishes at `/docs/<id>/guide/` (the root `README.md` stays the landing page).
  A `docsPage` in a use case must use a slug that still resolves in the **default** channel, which
  is why it stays a single segment.

## Injected front matter (per page)

`title` (first `# H1`, else the slug), `description` (first prose paragraph, ≤160 chars),
`owners: [purview-dev]`, `status` (the project's status), `experimental: true` (written only when
the catalogue flags the project, ADR 0004), `lastReviewed`, `sourceProject`,
`sourceRepo`, `projectName`, `sourcePath`, `editUrl`, `tags` (the repository's GitHub **topics**,
absent when there are none), and `sidebar.label`/`sidebar.order`.

Additional transformations: GitHub alert blockquotes become Starlight asides; relative links between
documents are rewritten to portal URLs; **relative** images are rewritten to GitHub raw URLs while
**absolute** image URLs pass through untouched — a registry badge such as
`![Purview.BuildSdk](https://img.shields.io/nuget/v/Purview.BuildSdk)` keeps its own URL, so a page can
show a live version without stating a number that rots; the first top-level H1 is removed from the body
(it becomes the front-matter `title`, which the page header renders) and any later H1 is demoted to `##`,
so each page shows its title once.

## What the site does with it

- Each documented project becomes a `starlight-sidebar-topics` topic whose items come from
  `src/.cache/docs/index.json` (so the mirror and the manifest must come from the same sync).
- The sidebar mirrors the published tree (ADR 0011): every folder in a slug becomes a nested
  `starlight` group labelled with the folder's name spelled out (`schemas` → `Schemas`), placed where
  its first page appears and holding all of that folder's pages in channel order. Nesting is
  recursive, a folder's `index.md` or nested `README.md` links the folder's own address, and a flat
  corpus renders exactly the flat list it always did (`buildChannelItems` in
  `src/lib/docs/sidebar.ts`).
- Each aggregated page also names the folders above it, above the title, in the same spelling as the
  sidebar (`PageTitle.astro` + `docSectionTrail`).
- Archived projects are excluded from the docs portal, the sidebar topics, and per-project LLM
  bundles, even if they declare `docs`.
- Every documented project gets `/projects/<id>/llms.txt` and `/projects/<id>/llms-full.txt`, linked
  from `/llms.txt` and from the project page.
- Staleness: a page is flagged when `lastReviewed` is older than 365 days
  (`src/src/lib/docs/staleness.ts`).
- A ```` ```likec4 ```` fence in a mirrored page is rendered when it compiles, and
  stays **source** when it does not: the whole `docs` collection is relaxed through
  astroc4's `validation: { collections: ['docs'] }` (`astro.config.ts`). Such a page
  was written for another renderer, so its fence is often a partial snippet; astroc4
  logs a warning and leaves it as a highlighted code block instead of failing the
  build. Pages the site authors (`src/pages/**`) are not in the collection, so they
  keep strict validation.

## Diagram pairs (ADR 0013)

A product page pairs a GitHub rendering with a LikeC4 view:

````markdown
<!-- purview:likec4 file="./diagrams/the-loop.c4" view="theLoop" title="The loop" -->
```mermaid
flowchart LR
  resume --> next
```
[LikeC4 model](./diagrams/the-loop.c4)
<!-- /purview:likec4 -->
````

`buildPage` runs `applyDiagramBlocks` (`src/lib/docs/diagrams.ts`) on the raw source before anything
else: the `.c4` file is read from the repository at the channel's ref (resolved relative to the page,
or `/`-rooted at the repository root), and the whole block becomes a ```` ```likec4 ```` fence holding
the model, with `view`, `project`, `title`, `description`, `height`, `collapsible` and `defaultOpen`
passed through. A missing model, an undeclared view or a malformed marker keeps the Mermaid and logs
`docs <id>: <page> line <n>: …`. Markers inside code fences are ignored.

## Repository redirects (`_redirects.yml`, ADR 0013)

`<docs root>/_redirects.yml` (the wiki root for `source: wiki`) holds `previous: current` pairs, either
under `redirects:` or as a bare map; each side is a slug or a Markdown path, reduced with `docSlug`.
It is read per channel and stored as `channels[].redirects` in `src/.cache/docs/index.json`;
`docsPageRedirects(projects, manifest)` writes each channel's pairs from that channel's base, after the
catalogue's `docs.redirects` (which wins on a conflict). An invalid file is a `DocsValidationError`; a
destination that is not a page, or a source that still is, is an advisory line in the sync log.

## Consumer skills

`skills/` at the repository root is what product repositories install
(`npx skills add purview-dev/purview-dev`): `purview-site-integration`, `purview-site-docs` and
`purview-site-assessment` (with `scripts/assess.mjs`). They restate these rules for authors, so a change
to slugs, diagram pairs or redirects here changes them in the same pull request.

## Verifying

```shell
just data-sync                      # expect "docs <id>: <n> pages" per project
node -e "console.log(require('./src/.cache/docs/index.json').projects)"
just build && just check-generated  # asserts the mirror matches the manifest
```

A reorganised tree is verified in the built output: the old flat slug must be a meta-refresh page
pointing at the new address, on every channel that publishes the tree (`/docs/<id>/<old>/` and
`/docs/<id>/prerelease/<old>/`), and the sidebar of a nested channel must contain a `group-label`.

A page that shows a registry badge instead of a version is verified the same way: the badge URL must
appear verbatim in the built page (`dist/docs/<id>/<page>/index.html`), not rewritten to a
`raw.githubusercontent.com` address. `docs.test.ts` covers an absolute badge URL beside a relative one.
