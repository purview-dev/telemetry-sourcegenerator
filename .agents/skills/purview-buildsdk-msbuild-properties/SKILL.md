---
name: purview-buildsdk-msbuild-properties
description: Add, rename, deprecate or remove an MSBuild property in a package built with Purview.BuildSdk, and describe it in an MSBuild Editor .buildschema.json sidecar so it gets csproj IntelliSense. Use when a package ships .props/.targets under Sdk/, build/ or buildTransitive/ and needs a new setting, a compiler-visible (build_property.*) value for its analyzers, a documented default, or a check that its schema and props agree (ValidatePurviewPropertySchema, PRPSC0001-PRPSC0004). Also covers where a sidecar must sit to be loaded, when a property must be set before the Sdk.props import, and declaring a new sidecar in .config/purview-build.json PackValidation.RequiredContent - including a Purview.Build "contains undeclared content ... not defined in RequiredContent" failure.
license: MIT
metadata:
  package: Purview.BuildSdk
  repository: purview-dev/build-sdk
---

# MSBuild properties and their schema

A property a package ships is described in several places: its definition and default, the
`CompilerVisibleProperty` list when an analyzer reads it, the package's docs, and an MSBuild Editor
sidecar schema that gives it csproj IntelliSense. They drift unless one of them is canonical and the
others are checked against it.

**The sidecar is canonical.** Write the description, type and default there first, then make the code
and the docs agree with it. `ValidatePurviewPropertySchema` checks the part that can be checked
mechanically.

## Where a sidecar goes

MSBuild Editor reads `<path of an imported file>.buildschema.json` — the **full** file name plus the
suffix, in the **same folder** as the file it describes. Verified in the editor's source
(`MSBuildSchemaProvider.GetSchema`: `path + ".buildschema.json"`).

| The package ships | The consumer's build imports | The sidecar is |
| -- | -- | -- |
| an MSBuild SDK (`Sdk/Sdk.props`, `Sdk/Sdk.targets`) | `Sdk/Sdk.props` | `Sdk/Sdk.props.buildschema.json` |
| `build/Acme.Kit.props` | `build/Acme.Kit.props` | `build/Acme.Kit.props.buildschema.json` |
| `buildTransitive/Acme.Kit.props` | `buildTransitive/Acme.Kit.props` | `buildTransitive/Acme.Kit.props.buildschema.json` |

Three consequences:

- `Sdk.buildschema.json` or `Acme.Kit.buildschema.json` is **never loaded**: the extension of the
  described file is part of the name. `PRPSC0004` reports a sidecar with no file beside it.
- One sidecar is enough per package. The editor loads every sidecar for every file it imports and
  applies them project-wide, so describe all the package's properties beside the file a consumer
  always imports — usually the `.props` — even those its `.targets` defines.
- A package that ships the same file under both `build/` and `buildTransitive/` needs the sidecar in
  both, because a direct and a transitive reference import different copies.

With Purview.BuildSdk, author the files under the project's `Sdk/` folder: `PurviewAutoSdkPack` maps
`Sdk/build/**` to `build/**` and `Sdk/buildTransitive/**` to `buildTransitive/**`, and a sidecar
travels with its neighbour because it sits in the same folder. An MSBuild SDK project disables
`PurviewAutoSdkPack` and packs `Sdk/**` to `Sdk/` itself; a sidecar there is picked up by that same
item.

### Declare the sidecar to pack validation

A sidecar is a new file in the package, and a repository built with Purview.Build validates every
package's contents against `.config/purview-build.json`. Whenever you add a sidecar — or any other
`.props`/`.targets` the package ships — open that file and check `PackValidation.RequiredContent`.
It is keyed by lower-case package id, and lists the package-relative path of every file the package
must contain:

```json
"PackValidation": {
  "RequiredContent": {
    "acme.kit": [
      "build/Acme.Kit.props",
      "build/Acme.Kit.props.buildschema.json",
      "buildTransitive/Acme.Kit.props",
      "buildTransitive/Acme.Kit.props.buildschema.json"
    ]
  }
}
```

