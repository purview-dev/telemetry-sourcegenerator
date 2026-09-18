# Release Process

This document describes the complete release process for this repository.

---

## Overview

Releases are driven by GitHub Actions using the reusable
[`purview-dev/build`](https://github.com/purview-dev/build) pipelines. No release steps are
performed manually.

```text
Feature branch
  → PR to main          (pr.yml runs purview-build.yml: restore + build + lint + tests)
  → merge to main       (release.yml runs purview-release.yml: build, test, pack, publish)
  → GitHub Release      (NuGet package + changelog, created by the pipeline)
```

### Workflow components

| Component | File | Purpose |
| ----------- | ------ | --------- |
| PR gate | `.github/workflows/pr.yml` | Runs the reusable `purview-dev/build` `purview-build.yml` pipeline (restore, build, lint, tests) and builds/tests the sample solution |
| CD pipeline | `.github/workflows/release.yml` | Runs the reusable `purview-dev/build` `purview-release.yml` pipeline on push to `main` |
| Local pipeline | `Justfile` `pipeline-*` recipes | Mirror the CI/CD pipelines locally via the `Purview.Build` tool (`.tools/purview-build`) |

## Developer workflow

### 1. Make your changes

```bash
git checkout -b feat/my-change
# ... make changes ...
```

### 2. Validate locally

Run the PR gate before pushing:

```bash
just pipeline-pr
```

This restores, builds, lints, and runs the integration tests — the same gate CI enforces.

### 3. Open a PR

```bash
git add .
git commit -m "feat: my change"
git push origin feat/my-change
# Open a PR to main
```

`pr.yml` must pass (build, lint, tests, plus the sample build/test job) before the PR can merge.

### 4. Merge the PR

Merge the PR into `main`.

### 5. Release is published automatically

Merging to `main` triggers `release.yml`, which runs the reusable `purview-release.yml` pipeline:

1. Restores and builds the main and sample solutions.
2. Runs the integration tests.
3. Packs the NuGet package (version read from `package.json`).
4. Publishes the package and creates a GitHub Release.

## Versioning

- The version lives in `package.json`. **Current Version:** 5.0.0-prerelease.11
- It is applied to `Version` / `PackageVersion` by `Purview.BuildSdk` via package.json
  version detection (`UsePackageJsonVersion`, default `true`).
- `just version` prints the current version.
- After bumping `package.json`, run `just update-version` to sync the version into docs/samples.

## Building the package locally

| Command | Purpose |
| --- | --- |
| `just pack` | Updates the version, then packs the NuGet package into `artifacts/`. |
| `just pipeline-local-release` | Packs and publishes to a local NuGet feed (see the Justfile note on argument quoting for the feed path). |
| `just pipeline-release` | Full release pipeline (build, test, pack, publish, GitHub release). |

## Pre-release validation

Before a release, validate locally:

1. `just pipeline-pr` — restore, build, lint, tests.
2. `just build-s && just test-s` — sample solution build and tests.
3. `just pipeline-local-release` — confirm the pack + local publish succeed.

## Releasing

1. Bump the version in `package.json`.
2. Run `just update-version` to sync docs/samples.
3. Push a PR with the version bump; merge to `main`.
4. `release.yml` publishes the release automatically.

> The legacy `scripts/setup-release.*` files describe a changeset-based (`@changesets/cli`) flow that
> is **not** used by this repository. They are retained for reference only; the actual release
> automation lives in `.github/workflows/pr.yml` and `.github/workflows/release.yml`.