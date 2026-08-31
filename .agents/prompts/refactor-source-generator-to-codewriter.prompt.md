---
agent: ask
description: "Refactor a legacy source generator emitter from string/StringBuilder to CodeWriter + XmlCodeWriter-style XML extensions with behavior parity."
---

You are modernizing a source generator implementation in this repository. Apply the guidance from the `source-generator-codewriter-modernization` skill for incremental pipelines, value equality, deterministic output, and CodeWriter scope safety.

## Inputs

- Target file(s): `${input:targetFiles:Path(s) to emitter file(s)}`
- Generator type name: `${input:generatorName:Generator class name}`
- Generator version: `${input:generatorVersion:Version string (for generated attributes/header)}`
- Keep output byte-identical where possible: `${input:preserveFormatting:true|false}`

## Task

Refactor the selected legacy emitter implementation from manual `string` / `StringBuilder` output construction to `CodeWriter` and XML documentation extension helpers from `XmlCommentWriter` (XmlCodeWriter-style API usage).

### Requirements

1. Use structured declaration APIs where applicable:
    - `WriteClass/WriteStruct/WriteRecordClass/WriteInterface/WriteEnum`
    - `WriteMethod`, `WriteProperty`, `WriteField`, `WriteConstructor`
2. Use XML helper extensions instead of raw `///` composition:
    - `XmlSummary`, `XmlParam`, `XmlReturn`, `XmlRemarks`, `XmlCode` or `XmlCodeBlock`
3. Use `TypeReferenceOptions` when type text becomes complex (nullability, generics, arrays).
4. Ensure writer lifetime is output-scoped (`generationContext.CreateCodeWriter()` inside callback).
5. Preserve behavior, diagnostics, and generated names.
6. Keep changes minimal and focused; do not reformat unrelated logic.

### Migration strategy

- Identify emitter phases: header, namespace, type declarations, member declarations.
- Replace indentation/braces with scoped APIs.
- Replace signature strings with declaration options.
- Replace XML comments with XmlCommentWriter extension methods.
- Keep semantic equivalence; call out any intentional deltas.

### Verification

- Run relevant tests.
- Confirm generated files still compile.
- Confirm no `CodeWriter` scope leaks (`OpenScopeCount == 0` when materialized).

### Output format

Return:

1. Files changed
2. Why each change was necessary
3. Risks/behavior differences (if any)
4. Verification performed
