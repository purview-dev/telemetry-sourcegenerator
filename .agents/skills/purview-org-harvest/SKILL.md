---
name: purview-org-harvest
description: >-
  Use when a Purview repository's conventions may have fallen behind the organisation -
  inventorying every public purview-dev repository at main, deciding which repository is
  best-in-class for each concern before adopting anything, and recording the result in a dated
  snapshot. Always reads the remote: a local clone is a cache, and it may be stale, renamed or
  missing entirely.
license: MIT
metadata:
  author: Purview-Dev
  version: "1.0"
  repository: purview-dev/engineering-playbook
---

# Purview organisation harvest

Seventeen public repositories, each with its own wiki, its own `skills/` folder and its own `AGENTS.md`.
They are not equally up to date, and the one you have cloned is not necessarily the one that has the
answer. Two failures follow, and this skill exists to prevent both.

**Copying from a stale clone.** A clone is a cache with no expiry. The convention you read there may
be two months behind the one the owner now documents, and nothing about the local file says so.

**Copying from the wrong owner.** "How do we ship?" is answered by `build`. "How do we keep a boundary
honest?" is answered by `architests`. Asking a library that does neither produces a confident answer
with no evidence, which is worse than no answer.

So: establish membership, read at `main`, name one owner per concern, write down what you found.

## 1. Scope

Name two things before reading anything:

| Input | Example |
| -- | -- |
| The repository being changed | `containers` |
| The concern in question | "how a test reaches a real database" |

A harvest without a named concern becomes a tour, and a tour produces a list of differences nobody
acted on. The concern is what makes "best-in-class" a decision rather than a preference.

## 2. Discover membership

`gh repo list` is the membership authority. A catalogue, a README table, an `llms.txt` and a local
folder listing are all convenience indexes: they are maintained by hand, so they under-count, and the
omission is silent.

```bash
gh auth status
gh repo list purview-dev --limit 200 \
  --json name,visibility,isArchived,pushedAt,defaultBranchRef
```

Drop `isArchived` and anything that is not `PUBLIC`. **If `gh auth status` fails, stop.** Degrading to
the local clone is the exact failure this skill prevents, and it degrades silently.

The filter denies by default: a repository must be exactly `PUBLIC` and exactly not archived to be
recorded, so a visibility you have never seen fails closed. `gh repo list` also only lists what your
token can see, which is why the token's visibility and the file's contents are two separate questions:
the section below is about the second one.

A local clone is admissible only when its `HEAD` equals the SHA the snapshot recorded:

```bash
git -C ../containers rev-parse HEAD
```

That is a speed optimisation, never a substitute.

## 3. Read it at `main`

Every repository is read at `main`, and the commit is recorded. The snapshot is what makes the claim
checkable a month later, when nobody remembers which commit the analysis saw.

```bash
node scripts/inventory.mjs            # drift between the organisation and the snapshot
node scripts/inventory.mjs --write    # regenerate .config/org-inventory.json
node scripts/inventory.mjs --repo containers
```

The script is deterministic and dependency-free: it shells out to `gh`, so it reuses the CLI's own
authentication, and it runs the same on Windows and on a Linux agent — it is Node, not a shell script,
so there is no PowerShell edition and no Bash edition to keep in step. It writes
`.config/org-inventory.json` in the repository you run it from — generated, never hand-edited.

To inspect a decision without a network call, replay a recorded harvest with `--from` and `--details`.
That is the same script, not a variant, so what you review is what runs.

## 4. Harvest in two tiers

Seventeen repositories is not a reading list; it is a budget. Two tiers keep it affordable.

| Tier | Call | Answers |
| -- | -- | -- |
| 1 — shape | `gh api repos/purview-dev/<repo>/git/trees/main?recursive=1`, for all seventeen | Which folders exist, how many pages the wiki has, whether `skills/` is authored, whether `AGENTS.md` exists. Cheap enough for everything. |
| 2 — content | `gh api -H 'Accept: application/vnd.github.raw' repos/purview-dev/<repo>/contents/<path>`, **only for the owner of the concern** | What the convention actually says. |

