# Release Process

This document describes the complete release process for this repository.
It is **designed to be reusable** — copy it (along with the scripts in `scripts/`) to any other repository and substitute the org/repo names.

---

## Table of Contents

1. [Overview](#overview)
2. [Developer Workflow](#developer-workflow)
3. [Quick Setup (DSC Scripts)](#quick-setup-dsc-scripts)
4. [Manual Setup Guide](#manual-setup-guide)
   - [Step 1 — Authenticate the release bot](#step-1--authenticate-the-release-bot)
   - [Step 2 — Branch ruleset](#step-2--branch-ruleset-for-main)
   - [Step 3 — Tag ruleset](#step-3--tag-ruleset-for-v)
   - [Step 4 — Immutable releases](#step-4--immutable-releases)
   - [Step 5 — Verify](#step-5--verify)
5. [Token Reference by Scope](#token-reference-by-scope)
6. [DSC Script Reference](#dsc-script-reference)
7. [Adapting to Another Repository](#adapting-to-another-repository)
8. [Troubleshooting](#troubleshooting)

---

## Overview

Releases are **fully automated and gated**. No human ever pushes a version tag or manually creates a GitHub release. The flow is:

```text
Feature branch
  → just changeset         (describe your change)
  → PR to main             (changeset-check + ci-gate must pass)
  → merge                  (release bot creates Version PR)
  → Version PR             (ci-gate must pass)
  → merge                  (CD pipeline publishes the release)
```

### Why this is bullet-proof

| Risk | Mitigation |
| ------ | ------------ |
| Tag pushed manually before CD runs | Tag ruleset: only `github-actions[bot]` or your GitHub App can create `v*` tags |
| Release created without build artifacts | Draft-first: NuGet assets attached before `--draft=false` |
| Published release tampered with | Immutable releases: published releases are locked |
| Mid-release failure leaves broken state | ERR trap: deletes draft + tag on any error → clean retry |
| Double-release on re-run | CD checks tag existence first; skips if already released |
| PR merged without a changeset | `changeset-check` required status check blocks merging |
| Version PR bypasses CI | `CHANGESET_TOKEN` / GitHub App ensures CI triggers on bot-created PR |

### Workflow components

| Component | File | Purpose |
| ----------- | ------ | --------- |
| Changeset bot | `.github/workflows/release-pr.yml` | Creates/updates Version PR on push to main |
| CD pipeline | `.github/workflows/cd.yml` | Triggers on `package.json` change; builds, tests, packs, releases |
| Changeset check | `.github/workflows/ci.yml` (`changeset-check` job) | Blocks PRs without a changeset file |
| CI gate | `.github/workflows/ci.yml` (`ci-gate` job) | Single required status check |
| Changeset config | `.changeset/config.json` | Tells the bot which repo and changelog format to use |
| Version script | `package.json` `version-packages` | Runs `changeset version` then `update-version.ts` |

---

## Developer Workflow

### 1. Make your changes

```bash
git checkout -b feat/my-change
# ... make changes ...
```

### 2. Add a changeset

```bash
just changeset
```

This opens an interactive prompt. Select the bump type:

| Type | When to use |
| ------ | ------------- |
| `patch` | Bug fixes, internal refactors, dependency bumps |
| `minor` | New features, new options, backwards-compatible additions |
| `major` | Breaking changes — any change that requires consumer code changes |

A `.changeset/<random-name>.md` file is created. Commit it with your changes.

### 3. Open a PR

```bash
git add .
git commit -m "feat: my change"
git push origin feat/my-change
# Open a PR to main
```

The `Changeset Check` status check will verify a `.changeset/*.md` file is present.

If your PR needs no release note (docs-only, CI-only), add the `skip-changeset` label.

### 4. Merge the PR

Once `CI Gate` and `Changeset Check` are green, merge the PR.

### 5. Version PR is created automatically

The `release-pr.yml` workflow creates (or updates) a **Version Packages** PR:

- Branch: `changeset-release/main`
- Bumps `package.json` version, updates `CHANGELOG.md`, deletes consumed `.changeset/*.md` files, syncs docs/wiki

Review, ensure CI passes, then merge it.

### 6. Release is published automatically

Merging the Version PR triggers `cd.yml`, which:

1. Reads the new version from `package.json`
2. Skips if the release tag already exists (idempotent)
3. Runs format check, build, tests (main + sample solution)
4. Packs NuGet artifacts
5. Creates a **draft** GitHub Release with assets attached
6. Publishes the draft → becomes immutable
7. On any failure: ERR trap deletes the draft + tag → clean retry

### Checking pending changesets

```bash
just changeset-status
```

---

## Quick Setup (DSC Scripts)

Two idempotent scripts automate the repository configuration. Safe to re-run.

### Bash (Linux / macOS / GitHub Actions)

```bash
# Personal repo, fine-grained PAT
./scripts/setup-release.sh \
  --owner myuser --repo myrepo \
  --scope personal --token-type fine-grained

# Organisation repo, GitHub App (fully automated)
./scripts/setup-release.sh \
  --owner myorg --repo myrepo \
  --scope org --token-type app \
  --app-id 123456 --app-pem ./my-app.private-key.pem

# GitHub Enterprise Server, classic PAT
./scripts/setup-release.sh \
  --owner myorg --repo myrepo \
  --scope ghes --token-type classic \
  --ghes-host github.example.com

# Dry run — see what would happen without making changes
./scripts/setup-release.sh --owner myorg --repo myrepo --dry-run
```

### PowerShell (Windows / cross-platform)

```powershell
# Personal repo, fine-grained PAT
.\scripts\setup-release.ps1 `
  -Owner myuser -Repo myrepo `
  -Scope personal -TokenType fine-grained

# Organisation repo, GitHub App (fully automated)
.\scripts\setup-release.ps1 `
  -Owner myorg -Repo myrepo `
  -Scope org -TokenType app `
  -AppId 123456 -AppPem .\my-app.private-key.pem

# GitHub Enterprise Server, classic PAT
.\scripts\setup-release.ps1 `
  -Owner myorg -Repo myrepo `
  -Scope ghes -TokenType classic `
  -GhesHost github.example.com

# Dry run
.\scripts\setup-release.ps1 -Owner myorg -Repo myrepo -DryRun
```

### What the scripts configure

| Step | Automatable | Notes |
| ------ | ------------- | ------- |
| Branch ruleset (`main`) | ✅ Fully automated | Uses `gh api` |
| Tag ruleset (`v*`) | ✅ With GitHub App | App installation ID used as bypass actor |
| Tag ruleset (`v*`) | ⚠ UI step with PAT | `github-actions[bot]` bypass requires UI |
| `skip-changeset` label | ✅ Fully automated | Used by `changeset-check` job |
| `CHANGESET_TOKEN` secret | Guided | Script prints exact URL + instructions |
| `APP_ID` / `APP_PRIVATE_KEY` secrets | ✅ If `--app-id` and `--app-pem` provided | |
| Immutable releases | ⚠ UI step | No API endpoint exists |

---

## Manual Setup Guide

### Step 1 — Authenticate the release bot

The Version PR bot uses `GITHUB_TOKEN` by default. But GitHub's anti-loop protection means `GITHUB_TOKEN`-created PRs **do not trigger CI** — so `CI Gate` never passes and the Version PR can never merge.

You must configure one of the following three options:

---

#### Option A — GitHub App (recommended for orgs and enterprises)

GitHub Apps issue short-lived tokens per workflow run. The private key never expires; no rotation needed.

**When to use:**

- Organisation or enterprise repos
- Any environment where token expiry management is undesirable
- When you also want the tag ruleset bypass to be automatable

**Setup:**

1. **Create the app**

   | Scope | URL |
   | ------- | ----- |
   | Personal | <https://github.com/settings/apps/new> |
   | Organisation | `https://github.com/organizations/{org}/settings/apps/new` |
   | GHEC | Same as organisation on github.com |
   | GHES | `https://{hostname}/settings/apps/new` |

2. **Fill in fields:**
   - App name: `{org}-release-bot` (or any unique name)
   - Homepage URL: your repository URL
   - Webhook: **uncheck "Active"**

3. **Set permissions:**

   | Permission | Level | Why |
   | ------------ | ------- | ----- |
   | Contents | Read & write | Push version-bump commits, CHANGELOG |
   | Pull requests | Read & write | Create and update Version PR |
   | Workflows | Read & write | Update workflow files if changeset touches `.github/` |
   | Metadata | Read | Always required (auto-set) |

4. **Click Create GitHub App**

5. **Download private key:**
   - On the app settings page → scroll to **Private keys** → **Generate a private key**
   - Save the downloaded `.pem` file securely

6. **Note the App ID** shown on the settings page

7. **Install the app on the repository:**
   - App settings → **Install App** → select your org/account → **Only select repositories** → pick the repo

8. **Set secrets:**

   ```bash
   gh secret set APP_ID --repo {owner}/{repo} --body "123456"
   gh secret set APP_PRIVATE_KEY --repo {owner}/{repo} < /path/to/app.private-key.pem
   ```

   PowerShell:

   ```powershell
   gh secret set APP_ID --repo {owner}/{repo} --body "123456"
   Get-Content .\app.private-key.pem -Raw | gh secret set APP_PRIVATE_KEY --repo {owner}/{repo}
   ```

9. **Get installation ID** (needed for the tag ruleset bypass — see Step 3):

   ```bash
   # Via gh CLI (once the app is installed on the repo):
   gh api repos/{owner}/{repo}/installation --jq '.id'

   # Alternatively: App settings → Installations → click the installed org/account
   # The installation ID appears in the URL: /installations/{id}
   ```

**The `release-pr.yml` workflow auto-detects `APP_ID` and uses the App token when present.**

---

#### Option B — Fine-grained Personal Access Token

Scoped to a specific repository with exact permissions. Maximum expiry: 1 year. **Set a calendar reminder to rotate before expiry.**

**When to use:**

- Personal repos or small teams
- GHES 3.10+
- When a GitHub App is not practical

**Required permissions:**

| Permission | Level | Why |
| ------------ | ------- | ----- |
| Contents | Read & write | Push version-bump commits |
| Pull requests | Read & write | Create/update Version PR |
| Workflows | Read & write | Update `.github/workflows/` if touched |
| Metadata | Read | Auto-required |

**Setup by scope:**

<details>
<summary><strong>Personal account</strong></summary>

1. Navigate to: <https://github.com/settings/personal-access-tokens/new>
2. **Resource owner:** your personal account
3. **Repository access:** Only select repositories → pick the repo
4. Set permissions (table above) and an expiry
5. Copy the token and set the secret:

   ```bash
   gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   # paste when prompted
   ```

</details>

<details>
<summary><strong>Organisation-owned repository</strong></summary>

1. Ensure the org allows fine-grained PATs:
   - Navigate to: `https://github.com/organizations/{org}/settings/personal-access-tokens`
   - Set policy to **Allow fine-grained personal access tokens**
   - If approval is required, submit a request and wait for admin approval

2. Navigate to: <https://github.com/settings/personal-access-tokens/new>
3. **Resource owner:** select the **organisation** (not your personal account)
4. **Repository access:** Only select repositories → pick the repo
5. Set permissions (table above) and an expiry
6. **SAML SSO:** Fine-grained PATs do **not** require a separate SSO authorization step
7. Copy the token:

   ```bash
   gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   ```

</details>

<details>
<summary><strong>GitHub Enterprise Cloud (GHEC)</strong></summary>

Same as organisation above. Additionally:

- Enterprise admins can enforce a fine-grained PAT policy at the enterprise level
- Check with your enterprise admin if you get "policy does not allow" errors
- Enterprise-managed users (EMU): token creation may be restricted to the IdP — follow your enterprise's provisioning process

</details>

<details>
<summary><strong>GitHub Enterprise Server (GHES 3.10+)</strong></summary>

1. Ensure the org allows fine-grained PATs:
   - Navigate to: `https://{hostname}/organizations/{org}/settings/personal-access-tokens`

2. Navigate to: `https://{hostname}/settings/personal-access-tokens/new`
3. Set permissions (table above) and an expiry
4. Copy the token:

   ```bash
   GH_HOST={hostname} gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   ```

> ⚠ **GHES < 3.10:** Fine-grained PATs are not available. Use Option C.

</details>

---

#### Option C — Classic Personal Access Token (legacy / fallback)

Grants broad `repo` scope across **all** repositories the token owner has access to. Use only when Options A and B are unavailable.

**When to use:**

- GHES < 3.10
- Organisation policy explicitly blocks fine-grained PATs and GitHub Apps

**Setup by scope:**

<details>
<summary><strong>Personal account</strong></summary>

1. Navigate to: <https://github.com/settings/tokens/new>
2. Scope: `repo`
3. Set an expiry (recommended, even though it's optional)
4. Copy the token:

   ```bash
   gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   ```

</details>

<details>
<summary><strong>Organisation-owned repository</strong></summary>

1. Navigate to: <https://github.com/settings/tokens/new>
2. Scope: `repo`
3. **SAML SSO:** After creating the token, click **"Authorize"** next to the organisation name
4. Copy the token:

   ```bash
   gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   ```

</details>

<details>
<summary><strong>GitHub Enterprise Cloud (GHEC)</strong></summary>

Same as organisation. SAML SSO authorization is required. If the enterprise uses IP allow lists, ensure the CI runner IPs are allowed.

</details>

<details>
<summary><strong>GitHub Enterprise Server (GHES any version)</strong></summary>

1. Navigate to: `https://{hostname}/settings/tokens/new`
2. Scope: `repo`
3. Copy the token:

   ```bash
   GH_HOST={hostname} gh secret set CHANGESET_TOKEN --repo {owner}/{repo}
   ```

</details>

---

#### Option summary

| Scenario | Recommended option | Token expiry | Tag ruleset bypass automatable |
| ---------- | ------------------- | -------------- | ------------------------------- |
| Organisation or enterprise | **A — GitHub App** | Never (private key) | ✅ Yes |
| Personal repo, small team | **B — Fine-grained PAT** | Max 1 year | ⚠ UI step |
| GHES < 3.10 | **C — Classic PAT** | Optional | ⚠ UI step |
| Org blocks fine-grained PATs | **A or C** | — | ✅ A / ⚠ C |

---

### Step 2 — Branch ruleset for `main`

Requires a PR and `CI Gate` to pass before any commit reaches `main`.

**Automated (already applied to this repo):**

```bash
gh api repos/{owner}/{repo}/rulesets \
  --method POST \
  --header "Content-Type: application/json" \
  --input - <<'EOF'
{
  "name": "Protect main branch",
  "target": "branch",
  "enforcement": "active",
  "conditions": {
    "ref_name": { "include": ["refs/heads/main"], "exclude": [] }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    {
      "type": "pull_request",
      "parameters": {
        "required_approving_review_count": 0,
        "dismiss_stale_reviews_on_push": false,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_review_thread_resolution": false
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": false,
        "required_status_checks": [{ "context": "CI Gate" }]
      }
    }
  ]
}
EOF
```

PowerShell equivalent:

```powershell
$body = @{
  name = "Protect main branch"; target = "branch"; enforcement = "active"
  conditions = @{ ref_name = @{ include = @("refs/heads/main"); exclude = @() } }
  rules = @(
    @{ type = "deletion" }
    @{ type = "non_fast_forward" }
    @{ type = "pull_request"
       parameters = @{ required_approving_review_count = 0 } }
    @{ type = "required_status_checks"
       parameters = @{ strict_required_status_checks_policy = $false
                       required_status_checks = @(@{ context = "CI Gate" }) } }
  )
} | ConvertTo-Json -Depth 10

$body | gh api repos/{owner}/{repo}/rulesets --method POST --header "Content-Type: application/json" --input -
```

**Via UI:** Settings → Rules → Rulesets → New ruleset → New branch ruleset

| Setting | Value |
| --------- | ------- |
| Ruleset name | `Protect main branch` |
| Target branches | `refs/heads/main` |
| Rules | ✅ Restrict deletions |
| | ✅ Block force pushes |
| | ✅ Require a pull request before merging (0 approvals) |
| | ✅ Require status checks → add `CI Gate` |

> **For GHES:** Rulesets require GHES 3.11+. On earlier versions use branch protection rules:
>
> ```bash
> GH_HOST={hostname} gh api repos/{owner}/{repo}/branches/main/protection \
>   --method PUT \
>   --input - <<'EOF'
> {
>   "required_status_checks": { "strict": false, "contexts": ["CI Gate"] },
>   "enforce_admins": false,
>   "required_pull_request_reviews": null,
>   "restrictions": null
> }
> EOF
> ```

---

### Step 3 — Tag ruleset for `v*`

Prevents anyone from pushing `v*` tags manually. Tags can only be created by the CD pipeline.

> **This is the most important security guard in the release flow.**

#### With GitHub App (Option A) — fully automatable

```bash
# 1. Get the app's installation ID on the repo:
INSTALLATION_ID=$(gh api repos/{owner}/{repo}/installation --jq '.id')

# 2. Create the tag ruleset with the App as bypass actor:
gh api repos/{owner}/{repo}/rulesets \
  --method POST \
  --header "Content-Type: application/json" \
  --input - <<EOF
{
  "name": "Protect release tags",
  "target": "tag",
  "enforcement": "active",
  "conditions": {
    "ref_name": { "include": ["refs/tags/v*"], "exclude": [] }
  },
  "rules": [{ "type": "creation" }],
  "bypass_actors": [{
    "actor_id": ${INSTALLATION_ID},
    "actor_type": "Integration",
    "bypass_mode": "always"
  }]
}
EOF
```

PowerShell:

```powershell
$installationId = (gh api repos/{owner}/{repo}/installation | ConvertFrom-Json).id

$body = @{
  name = "Protect release tags"; target = "tag"; enforcement = "active"
  conditions    = @{ ref_name = @{ include = @("refs/tags/v*"); exclude = @() } }
  rules         = @( @{ type = "creation" } )
  bypass_actors = @( @{ actor_id = $installationId; actor_type = "Integration"; bypass_mode = "always" } )
} | ConvertTo-Json -Depth 10

$body | gh api repos/{owner}/{repo}/rulesets --method POST --header "Content-Type: application/json" --input -
```

#### With PAT (Options B or C) — UI step required

The `github-actions[bot]` bypass actor cannot be specified programmatically without `admin:org` OAuth scope (which is unavailable with fine-grained or classic PATs). This step must be done via the GitHub UI:

1. Navigate to: `https://github.com/{owner}/{repo}/settings/rules/new?target=tag`
2. **Ruleset name:** `Protect release tags`
3. **Target tags:** `refs/tags/v*`
4. **Rules:** ✅ Restrict creations
5. **Bypass actors:** Add → search **"GitHub Actions"** → select → Save

> For **GHES**: Rulesets require GHES 3.11+. On earlier versions, the tag ruleset cannot be enforced via rules. Enforce via policy instead: document that tags must only be created via the CD pipeline.

---

### Step 4 — Immutable releases

Immutable releases prevent any modification to a published release — assets cannot be replaced or deleted, and the release body cannot be changed.

**Why it's safe with this release flow:**

- `gh release create --draft` creates a **draft** — drafts are never immutable
- Assets are uploaded to the draft (fully mutable at this point)
- `gh release edit --draft=false` publishes and locks the release
- The `trap cleanup ERR` fires on any error **before** the `--draft=false` line
- Therefore: failures always clean up a mutable draft; a locked release is never left broken

**Enable via UI:**

Settings → General → Releases → ✅ **Immutable releases**

> There is no REST API endpoint for this setting.

---

### Step 5 — Verify

1. Create a feature branch, run `just changeset`, commit and open a PR
2. Verify `Changeset Check` and `CI Gate` appear as required status checks on the PR
3. Merge the PR; verify a `changeset-release/main` PR appears within minutes
4. Verify CI runs on the Version PR (`CI Gate` passes)
5. Merge the Version PR; verify the CD pipeline runs and publishes a GitHub Release with NuGet assets attached
6. Verify the release is marked as immutable (lock icon on the release page)
7. Attempt to push a `v*` tag manually — verify it is rejected by the tag ruleset

---

## Token Reference by Scope

Quick reference covering all combinations of scope and token type.

| Scope | Token type | Where to create | Org admin action needed? | SAML SSO step? | Notes |
| ------- | ----------- | ----------------- | -------------------------- | --------------- | ------- |
| Personal | GitHub App | <https://github.com/settings/apps/new> | No | No | Preferred; no expiry |
| Personal | Fine-grained PAT | <https://github.com/settings/personal-access-tokens/new> | No | No | Max 1 year; set reminder |
| Personal | Classic PAT | <https://github.com/settings/tokens/new> | No | No | Broad scope; avoid if possible |
| Org | GitHub App | <https://github.com/organizations/{org}/settings/apps/new> | To install on org | No | Best for teams |
| Org | Fine-grained PAT | <https://github.com/settings/personal-access-tokens/new> | Yes — allow fine-grained PATs | No | Org approval may be required |
| Org | Classic PAT | <https://github.com/settings/tokens/new> | No (but SSO authz needed) | Yes — authorize for org | Broad scope; fallback only |
| GHEC | GitHub App | `https://github.com/organizations/{org}/settings/apps/new` | Yes — enterprise may restrict | No | EMU: check enterprise policy |
| GHEC | Fine-grained PAT | <https://github.com/settings/personal-access-tokens/new> | Yes — org and enterprise must allow | No | Enterprise policy may block |
| GHEC | Classic PAT | <https://github.com/settings/tokens/new> | No (SSO authz needed) | Yes | Broad scope |
| GHES 3.10+ | Fine-grained PAT | `https://{hostname}/settings/personal-access-tokens/new` | Yes — org must allow | Depends on GHES config | GHES 3.10+ only |
| GHES any | Classic PAT | `https://{hostname}/settings/tokens/new` | No | Depends on GHES config | Works on all GHES versions |
| GHES any | GitHub App | `https://{hostname}/settings/apps/new` | To install | No | Fully supported on all GHES |

### Secret names and workflow detection

The `release-pr.yml` workflow auto-detects which authentication method to use:

```yaml
# Prefers GitHub App when APP_ID is present; falls back to CHANGESET_TOKEN
- uses: actions/create-github-app-token@v1
  id: app-token
  if: ${{ secrets.APP_ID != '' }}
  with:
    app-id: ${{ secrets.APP_ID }}
    private-key: ${{ secrets.APP_PRIVATE_KEY }}

# Token used for checkout and changeset action:
token: ${{ steps.app-token.outputs.token || secrets.CHANGESET_TOKEN }}
```

| Secret name | Used when | Option |
|-------------|-----------|--------|
| `APP_ID` + `APP_PRIVATE_KEY` | GitHub App token is preferred | A |
| `CHANGESET_TOKEN` | Fallback when `APP_ID` is not set | B or C |

---

## DSC Script Reference

Both scripts (`setup-release.sh` and `setup-release.ps1`) are idempotent DSC-style configuration scripts. Safe to re-run at any time.

### Parameters

| Parameter | Type | Required | Default | Description |
| ----------- | ------ | ---------- | --------- | ------------- |
| `--owner` / `-Owner` | string | ✅ | — | GitHub user or org name |
| `--repo` / `-Repo` | string | ✅ | — | Repository name |
| `--scope` / `-Scope` | enum | — | `personal` | `personal` · `org` · `enterprise` · `ghes` |
| `--token-type` / `-TokenType` | enum | — | `app` | `app` · `fine-grained` · `classic` |
| `--ghes-host` / `-GhesHost` | string | When scope=ghes | — | GHES hostname, e.g. `github.example.com` |
| `--app-id` / `-AppId` | string | — | — | GitHub App ID (used to set secret and get installation) |
| `--app-pem` / `-AppPem` | path | — | — | Path to `.pem` private key (used to set secret) |
| `--dry-run` / `-DryRun` | flag | — | false | Print actions without applying |

### What each step does

| Step | Action | Idempotency check |
| ------ | -------- | ------------------- |
| 1. Branch ruleset | `POST /repos/{r}/rulesets` | Checks for existing ruleset by name |
| 2. Tag ruleset | `POST /repos/{r}/rulesets` (with bypass if App) | Checks for existing ruleset by name |
| 3. `skip-changeset` label | `POST /repos/{r}/labels` | Checks for existing label by name |
| 4. Secrets | `gh secret set` | Checks `GET /repos/{r}/actions/secrets/{name}` |
| 5. Immutable releases | Prints UI instructions | Always prints (no API check possible) |

### Examples for all scope × token-type combinations

```bash
# personal × app
./scripts/setup-release.sh --owner myuser --repo myrepo \
  --scope personal --token-type app \
  --app-id 123456 --app-pem ./app.pem

# personal × fine-grained
./scripts/setup-release.sh --owner myuser --repo myrepo \
  --scope personal --token-type fine-grained

# personal × classic
./scripts/setup-release.sh --owner myuser --repo myrepo \
  --scope personal --token-type classic

# org × app
./scripts/setup-release.sh --owner myorg --repo myrepo \
  --scope org --token-type app \
  --app-id 123456 --app-pem ./app.pem

# org × fine-grained
./scripts/setup-release.sh --owner myorg --repo myrepo \
  --scope org --token-type fine-grained

# enterprise (GHEC) × app
./scripts/setup-release.sh --owner myenterprise --repo myrepo \
  --scope enterprise --token-type app \
  --app-id 123456 --app-pem ./app.pem

# ghes × classic (GHES < 3.10)
./scripts/setup-release.sh --owner myorg --repo myrepo \
  --scope ghes --token-type classic \
  --ghes-host github.example.com

# ghes × fine-grained (GHES 3.10+)
./scripts/setup-release.sh --owner myorg --repo myrepo \
  --scope ghes --token-type fine-grained \
  --ghes-host github.example.com

# dry run — inspect without changing anything
./scripts/setup-release.sh --owner myorg --repo myrepo \
  --scope org --token-type app --dry-run
```

---

## Adapting to Another Repository

1. **Copy files:**

   ```
   .changeset/config.json       → update "repo" to "{owner}/{repo}"
   .changeset/README.md
   .github/workflows/release-pr.yml
   .github/workflows/cd.yml     → update MAIN_SOLUTION, SAMPLE_SOLUTION, PACKAGE_PROJECT
   scripts/setup-release.sh
   scripts/setup-release.ps1
   ```

   Plus add the `changeset-check` job and `ci-gate` updates to your existing CI workflow.

2. **Install dependencies:**

   ```bash
   bun add -D @changesets/cli @changesets/changelog-github
   ```

3. **Add scripts to `package.json`:**

   ```json
   {
     "scripts": {
       "changeset": "changeset",
       "version-packages": "changeset version"
     }
   }
   ```

   If you have a version-sync script (like `update-version.ts`), append `&& bun .build/update-version.ts`.

4. **Add Justfile recipes:**

   ```makefile
   changeset:
       bun changeset

   changeset-status:
       bun changeset status
   ```

5. **Configure the repository** (run the DSC script):

   ```bash
   ./scripts/setup-release.sh --owner {owner} --repo {repo} \
     --scope {personal|org|enterprise|ghes} --token-type {app|fine-grained|classic}
   ```

6. **Add secrets** (see Step 1 above for your chosen token type)

7. **Complete manual steps:**
   - Tag ruleset bypass (if using PAT): GitHub UI → add "GitHub Actions" bypass actor
   - Immutable releases: Settings → General → ✅ Immutable releases

8. **Adapt `cd.yml`** for your project type. Key variables:

   ```yaml
   env:
     MAIN_SOLUTION: ./src/MyProject.slnx
     SAMPLE_SOLUTION: ./samples/SampleApp/SampleApp.slnx   # remove if no samples
     PACKAGE_PROJECT: ./src/MyProject/MyProject.csproj
     ARTIFACTS_DIR: ./artifacts/nuget
   ```

   For non-.NET projects, replace the build/test/pack steps with your equivalent commands.

---

## Troubleshooting

### Version PR not triggering CI

The `CHANGESET_TOKEN` secret is missing, expired, or has insufficient permissions. The bot falls back to `GITHUB_TOKEN` which cannot trigger workflow runs.

**Diagnose:** Check the `release-pr.yml` run logs for "Generate GitHub App token" step — if it was skipped (APP_ID not set) and CHANGESET_TOKEN is empty, the PR was created with GITHUB_TOKEN.

**Fix:**

1. Set or rotate the `CHANGESET_TOKEN` secret (or `APP_ID` + `APP_PRIVATE_KEY`)
2. Close and reopen the Version PR, or trigger the workflow manually:

   ```bash
   gh workflow run release-pr.yml --repo {owner}/{repo}
   ```

### CD pipeline skips the release

The release tag already exists. This is intentional idempotency.

```bash
# Check what's there:
gh release view v{version} --repo {owner}/{repo}

# If the existing release is broken (no assets), delete it:
gh release delete v{version} --cleanup-tag --yes --repo {owner}/{repo}

# Re-trigger the CD pipeline:
gh workflow run cd.yml --repo {owner}/{repo}
```

### CD pipeline fails mid-release

The ERR trap should have cleaned up automatically. Verify:

```bash
# Check no draft release was left behind:
gh release list --repo {owner}/{repo}

# Check no orphaned tag was left:
git ls-remote --tags https://github.com/{owner}/{repo} 'refs/tags/v*'
```

If a draft or tag was left, clean up manually:

```bash
gh release delete v{version} --cleanup-tag --yes --repo {owner}/{repo}
```

Then re-trigger:

```bash
gh workflow run cd.yml --repo {owner}/{repo}
```

### Tag push rejected by ruleset

```
remote: error: GH013: Repository rule violations found for refs/tags/v4.3.0
```

This is the tag ruleset working correctly. Tags must be created via the CD pipeline (by merging a Version PR). Do not push tags manually.

### Changeset check fails — no changeset file

```
Error: No changeset file found.
```

Run `just changeset` on your feature branch. If the PR is genuinely docs-only or CI-only, add the `skip-changeset` label.

### CHANGESET_TOKEN expired

The Version PR will be created with `GITHUB_TOKEN` (no CI runs) or the bot will fail entirely.

**Fix:** Rotate the token, update the secret, then re-trigger `release-pr.yml`:

```bash
gh workflow run release-pr.yml --repo {owner}/{repo}
```

### Fine-grained PAT rejected — org approval required

The org requires admin approval for fine-grained PATs. You'll see an error in the workflow logs mentioning "approval" or "policy".

**Fix:** Contact an org admin to approve the PAT at:
`https://github.com/organizations/{org}/settings/personal-access-tokens`

Or switch to Option A (GitHub App), which doesn't require per-token approval.

### GHES: `gh api` returns wrong results

Ensure `GH_HOST` is set:

```bash
export GH_HOST=github.example.com
gh api repos/{owner}/{repo}/rulesets
```

Or pass it inline:

```bash
GH_HOST=github.example.com gh api repos/{owner}/{repo}/rulesets
```
