<#
.SYNOPSIS
    Desired-State Configuration for the changeset release flow.

.DESCRIPTION
    Idempotent. Safe to re-run: existing rulesets and labels are checked first.
    Configures the repository to use the @changesets/cli release flow with
    draft-first GitHub Releases that are compatible with immutable releases.

    Handles all account scopes (personal, org, enterprise, GHES) and all token
    types (GitHub App, fine-grained PAT, classic PAT).

.PARAMETER Owner
    GitHub owner — user name or organisation name. Required.

.PARAMETER Repo
    Repository name. Required.

.PARAMETER Scope
    Account scope. One of: personal, org, enterprise, ghes.
    Default: personal

.PARAMETER TokenType
    How the release bot authenticates. One of: app, fine-grained, classic.
    Default: app

.PARAMETER GhesHost
    Hostname for GitHub Enterprise Server (e.g. github.example.com).
    Required when -Scope ghes.

.PARAMETER AppId
    GitHub App ID. Used with -TokenType app to set the APP_ID secret
    and retrieve the installation ID for the tag ruleset bypass.

.PARAMETER AppPem
    Path to the GitHub App private-key .pem file.
    Used with -TokenType app to set APP_PRIVATE_KEY and generate a JWT
    to retrieve the app installation ID.

.PARAMETER DryRun
    Print what would be done without making changes.

.EXAMPLE
    # Personal repo, fine-grained PAT
    .\scripts\setup-release.ps1 -Owner myuser -Repo myrepo `
        -Scope personal -TokenType fine-grained

.EXAMPLE
    # Organisation repo, GitHub App (fully automated tag ruleset bypass)
    .\scripts\setup-release.ps1 -Owner myorg -Repo myrepo `
        -Scope org -TokenType app `
        -AppId 123456 -AppPem .\my-app.private-key.pem

.EXAMPLE
    # GitHub Enterprise Server, classic PAT
    .\scripts\setup-release.ps1 -Owner myorg -Repo myrepo `
        -Scope ghes -TokenType classic `
        -GhesHost github.example.com

.EXAMPLE
    # Dry run — see what would happen without making changes
    .\scripts\setup-release.ps1 -Owner myorg -Repo myrepo -DryRun

.NOTES
    Requires the GitHub CLI (gh) to be installed and authenticated.
    For GitHub App tag-ruleset bypass: the app must be installed on the
    repository before running this script.
#>

[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter(Mandatory)]
    [string]$Owner,

    [Parameter(Mandatory)]
    [string]$Repo,

    [ValidateSet('personal', 'org', 'enterprise', 'ghes')]
    [string]$Scope = 'personal',

    [ValidateSet('app', 'fine-grained', 'classic')]
    [string]$TokenType = 'app',

    [string]$GhesHost = '',

    [string]$AppId = '',

    [string]$AppPem = '',

    [string]$BaseBranch = 'main',

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ─── helpers ──────────────────────────────────────────────────────────────────
function Write-Step   ([string]$msg) { Write-Host "`n▶ $msg" -ForegroundColor Cyan  }
function Write-OK     ([string]$msg) { Write-Host "  ✅ $msg" -ForegroundColor Green  }
function Write-Info   ([string]$msg) { Write-Host "  ℹ  $msg" -ForegroundColor White  }
function Write-Warn   ([string]$msg) { Write-Host "  ⚠  $msg" -ForegroundColor Yellow }
function Write-Dry    ([string]$msg) { Write-Host "  [dry-run] $msg" -ForegroundColor Yellow }
function Write-Err    ([string]$msg) { Write-Host "  ✖  $msg" -ForegroundColor Red; throw $msg }

function Invoke-GhApi {
    param([string]$Path, [string]$Method = 'GET', [string]$Body = '', [switch]$Silent)
    $args = @("api", $Path)
    if ($Method -ne 'GET') { $args += @('--method', $Method) }
    if ($Body)             { $args += @('--input', '-') }
    if ($DryRun) {
        Write-Dry "gh $($args -join ' ')$(if($Body){ ' <body>' })"
        return $null
    }
    try {
        if ($Body) {
            $result = $Body | gh @args 2>&1
        } else {
            $result = gh @args 2>&1
        }
        if ($LASTEXITCODE -ne 0) {
            if (-not $Silent) { throw "gh api $Path failed: $result" }
            return $null
        }
        return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
    } catch {
        if (-not $Silent) { throw }
        return $null
    }
}

function Test-RulesetExists ([string]$Name) {
    $rulesets = Invoke-GhApi "repos/$FullRepo/rulesets" -Silent
    if ($null -eq $rulesets) { return $false }
    return ($rulesets | Where-Object { $_.name -eq $Name }).Count -gt 0
}

