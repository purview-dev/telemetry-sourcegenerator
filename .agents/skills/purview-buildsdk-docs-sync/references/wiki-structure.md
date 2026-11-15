# The wiki structure

`docs/wiki/` is the source for the project's pages on the documentation site. The site's sync reads
the folder, publishes each page at a URL derived from its path, and rewrites relative `.md` links into
site routes. That makes the folder tree part of the published interface, not just filing.

## Layout

```text
docs/wiki/
  _Sidebar.md                 page order and sections (not published)
  Home.md                     the browsing index (not published)
  Getting-Started.md          the landing page (docs.rootPage)
  <Deep-Linked-Page>.md       any page the site catalogue deep-links (docsPage)
  <topic>/
    <Page-Name>.md
  <topic>/
    ...
```

- **Root pages need a site contract.** Only the landing page and the pages the site's project
  catalogue deep-links through `docsPage` stay at the root. A deep link is a single path segment, so
  such a page cannot live in a folder unless the catalogue entry moves with it. Find the contract in
  the site's catalogue entry for the project (`docs.rootPage`, every `useCases[].docsPage`).
- **One level of folders.** A page is at the root or in exactly one topic folder. A topic folder is
  named for the area (`configuration/`, `packaging/`), lower case, and holds at least two pages unless
  the area is clearly going to grow. Do not name a folder `build/` — the standard .NET `.gitignore`
  ignores `[Bb]uild/`.
- **URL-safe names.** Folder and page names are letters, digits and single dashes. The path becomes
  the slug, lower-cased: `packaging/SourceLink.md` → `/docs/<project>/packaging/sourcelink/`.
- **One title.** Each page opens with exactly one `# ` heading — the title the site shows — and a
  one-paragraph description of what the page answers. Section headings are `##` and below.
- **Relative links, from the file they are in.** From `configuration/Property-Schema.md`, the reference
  page is `../Configuration-Reference.md` and a sibling is `Version-Detection.md`. A link that climbs
  out of `docs/wiki/` (`../../README.md`) is a repository path; the site rewrites it to a GitHub URL.

## `_Sidebar.md`

```markdown
<!-- Page order for the documentation site: every page once, grouped by section, in reading order.
     Section labels match the `## ` headings in Home.md. -->

- [Home](Home.md)
- **Get started**
  - [Getting Started](Getting-Started.md)
  - [Engineering Principles](concepts/Engineering-Principles.md)
- **Configuration**
  - [Configuration Reference](Configuration-Reference.md)
  - [Property Schema](configuration/Property-Schema.md)
```

Every page appears exactly once, with its path from `docs/wiki/`. A page missing here is published
with no place in the sidebar. A page listed twice has two competing positions.

## `Home.md`

```markdown
# <Product> Wiki

<one paragraph: what the product is>

## How this wiki is organised

| Section | Where it lives |
| -- | -- |
| Get started | the wiki root and `concepts/` |
| Configuration | `Configuration-Reference.md` and `configuration/` |

## Get started

| Page | What it answers |
| -- | -- |
| [Getting Started](Getting-Started.md) | Installing the SDK and building a first project. |
```

The `## ` headings are the sidebar's section labels, word for word, in the same order. Every page is
linked from at least one section table. Feature highlights and requirements may follow the sections.

## Moving or renaming a page

The published URL changes, and inbound links — from other repositories, from search results,
from bookmarks — break. So:

1. Move it with `git mv`, so history follows the file.
2. Fix every inbound link: other wiki pages (relative to *their* folder), `_Sidebar.md`, `Home.md`, the
   READMEs, `AGENTS.md`, the skills, and any test that names the path.
3. Record `old-slug: new/slug` pairs (lower-cased, no `.md`) and ask the user to add them to the site
   catalogue's `docs.redirects`, which keeps the old URL working. Editing the site's repository is
   outside this repository — do not do it unasked.

## The test

A repository with a wiki should hold it to these rules in its test suite, reading files only, with
no network:

| Assertion | Catches |
| -- | -- |
| the wiki folder resolves to this repository (`_Sidebar.md`, `Home.md` and the landing page exist) | every other assertion passing vacuously |
| every page is at the root or one folder deep, and the folders are exactly the expected set | accidental nesting, a stray new topic |
| root pages are exactly the site-contract pages | a page left at the root, or a deep-linked page moved |
| every folder and page name is URL-safe | a slug the sync would rewrite |
| `_Sidebar.md` lists every page exactly once | an unpublished or doubly-ordered page |
| `Home.md` links every page | a page no reader can browse to |
| every sidebar section has a matching `## ` in `Home.md` | the two indexes disagreeing |
| every relative `.md` link in the wiki resolves | a page moved without its inbound links |
| every page opens with exactly one `# ` title (code fences and inline code ignored) | untitled or doubly-titled pages |

Strip fenced code blocks and inline code before reading headings or links. A quoted `# comment` in a
shell example is not a title.
