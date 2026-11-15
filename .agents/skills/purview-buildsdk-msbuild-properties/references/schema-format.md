# The `.buildschema.json` format, for properties

The meta-schema is
<https://github.com/mhutch/MonoDevelop.MSBuildEditor/raw/main/MonoDevelop.MSBuild/Schemas/buildschema.json>.
It declares **JSON Schema draft-04** on purpose — Visual Studio fails to validate against anything
newer — and sets `additionalProperties: false` almost everywhere, so an unknown key is an error, not
an extension point. There is no slot for custom metadata: anything a property needs to say goes in
its `description`.

## The file

```json
{
  "$schema": "https://github.com/mhutch/MonoDevelop.MSBuildEditor/raw/main/MonoDevelop.MSBuild/Schemas/buildschema.json",
  "license": "MIT",
  "properties": {
    "AcmeKitStrict": {
      "description": "Reports ACME0001 as an error instead of a warning.",
      "type": "bool",
      "defaultValue": "false"
    }
  },
  "types": {}
}
```

Top-level keys: `$schema`, `license`, `properties`, `items`, `targets`, `metadata`, `types`,
`intellisenseImports`. All optional.

## A property entry

A string is shorthand for `{ "description": "..." }`. The object form allows only:

| Key | Use |
| -- | -- |
| `description` | What it does, what its values mean, whether it must be set before the import, and — when there is no `defaultValue` — `No default: <why>.` |
| `type` | An intrinsic type name, an inline type, or `{ "$ref": "#/types/<name>" }`. |
| `defaultValue` | The evaluated default, as a string. Omit it rather than guess. |
| `isList`, `listSeparators` | A list value. `listSeparators` is `";"`, `","` or `";,"`. |
| `isLiteral` | Only literal values, no `$(...)` expressions. |
| `deprecationMessage` | Marks the property deprecated; shown where it is set. Must not be empty. |
| `versionInfo`, `helpUrl` | Optional. |

## Intrinsic types

`bool`, `int`, `string`, `guid`, `url`, `version`, `version-suffixed`, `nuget-id`, `nuget-version`,
`file`, `folder`, `folder-with-slash`, `file-or-folder`, `extension`, `target-framework`,
`target-framework-moniker`, `runtime-id`, `configuration`, `platform`, `warning-code`,
`clr-namespace`, `clr-type`, `clr-type-name`, `sdk`, `sdk-version`, `target-name`, `item-name`,
`property-name`, `culture`, `lcid` and a few more — the meta-schema's `valueTypeIntrinsic` is the
complete list. Pick the narrowest: `warning-code` with `isList` for a `NoWarn`-style list,
`target-framework` with `isList` for a TFM set, `folder` for a directory.

## Enumerations

Define the values once under `types` and reference them:

```json
"properties": {
  "AcmeKitMode": {
    "description": "How strictly the kit applies its rules.",
    "type": { "$ref": "#/types/acme-kit-mode" },
    "defaultValue": "Warn"
  }
},
"types": {
  "acme-kit-mode": {
    "description": "A strictness level.",
    "values": {
      "Off": "No checks.",
      "Warn": "Report as warnings.",
      "Error": "Report as errors."
    }
  }
}
```

A type name is lowercase and hyphenated (`^[a-z][a-z\d-]*$`). Set `"allowUnknownValues": true` when
the listed values are suggestions rather than the whole set — a package runner, a provider package.
A value can be an object with `description`, `deprecationMessage`, `aliases` and `helpUrl` instead of
a plain description.

## Validating

The meta-schema can be associated in VS Code (`json.schemas` → `fileMatch: ["/*.buildschema.json"]`)
or in Visual Studio's JSON editor *Schema* box. Purview.BuildSdk validates its own sidecar against a
vendored copy of the meta-schema in its test suite, so the check runs offline.