function Test-LabelExists ([string]$Name) {
    $labels = Invoke-GhApi "repos/$FullRepo/labels" -Silent
    if ($null -eq $labels) { return $false }
    return ($labels | Where-Object { $_.name -eq $Name }).Count -gt 0
}

function Test-SecretExists ([string]$Name) {
    $secret = Invoke-GhApi "repos/$FullRepo/actions/secrets/$Name" -Silent
    return $null -ne $secret
}

function Show-TagRulesetUiInstructions {
    Write-Host ""
    Write-Host "  Manual step required — Tag Ruleset:" -ForegroundColor White
    Write-Host "  1. Navigate to: https://github.com/$FullRepo/settings/rules/new?target=tag"
    Write-Host "  2. Ruleset name:  Protect release tags"
    Write-Host "  3. Target tags:   refs/tags/v*"
    Write-Host "  4. Rules:         ✅ Restrict creations"
    Write-Host "  5. Bypass actors: Add → search 'GitHub Actions' → select → Save"
    Write-Host ""
}

# ─── environment setup ────────────────────────────────────────────────────────
if ($Scope -eq 'ghes') {
    if (-not $GhesHost) { Write-Err "-GhesHost is required when -Scope ghes" }
    $env:GH_HOST = $GhesHost
    Write-Info "Targeting GHES host: $GhesHost"
}

if ($Scope -eq 'ghes' -and $TokenType -eq 'fine-grained') {
    # GHES < 3.10 does not support fine-grained PATs
    Write-Warn "Fine-grained PATs require GHES 3.10+. If your server is older, use -TokenType classic."
}

$FullRepo = "$Owner/$Repo"

# ─── header ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Changeset Release Flow — Desired State Configuration" -ForegroundColor Cyan -NoNewline
if ($DryRun) { Write-Host " [DRY RUN]" -ForegroundColor Yellow } else { Write-Host "" }
Write-Host "  Repo:       $FullRepo"
Write-Host "  Scope:      $Scope"
Write-Host "  Token type: $TokenType"
Write-Host ""

# ─── Step 0: .changeset/config.json ──────────────────────────────────────────
Write-Step "Step 0/5 — .changeset/config.json"

$ChangesetConfig = '.changeset/config.json'
if (Test-Path $ChangesetConfig) {
    $cfg = Get-Content $ChangesetConfig -Raw | ConvertFrom-Json
    $currentRepo = ''
    if ($cfg.changelog -is [array] -and $cfg.changelog.Count -gt 1 -and $cfg.changelog[1] -is [psobject]) {
        $currentRepo = $cfg.changelog[1].repo
    }
    if ($currentRepo -eq $FullRepo -and $cfg.baseBranch -eq $BaseBranch) {
        Write-OK ".changeset/config.json already correct — skipping."
    } else {
        Write-Info "Patching .changeset/config.json: repo → $FullRepo, baseBranch → $BaseBranch"
        if ($DryRun) {
            Write-Dry "Update $ChangesetConfig: changelog[1].repo=$FullRepo, baseBranch=$BaseBranch"
        } else {
            if ($cfg.changelog -is [array] -and $cfg.changelog.Count -gt 1) {
                $cfg.changelog[1] | Add-Member -MemberType NoteProperty -Name 'repo' -Value $FullRepo -Force
            }
            $cfg | Add-Member -MemberType NoteProperty -Name 'baseBranch' -Value $BaseBranch -Force
            $cfg | ConvertTo-Json -Depth 10 | Set-Content $ChangesetConfig -Encoding UTF8
            Write-OK ".changeset/config.json updated."
        }
    }
} else {
    Write-Warn ".changeset/config.json not found — run 'npx changeset init' first, then re-run this script."
}

# ─── Step 1: branch ruleset ───────────────────────────────────────────────────
Write-Step "Step 1/5 — Branch ruleset (protect main)"