Add each sidecar beside the file it describes, in the list's existing order. Both directions fail
the pipeline's `ValidatePackModule`, and neither shows up until `just pipeline-pack-validate` or a
release:

- **A packed file the list does not name** — `contains undeclared content 'Sdk/Sdk.props.buildschema.json'
  that is not defined in RequiredContent`. The package is right; the list is stale.
- **A listed file the package lacks** — the sidecar was renamed, or did not pack. Fix the pack, not
  the list.

With no `.config/purview-build.json`, or no entry for the package, there is nothing to update. To
check, pack and validate without publishing:

```bash
just pipeline-pack-validate    # or: dotnet purview-build --Build:RunPack=true --Build:ValidatePack=true --Release:Mode=None
```

## Add a property

1. **Define it, with its default, in the `.props`.** Guard it so a value set earlier wins:

   ```xml
   <AcmeKitStrict Condition="'$(AcmeKitStrict)' == ''">false</AcmeKitStrict>
   ```

   Give a boolean an explicit `true`/`false` default. Leave a property unset only when "unset" is a
   real third state — "detected", "inherited from another property" — and say so in the schema.

2. **Decide whether it must be set before the import.** A `.props` file is evaluated before the
   project body. If anything in the `.props` (or a file it imports) *reads* the property — in a value
   or a `Condition`, other than its own `'$(X)' == ''` guard — a value set in the project file arrives
   too late. Then it must be set in `Directory.Build.props` **before** the `<Import>`, and its
   description says so. If only the `.targets` reads it, a project file can set it too; prefer that
   shape for anything per-project.

3. **Make it compiler-visible only if an analyzer or generator reads it.** Add it beside the package's
   other exports, with a `<Description>`:

   ```xml
   <ItemGroup>
     <CompilerVisibleProperty Include="AcmeKitStrict">
       <Description>When true, the Acme analyzers report ACME0001 as an error.</Description>
     </CompilerVisibleProperty>
   </ItemGroup>
   ```

   The analyzer reads `build_property.AcmeKitStrict` from
   `AnalyzerConfigOptionsProvider.GlobalOptions`. Nothing else exposes an MSBuild property to the
   compiler, and an export nothing reads is noise in every consumer's generated `.editorconfig`.

