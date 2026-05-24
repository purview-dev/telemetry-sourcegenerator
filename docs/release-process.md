# Release Process

This document describes the complete release process for this repository. It is designed to be reusable — copy it to any other repository and substitute the org/repo name as needed.

---

## Overview

Releases are **fully automated and gated**. No human ever pushes a version tag or manually creates a GitHub release. The flow is:

```
Feature branch
  → just changeset         (describe your change)
  → PR to main             (changeset-check + ci-gate must pass)
  → merge                  (release bot creates Version PR)
  → Version PR             (ci-gate must pass)
  → merge                  (CD pipeline publishes the release)
```

### Why this is bullet-proof

| Risk | Mitigation |
|------|------------|
| Tag pushed locally before CD runs | Tag ruleset: only `github-actions[bot]` (or your GitHub App) can create `v*` tags |
| Release created without build artifacts | Draft-first: assets attached before `--draft=false` |
| Published release tampered with | Immutable releases enabled: published releases are locked |
| Mid-release failure leaves broken state | ERR trap: deletes draft + tag on any error → clean retry |
| Double-release | CD checks tag existence first; skips if already released |
| PR merged without a changeset | `changeset-check` required status check blocks merging |
| Version PR bypasses CI | `CHANGESET_TOKEN` PAT or GitHub App ensures CI triggers on Version PR |

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
- `patch` — bug fixes, internal changes
- `minor` — new features, backwards-compatible changes
- `major` — breaking changes

Write a one-line summary of the change (this becomes the CHANGELOG entry).

A `.changeset/<random-name>.md` file is created and added to your branch.

### 3. Commit and open a PR

```bash
git add .
git commit -m "feat: my change"
git push origin feat/my-change
# Open a PR to main
```

The `changeset-check` status check will verify that a `.changeset/*.md` file is present. If your PR genuinely needs no release note (docs-only, CI-only), add the `skip-changeset` label.

### 4. Merge the PR

Once `CI Gate` and `Changeset Check` are green and the PR is approved, merge it.

### 5. Version PR is created automatically

The `release-pr.yml` workflow runs and the changeset bot creates (or updates) a **Version Packages** PR:
- Branch: `changeset-release/main`
- Contents: bumped `package.json`, updated `CHANGELOG.md`, deleted `.changeset/*.md` files, synced wiki/docs

Review the Version PR, ensure CI passes, then merge it.

### 6. Release is published automatically

Merging the Version PR triggers `cd.yml`. The pipeline:
1. Reads the new version from `package.json`
2. Checks that no release for this version already exists
3. Runs the full build and test suite (format check, main solution, sample solution)
4. Packs the NuGet artifacts
5. Creates a **draft** GitHub release (with assets attached)
6. Publishes the draft → release becomes immutable
7. If anything fails before publish, the ERR trap deletes the draft + tag for a clean retry

### Checking pending changesets

```bash
just changeset-status
```

---

## Setup Guide

### Prerequisites

- `bun` installed (`npm install -g bun` or `curl -fsSL https://bun.sh/install | bash`)
- `gh` CLI authenticated

### Step 1: Configure the `CHANGESET_TOKEN` secret

The release bot needs a token to create the Version PR. If it uses the default `GITHUB_TOKEN`, GitHub's anti-loop protection prevents CI from running on the bot-created PR — meaning `CI Gate` never passes and the Version PR can never merge.

Choose one of three options:

---

#### Option A: GitHub App (Recommended for organizations and enterprises)

GitHub Apps generate short-lived installation tokens automatically per workflow run. The private key never expires and tokens are refreshed without human intervention. This is the correct choice for teams and CI at scale.

**Setup:**