$BranchRulesetName = 'Protect main branch'
if (Test-RulesetExists $BranchRulesetName) {
    Write-OK "Branch ruleset '$BranchRulesetName' already exists — skipping."
} else {
    Write-Info "Creating branch ruleset: $BranchRulesetName ..."

    $BranchRulesetBody = @{
        name        = $BranchRulesetName
        target      = 'branch'
        enforcement = 'active'
        conditions  = @{
            ref_name = @{ include = @('refs/heads/main'); exclude = @() }
        }
        rules       = @(
            @{ type = 'deletion' }
            @{ type = 'non_fast_forward' }
            @{
                type       = 'pull_request'
                parameters = @{
                    required_approving_review_count = 0
                    dismiss_stale_reviews_on_push   = $false
                    require_code_owner_review       = $false
                    require_last_push_approval      = $false
                    required_review_thread_resolution = $false
                }
            }
            @{
                type       = 'required_status_checks'
                parameters = @{
                    strict_required_status_checks_policy = $false
                    required_status_checks               = @(
                        @{ context = 'CI Gate' }
                    )
                }
            }
        )
    } | ConvertTo-Json -Depth 10

    if (-not $DryRun) {
        Invoke-GhApi "repos/$FullRepo/rulesets" -Method POST -Body $BranchRulesetBody | Out-Null
    } else {
        Write-Dry "POST repos/$FullRepo/rulesets (branch ruleset)"
    }
    Write-OK "Branch ruleset created."
}

# ─── Step 2: tag ruleset ──────────────────────────────────────────────────────
Write-Step "Step 2/5 — Tag ruleset (protect v* tags)"

$TagRulesetName = 'Protect release tags'
if (Test-RulesetExists $TagRulesetName) {
    Write-OK "Tag ruleset '$TagRulesetName' already exists — skipping."
} else {
    Write-Info "Configuring tag ruleset for v* ..."

    if ($TokenType -eq 'app' -and $AppId) {
        # Use App ID directly as the Integration bypass actor.
        # The actor_id for an Integration (GitHub App) bypass is the App's numeric ID,
        # NOT the installation ID. The App must be installed on the repo/org first.
        Write-Info "GitHub App detected — creating tag ruleset with App ID $AppId as bypass actor ..."

        $TagRulesetBody = @{
            name        = $TagRulesetName
            target      = 'tag'
            enforcement = 'active'
            conditions  = @{
                ref_name = @{ include = @('refs/tags/v*'); exclude = @() }
            }
            rules        = @(
                @{ type = 'creation' }
                @{ type = 'deletion' }
                @{ type = 'non_fast_forward' }
                @{ type = 'update' }
            )
            bypass_actors = @(
                @{
                    actor_id    = [int]$AppId
                    actor_type  = 'Integration'
                    bypass_mode = 'always'
                }
            )
        } | ConvertTo-Json -Depth 10

        $created = $false
        if (-not $DryRun) {
            try {
                Invoke-GhApi "repos/$FullRepo/rulesets" -Method POST -Body $TagRulesetBody | Out-Null
                $created = $true
            } catch {
                Write-Warn "Tag ruleset API creation failed — ensure the App is installed on this repo/org."
            }
        } else {
            Write-Dry "POST repos/$FullRepo/rulesets (tag ruleset, App bypass actor_id=$AppId)"
            $created = $true
        }

        if ($created) {
            Write-OK "Tag ruleset created with GitHub App bypass (App ID $AppId)."
        } else {
            Show-TagRulesetUiInstructions
        }
    } else {
        Write-Warn "Tag ruleset bypass for github-actions[bot] requires the GitHub UI."
        Show-TagRulesetUiInstructions
    }
}

# ─── Step 3: skip-changeset label ─────────────────────────────────────────────
Write-Step "Step 3/5 — GitHub label: skip-changeset"

if (Test-LabelExists 'skip-changeset') {
    Write-OK "Label 'skip-changeset' already exists — skipping."
} else {
    Write-Info "Creating label 'skip-changeset' ..."
    if (-not $DryRun) {
        $labelBody = @{
            name        = 'skip-changeset'
            color       = 'e4e669'
            description = 'Opt this PR out of the changeset requirement (docs-only, CI-only, etc.)'
        } | ConvertTo-Json
        Invoke-GhApi "repos/$FullRepo/labels" -Method POST -Body $labelBody | Out-Null
    } else {
        Write-Dry "POST repos/$FullRepo/labels { name: skip-changeset, color: e4e669 }"
    }
    Write-OK "Label 'skip-changeset' created."
}

# ─── Step 4: secrets guidance ─────────────────────────────────────────────────
Write-Step "Step 4/5 — Repository secrets"

