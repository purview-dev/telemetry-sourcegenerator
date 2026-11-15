---
name: site-validation-loop
description: Use when validating a change to the Purview-Dev website — what each just recipe and script checks, the data-sync modes, how to reproduce failures offline, and the rules for generated artefacts.
category: purview-dev-website
roles:
    - validation
    - tooling
tags:
    - just
    - bun
    - ci
    - astro
---

# site-validation-loop Skill

`just validate` is the authoritative chain and the same pipeline runs in CI
(`purview-build.json` → `bun run ci:build`; deploy.yml runs `just validate` first). Treat a green
`just validate` as the definition of done.

## The chain, in order

| Step | Command | Catches |
| --- | --- | --- |
| Format check | `just format-check` (`oxfmt --check`) | Formatting drift in `src/**` (not `.astro`) |
| Lint | `just lint` (`oxlint`, `maxWarnings: 0`) | Type-aware + a11y issues in `src/**` |
| Typecheck | `just typecheck` (`astro check` + strict TS) | Component/frontmatter/content-schema type errors |
| Unit tests | `just test` (`bun test tests/unit`) | Manifest/schema rules, catalogue guard, docs aggregation rules, release transform, urls, staleness, branding |
| Assets | `just check-assets` | Branding sources vs generated public assets |
| Catalogue | `just check-projects` | Catalogue invariants (see below) |
| Build | `just build` (runs live data sync first) | Astro/Starlight build, sidebar/llms plugin wiring |
| Links | `just check-links` | Broken internal links **and missing anchors** across `dist/**/*.html` |
| Generated output | `just check-generated` | Docs cache/mirror agreement, release cache shape, mirrored project assets exist and are valid JSON, `llms*.txt`, sitemap, robots, no secrets/local paths |
| Dist tests | `just test:dist` | Per-project LLM bundles, rendered project/use-case surfaces, footer version, SEO outputs |

## The catalogue guard (`just check-projects`)

`src/scripts/check-projects.ts` validates the catalogue records (`src/src/data/`) against the generated data (offline,
deterministic). It fails the build on:

- ids/orders duplicated, `order` not a unique integer, `name` not slugifying to `id` for documented
  projects;
- a documented project with no aggregated pages, or a `docsPage` that does not resolve;
- a non-archived project with no use case, or a use case with `code` but no `language`;
- a package declared more than once, more than one `primary`, or a package with no release data;
- `status: archived` on a non-archived repository, or `stable` on a project whose packages have no
  stable version;
- missing repository topics (tags), a non-`main` default branch for a documented project, or
  `discussions: true` without repository discussions;
- `externalProjects` pointing at a `purview-dev` repository.

Beyond those blockers, the guard prints **advisory observations** (they never fail the build) when a
project disagrees with the versions and frameworks its packages have published:

- a `preview` project whose packages already publish a stable version on their registry (unless `experimental`);
- a NuGet package that is deprecated or unlisted;
- packages that trail the project's headline version;
- `targetFrameworks` that do not match the packages' NuGet nuspec metadata. Project-level frameworks
  are compared with the union of the **runtime** packages' frameworks; package-level frameworks are
  compared with that package's own. `netstandard*` is treated as an analyzer/generator build target
  (never a consumer framework), and `install: msbuild-sdk`/`dotnet-tool` projects and the `msbuild`
  sentinel are skipped.

Run it alone while iterating: `just check-projects`.

Apply the machine-applicable observations (missing/mismatched `targetFrameworks`, `preview` →
`stable` when a stable package exists) with `just fix-projects`; it is a dry run by default and only
edits the records with `--write`. It rewrites the affected `targetFrameworks` lists to exactly the
expected set (so it also removes frameworks a package no longer targets), but never touches blockers
or judgement calls such as replacing a deprecated package.

## Data-sync modes

`DATA_MODE` (`src/.env.example`) controls how docs/release data is obtained:

- `auto` (default) — use the cache if present, else fetch live, else fall back to committed fixtures.
- `live` — always fetch from GitHub and the package registries (`just live-data-sync`).
- `cache` — only use the existing cache (fails if missing).
- `fixture` — only use `src/fixtures/**` (fully offline, deterministic).

The same mode governs mirrored **project assets** (`assets:` in a record): `just data-sync` fetches
each declared repository file with the docs aggregator's `fetchRawFile`, writes it to `public/`, and
caches it under `.cache/assets/`. A declared asset that cannot be resolved fails the sync, so a
missing schema never silently ships a broken URL.

`DATA_FALLBACK_TO_FIXTURES=false` makes `auto` fail loudly instead of silently using fixtures. Set
`GITHUB_TOKEN` (or `gh auth token`) to avoid unauthenticated rate limits.

The one thing the **browser** fetches is the package **version numbers** a page already displays
(ADR 0008, `src/src/lib/releases/live.ts`): the build-time value renders first, a strictly higher
live version replaces it, and the result is stored in `localStorage` for six hours. The home-page
snapshot, the package tables on `/releases/` and project pages, and the version pinned in the
install snippets refresh this way. Nothing else is fetched client-side, and no check depends on the
live value — `just check-projects` and `tests/dist` assert the build-time output.

## Rules

- **Never edit generated files** (`src/src/content/docs/**`, `src/.cache/**`, `src/dist/**`).
  Regenerate with `just data-sync`, `just fetch-releases`, `just build`, or `just refresh-fixtures`.
- **Never weaken a check** to make a change pass. Fix the data or the implementation; changing the
  contract needs an ADR.
- Keep `src/fixtures/**` in sync when the release data shape changes (`just refresh-fixtures`, then
  review the diff).
- Do not run `just clean` to "fix" a failure: a fresh checkout then needs a live data sync.

## Reproducing failures

```shell
# Docs aggregation / sidebar problems
just live-data-sync && just check-generated

# Link or anchor failures: rebuild then re-crawl
just build && just check-links

# Catalogue drift in isolation
just check-projects

# Release-data problems without network
DATA_MODE=fixture just build && just test-dist
```
