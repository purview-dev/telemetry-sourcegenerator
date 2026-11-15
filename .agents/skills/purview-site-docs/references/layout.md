# Layout and what the site does to a page

## Which files are read

The catalogue entry's `docs.source` decides:

| `docs.source` | Read | Use it when |
| -- | -- | -- |
| `github-path` | every `<docs.path>/**/*.md` at the channel's ref (default branch, or a release tag for a `live` channel) | **the default.** Any repository with more than one page. Supports folders, channels and redirects |
| `wiki` | the top-level `.md` files of the GitHub wiki (`<repo>.wiki.git`) | only for an existing GitHub wiki; it has no history the site can pin, and no folders |
| `readme` | `README.md` alone, as one page | a project small enough to be one page. Grows into `github-path` — a README with more than about eight `##` sections or a few hundred lines is a docs tree waiting to happen |

Files that are never pages: `_Sidebar.md`, `_Footer.md`, `_redirects.yml`, `.c4` models, images, and
anything the entry lists in `docs.exclude` (matched on the base file name, `*` wildcards).

## The landing page

1. A conventional index at the docs root wins: `Home.md`, `index.md` or `README.md` (case-insensitive).
2. The entry's `docs.rootPage` may name another page only when the conventional one is excluded.
3. No index and no `rootPage` fails the sync: *"docs: no root page exists"*.

Prefer `Home.md` as the hub (what the project is, then a table of sections linking every page) and a
`Getting-Started.md` beside it.

## URL of every page

The path from the docs root, each segment lower-cased with anything other than letters, digits and
dashes collapsed to `-`, and the `.md` dropped:

| Source | Published at |
| -- | -- |
| `docs/wiki/Home.md` (root page) | `/docs/<id>/` |
| `docs/wiki/Getting-Started.md` | `/docs/<id>/getting-started/` |
| `docs/wiki/guide/README.md` | `/docs/<id>/guide/` |
| `docs/wiki/guide/First-Project.md` | `/docs/<id>/guide/first-project/` |
| `docs/wiki/Q&A/Why Not.md` | `/docs/<id>/q-a/why-not/` (works, but rename it) |
| `docs/wiki/___/Page.md` | **sync fails**: the folder reduces to nothing |

Two files that reduce to the same URL collide; keep names distinct after lower-casing. A page may not
be named `prerelease` (that segment is reserved for the prerelease documentation channel).

## Sidebar and order

- Folders become collapsible sidebar groups labelled from the folder name (`getting-started` →
  "Getting Started"), placed where their first page appears.
- Order: the entry's `docs.order` (explicit slugs) → the link order in `_Sidebar.md` → alphabetical.
- `_Sidebar.md` links pages by their path from the docs root:

```markdown
- [Home](Home.md)
- **Guide**
  - [Installation](guide/Installation.md)
  - [First project](guide/First-Project.md)
- **Configuration**
  - [Options](configuration/Options.md)
```

## What the site rewrites

| In the source | On the site |
| -- | -- |
| `[Options](../configuration/Options.md#timeouts)` | a link to `/docs/<id>/configuration/options/#timeouts`; an anchor that is not a heading on the target page is dropped |
| `[Code](../../src/Thing.cs)` (outside the docs folder) | `https://github.com/<repo>/blob/<ref>/src/Thing.cs` |
| `![Flow](./images/flow.png)` | `https://github.com/<repo>/raw/<ref>/<docs.path>/images/flow.png` |
| `![badge](https://img.shields.io/…)` | unchanged |
| `> [!WARNING]` blockquote | a Starlight caution aside |
| the first `# Title` | the page header (title, sidebar label); removed from the body so it shows once |
| a second `# Heading` | demoted to `##` — one title per page |
| a `purview:likec4` block | the LikeC4 view (see `diagrams.md`) |
| a bare ```` ```mermaid ```` fence | a Mermaid diagram (both sides render it) |
| a bare ```` ```likec4 ```` fence | a LikeC4 diagram on the site — but **source code on GitHub**; use a pair |
| mkdocs/Material markup (`!!! note`, `{ .md-button }`, `{% … %}`) | stripped or converted; do not write it |

Front matter in a source page is not needed: the site writes its own (title, description, edit link,
last-reviewed date from the last commit, the repository's topics as tags). A page not touched for 365
days is flagged as stale on the site.

## Documentation channels

A project whose default branch runs ahead of its last release can publish two versions of its docs:
`live` (pinned to the latest stable tag, at `/docs/<id>/`) and `prerelease` (the default branch, at
`/docs/<id>/prerelease/`). This is declared in the catalogue entry, not in the repository. What it means
for you: a page reorganisation is visible on the prerelease channel immediately and on live only after
the release that carries it, and `_redirects.yml` is read per channel, so each channel forwards its own
old URLs at the right moment.

## Writing style that survives both renderers

- ATX headings (`##`), sentence or title case consistently, no trailing punctuation.
- Tables for anything enumerable.
- Wrap prose near 100–110 columns, so documentation diffs stay reviewable.
- HTML only where Markdown cannot express it; the site renders it, but GitHub sanitises it.
