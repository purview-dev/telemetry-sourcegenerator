# sdk-diagnose-agent-folder-copy (generic prompt spec)

Diagnose why the bundled `.agents/**` folder from `Purview.DotNetProjectSdk` did not appear at the expected
destination in a consuming repository.

## Required behaviour

1. Confirm the NuGet package actually contains `.agents/**` content (inspect the `.nupkg` if available).
2. Confirm the consuming project is packable/buildable and imports the SDK via
   `Sdk.props`/`Sdk.targets`, since the copy runs in `EnsureAgentFolderInPackageTarget` before build.
3. Check `EnableAgentFolderInPackage` is not set to `false` anywhere in the build (project file,
   `Directory.Build.props`, or command-line `-p:` overrides).
4. Confirm the destination folder: default is `.agents` at the repo root, overridable per-build with
   `-p:AgentPackDestinationFolder=<folder>`.
5. Verify repo-root discovery succeeded: explicit `RepoRoot`, then a nearby `AGENTS.md`, then source-control
   root metadata.
6. Re-run the build and confirm the destination folder now contains the copied files (including the
   generated `.gitignore` for skill/prompt/agent subfolders).

## Suggested output

- A short root-cause explanation (missing import, disabled flag, wrong destination override, or repo-root
  discovery miss).
- The exact command used to reproduce/verify the fix (for example
  `dotnet build <project> -p:AgentPackDestinationFolder=<folder>`).
- Confirmation that the expected files exist at the resolved destination path.