1. Create the app:
   - **Personal**: [github.com → Settings → Developer settings → GitHub Apps → New GitHub App](https://github.com/settings/apps/new)
   - **Organization**: `https://github.com/organizations/{org}/settings/apps/new`

2. Fill in:
   - **GitHub App name**: something like `{org}-release-bot`
   - **Homepage URL**: your repo URL
   - **Webhook**: uncheck "Active"

3. Set **Repository permissions**:
   | Permission | Level |
   |------------|-------|
   | Contents | Read & write |
   | Pull requests | Read & write |
   | Workflows | Read & write |

4. Click **Create GitHub App**

5. On the app settings page:
   - Note the **App ID**
   - Scroll to **Private keys** → **Generate a private key** → download the `.pem` file

6. Install the app on the repository:
   - App settings → **Install App** → select your org/account → select the specific repository

7. Add secrets to the repository:
   - `APP_ID` — the numeric App ID from step 5
   - `APP_PRIVATE_KEY` — the full contents of the `.pem` file

   ```bash
   gh secret set APP_ID --body "123456"
   gh secret set APP_PRIVATE_KEY < /path/to/your-app.private-key.pem
   ```

8. **For GHES**: Same steps at `https://{hostname}/settings/apps`. GitHub Apps are fully supported on GHES.

9. **For GHEC**: Same as org on github.com. The app must be installed on the org.

**The `release-pr.yml` workflow automatically detects `APP_ID` and uses the app token when present.** No other workflow changes needed.

**Tag ruleset bypass**: Add this app's installation as a bypass actor for the `v*` tag ruleset (see Step 3 below). You'll need the installation ID:
```bash
# After installing the app, get the installation ID:
gh api /repos/{owner}/{repo}/installation --jq '.id'
# This requires the app's JWT token — use your app's private key to authenticate
# Alternatively, find the installation ID in: App settings → Installations → click your org → check the URL
```

---

#### Option B: Fine-Grained Personal Access Token (Personal repos / small teams)

Fine-grained PATs are scoped to a specific repository with exact permissions. They **must** have an expiry (max 1 year). Set a calendar reminder to rotate before expiry — the release bot will stop working silently if it expires.

**Required permissions:**
| Permission | Level | Reason |
|------------|-------|--------|
| Contents | Read & write | Push commits (version bump, CHANGELOG) |
| Pull requests | Read & write | Create / update the Version PR |
| Workflows | Read & write | If changeset PRs ever touch `.github/workflows/` |
| Metadata | Read | Always required (automatic) |

**Personal account:**
1. Go to **Settings → Developer settings → Personal access tokens → Fine-grained tokens → Generate new token**
2. **Resource owner**: your account
3. **Repository access**: "Only select repositories" → pick this repo
4. Set permissions as above; set expiry (max 1 year)
5. Copy the token:
   ```bash
   gh secret set CHANGESET_TOKEN
   # paste when prompted
   ```

**Organization-owned repo:**
- The org admin must enable fine-grained PATs: **Org Settings → Personal access tokens → Allow fine-grained personal access tokens**
- If the org requires admin approval, submit the request and wait before testing
- Set **Resource owner** to the **org** (not your personal account)
- SAML SSO: fine-grained PATs do **not** require separate SSO authorization
- The token owner must have write access to the repo

**GitHub Enterprise Server (GHES 3.10+):**
- Navigate to `https://{hostname}/settings/tokens → Fine-grained tokens`
- Org admin must allow fine-grained PATs in org settings
- Same permissions apply
- On GHES < 3.10, use Option C

**GitHub Enterprise Cloud (GHEC):**
- Same as personal/org on github.com
- Org must opt-in: **Org Settings → Personal access tokens policy → Allow fine-grained personal access tokens**
- If org requires admin approval, wait for approval before testing

---

#### Option C: Classic PAT (Legacy / fallback only)

Classic PATs grant broad `repo` scope across **all repositories** the token owner has access to. Use only when fine-grained PATs are unavailable (GHES < 3.10) or blocked by org policy.

1. **Settings → Developer settings → Personal access tokens → Tokens (classic) → Generate new token**
2. Scope: `repo`
3. Set an expiry (recommended)
4. For org repos with SAML SSO: after creating the token, click **"Authorize"** next to the org name
5. ```bash
   gh secret set CHANGESET_TOKEN
   ```

---

#### Option summary

| Scenario | Recommendation |
|----------|----------------|
| Organization or enterprise | Option A (GitHub App) |
| Personal repo, small team | Option B (fine-grained PAT) |
| GHES < 3.10 | Option C (classic PAT) |
| Org blocks fine-grained PATs | Option A or Option C |

---

### Step 2: Configure GitHub Branch Ruleset for `main`

The `main` branch ruleset requires PRs and ensures CI passes before merge. This was configured programmatically but can also be set via the UI:

**Settings → Rules → Rulesets → New ruleset → New branch ruleset**

| Setting | Value |
|---------|-------|
| Ruleset name | `Protect main branch` |
| Target branches | `refs/heads/main` |
| Rules | ✅ Restrict deletions |
| | ✅ Block force pushes |
| | ✅ Require a pull request before merging (0 approvals) |
| | ✅ Require status checks to pass → Add `CI Gate` |

**Via CLI** (already applied for this repo):
```bash
gh api repos/{owner}/{repo}/rulesets \
  --method POST \
  --header "Content-Type: application/json" \
  --input - <<'EOF'
{
  "name": "Protect main branch",
  "target": "branch",
  "enforcement": "active",
  "conditions": { "ref_name": { "include": ["refs/heads/main"], "exclude": [] } },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "pull_request", "parameters": { "required_approving_review_count": 0 } },
    { "type": "required_status_checks", "parameters": {
        "strict_required_status_checks_policy": false,
        "required_status_checks": [{ "context": "CI Gate" }]
      }
    }
  ]
}
EOF
```

---

### Step 3: Configure GitHub Tag Ruleset for `v*`

This is the most important security guard. It prevents **anyone** from pushing a `v*` tag manually — tags can only be created by `github-actions[bot]` (or your GitHub App if using Option A).

> **⚠️ This step must be done via the GitHub UI** — the API requires `admin:org` scope or GitHub App JWT authentication to specify `github-actions[bot]` as the bypass actor.

**Settings → Rules → Rulesets → New ruleset → New tag ruleset**

| Setting | Value |
|---------|-------|
| Ruleset name | `Protect release tags` |
| Target tags | `refs/tags/v*` |
| Rules | ✅ Restrict creations |
| Bypass list | Add → search **"GitHub Actions"** → select it |

If using **Option A (GitHub App)** for the bypass instead:
1. After installing your GitHub App, get its installation ID (see Option A setup above)
2. Add via the CLI:
   ```bash
   # Replace INSTALLATION_ID with your app's installation ID
   gh api repos/{owner}/{repo}/rulesets \
     --method POST \
     --header "Content-Type: application/json" \
     --input - <<'EOF'
   {
     "name": "Protect release tags",
     "target": "tag",
     "enforcement": "active",
     "conditions": { "ref_name": { "include": ["refs/tags/v*"], "exclude": [] } },
     "rules": [{ "type": "creation" }],
     "bypass_actors": [{
       "actor_id": INSTALLATION_ID,
       "actor_type": "Integration",
       "bypass_mode": "always"
     }]
   }
   EOF
   ```

---

### Step 4: Enable Immutable Releases

Immutable releases prevent published releases from being modified or having assets replaced. With the draft-first release flow, this is safe and desirable:

- **Drafts are never immutable** → the CD pipeline can create, upload to, and delete drafts freely
- **Publishing locks the release** → security guarantee that what was released cannot be tampered with
- **ERR cleanup trap fires before publish** → failures always clean up a mutable draft, never a locked release

**To enable:**

**Settings → General → Releases → ✅ Immutable releases**

There is no API endpoint for this setting; it must be enabled via the UI.

---

### Step 5: Verify the setup

1. Create a test branch, run `just changeset`, commit and open a PR to `main`
2. Verify `Changeset Check` and `CI Gate` appear as required status checks
3. Merge the PR; verify a `changeset-release/main` PR appears within minutes
4. Verify CI runs on the Version PR (`CI Gate` passes)
5. Merge the Version PR; verify the CD pipeline runs and publishes a GitHub release with NuGet assets attached

---

## Troubleshooting

### Version PR not triggering CI

The `CHANGESET_TOKEN` secret is missing, expired, or doesn't have sufficient permissions. The bot created the PR with `GITHUB_TOKEN` which cannot trigger CI.

**Fix**: Verify the `CHANGESET_TOKEN` secret is set and valid. For fine-grained PATs, check the expiry date. For GitHub App, check the private key is correct and the app is installed on the repo.

### CD pipeline skips release

The release already exists for this version. Check with:
```bash
gh release view v{version}
```

If the release exists but is broken (no assets), delete it and manually trigger the CD:
```bash
gh release delete v{version} --cleanup-tag --yes
gh workflow run cd.yml
```

### CD pipeline fails mid-release

The ERR trap should have cleaned up automatically. Verify:
```bash
gh release list  # should show no draft for this version
git ls-remote --tags origin 'refs/tags/v*'  # should show no tag for this version
```

If a draft or tag was left behind, clean up manually:
```bash
gh release delete v{version} --cleanup-tag --yes
```

Then trigger the CD workflow again:
```bash
gh workflow run cd.yml
```

### Tag push blocked by ruleset

If someone tries `git push --tags` and gets blocked:
```
remote: error: GH013: Repository rule violations found for refs/tags/v4.3.0
```

This is the tag ruleset working correctly. Tags must be created via the CD pipeline (by merging a Version PR).

### Changeset check fails on a docs-only PR

Add the `skip-changeset` label to the PR. The `changeset-check` job will auto-pass.

### CHANGESET_TOKEN expires

The Version PR will be created with `GITHUB_TOKEN` fallback (if `CHANGESET_TOKEN` is unset or empty), but CI won't run on it. PRs created by GITHUB_TOKEN don't trigger workflows.

**Fix**: Rotate the token, update the `CHANGESET_TOKEN` secret, then manually trigger `release-pr.yml`:
```bash
gh workflow run release-pr.yml
```

---

## Adapting this to another repository

1. Copy `.changeset/config.json` — change `"repo"` to `"{owner}/{repo}"`
2. Copy `.github/workflows/release-pr.yml` — no changes needed
3. Copy `.github/workflows/cd.yml` — update `MAIN_SOLUTION`, `SAMPLE_SOLUTION`, `PACKAGE_PROJECT`, and the pack command for your project type
4. Copy the `changeset-check` job and updated `ci-gate` from `.github/workflows/ci.yml`
5. Add `CHANGESET_TOKEN` (or `APP_ID`/`APP_PRIVATE_KEY`) as repo secrets (see Step 1 above)
6. Create branch and tag rulesets (Steps 2–3 above)
7. Enable immutable releases (Step 4 above)
8. Replace `bun` with `npm` or `yarn` if preferred — update `package.json` scripts and workflow `run:` steps accordingly
