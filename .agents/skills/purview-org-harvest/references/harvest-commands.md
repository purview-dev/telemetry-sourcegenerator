# Harvest commands

Every invocation here is safe to run read-only, and each one is followed by the trap that cost
somebody time. Both shells are shown because the harvest runs on a developer's Windows machine and on
a Linux agent, and the quoting rules are not the same.

## Membership

```bash
gh auth status
gh repo list purview-dev --limit 200 --json name,visibility,isArchived,pushedAt,defaultBranchRef
```

```powershell
gh auth status
gh repo list purview-dev --limit 200 --json name,visibility,isArchived,pushedAt,defaultBranchRef |
    ConvertFrom-Json | Sort-Object name |
    Format-Table name, visibility, isArchived, pushedAt
```

**Trap: the default limit is 30.** Without `--limit` the list is silently short, and a short list is
worse than a failed one because the omission never announces itself.

**Trap: `gh repo list` shows what your token can see.** An organisation member sees private
repositories too; filter to `visibility -eq 'PUBLIC'` and `-not isArchived` before recording anything.

## The SHA a convention was read at

```bash
gh api repos/purview-dev/<repo>/git/ref/heads/main --jq .object.sha
```

**Trap: a renamed repository resolves by redirect.** Asking for a repository under its former name
answers `purview-dev/purview-dev.github.io` and exits 0, so a stale name never fails. Compare the
returned `full_name` with the name you asked for.

## Tier 1: the shape

```bash
gh api repos/purview-dev/<repo>/git/trees/main?recursive=1 --jq '.tree[].path'
```

**Trap: `truncated`.** GitHub caps a recursive tree, and the response says so with `"truncated": true`
rather than erroring. A truncated tree under-counts skills and pages, so check the flag before
believing a zero.

**Trap: the tree lists blobs and trees.** Filter to `type == "blob"` when counting files, or a folder
whose name matches your pattern is counted as its contents.

## Tier 2: the content

```bash
gh api -H 'Accept: application/vnd.github.raw' \
  repos/purview-dev/<owner>/contents/docs/wiki/_Sidebar.md
```

**Trap: `contents` returns JSON unless you ask for raw.** Without the `Accept` header the response is
a base64 `content` field, and the analysis quietly proceeds on a JSON document.

**Trap: `contents` caps at 1000 files for a directory listing.** Use the tree API for enumerating and
`contents` for reading one file.

## Windows PowerShell, specifically

**Embedded double quotes are stripped from a native command's arguments.** This looks correct and is
not:

```powershell
gh repo list purview-dev --jq '.[] | [.name, (.pushedAt // "-")] | @tsv'
```

`gh` receives `// -` and answers `failed to parse jq expression ... unexpected token`. Either keep the
jq program free of string literals, or do the shaping in PowerShell:

```powershell
$repos = gh repo list purview-dev --limit 200 --json name,visibility | ConvertFrom-Json
$repos | Sort-Object name | ForEach-Object { '{0}|{1}' -f $_.name, $_.visibility }
```

**A filtered result is a scalar when it is one item.** `(... | Where-Object { ... }).Count` returns
nothing rather than `1`, so `.Count` in a summary line reads as empty. Wrap it: `@(...).Count`.

## The snapshot

```bash
node skills/purview-org-harvest/scripts/inventory.mjs
node skills/purview-org-harvest/scripts/inventory.mjs --write
node skills/purview-org-harvest/scripts/inventory.mjs --repo containers --write
```

It resolves the snapshot path from `git rev-parse --show-toplevel`, so it writes into the repository
you are standing in rather than into the one the script was installed from. That is what lets another
repository adopt it unchanged.

`--repo` merges on write, so reading one repository does not erase the other sixteen.

## Replaying a harvest, with no network

Two flags replace the two `gh` calls. This is how the repository's own tests exercise the script on
every platform without touching GitHub, and how a reviewer reproduces what a harvest decided:

```bash
node skills/purview-org-harvest/scripts/inventory.mjs \
  --from recorded-membership.json \
  --details recorded-details.json \
  --print
```

`--from` takes exactly what `gh repo list --json name,visibility,isArchived,pushedAt,defaultBranchRef`
printed. `--details` takes an object keyed by repository name, each with the `commitSha` a tier-1
harvest found and the `paths` the tree contained. A repository that is in the membership and missing
from the details is an error, not a silent omission — a replay that quietly skipped a repository would
look like a clean run.

Two behaviours worth knowing before you trust a run:

- **A snapshot carrying a byte-order mark is accepted.** It is the one file in this workflow a person
  is likely to open and re-save, and a BOM is not a corruption.
- **A snapshot containing anything that is not public and unarchived is refused**, and nothing is
  written. This includes an entry a hand edit left behind, which the merge would otherwise carry
  forward: delete it and re-run `--write`.
