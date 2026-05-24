#!/usr/bin/env bash
# =============================================================================
# setup-release.sh — Desired-State Configuration for the changeset release flow
#
# Idempotent. Safe to re-run: existing rulesets and labels are checked first.
#
# USAGE
#   ./scripts/setup-release.sh [OPTIONS]
#
# OPTIONS
#   --owner          GitHub owner (user or org). Required.
#   --repo           Repository name. Required.
#   --scope          Account scope: personal | org | enterprise | ghes
#                    Default: personal
#   --token-type     How the release bot authenticates: app | fine-grained | classic
#                    Default: app
#   --ghes-host      Hostname for GHES. Required when --scope ghes.
#                    Example: github.example.com
#   --app-id         GitHub App ID (only needed with --token-type app)
#   --app-pem        Path to GitHub App private key .pem file
#                    (only needed with --token-type app, used for ruleset bypass)
#   --dry-run        Print what would be done without making changes.
#   -h, --help       Show this help message.
#
# EXAMPLES
#   # Personal repo, fine-grained PAT
#   ./scripts/setup-release.sh --owner myuser --repo myrepo \
#     --scope personal --token-type fine-grained
#
#   # Organisation repo, GitHub App (fully automated tag ruleset bypass)
#   ./scripts/setup-release.sh --owner myorg --repo myrepo \
#     --scope org --token-type app \
#     --app-id 123456 --app-pem ./my-app.private-key.pem
#
#   # GitHub Enterprise Server, classic PAT
#   ./scripts/setup-release.sh --owner myorg --repo myrepo \
#     --scope ghes --token-type classic \
#     --ghes-host github.example.com
#
# REQUIREMENTS
#   - gh CLI authenticated (gh auth login)
#   - For --token-type app + tag ruleset: the app must be installed on the repo
#     before this script runs.
# =============================================================================
set -euo pipefail

# ─── defaults ────────────────────────────────────────────────────────────────
OWNER=""
REPO=""
SCOPE="personal"
TOKEN_TYPE="app"
GHES_HOST=""
APP_ID=""
APP_PEM=""
DRY_RUN=false

# ─── colours ─────────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'
CYAN='\033[0;36m'; BOLD='\033[1m'; RESET='\033[0m'
info()    { echo -e "${CYAN}  ℹ ${RESET}$*"; }
success() { echo -e "${GREEN}  ✅ ${RESET}$*"; }
warning() { echo -e "${YELLOW}  ⚠ ${RESET}$*"; }
error()   { echo -e "${RED}  ✖ ${RESET}$*" >&2; }
header()  { echo -e "\n${BOLD}${CYAN}▶ $*${RESET}"; }
dry()     { echo -e "${YELLOW}  [dry-run]${RESET} $*"; }

# ─── helper functions ─────────────────────────────────────────────────────────
gh_api() {
  if [[ "${DRY_RUN}" == true ]]; then
    dry "gh api $*"
    return 0
  fi
  gh api "$@"
}

ruleset_exists() {
  local name="$1"
  gh api "repos/${FULL_REPO}/rulesets" --jq ".[] | select(.name == \"${name}\") | .id" 2>/dev/null | grep -q .
}

label_exists() {
  local name="$1"
  gh api "repos/${FULL_REPO}/labels" --jq ".[].name" 2>/dev/null | grep -qxF "${name}"
}

print_help() {
  awk '
    /^# =============================================================================$/ {
      delimiter_count++
      if (delimiter_count == 1) {
        next
      }
      if (delimiter_count == 2) {
        exit
      }
    }
    delimiter_count >= 1 {
      sub(/^# ?/, "")
      print
    }
  ' "$0"
}

_tag_ruleset_ui_instructions() {
  echo ""
  echo -e "  ${BOLD}Manual step required — Tag Ruleset:${RESET}"
  echo -e "  1. Navigate to: https://github.com/${FULL_REPO}/settings/rules/new?target=tag"
  echo -e "  2. Name:   Protect release tags"
  echo -e "  3. Target: refs/tags/v*"
  echo -e "  4. Rules:  ✅ Restrict creations"
  echo -e "  5. Bypass: Add → search 'GitHub Actions' → select it → Save"
  echo ""
}