switch ($TokenType) {
    'app' {
        if (-not (Test-SecretExists 'APP_ID')) {
            Write-Warn "Secret 'APP_ID' not set."
            if ($AppId) {
                if ($DryRun) {
                    Write-Dry "gh secret set APP_ID --repo $FullRepo --body $AppId"
                } else {
                    gh secret set APP_ID --repo $FullRepo --body $AppId
                    Write-OK "APP_ID secret set."
                }
            } else {
                Write-Host "  Run:  gh secret set APP_ID --repo $FullRepo"
            }
        } else {
            Write-OK "Secret 'APP_ID' already set."
        }

        if (-not (Test-SecretExists 'APP_PRIVATE_KEY')) {
            Write-Warn "Secret 'APP_PRIVATE_KEY' not set."
            if ($AppPem -and (Test-Path $AppPem)) {
                if ($DryRun) {
                    Write-Dry "gh secret set APP_PRIVATE_KEY --repo $FullRepo < $AppPem"
                } else {
                    Get-Content $AppPem -Raw | gh secret set APP_PRIVATE_KEY --repo $FullRepo
                    Write-OK "APP_PRIVATE_KEY secret set."
                }
            } else {
                Write-Host "  Run:  Get-Content .\app.private-key.pem -Raw | gh secret set APP_PRIVATE_KEY --repo $FullRepo"
            }
        } else {
            Write-OK "Secret 'APP_PRIVATE_KEY' already set."
        }
    }

    { $_ -in 'fine-grained', 'classic' } {
        if (-not (Test-SecretExists 'CHANGESET_TOKEN')) {
            Write-Warn "Secret 'CHANGESET_TOKEN' not set."
            Write-Host ""

            switch ($TokenType) {
                'fine-grained' {
                    switch ($Scope) {
                        'personal' {
                            Write-Host "  Create a fine-grained PAT at:"
                            Write-Host "    https://github.com/settings/personal-access-tokens/new"
                            Write-Host "  Resource owner:    $Owner"
                            Write-Host "  Repository access: Only select → $Repo"
                        }
                        { $_ -in 'org', 'enterprise' } {
                            Write-Host "  Ensure your org allows fine-grained PATs:"
                            Write-Host "    https://github.com/organizations/$Owner/settings/personal-access-tokens"
                            Write-Host "  Create a fine-grained PAT at:"
                            Write-Host "    https://github.com/settings/personal-access-tokens/new"
                            Write-Host "  Resource owner:    $Owner  (the org, not your personal account)"
                            Write-Host "  Repository access: Only select → $Repo"
                        }
                        'ghes' {
                            Write-Host "  Requires GHES 3.10+. Create at:"
                            Write-Host "    https://$GhesHost/settings/personal-access-tokens/new"
                            Write-Host "  Ensure org allows fine-grained PATs:"
                            Write-Host "    https://$GhesHost/organizations/$Owner/settings/personal-access-tokens"
                        }
                    }
                    Write-Host "  Required permissions: Contents (r/w), Pull requests (r/w), Workflows (r/w)"
                }

                'classic' {
                    switch ($Scope) {
                        'personal' {
                            Write-Host "  Create a classic PAT at:"
                            Write-Host "    https://github.com/settings/tokens/new"
                        }
                        { $_ -in 'org', 'enterprise' } {
                            Write-Host "  Create a classic PAT at:"
                            Write-Host "    https://github.com/settings/tokens/new"
                            Write-Host "  Scope: repo"
                            Write-Host "  SAML SSO: After creating, click 'Authorize' for org $Owner"
                        }
                        'ghes' {
                            Write-Host "  Create a classic PAT at:"
                            Write-Host "    https://$GhesHost/settings/tokens/new"
                        }
                    }
                    Write-Host "  Scope: repo  (includes contents, pull requests, workflows)"
                }
            }

            Write-Host ""
            Write-Host "  Then run:  gh secret set CHANGESET_TOKEN --repo $FullRepo"
        } else {
            Write-OK "Secret 'CHANGESET_TOKEN' already set."
        }
    }
}

# ─── Step 5: immutable releases ───────────────────────────────────────────────
Write-Host ""
Write-Host "Immutable releases (manual step)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Enable at: https://github.com/$FullRepo/settings"
Write-Host "  Under 'Releases' → check 'Immutable releases'."
Write-Host ""
Write-Host "  The CD pipeline uses a draft-first approach so this is always safe:"
Write-Host "   • Drafts are never immutable — assets attach freely"
Write-Host "   • Publishing marks the release immutable (security guarantee)"
Write-Host "   • The ERR cleanup trap fires before publish, so failures"
Write-Host "     always clean up a mutable draft, never a locked release"
Write-Host ""

# ─── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Setup complete for $FullRepo"          -ForegroundColor Green
Write-Host "════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "  Verify the setup:"
Write-Host "  1. Create a branch, run 'just changeset', commit and open a PR"
Write-Host "  2. Confirm 'Changeset Check' and 'CI Gate' appear as required checks"
Write-Host "  3. Merge the PR; verify a changeset-release/main PR appears"
Write-Host "  4. Verify CI runs on the Version PR"
Write-Host "  5. Merge the Version PR; verify a GitHub Release is published"
Write-Host ""
