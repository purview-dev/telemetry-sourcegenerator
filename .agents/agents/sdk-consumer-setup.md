# sdk-consumer-setup (generic agent spec)

## Goal

Help a consuming repository adopt or troubleshoot `Purview.DotNetProjectSdk` correctly, without breaking existing build behaviour.

## Workflow

1. Confirm the SDK is imported in `Directory.Build.props`/`Directory.Build.targets` via
   `<Import Sdk="Purview.DotNetProjectSdk" Project="Sdk.props" />` and the matching `Sdk.targets` import.
2. Check pre-import bootstrap properties are set **before** the `Sdk.props` import when they must affect
   evaluation: `NamespacePrefix`, `UsePackageJsonVersion`, `RootPackageJson`.
3. If version resolution looks wrong, verify `package.json` discovery: explicit `RootPackageJson`, then CI
   variables, `.git` root, or a nearby `package.json`. `UsePackageJsonVersion=Strict` fails fast instead of
   silently skipping resolution.
4. If the bundled `.agents/**` content isn't appearing in the repo root, check `EnableAgentFolderInPackage`
   (default `true`) and `AgentPackDestinationFolder` (default `.agents`) — the copy runs before build via
   `EnsureAgentFolderInPackageTarget`.
5. For test-framework or project-shape questions, confirm the project follows repo naming and placement
   conventions the SDK expects, rather than introducing bespoke structure.
6. Re-run `dotnet build` (or the repo's canonical build command) after each configuration change to confirm
   the fix.

## Constraints

- Prefer minimal, targeted property changes over broad `Directory.Build.props` rewrites.
- Do not disable `PurviewAutoSdkPack` or `EnableAgentFolderInPackage` unless the consumer explicitly asks to
  opt out.
- Do not duplicate SDK-managed properties in individual project files unless the scenario is intentionally
  project-specific.

## Related skill

See `../skills/sdk-configuration-reference/SKILL.md` for the full property reference.
