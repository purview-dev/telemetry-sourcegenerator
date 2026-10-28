# `--treenode-filter` reference

TUnit runs on Microsoft.Testing.Platform, not VSTest, so the VSTest `--filter` syntax does not
apply. Narrowing is done with `--treenode-filter`.

## The path

```text
/<Assembly>/<Namespace>/<Class name>/<Test name>
```

Each segment maps to one level of the tree:

| Position | Matches |
| -- | -- |
| 1 | Assembly |
| 2 | Namespace |
| 3 | Class |
| 4 | Test name |

```text
/*/*/LoginTests/*                        every test in LoginTests
/*/*/*/AcceptCookiesTest                 one test by name
/*/MyProject.Tests.Integration/*/*       every test in a namespace
/*/*/*/*                                 everything
```

## Operators

### `*` — wildcard

Matches a whole segment or part of one.

```text
/*/*/LoginTests*/*
/*/*/MyProject.Tests.Api*/*
```

### `=` and `!=` — property predicates

In the last segment, inside `[...]`.

```text
/*/*/*/*[Category=Unit]
/*/*/*/*[Category!=Slow]
/*/*/*/*[Owner=*Team-Backend*]
```

### `&` and `|` — combining

Inside a **single** bracket group, or a single parenthesised segment.

```text
/*/*/*/*[(Category=Unit)&(Priority=High)]
/**[(Category=Unit)|(Priority=High)]
/*/*/(LoginTests)|(SignupTests)/*
```

### `**` — match all

Any depth, but **only at the end** of the path.

```text
/**
/MyAssembly/**
```

`/**/Path` is not valid.

## Rules that catch people out

- Only **one** property group `[...]` per path segment. `[Category=Smoke]|[Priority=High]` is not
  valid; write `[(Category=Smoke)|(Priority=High)]`.
- `**` must be last.
- A segment left empty is not a wildcard — write `*`.

## The `--` separator

```bash
dotnet test --treenode-filter "/*/*/*/*[Category=Unit]"        # preferred
dotnet test -- --treenode-filter "/*/*/*/*[Category=Unit]"     # older SDKs
```

Newer .NET SDKs forward unrecognised arguments to the MTP runner directly. On an older SDK the `--`
separator is required, and omitting it produces an argument error rather than a filter.

## Common selections

| Intent | Filter |
| -- | -- |
| Everything | `/*/*/*/*` |
| All unit tests | `/*/*/*/*[Category=Unit]` |
| High-priority unit tests | `/*/*/*/*[(Category=Unit)&(Priority=High)]` |
| One class | `/*/*/MyFeatureTests/*` |
| A name pattern | `/*/*/*/MyScenario*` |
| Critical integration tests | `/*/MyProject.Tests.Integration/*/*[Priority=Critical]` |

## Zero tests ran

1. `--treenode-filter`, not `--filter`.
2. Try without the `--` separator first, then with it.
3. Broaden to `/*/*/*/*`, then narrow one segment at a time.
4. Confirm the project has `[Test]` methods.
5. Confirm the project is **detected** as a test project —
   `dotnet msbuild <project> -getProperty:IsTestProject`. A name without the dot, such as
   `UnitTests` rather than `Api.UnitTests`, is not detected and gets no runner.
6. Confirm every property predicate sits in one bracket group.
