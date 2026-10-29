# Authoring a skill that fires, and keeps firing

## The description is the whole activation decision

`name` and `description` are the only things loaded at startup. Everything else — the body, the
references, the scripts — is invisible until an agent has already decided to activate the skill on
the strength of those two fields.

So write the description for a *matcher*, not for a reader:

- Say **what it does** and **when to use it**, in that order.
- Include the words a real task would contain: the tool names, the file names, the error codes, the
  property names, the phrases a person would type.
- Name the thing the skill is *not*, when a neighbouring skill could be confused with it.

| | |
| -- | -- |
| Poor | `Helps with the build SDK.` |
| Better | `Configure Purview.BuildSdk from Directory.Build.props or a .csproj. Use when setting NamespacePrefix, choosing a testing framework, or working out whether a property must be set before the SDK import.` |

The limit is 1024 characters. Most of that budget is better spent on trigger words than on prose.

## Naming for a shared namespace

Skills install into one flat directory per agent, shared with every other skill a developer has
installed from anywhere. A generic name is a collision waiting to happen, and the loser is whichever
one installed second.

- Prefix with the vendor and the product: `purview-buildsdk-configuration`, not `sdk-configuration`.
- `sdk-*`, `project-*`, `test-*`, `build-*` are all too common to use unprefixed.
- The name must be 1–64 characters, lowercase `a-z0-9` with single hyphens, no leading, trailing or
  consecutive hyphen — and it must equal the folder name, because the CLI installs by the
  frontmatter name while a human reads the folder.

Renaming a skill means renaming the folder and the frontmatter together, and re-running the install.

## Progressive disclosure in practice

| Stage | Loaded | Budget |
| -- | -- | -- |
| Metadata | `name`, `description` | ~100 tokens, always |
| Instructions | the whole `SKILL.md` body | under 5000 tokens recommended; keep the file under 500 lines |
| Resources | `references/`, `scripts/`, `assets/` | only when the task reaches them |

What belongs where:

- **`SKILL.md`** — the decisions, the rules, the things that are wrong by default, and a pointer to
  everything else. If a reader needs it to make the *first* correct move, it goes here.
- **`references/`** — long tables, full option lists, grammars, enumerations. Keep each file
  focused; an agent loads the whole file.
- **`scripts/`** — executable code. Self-contained, or documenting its dependencies clearly, with
  helpful error messages.
- **`assets/`** — templates, schemas, images, lookup data.

Keep references one level deep. Avoid a chain where one reference sends an agent to another.

## Links

Use relative paths from the skill root:

```markdown
See [the reference](references/REFERENCE.md) for detail.
Run `scripts/extract.py`.
```

Only the skill's own folder is installed, so a link that climbs out of it (`../other-skill/...`) is
broken on every machine except the one that authored it. To point at another skill, name it in prose
and let the agent find it by name.

## Keeping a skill true

A skill that describes a product goes stale the moment the product changes, and nothing about a
stale skill looks broken — it just gives confidently wrong advice.

- Change the skill in the **same commit** as the behaviour it documents.
- Prefer stating the property name, the diagnostic code or the target name over paraphrasing the
  behaviour; those are greppable when the code changes.
- Where a number or a list is authoritative somewhere else — a default, an enumeration — say where
  it comes from, so a reader can check it.
