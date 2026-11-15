# `_redirects.yml` — keeping moved pages reachable

A page's path is its URL. Moving `Getting-Started.md` into `guide/` moves
`/docs/<id>/getting-started/` to `/docs/<id>/guide/getting-started/`, and every bookmark, search result
and link from another repository to the old address would 404. `_redirects.yml` records the move in the
same commit as the move.

## Where and what

`<docs root>/_redirects.yml` — beside `Home.md` (for a GitHub wiki, at the wiki root). It is never a page.

```yaml
# previous: current
# Either side may be a slug (`guide/getting-started`) or a Markdown path from the
# docs root (`guide/Getting-Started.md`); both are reduced to the published slug.
redirects:
  getting-started: guide/getting-started
  Custom-Rules.md: schemas/Custom-Rules.md
  old-landing: index            # `index` is the docs root, /docs/<id>/
```

A bare map without the `redirects:` key is accepted too. Keep to this shape: one `previous: current`
pair per line, comments with `#`, quotes only when a name needs them.

## How the site uses it

1. The sync reads the file at each documentation channel's ref — so a reorganisation on the default
   branch forwards the prerelease channel's URLs straight away, and the live channel's only once the
   release that contains it is published.
2. For each pair it writes a small redirect page at the old address pointing to the new one
   (`noindex`, not searchable, not in the sitemap).
3. A redirect is **written only when its destination is a published page and its source is not.** A
   real page always wins over a redirect.
4. A pair that points nowhere, or whose source still exists, is reported in the sync log and skipped.
5. A file that is not valid YAML, is not a map, or holds a pair that cannot become a URL, **fails the
   site's sync** naming the file — the moves it was meant to protect would otherwise vanish silently.

The site's catalogue entry may also carry `docs.redirects` for the same purpose; when both name the
same old slug the catalogue entry wins. Prefer the file: the move and its redirect then live in one
commit, in the repository that made them.

## Rules

- Add the pair in the commit that moves the page. Never remove a released pair.
- Point at the final address. When a page moves twice, update the first pair's destination rather than
  chaining `a → b → c`.
- Redirect pages only — not anchors, not folders. A folder with a `README.md` is a page (`guide`), so
  it can be a destination.
- After the move, fix inbound links anyway: a redirect is for readers outside the repository.
