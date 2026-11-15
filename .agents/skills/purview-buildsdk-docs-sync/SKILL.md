---
name: purview-buildsdk-docs-sync
description: Bring a Purview.BuildSdk repository's written surfaces back in line with its code - the authored skills (skills/*/SKILL.md and references/), the agent instructions (AGENTS.md, CLAUDE.md, .github/copilot-instructions.md), the READMEs (repository root, each project's README.md, a project's Sdk/README.md, which is what its package ships) and the docs/wiki/ pages - and keep the wiki hierarchical, one topic folder per area, with _Sidebar.md and Home.md in step. Use after a feature, property, recipe, diagnostic or project is added, renamed or removed; when docs or skills may have drifted from the code; when reorganising or adding wiki pages; or when asked to audit or "sync the docs". Every statement is checked against the code, never against another document.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# Keeping skills, agent instructions, READMEs and the wiki true

A repository describes itself in four places, read by different people:

| Surface | Read by | Lives in |
| -- | -- | -- |
| Authored skills | agents in *other* repositories, after `npx skills add` | `skills/<name>/SKILL.md`, `skills/<name>/references/*.md` |
| Agent instructions | agents working *in* this repository | `AGENTS.md`; `CLAUDE.md` and `.github/copilot-instructions.md` are thin wrappers that point at it |
| READMEs | GitHub visitors, and NuGet users of each package | the repository root, each project folder, a project's `Sdk/` folder |
| Wiki | readers of the documentation site | `docs/wiki/` |

They drift independently, and nothing fails when they do: the reader simply follows a document that
describes last release's code. **The code is the only source of truth.** Never correct one document
from another — two documents agreeing proves nothing if both are stale.

## The one rule

**Every claim is checked against the code before it is kept, written or changed.** A claim is
anything a reader could act on: a property name and its default, a recipe, a command, a diagnostic
id, a file path, a package id, a target framework, a default behaviour, a version. Look each one up:
read the `.props`/`.targets`, the `Justfile` and its modules, the analyzer's descriptor, the
`.csproj`, the test that pins the behaviour. A claim you cannot find in the code is **removed or
marked as a question for the user** — never kept on trust, never guessed.

How to check each kind of claim: [references/truth-audit.md](references/truth-audit.md).

## Which README is which

What a package ships as its README decides which file a NuGet user reads, so work it out from the
build, not from the file names. With Purview.BuildSdk, for a packable project with no explicit
`PackageReadmeFile`:

1. the project's own `README.md`, beside its `.csproj`, wins;
2. otherwise the repository-root `README.md` is packed;
3. a file under the project's `Sdk/` folder named `*.md` is packed to the package root by
   `PurviewAutoSdkPack`, so `Sdk/README.md` *is* the package README when the project packs `Sdk/`.

So in a repository with several packages, each package's README is the per-project one, and the root
`README.md` describes the repository as a whole. A project README covers only what that package
does, how to install it and its own settings; it links to the wiki for the rest rather than copying
it. Check which file each project actually packs (`dotnet pack`, then list the `.nupkg`) before
editing one, and say in the root README which per-project READMEs exist.

## The wiki is hierarchical

`docs/wiki/` is one folder per topic area, plus a few root pages. Two index files tie it together:

- `_Sidebar.md` — every page exactly once, grouped into sections (`- **Section**`), in reading order.
  The documentation site reads its links in document order for page order, and does not publish it.
- `Home.md` — the browsing index: a short intro, a "How this wiki is organised" table, then one
  `## Section` heading per sidebar section, each with a `| Page | What it answers |` table.

The rules, the published-URL consequences, and the test that enforces them:
[references/wiki-structure.md](references/wiki-structure.md). The short form:

- a page lives at the wiki root **only** when the site depends on its URL: the landing page
  (`docs.rootPage`) and any page the site's project catalogue deep-links (`docsPage`). Everything else
  is in exactly one topic folder, never deeper;
