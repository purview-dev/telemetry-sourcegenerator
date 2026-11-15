---
name: purview-site-assessment
description: Assess how well a Purview repository integrates with purview.dev and recommend fixes - runs a dependency-free script over the checkout (docs layout and hierarchy, root page, slugs and collisions, titles, broken links and images, ASCII-art and unlabelled Mermaid diagrams, purview:likec4 diagram pairs and their .c4 models, _redirects.yml and page moves in git history that left no redirect, agent skills front matter), cross-checks the site's catalogue entry and the live GitHub metadata, and produces a prioritised report with a concrete fix for each finding. Use before a repository joins purview.dev, after reorganising docs, when the site shows something wrong for the project, or when asked to audit or "assess" site integration.
license: MIT
metadata:
  site: https://purview.dev
  repository: purview-dev/purview-dev
---

# Assessing a repository's purview.dev integration

The rules being checked are in `purview-site-docs` (how documentation is written) and
`purview-site-integration` (everything else the site reads). This skill finds where a repository
departs from them, ranks the findings, and proposes fixes. It **reports first and edits only when
asked**; and it never edits `purview-dev/purview-dev` — catalogue changes are proposed as a snippet for
the site maintainers.

## 1. Fetch the catalogue entry (when the project is on the site)

The entry carries contracts the repository must keep: the docs path, the root page, every use case's
`docsPage` deep link, and any redirects recorded on the site side.

```shell
curl -sfL https://raw.githubusercontent.com/purview-dev/purview-dev/main/src/src/data/projects/<id>.yml -o .purview-entry.yml
```

`<id>` is normally the repository name (`unions`, `build-sdk`, `pm-skills`). A 404 means the project is
not on the site yet — assess without it and include the entry proposal from
`purview-site-integration` (references/catalogue-contract.md) in the report. Delete
`.purview-entry.yml` when done; never commit it.

## 2. Run the script

```shell
node <this-skill>/scripts/assess.mjs . --catalogue .purview-entry.yml
```

`<this-skill>` is where the skill was installed (for example `.agents/skills/purview-site-assessment`).
Options: `--docs <path>` to name the docs folder (default `docs/wiki`, then `docs`), `--json` for
machine-readable output. It reads files and `git log` only, needs Node 18+, and exits 1 when an
error-level finding exists, so it can also run in CI.

Every rule id it prints is explained, with how to fix it, in [references/rules.md](references/rules.md).

## 3. Check what the script cannot see

```shell
gh api repos/purview-dev/<repo> --jq '{description, topics, archived, default_branch, has_discussions}'
gh api "repos/purview-dev/<repo>/releases?per_page=5" --jq '.[] | {tag_name, prerelease, published_at}'
```

| Check | Finding when it fails |
| -- | -- |
| 6–15 topics, lower-case and hyphenated | **META-001** warning — the card shows no or weak tags |
| a description that agrees with the entry's `shortDescription` | **META-002** info |
| `default_branch` is `main` (or the entry says otherwise) | **META-003** error — the docs and edit links follow it |
| at least one release, tagged `vX.Y.Z` or `vX.Y.Z-prerelease.N`, matching the package version | **META-004** warning — no version is shown |
| entry `status` agrees: stable registry version → `stable`; prerelease only → `preview`; archived repository → `archived` | **META-005** warning |
| the repository is not archived while the entry says `stable`/`preview` | **META-006** error |

Then read what a script cannot judge:

- **Hierarchy.** Do the topic folders match how a reader looks for things? Are pages in the folder
  for their topic? A DOCS-003 finding needs a proposed tree, not just "add folders".
- **Diagrams.** For each DIAG-001 (ASCII art) and DIAG-008 (Mermaid in a text fence), decide whether it
  is an architecture or flow diagram (→ LikeC4 model + Mermaid pair) or a small sketch (→ a labelled
  ```` ```mermaid ```` fence is enough). Draft the model for the most important one.
- **README vs docs.** Does the README point into the docs, or duplicate them?
- **Skills.** Does each `description` say what the skill does *and when to use it*?

## 4. Report

Lead with a one-paragraph verdict, then the findings grouped by priority, each with the fix written
out — the redirect line, the renamed path, the converted fence — not just the rule:

```markdown
# purview.dev integration: <repo>

<verdict: ready / ready after the blockers / not ready, and why, in one paragraph>

## Blockers (break the site's sync or build)
| Rule | Where | Problem | Fix |

## Should fix (readers see something wrong)
| Rule | Where | Problem | Fix |

## Improvements
| Rule | Where | Problem | Fix |

## Proposed changes
- `docs/wiki/_redirects.yml` — the full file.
- The proposed docs tree, when DOCS-003 fired.
- One converted diagram pair (model + Mermaid), when DIAG-001/008 fired.
- The catalogue entry snippet for the site maintainers, when the entry is missing or drifts.
```

Blockers are every `error`; "should fix" is every `warning` plus META errors; improvements are
`info`. Collapse repeated findings into one row with a count and the list of places.

## 5. Fixing (only when asked)

Work in this order — each step changes what the next one sees:

1. Blockers: unclosed fences, broken links and images, slug collisions, missing deep-link pages.
2. Redirects for moves already made (RED-004), before any further move.
3. Reorganise a flat tree into topic folders (`git mv`), adding each move to `_redirects.yml` and fixing
   inbound links in the same commit. Leave catalogue deep-linked pages at the root.
4. Convert diagrams. Validate each model with `npx likec4 validate <folder>`.
5. Rerun the script; the report should show no errors, and only warnings you have explained.
