# sdk-diagnose-agent-folder-copy (generic prompt spec)

Diagnose why the bundled `.agents/**` folder from `Purview.BuildSdk` did not appear at the expected
destination in a consuming repository.

## Required behaviour

1. Confirm the NuGet package actually contains `.agents/**` content (inspect the `.nupkg` if available).
2. Confirm the consuming project is packable/buildable and imports the SDK via
   `Sdk.props`/`Sdk.targets`, since the copy runs in `EnsureAgentFolderInPackageTarget` before build.
3. Check `EnableAgentFolderInPackage` is not set to `false` anywhere in the build (project file,
   `Directory.Build.props`, or command-line `-p:` overrides).
4. Confirm the destination folder: default is `.agents` at the repo root, overridable per-build with
   `-p:AgentPackDestinationFolder=<folder>`, and the source defaults to the package-level `.agents`
   folder (override with `PurviewAgentFolderSourcePath`).
5. Verify repo-root discovery succeeded: explicit `RepoRoot`, then a nearby `AGENTS.md`, then source-control
   root metadata.
6. If the destination looks stale or incomplete, inspect the change-detection manifest
   (`<repo root>/.purview/agent-sync.cache`). It lists the files the SDK believes it already mirrored; a
   matching entry with a present destination file means the sync was skipped as up to date. Delete the
   manifest (or the affected destination file) to force a fresh copy.
7. Retry notices are demoted to low-importance messages, so a healthy build shows no `MSB3026` warnings.
   A copy that still fails after every retry is reported as an error naming the source, destination and
   OS error - search the build log for `The Purview SDK could not copy`. Temporarily set
   `PurviewAgentFolderCopyRetries` / `PurviewAgentFolderCopyRetryDelayMilliseconds` to retry longer, and
   `PurviewSuppressCopyRetryWarnings=false` to see every retry attempt.
8. Re-run the build and confirm the destination folder now contains the copied files (including the
   generated `.gitignore` for skill/prompt/agent subfolders).

## Suggested output

- A short root-cause explanation (missing import, disabled flag, wrong destination override, repo-root
  discovery miss, or a destination held open by another process).
- The exact command used to reproduce/verify the fix (for example
  `dotnet build <project> -p:AgentPackDestinationFolder=<folder>`).
- Confirmation that the expected files exist at the resolved destination path, plus whether the
  manifest skipped the sync (in which case the content was already up to date).
