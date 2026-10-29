# Migrating a legacy emitter to CodeWriter

A playbook for moving a source generator's emission from manual `string` / `StringBuilder`
construction to `CodeWriter` plus the `XmlCommentWriter` documentation extensions, with behaviour
parity.

## Before you start, collect

- The emitter file(s) being changed.
- The generator type name, and the version string it stamps into generated attributes or headers.
- Whether the output must stay **byte-identical**. If it must, every whitespace decision below is a
  constraint rather than a preference, and the diff of the generated snapshots is the acceptance
  test.

## Requirements

1. Use the structured declaration APIs rather than hand-written declaration text:
   `Class` / `Struct` / `RecordClass` / `Interface` / `Enum`, and `Method`, `Property`, `Field`,
   `Constructor`.
2. Use the XML helper extensions instead of composing `///` lines: `XmlSummary`, `XmlParam`,
   `XmlReturn`, `XmlRemarks`, `XmlCode` / `XmlCodeBlock`.
3. Use `TypeReference` once the type text gets complex — nullability, generics, arrays.
4. Keep the writer **output-scoped**: `generationContext.CreateCodeWriter()` *inside* the output
   callback. Never create it earlier in the pipeline, and never store it in incremental provider
   state or in a custom context.
5. Preserve behaviour, diagnostics and generated names.
6. Keep the change minimal. Do not reformat logic the migration does not touch.

## Strategy

Work phase by phase, because each phase has a different mechanical replacement:

| Emitter phase | Replace with |
| --- | --- |
| header | the header helper, not a literal banner string |
| namespace | the namespace scope API |
| type declarations | declaration option records, not signature strings |
| member declarations | `Method` / `Property` / `Field` / `Constructor` |
| indentation and braces | scoped APIs — never manual `{`/`}` and `\t` |
| `///` comment composition | `XmlSummary`, `XmlParam`, `XmlReturn`, `XmlRemarks`, `XmlCode` |

Call out any intentional semantic delta explicitly; a migration that quietly changes emitted names or
diagnostics is the failure mode this playbook exists to avoid.

## Verification

- Run the impacted tests.
- Confirm the generated files still compile.
- Confirm there are **no scope leaks**: `OpenScopeCount == 0` when the writer is materialized. Enable
  CodeWriter scope validation in the test run.
- Where byte-identical output was required, diff the generated snapshots.

## Report

1. Files changed.
2. Why each change was necessary.
3. Risks and behaviour differences, if any.
4. The verification that actually ran.
