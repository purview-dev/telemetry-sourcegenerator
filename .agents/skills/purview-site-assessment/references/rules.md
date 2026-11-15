# Assessment rules

Severity is what the finding costs: an **error** breaks the site's sync or build, or loses content; a
**warning** is visible to readers; an **info** is an improvement.

## Repository

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| REPO-001 | error | no `README.md` | add one: what it is, install, a minimal example, a link into the docs |

## Documentation

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| DOCS-001 | warning / info | no docs folder; warning when the README is long enough to be a docs tree (≥ 8 `##` sections or ≥ 300 lines) | create `docs/wiki/` with topic folders; keep the README as the front door |
| DOCS-002 | warning | no `Home.md`/`index.md`/`README.md` at the docs root | add `Home.md`, or have the entry set `docs.rootPage` |
| DOCS-003 | warning / info | a flat folder of 7+ pages, or 10+ pages left at the root beside folders | group into topic folders; keep only the hub, getting-started and deep-linked pages at the root |
| DOCS-004 | info | folders more than two levels deep | flatten unless a sub-area has several pages of its own |
| DOCS-005 | error / warning | a path that reduces to an unpublishable slug (error), a name the site has to rewrite (warning), or a page or folder named `prerelease` (error) | letters, digits and single dashes |
| DOCS-006 | error | two files publish at the same URL | rename one |
| DOCS-007 | warning | no `#` title, or more than one | exactly one `# Title` first |
| DOCS-008 | error | a relative link to a file that does not exist | fix the path; it resolves from the page's own folder |
| DOCS-009 | error | a relative image that does not exist | fix the path or add the image |
| DOCS-010 | info | a fence with no language | label it |
| DOCS-011 | info / warning | no `_Sidebar.md` (info), a sidebar link to a missing page (warning), a page not in the sidebar (info) | list every page once, by section |
| DOCS-012 | info | `mkdocs.yml` present | delete it; the site ignores it |
| DOCS-013 | error | a code fence never closed | close it |

## Diagrams

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| DIAG-001 | warning | ASCII-art diagram in a text fence | LikeC4 model + Mermaid fence in a `purview:likec4` block; legends become tables |
| DIAG-002 | info | a ```` ```mermaid ```` fence with no LikeC4 pair | fine as is; pair architecture and flow diagrams |
| DIAG-003 | warning | a bare ```` ```likec4 ```` fence — GitHub shows source | move the model to a `.c4` file and use a pair |
| DIAG-004 | error | a malformed pair: unclosed or orphan marker, no `file`, a model that is missing or outside the repository, or a `view` the model does not declare | fix the marker or the model; the site shows the Mermaid until then |
| DIAG-005 | warning | a model that imports or has no `specification` | one self-contained file per diagram |
| DIAG-006 | warning | a pair with no Mermaid fence, so GitHub shows nothing | add the Mermaid rendering between the markers |
| DIAG-007 | info | a pair that does not name its `view` | add `view="<id>"` |
| DIAG-008 | warning | Mermaid source inside a ```` ```text ```` fence | label it ```` ```mermaid ````; both renderers draw it |

The text-fence classifier skips folder trees (`├──`, `└──`), which are listings rather than diagrams.

## Redirects

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| RED-001 | error | `_redirects.yml` unreadable, or a pair that cannot become a URL or points at itself — the site's sync fails | one `previous: current` per line under `redirects:` |
| RED-002 | warning | a destination that is not a page | point at the page's current path |
| RED-003 | info | a source that is still a page, so the redirect is inactive | expected while the move is unreleased |
| RED-004 | warning | a page renamed or deleted in git history with no redirect (here or in the catalogue's `docs.redirects`) | add the pair the finding suggests, unless the page was never published |
| RED-005 | warning | a chain (`a → b`, `b → c`) | point every old slug at the final page |

RED-004 reads the last 300 commits that touched the docs folder. A page that existed only between two
site builds was never published; ignore those.

## Catalogue (with `--catalogue`)

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| CAT-001 | error | the entry's `docs.path` (or `--docs`) has no Markdown | create it, or correct the entry |
| CAT-002 | error | the entry's `docs.rootPage` does not exist | restore the page, or change the entry in the same release |
| CAT-003 | error | a use case's `docsPage` is not a page — the site's build fails | keep the page at the root under its name, or move the deep link |
| CAT-004 | info | the entry carries `docs.redirects` | move them into `_redirects.yml`, then drop them from the entry |

## Agent skills

| Rule | Severity | Finding | Fix |
| -- | -- | -- | -- |
| SKILL-001 | error | a `skills/<name>/` folder with no `SKILL.md` | add it, or move the folder |
| SKILL-002 | error | no front matter, or no `name` / `description` | add them |
| SKILL-003 | error | `name` differs from the folder | make them equal |
| SKILL-004 | warning | `name` is not a lower-case slug | rename |
| SKILL-005 | error | `SKILL.md` links a file that is not in the skill folder | ship it beside `SKILL.md` — only the folder is installed |

## Metadata (checked by hand with `gh`)

META-001 … META-006 are listed in `SKILL.md`, step 3.
