---
"purview-telemetry-sourcegenerator": minor
---

Use `AddEmbeddedAttributeDefinition` to inject `Microsoft.CodeAnalysis.EmbeddedAttribute` via Roslyn's official API, and apply `[global::Microsoft.CodeAnalysis.Embedded]` to all generated marker attribute types.

This ensures generated marker types are invisible to IDE tooling and consumer code, eliminating spurious symbol conflicts. Requires Roslyn 4.14.0+ (ships with Visual Studio 2022 17.14+ and .NET 10 SDK).
