---
name: audit-catalogue-projects
description: Use when auditing or repairing the catalogue records under src/src/data/ — the invariant checklist for documentation, stability, metadata, tags, packages, relationships, use cases and ordering, plus the findings report format.
category: purview-dev-website
roles:
    - catalogue
    - audit
tags:
    - projects-yml
    - validation
    - metadata
---

# audit-catalogue-projects Skill

Use this skill to answer "is every project in the catalogue correct?" with evidence, and to repair
the unambiguous problems.

Related: `project-manifest-reference` (fields and vocabularies), `docs-aggregation-rules`,
`use-case-authoring`, `github-repo-metadata`, `site-validation-loop`.

## Method

1. **Deterministic first.** Run `just check-projects` and `bun run test`. These already enforce the
   structural invariants; start from their output rather than re-deriving it.
2. **Then the judgement checks** that no script can make: is the description truthful, is the
   category right, is the stability honest, is the use-case evidence real.
3. **Gather live evidence per project** with `gh` and NuGet (commands in `github-repo-metadata`).
4. **Report before repairing.** Produce the findings table, then apply `blocker`/`drift` fixes and
   re-run the guards.

## Invariant checklist

### Structure (enforced by `loadProjects()` / `just check-projects`)

- `id` is a lowercase `[a-z0-9-]` slug; `repository` is `purview-dev/<repo>` for `projects:`.
- `related`, `supersedes`, `supersededBy` reference known ids; a superseded project names its
  successor and vice versa where the pairing exists.
- Every NuGet package id appears exactly once across the catalogue.
- `order` values are unique integers and the sorted order is the intended presentation order.
- For every project with `docs`, `name` slugifies to exactly `id`.
- Every non-archived project has ≥1 use case; every use case with `code` has `language`.
- Every `docsPage` resolves to a page in `src/.cache/docs/index.json` for that project.
- `externalProjects` never point at a `purview-dev` repository and always have a working `url`.

### Documentation

- `docs.source` matches reality: `github-path` when the repository has a docs folder,
  `wiki` when the content lives in the wiki, `readme` only for single-page projects.
- `docs.path` exists **on branch `main`** and contains `.md` files.
- `rootPage` names a page that exists under `docs.path`; when it names a non-index page and a
  conventional root page (`index.md`/`Home.md`/`readme.md`) also exists, that conventional page must be
  listed in `exclude`.
- `exclude` keeps the navigation files out of the page list: `_Sidebar.md` (and `_Footer.md` for a
  `wiki` source). It is a filter over the files the repository ships, so a pattern naming a file that
  no longer exists is a no-op, not a warning or a failure.
- The aggregated page count is non-zero and the landing page is the page a reader should land on.
- Archived projects that still declare `docs` are intentional (they are excluded from the portal).
- Documentation is not duplicated here: this repository only holds the mirror.
- Pages moved in the repository's history have a redirect — in its `_redirects.yml` or the entry's
  `docs.redirects` (ADR 0013). Diagrams are LikeC4/Mermaid pairs, not ASCII art or Mermaid in a
  `text` fence. The consumer assessment script reports both against a clone (below); file the
  repository-side fixes as `upstream` items, since they belong in the product repository.

### Stability

- `archived` repository ⇒ `status: archived`.
- No stable NuGet release ⇒ not `stable` (`preview`).
- `experimental: true` is orthogonal to `status`: report `experimental` with `status: stable` as
  `drift` (the flag says "no support promise" while the channel says "recommended for use"), and
  `experimental` on an archived project as a `blocker` (the schema rejects it outright).
- `stable` ⇒ a stable, listed NuGet release exists and the repository is not archived.
- Deprecated NuGet packages are reported, not silently accepted.
- A project with `packages: []` is intentional: build tooling consumed as a tool/SDK still declares
  its package id when one exists.

### Metadata and tags

- Repository description is present, factual, and consistent with `shortDescription`/`description`.
- **Topics are non-empty** — they are the only source of site tags — and follow the lowercase,
  hyphenated taxonomy.
- `discussions: true` only when the repository has discussions enabled.
- `homepage`/`url` targets resolve.
- `featured` is used deliberately (a handful of headline projects, not most of the catalogue).

### Packages, frameworks and install

- Each declared package id exists on NuGet and has published versions (the guard checks the release
  cache).
- Exactly one `primary` when a project has multiple packages.
- `targetFrameworks` match the package's actual TFMs (`netstandard2.0` for generators/analyzers is
  normal). `just check-projects` reports a mismatch as an advisory observation: project-level
  frameworks against the union of the runtime packages' published TFMs, package-level frameworks
  against that package's own. `netstandard*` is treated as an analyzer/generator target, not a
  consumer framework.
- `install` is set only when the package is not a plain NuGet reference: `msbuild-sdk` for
  `Purview.BuildSdk`, `dotnet-tool` for `Purview.Build`.

### Use cases

- Coverage across audiences is meaningful for the project's shape (adopter, approver, extender).
- `scenario` describes a situation; `outcome` describes the result; `evidence` is a fact.
- Snippets are short, compile-plausible, and use the right `language`.
- `docsPage` points at the page that actually carries the depth.

## Evidence commands

```shell
# Deterministic guards
just check-projects
bun run test

# Per-project live evidence
gh api repos/purview-dev/<repo> --jq '{description, topics, archived, default_branch, has_discussions, homepage}'
gh api "repos/purview-dev/<repo>/git/trees/main?recursive=1" --jq '[.tree[].path | select(endswith(".md"))]'
gh api "repos/purview-dev/<repo>/releases?per_page=10" --jq '.[] | {tag_name, prerelease}'
curl.exe -s "https://api.nuget.org/v3-flatcontainer/<packageid>/index.json"

# Repository-side integration (docs layout, diagrams, redirects, skills) against a fresh clone
git clone --depth 300 https://github.com/purview-dev/<repo>.git <scratch>/<repo>
node skills/purview-site-assessment/scripts/assess.mjs <scratch>/<repo> --catalogue src/src/data/projects/<id>.yml

# Aggregated result
node -e "const m=require('./src/.cache/docs/index.json');for(const p of m.projects)console.log(p.projectId,p.pages.length)"
```

If `.cache/docs/index.json` or the release cache is stale, run `just data-sync` (live) first so the
evidence and the guards agree.

## Report format

```markdown
| Project | Field | Observed | Expected | Severity | Evidence | Fix |
| --- | --- | --- | --- | --- | --- | --- |
| example-tool | status | stable | preview | drift | no stable NuGet version (flat container: only 1.2.0-prerelease.3) | set status: preview |
| example-tool | — | topics: [] | 6–15 topics | drift | `gh api repos/...` shows an empty topics array | set repository topics |
```

- Severity: `blocker` (schema/build failure or a broken page), `drift` (data contradicts reality),
  `polish` (quality), `upstream` (must be fixed in the product repository: missing docs, missing
  topics you cannot set, no stable release).
- Close the report with: number of projects audited, checks run, fixes applied, and the items left
  for a human.
- Apply only `blocker` and `drift` fixes; never restructure a record or change a category to make a
  check pass.

## Guardrails

- Do not add use cases or evidence you cannot trace to the repository or the aggregated docs; report
  the gap instead.
- Do not change the schema vocabulary; adding a `status`/`category` value needs an ADR.
- Do not hand-edit the docs mirror or caches to make the audit pass.
- If an audit fix changes a catalogue record, re-run `just check-projects`, `bun run test`, and
  `just validate` before reporting completion.
