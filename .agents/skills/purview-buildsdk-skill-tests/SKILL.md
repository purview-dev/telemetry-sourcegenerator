---
name: purview-buildsdk-skill-tests
description: Author agent skills in a repository that uses Purview.BuildSdk, and validate them with Purview.Skills.Testing or the SDK's pack-time ValidatePurviewSkills target. Use when adding or renaming a skill, writing SKILL.md frontmatter, splitting content into references/ or scripts/, fixing a PRSKL0005 pack failure or a skill-name, skill-description or skill-bundled-resources test failure, or deciding where skills belong in a repository.
license: MIT
metadata:
  package: Purview.Skills.Testing
  repository: purview-dev/build-sdk
---

# Authoring and validating agent skills

For installing skills a package ships, use `purview-buildsdk-skill-installation`.

## Where skills live

**At the repository root, in `skills/<name>/SKILL.md`.** Always.

The skills CLI only discovers skills at a root `skills/` folder, and it reaches a repository by
The skills CLI only discovers skills at a root `skills/` folder, and it reaches a repository by
cloning it, so nothing else is visible to it. Nothing is packed into a NuGet package — a package
carries .NET artifacts, and skills are delivered by the CLI from git.

```text
skills/
  my-skill/
    SKILL.md          required
    references/       loaded on demand
    scripts/          executable, run on demand
    assets/           templates and static resources
```

Link to a skill by its **authored** path, `./skills/<name>/SKILL.md`. A link into `.agents/skills/`
points at a gitignored file that does not exist in a fresh clone.

## `SKILL.md` format

```markdown
---
name: my-skill
description: What it does, and when to use it. Include the words a task would contain.
license: MIT
metadata:
  package: My.Package
---

# My skill

Instructions.
```

| Field | Required | Constraint |
| -- | -- | -- |
| `name` | yes | 1–64 chars, lowercase `a-z0-9` and single hyphens, no leading, trailing or consecutive hyphen, **must equal the folder name** |
| `description` | yes | 1–1024 chars, non-empty |
| `license` | no | A licence name, or the name of a bundled licence file |
| `compatibility` | no | ≤500 chars, only when the skill has real environment requirements |
| `metadata` | no | A map of string to string. Anything not defined above belongs here |
| `allowed-tools` | no | Space-separated pre-approved tools. Experimental |

**There are no other top-level fields.** `category`, `roles` and `tags` are common inventions and a
strict client rejects them; put them under `metadata`.

The full specification is at <https://agentskills.io/specification>.
[references/authoring.md](references/authoring.md) covers description writing, naming for a shared
namespace, and progressive disclosure.

## Progressive disclosure

An agent loads a skill in three stages: the `name` and `description` at startup, the whole
`SKILL.md` body once the skill activates, and `references/`, `scripts/` or `assets/` only when the
task reaches them.

So: keep `SKILL.md` **under 500 lines**, and move detail into `references/`. The body is a cost paid
on every activation; a reference file is paid only when it is needed.

Keep file references one level deep from `SKILL.md`, and relative to the skill root. A path that
climbs out of the skill folder is broken everywhere except the authoring repository, because only
the skill's own folder is installed.

## Validating

### In tests — `Purview.Skills.Testing`

```csharp
[InheritsTests]
public sealed class SkillTests : PurviewSkillTests;
```

**`[InheritsTests]` is load-bearing.** TUnit does not walk base types during discovery, so without
it the class compiles, the run is green and nothing is validated. TUnit reports `TUnit0030`; treat
it as an error.

| Rule id | What it checks |
| -- | -- |
| `skills-discovery` | The folder, when present, holds at least one skill |
| `skill-structure` | Every folder has a `SKILL.md` whose frontmatter parses |
| `skill-name` | Spec-legal, unique, and equal to the folder name |
| `skill-description` | Present, non-empty, within 1024 characters |
| `skill-optional-fields` | Only spec-defined fields, each within its limit |
| `skill-bundled-resources` | Every linked file exists, inside the skill folder |
| `skill-body-size` | `SKILL.md` stays under the recommended 500 lines |

A repository with **no** `skills/` folder passes every rule — authoring skills is optional. An
**empty** folder fails `skills-discovery`, because every other rule is vacuous against an empty
catalogue.

Overrides: `RepositoryRoot` (default: the SDK's repository-root marker walk),
`SkillsFolderName` (default `skills`), and `SkipRule(string)`.

The evaluator takes a `SkillCatalogue`, so a rule can be exercised against a fixture folder rather
than a real repository — the same three-layer shape this repository uses throughout: a reader, an
evaluator over an in-memory model, and a thin test base class.

### At pack time — `ValidatePurviewSkills`

It runs `BeforeTargets="Pack"`, so a malformed skill fails in the repository that owns it rather
than reaching the CLI, which would silently skip it.

```bash
dotnet msbuild <project> -t:ValidatePurviewSkills
```

Failures are reported as `PRSKL0005`, one per problem. Every authored skill is checked.

The two checks overlap deliberately: the pack-time one runs without a test host, the test one
covers more of the specification.

## What is deliberately not checked

Prose quality, and whether an agent actually obeys the skill. A build can only check that a skill is
well formed. Treat a green run as "installable", not "good".