# ─── argument parsing ─────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --owner)         OWNER="$2";     shift 2 ;;
    --repo)          REPO="$2";      shift 2 ;;
    --scope)         SCOPE="$2";     shift 2 ;;
    --token-type)    TOKEN_TYPE="$2";shift 2 ;;
    --ghes-host)     GHES_HOST="$2"; shift 2 ;;
    --app-id)        APP_ID="$2";    shift 2 ;;
    --app-pem)       APP_PEM="$2";   shift 2 ;;
    --dry-run)       DRY_RUN=true;   shift   ;;
    -h|--help)
      print_help
      exit 0
      ;;
    *) error "Unknown argument: $1"; exit 1 ;;
  esac
done

# ─── validation ───────────────────────────────────────────────────────────────
[[ -z "${OWNER}" ]] && { error "--owner is required"; exit 1; }
[[ -z "${REPO}" ]]  && { error "--repo is required";  exit 1; }

case "${SCOPE}" in personal|org|enterprise|ghes) ;; *)
  error "--scope must be one of: personal, org, enterprise, ghes"; exit 1 ;;
esac

case "${TOKEN_TYPE}" in app|fine-grained|classic) ;; *)
  error "--token-type must be one of: app, fine-grained, classic"; exit 1 ;;
esac

if [[ "${SCOPE}" == "ghes" && -z "${GHES_HOST}" ]]; then
  error "--ghes-host is required when --scope ghes"; exit 1
fi

# ─── gh CLI setup ─────────────────────────────────────────────────────────────
if [[ "${SCOPE}" == "ghes" ]]; then
  export GH_HOST="${GHES_HOST}"
  API_BASE="https://${GHES_HOST}/api/v3"
  info "Targeting GHES host: ${GHES_HOST}"
else
  API_BASE="https://api.github.com"
fi

FULL_REPO="${OWNER}/${REPO}"

# ─── main setup ───────────────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}Changeset Release Flow — Desired State Configuration${RESET}"
echo -e "  Repo:       ${FULL_REPO}"
echo -e "  Scope:      ${SCOPE}"
echo -e "  Token type: ${TOKEN_TYPE}"
[[ "${DRY_RUN}" == true ]] && echo -e "  ${YELLOW}DRY RUN — no changes will be made${RESET}"
echo ""

# ── Step 1: branch ruleset ────────────────────────────────────────────────────
header "Step 1/4 — Branch ruleset (protect main)"

BRANCH_RULESET_NAME="Protect main branch"

if ruleset_exists "${BRANCH_RULESET_NAME}"; then
  success "Branch ruleset '${BRANCH_RULESET_NAME}' already exists — skipping."
else
  info "Creating branch ruleset: ${BRANCH_RULESET_NAME}..."

  BRANCH_RULESET_JSON=$(cat <<'ENDJSON'
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
ENDJSON
)

  if [[ "${DRY_RUN}" == true ]]; then
    dry "POST repos/${FULL_REPO}/rulesets  (branch ruleset payload above)"
  else
    echo "${BRANCH_RULESET_JSON}" | gh api "repos/${FULL_REPO}/rulesets" \
      --method POST --header "Content-Type: application/json" --input - > /dev/null
    success "Branch ruleset created."
  fi
fi

# ── Step 2: tag ruleset ───────────────────────────────────────────────────────
header "Step 2/4 — Tag ruleset (protect v* tags)"

TAG_RULESET_NAME="Protect release tags"

if ruleset_exists "${TAG_RULESET_NAME}"; then
  success "Tag ruleset '${TAG_RULESET_NAME}' already exists — skipping."