4. **Describe it in the sidecar.** See [references/schema-format.md](references/schema-format.md) for
   the entry format, types and enumerations:

   ```json
   "AcmeKitStrict": {
     "description": "Reports ACME0001 as an error instead of a warning.",
     "type": "bool",
     "defaultValue": "false"
   }
   ```

   - `defaultValue` is the value a plain project evaluates to — not what it is "usually".
   - No default? End the description with `No default: <why>.` — "detected from the project",
     "unset, which is treated as false".
   - Must be set before the import? Say `Set it before the \`Sdk.props\` import.` (or name the
     package's own `.props`).

5. **Document it** in the package's README or docs table, with the same default.

6. **Record it** in `CHANGELOG.md` under the heading for the version in `package.json` →
   `### Added`. Not `[Unreleased]`: a merge to `main` publishes that version, so the entry would read as
   unreleased until it was already live. `[Unreleased]` is only for a pre-release version.

7. **Check it.** Run `dotnet msbuild <project> -t:ValidatePurviewPropertySchema` (see below), then the
   package's own tests. If this property brought the package's **first** sidecar, or a new
   `.props`/`.targets` file, declare it to pack validation as well
   ([above](#declare-the-sidecar-to-pack-validation)).

## Rename a property

Never rename a released property in place: every consumer that sets the old name silently loses it.

1. Add the new name as in *Add a property*, defaulting from the old one so an existing setting still
   works:

   ```xml
   <AcmeKitStrictMode Condition="'$(AcmeKitStrictMode)' == '' AND '$(AcmeKitStrict)' != ''">$(AcmeKitStrict)</AcmeKitStrictMode>
   <AcmeKitStrictMode Condition="'$(AcmeKitStrictMode)' == ''">false</AcmeKitStrictMode>
   ```

2. Read only the new name everywhere else, including the `CompilerVisibleProperty` export. Export the
   old one too until it is removed if analyzers in the wild read it.
3. Keep the old entry in the sidecar, with a `deprecationMessage` naming the replacement, so the editor
   flags the old name where it is set:

   ```json
   "AcmeKitStrict": {
     "description": "Replaced by AcmeKitStrictMode.",
     "type": "bool",
     "deprecationMessage": "Use AcmeKitStrictMode instead."
   }
   ```

4. Changelog: `### Added` for the new name, `### Deprecated` for the old.

## Deprecate a property

Keep its behaviour and its default, add a `deprecationMessage` to its sidecar entry saying what to do
instead, mark the docs row deprecated, and add a `### Deprecated` changelog entry. A deprecated
property is still a property: it keeps its schema entry until it is removed.

## Remove a property

Only in a major version, and in one change: the definition, every read, the `CompilerVisibleProperty`
item, the sidecar entry and the docs row. Leaving any one behind is exactly what the checks catch —
`PRPSC0002` for a sidecar entry nothing defines. Changelog: `### Removed`, naming what to set instead.

## Checking a package: `ValidatePurviewPropertySchema`

Purview.BuildSdk ships an opt-in target for any package that authors `.props`/`.targets`:

```xml
<!-- the package's .csproj -->
<PropertyGroup>
  <EnablePurviewPropertySchemaValidation>true</EnablePurviewPropertySchemaValidation>
  <!-- exports the package does not own; MSBuild already describes them -->
  <PurviewPropertySchemaIgnoredProperties>RootNamespace;Version</PurviewPropertySchemaIgnoredProperties>
</PropertyGroup>
```

With it on, the check runs before `Pack`. Run it by name at any time:

```bash
dotnet msbuild src/Acme.Kit/Acme.Kit.csproj -t:ValidatePurviewPropertySchema
```

It reads every `.props`/`.targets` under `PurviewPropertySchemaDirectory` (default: the project's
`Sdk/` folder) and every sidecar beside them, and fails with:

| Id | Meaning | Fix |
| -- | -- | -- |
| `PRPSC0001` | a `CompilerVisibleProperty` no sidecar describes | add the entry, or list the name in `PurviewPropertySchemaIgnoredProperties` |
| `PRPSC0002` | a sidecar entry no file defines, reads or exports | remove the entry, or define the property |
| `PRPSC0003` | a sidecar that is not JSON, or has no `"properties"` object | fix the file |
| `PRPSC0004` | a sidecar with no `.props`/`.targets` beside it | rename it to `<file>.buildschema.json` |

It is off by default and never runs in an ordinary build, so turning it on changes nothing for the
package's consumers. It checks agreement, not prose: whether a description is *right* is the review's
job.

## Getting the IntelliSense

Install the **MSBuild Editor** extension in Visual Studio
(<https://marketplace.visualstudio.com/items?itemName=mhutch.msbuildeditor>). It loads the sidecar of every file a project imports, SDKs resolved
through `global.json` `msbuild-sdks` included, and offers completion, the description, the default
and — for an enumeration — the allowed values. Without the extension a sidecar is inert.

## Guardrails

- **Do not change a released default** as part of documenting it. A wrong document is fixed in the
  document; a default change is a behaviour change with its own changelog entry.
- **Do not export a property "in case".** Each `CompilerVisibleProperty` lands in every consumer's
  generated analyzer config.
- **Do not hand-edit generated sidecars.** If a package generates its sidecar, change the generator.
- **Keep the meta-schema draft as it is** (`draft-04`): Visual Studio rejects newer drafts.