Tier 1 tells you where the evidence is. Tier 2 reads it, in the one repository that wrote it down.

```bash
gh api repos/purview-dev/architests/git/trees/main?recursive=1 --jq '.tree[].path'
gh api -H 'Accept: application/vnd.github.raw' \
  repos/purview-dev/architests/contents/docs/wiki/Architecture-As-Tests.md
```

More invocations, and what each one is for: `references/harvest-commands.md`.

## 5. Classify: one owner per concern

Pick exactly one owner per concern, from `references/best-in-class.md`. Two signs the classification
is wrong:

- **Two owners for one concern.** `build-sdk` and this playbook's `purview-engineering-principles`
  both answer "review this repository". That overlap is a finding to resolve, not a tie to break by
  preferring whichever you read first.
- **An owner that has never run the thing it documents.** Check the tree before trusting the page.

## 6. Divergence register

Write the differences down before changing anything, with an adopt-or-reject for each. A divergence
with no decision recorded is the same divergence next quarter.

| Column | Answers |
| -- | -- |
| Concern | What is being compared |
| Owner | The repository the convention comes from, and the path it is stated at |
| Here | What this repository does now |
| Decision | Adopt, or reject with the reason |
| Mechanism | The build rule, test or review obligation that keeps it true |

Rejecting is a legitimate outcome and is usually the right one at a smaller scale. What is not
legitimate is adopting a convention whose failure the repository cannot have.

## 7. Adopt

A convention arrives as a set of changes that land together, or it is a claim with no mechanism:

```text
the page      states the rule, why, how it is enforced, and what a smaller repository does instead
src/          demonstrates it, or says why it does not
the snapshot  records the commit it was read at
the skill     points at the owner, and never paraphrases it
```

**Link to the owner; do not restate it.** A paraphrase is a second source of truth, and the copy with
no owner is the one that loses.

## 8. Verify

```bash
just build          # zero warnings
just test           # architecture, skill and documentation rules
just lint-check
node scripts/inventory.mjs --print
```

If the harvest changed what the snapshot should record, regenerate it and commit the diff. The
snapshot being current is a review obligation rather than a build failure — a build that goes red on
a Tuesday because a clock passed is a build people disable.

## Private repositories

A private repository may inform a maintainer's judgement. It is never evidence, and it is never
written down. This is not a preference, and it is not a heuristic an agent gets to weigh against a
good argument. Five statements, in order of how often they are broken:

1. A committed snapshot contains public, non-archived repositories only. The writer denies by default
   and refuses to persist anything else, and a test asserts it.
2. A private repository may be read when `gh` is authenticated for it, and what it teaches may shape
   a rule.
3. Nothing published names it: not the snapshot, not the page, not the skill, not a workflow, not a
   config, not a commit message. Attribute the rule to "a Purview application repository", or to
   nothing.
4. **If a rule cannot be justified without the private source, it is not published.**
5. **If you cannot say where a name came from, do not write the name.** A repository the snapshot
   does not list is either private or gone, and both fail the same test.

The fourth is the one an agent rationalises around, because the evidence is often excellent. A rule
whose only justification is out of reach of the people it is published to cannot be reviewed by them,
which makes it folklore with a citation they cannot follow.

## Checks before you answer

- Did you read membership from the organisation rather than from a catalogue or a folder?
- Is every claim about a repository true at the SHA the snapshot recorded, not at your clone?
- Did you name exactly one owner per concern, and resolve any overlap rather than duplicating it?
- Did you reject anything? A harvest with no rejections usually means nothing was understood.
- Does every change you propose have a mechanism, or is it labelled a review obligation?
- Is every repository you named public?

## See also

- `references/best-in-class.md` — which repository owns which concern, and where to read it
- `references/harvest-commands.md` — the `gh` invocations, and the trap in each one
- `purview-engineering-principles` — the conventions themselves, once you know which owner to read
- `purview-architecture` — the project-shape decisions a harvest most often touches