else
  info "Configuring tag ruleset for v* ..."

  if [[ "${TOKEN_TYPE}" == "app" && -n "${APP_ID}" ]]; then
    # ── Option A: GitHub App — use App ID directly as the Integration bypass actor ──
    # The bypass actor actor_id for an Integration (GitHub App) is the App's numeric ID,
    # NOT the installation ID. The App must be installed on the repo (or org) first.
    info "GitHub App detected — creating tag ruleset with App ID ${APP_ID} as bypass actor..."

    TAG_RULESET_JSON=$(cat <<ENDJSON
{
  "name": "Protect release tags",
  "target": "tag",
  "enforcement": "active",
  "conditions": {
    "ref_name": { "include": ["refs/tags/v*"], "exclude": [] }
  },
  "rules": [{ "type": "creation" }, { "type": "deletion" }, { "type": "non_fast_forward" }, { "type": "update" }],
  "bypass_actors": [{
    "actor_id": ${APP_ID},
    "actor_type": "Integration",
    "bypass_mode": "always"
  }]
}
ENDJSON
)

    if [[ "${DRY_RUN}" == true ]]; then
      dry "POST repos/${FULL_REPO}/rulesets  (tag ruleset with App bypass, actor_id=${APP_ID})"
    else
      if echo "${TAG_RULESET_JSON}" | gh api "repos/${FULL_REPO}/rulesets" \
          --method POST --header "Content-Type: application/json" --input - > /dev/null 2>&1; then
        success "Tag ruleset created with GitHub App bypass (App ID ${APP_ID})."
      else
        warning "Tag ruleset creation via API failed — ensure the App is installed on this repo/org."
        _tag_ruleset_ui_instructions
      fi
    fi

  else
    # ── Options B & C: PAT — github-actions[bot] bypass requires UI ──────────
    warning "Tag ruleset bypass for github-actions[bot] requires the GitHub UI."
    _tag_ruleset_ui_instructions
  fi
fi

# ── Step 3: skip-changeset label ──────────────────────────────────────────────
header "Step 3/4 — GitHub label: skip-changeset"

if label_exists "skip-changeset"; then
  success "Label 'skip-changeset' already exists — skipping."
else
  info "Creating label 'skip-changeset'..."

  if [[ "${DRY_RUN}" == true ]]; then
    dry "POST repos/${FULL_REPO}/labels  { name: skip-changeset, color: e4e669, description: ... }"
  else
    gh api "repos/${FULL_REPO}/labels" \
      --method POST \
      --header "Content-Type: application/json" \
      --field name="skip-changeset" \
      --field color="e4e669" \
      --field description="Opt this PR out of the changeset requirement (docs-only, CI-only, etc.)" > /dev/null
    success "Label 'skip-changeset' created."
  fi
fi

# ── Step 4: secrets guidance ──────────────────────────────────────────────────
header "Step 4/4 — Repository secrets"

_check_secret() {
  local secret_name="$1"
  # GitHub API returns 200 if secret exists, 404 if not
  if gh api "repos/${FULL_REPO}/actions/secrets/${secret_name}" &>/dev/null; then
    success "Secret '${secret_name}' already set."
    return 0
  fi
  return 1
}