- folder and page names are letters, digits and single dashes (`configuration/Property-Schema.md`);
- each page opens with exactly one `# ` title, then a one-paragraph description of what it answers;
- links are relative to the file they are written in (`../packaging/SourceLink.md` from a folder page);
- moving a page changes its published URL — record the old and new slugs so the site can redirect.

## The sweep

Run it as a sweep, not an edit: a change that lands in one surface and stops there is exactly the
drift this skill exists to prevent.

1. **Inventory the change.** From the diff (or, for an audit, the code): what was added, renamed,
   re-defaulted or removed — properties, targets, recipes, diagnostics, projects, packages, skills,
   files. Each item is a row in the sweep.
2. **Find every mention.** For each item, search all four surfaces, including old names:
   `git grep -n -e OldName -e NewName -- '*.md'`. Also search `CHANGELOG.md`, but never rewrite
   released entries — they are history.
3. **Correct each mention against the code** (the one rule). Prefer deleting a duplicated explanation
   and linking to its one home over keeping two copies in step.
4. **Skills**
   - Update the `description` so the skill *fires* on the new question, and keep it under 1024
     characters.
   - Keep `SKILL.md` under 500 lines, with detail in `references/`.
   - A skill describes what a **consumer** does. Guidance about working in this repository belongs in
     `AGENTS.md`, or in a repository-local skill under `.agents/skills/`, which is not published.
   - A new skill is named `<vendor>-<product>-<topic>`, and its folder name matches its `name`.
5. **Agent instructions.**
   - `AGENTS.md` carries the skill table (one row per authored skill, linking its authored path
     `./skills/<name>/SKILL.md`, never `.agents/skills/`), the repository map, the canonical commands
     and the invariants a contributor must not break.
   - The wrappers stay one-line pointers to it.
6. **READMEs.**
   - The root README covers what the repository ships, the quick start, the feature summary and where
     the docs are.
   - Each per-project README covers its own package.
   - Long reference tables belong in the wiki. Keep only those the tests hold the README to.
7. **Wiki.**
   - Update the page that owns the topic.
   - A new page goes in its topic's folder and gets added to `_Sidebar.md` (once, in reading order)
     and to that section's table in `Home.md`.
   - A new topic area gets a folder, a sidebar section and a `Home.md` heading, all with the same
     name.
8. **Changelog.** Add one entry per user-visible change: `### Documentation` for a docs-only fix,
   naming what was wrong. It goes under the heading for the version in `package.json`
   (`## [x.y.z] - <date>`, created when a bump moves the version), **not** under `[Unreleased]`. A
   merge to `main` publishes that version, so an entry parked under `[Unreleased]` reads as unreleased
   until it is already live, and the changelog is out of date the moment it ships. `[Unreleased]` is
   only for a pre-release version (`x.y.z-alpha.1`, `-beta`, `-rc`), whose changes genuinely have not
   shipped as a release yet.
9. **Verify** (below), and state the commands you ran.

## Verify

Each check proves something different, so run all that the repository has:

```bash
git grep -n -e '<each renamed or removed name>' -- '*.md'   # nothing stale left
just skills-validate     # PRSKL0005: every skill well formed, every references/ link resolves
just test                # the repository's doc tests: wiki structure, skill rules, property tables
```

- **The wiki test:** if the repository has none, add one (see
  [references/wiki-structure.md](references/wiki-structure.md#the-test)). Without it, every rule
  above is a convention nobody checks.
- **Links outside the wiki:** for every link a README or skill makes into `docs/wiki/`, open the
  target and confirm the page and the `#anchor` exist.
- **Skill references:** follow each relative link once.

## Guardrails

- **Do not change behaviour to match a document.** When the code and the docs disagree, the docs are
  wrong — unless the user says the code is. Report a disagreement that looks like a code bug rather
  than documenting the bug as intended.
- **Do not invent.** No undocumented defaults, no example output you did not run, no version numbers
  from memory.
- **Do not move a root page** the site deep-links without telling the user. It breaks inbound links
  outside this repository.
- **Do not rewrite released changelog entries,** and do not touch generated files.
- Keep the sweep to documentation: a refactor found along the way is a separate change.