case "${TOKEN_TYPE}" in
  app)
    if ! _check_secret "APP_ID" 2>/dev/null; then
      echo ""
      echo -e "  ${BOLD}Action required — set APP_ID secret:${RESET}"
      if [[ -n "${APP_ID}" ]]; then
        if [[ "${DRY_RUN}" == true ]]; then
          dry "gh secret set APP_ID --repo ${FULL_REPO} --body ${APP_ID}"
        else
          gh secret set APP_ID --repo "${FULL_REPO}" --body "${APP_ID}"
          success "APP_ID secret set."
        fi
      else
        echo "  Run:  gh secret set APP_ID --repo ${FULL_REPO}"
      fi
    fi

    if ! _check_secret "APP_PRIVATE_KEY" 2>/dev/null; then
      echo ""
      echo -e "  ${BOLD}Action required — set APP_PRIVATE_KEY secret:${RESET}"
      if [[ -n "${APP_PEM}" && -f "${APP_PEM}" ]]; then
        if [[ "${DRY_RUN}" == true ]]; then
          dry "gh secret set APP_PRIVATE_KEY --repo ${FULL_REPO} < ${APP_PEM}"
        else
          gh secret set APP_PRIVATE_KEY --repo "${FULL_REPO}" < "${APP_PEM}"
          success "APP_PRIVATE_KEY secret set."
        fi
      else
        echo "  Run:  gh secret set APP_PRIVATE_KEY --repo ${FULL_REPO} < /path/to/app.private-key.pem"
      fi
    fi
    ;;

  fine-grained|classic)
    if ! _check_secret "CHANGESET_TOKEN" 2>/dev/null; then
      echo ""
      echo -e "  ${BOLD}Action required — set CHANGESET_TOKEN secret:${RESET}"

      case "${TOKEN_TYPE}" in
        fine-grained)
          case "${SCOPE}" in
            personal)
              echo "  Create at: https://github.com/settings/personal-access-tokens/new"
              echo "  Resource owner: ${OWNER}"
              echo "  Repository access: Only select → ${REPO}"
              ;;
            org|enterprise)
              echo "  Ensure org allows fine-grained PATs:"
              echo "    https://github.com/organizations/${OWNER}/settings/personal-access-tokens"
              echo "  Create at: https://github.com/settings/personal-access-tokens/new"
              echo "  Resource owner: ${OWNER} (the org, not your personal account)"
              echo "  Repository access: Only select → ${REPO}"
              ;;
            ghes)
              echo "  Create at: https://${GHES_HOST}/settings/personal-access-tokens/new"
              echo "  Ensure org allows fine-grained PATs (GHES 3.10+ only):"
              echo "    https://${GHES_HOST}/organizations/${OWNER}/settings/personal-access-tokens"
              ;;
          esac
          echo "  Required permissions: Contents (r/w), Pull requests (r/w), Workflows (r/w)"
          ;;
        classic)
          case "${SCOPE}" in
            personal|org|enterprise)
              echo "  Create at: https://github.com/settings/tokens/new"
              echo "  Scope: repo  (includes contents, pull requests, workflows)"
              [[ "${SCOPE}" != "personal" ]] && \
                echo "  SSO: After creating, authorize for org ${OWNER}"
              ;;
            ghes)
              echo "  Create at: https://${GHES_HOST}/settings/tokens/new"
              echo "  Scope: repo"
              ;;
          esac
          ;;
      esac

      echo ""
      echo "  Then run:  gh secret set CHANGESET_TOKEN --repo ${FULL_REPO}"
    fi
    ;;
esac

# ── Step 5: immutable releases guidance ───────────────────────────────────────
header "Immutable releases (manual step)"

echo -e "  Enable at: https://github.com/${FULL_REPO}/settings"
echo -e "  Under 'Releases' → check 'Immutable releases'."
echo ""
echo -e "  Why it's safe: the CD pipeline uses a draft-first approach."
echo -e "  Drafts are never immutable — assets attach freely. The release"
echo -e "  only becomes immutable on the final \`gh release edit --draft=false\`."
echo -e "  The ERR cleanup trap fires before that line, so failures always"
echo -e "  clean up a mutable draft, never a locked release."
echo ""

# ── Summary ───────────────────────────────────────────────────────────────────
echo ""
echo -e "${BOLD}${GREEN}════════════════════════════════════════${RESET}"
echo -e "${BOLD}${GREEN}  Setup complete for ${FULL_REPO}${RESET}"
echo -e "${BOLD}${GREEN}════════════════════════════════════════${RESET}"
echo ""
echo -e "  Verify the setup:"
echo -e "  1. Create a branch, run \`just changeset\`, commit and open a PR"
echo -e "  2. Confirm 'Changeset Check' and 'CI Gate' appear as required checks"
echo -e "  3. Merge the PR; verify a changeset-release/main PR appears"
echo -e "  4. Verify CI runs on the Version PR"
echo -e "  5. Merge the Version PR; verify a GitHub Release is published"
echo ""
