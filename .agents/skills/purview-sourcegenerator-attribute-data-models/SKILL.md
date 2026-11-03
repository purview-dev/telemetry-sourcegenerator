---
name: purview-sourcegenerator-attribute-data-models
description: >-
  Use when declaring or fixing a [Generate] attribute-data model in Purview.SourceGeneratorFramework -
  mapping constructor and named arguments with [Argument]/[Property], defaults, reusing a shape with
  [NestedModel], reading an ADM0001-ADM0015 diagnostic, or testing that a model reads the values an
  attribute application supplies.
metadata:
  product: Purview.SourceGeneratorFramework
  repository: purview-dev/sourcegenerator-framework
---

# Attribute-data models

`AttributeDataModelGenerator` turns a `readonly partial record struct` marked `[Generate(typeof(TAttribute))]`
into a parser for `TAttribute`'s `AttributeData`: `Exists`, `Empty`, `TargetAttribute`, the guarded
`FromAttributeData`/`TryFromAttributeData`/`AllAttributeData` overloads, and `FromAttributeDataUnchecked`.
The generated model is a value type, so it is safe to hold in an incremental pipeline (an `AttributeData`
is not).

The one thing to keep in mind: **the generated code looks names up case-sensitively at run time, and a
name it cannot find quietly yields the default value.** A mapping mistake compiles and produces `null`/`0`.
The naming analyzer (`ADM0011`–`ADM0015`) exists to report those mistakes where they are declared; do not
suppress it without understanding the finding, and test the model against real applications.

## Mapping each property

Each primary-constructor parameter becomes a property, PascalCased (`message_template` → `MessageTemplate`).
Its sources are read in this order, first hit wins:

| Declaration | Reads |
| --- | --- |
| *(no mapping attribute)* | the named argument with the property's name: `[Log(EventId = 7)]` |
| `[Property]` / `[Property(Name = "X")]` | the named argument `X` (default: the property name) |
| `[Argument("messageTemplate")]` | the constructor parameter **named** `messageTemplate` |
| `[Argument]` | the constructor parameter named after the property, camel-cased: `MessageTemplate` → `messageTemplate` |
| `[Argument(1)]` | the constructor argument at **position** 1 |
| `[Argument(...)] [Property]` | the named argument first, then the constructor argument |
| `[NestedModel]` | another generated model, read from the same attribute (below) |
| `[GenericTypeArgument]` | a generic type argument of the attribute class, as a `TypeIdentity` |
| `[Exclude]` | nothing; the parameter is left out of the model |

Methodology:

- **Map a constructor parameter by name, not by index.** A name survives a new overload or a reordered
  parameter list; an index silently starts reading a different parameter.
- **Map both sources when the attribute accepts both.** `[Argument("name")] [Property] string? Name` reads
  `[A(name: "x")]`, `[A("x")]` and `[A(Name = "x")]`. The named argument is read first because it is assigned
  after the constructor runs, so it is the attribute instance's effective value.
- **Only map `[Property]` to a public settable property or field.** A property with a private or `internal`
  setter can never be a named argument; read it through the constructor parameter that sets it (`ADM0012`).
- **Match case exactly.** `[Argument("MessageTemplate")]` never matches a parameter called
  `messageTemplate` (`ADM0011`).
- **Give every property that might be absent a `DefaultValue`**: `[Property(DefaultValue = -1)] int EventId`.
  A missing argument takes it; without one, it takes `default(T)`. `Empty` always uses `default(T)`.
- **Use `IsEnum = true` with a `string` property** when the enum type is not available to the generator; a
  bare member-name default (`DefaultValue = "Inherit"`) is expanded to the fully-qualified member.
- **Never name a property `Exists`, `Empty`, `TargetAttribute`, `AllAttributeData`, `FromAttributeData`,
  `TryFromAttributeData` or `FromAttributeDataUnchecked`** — they are generated (`ADM0014`, generation is
  skipped). Keyword names (`Event`, `Class`) are fine.

## Reusing a shape with `[NestedModel]`

A `[NestedModel]` parameter is populated by the nested model's `FromAttributeDataUnchecked`, from the
**outer** attribute's arguments, with no `TargetAttribute` check. The outer attribute does not have to derive
from the nested model's target; it only has to use the same constructor parameter and property names.

```csharp
[Generate(typeof(LogAttribute))]
readonly partial record struct LogAttributeData(
    [Argument("level")] [Property(DefaultValue = 99)] int Level,
    [Argument("messageTemplate")] [Property] string? MessageTemplate,
    [Argument("eventId")] [Property(DefaultValue = -1)] int EventId,
    [Property] string? Name
);

// InfoAttribute has messageTemplate, eventId and Name, but no level.
[Generate(typeof(InfoAttribute))]
readonly partial record struct InfoAttributeData([NestedModel] LogAttributeData Log);
```

- The nested model's `Exists` is always `true`; a mapping the outer attribute does not declare (`level`
  here) takes its `DefaultValue`. `ADM0013` (info) lists such mappings, so an intended absence is visible
  and an accidental one is caught.
- Map nested properties **by name**. A constructor index means a different parameter on each attribute
  (`ADM0015`).
- `MatchByInheritance = true` is for using a model on its own against derived attributes. A nested model
  does not need it.
- Call `FromAttributeData`/`TryFromAttributeData` from generator code. `FromAttributeDataUnchecked` skips the
  type check and is for nested models.

## Diagnostics

| Rule | Severity | Meaning and fix |
| --- | --- | --- |
| `ADM0001` | Error | The `[Generate]` target cannot be resolved. Use `typeof`, a resolvable full name, or a type-library `…FullName` constant. |
| `ADM0002` | Error | Unsupported property type (array, pointer, function pointer). |
| `ADM0003` | Error | `[Argument("name")]`/`[Argument(index)]` names no constructor parameter of the target. |
| `ADM0004` | Error | A `[NestedModel]` type is not itself a `[Generate]` model. |
| `ADM0005` | Error | A `DefaultValue` cannot be emitted for the property type. |
| `ADM0006` | Error | A non-nullable reference property has no default. Add a `DefaultValue` or make it nullable. |
| `ADM0007` | Error | `AutoDiscover` needs the `typeof` target overload. |
| `ADM0008` | Error | A `[GenericTypeArgument]` property must be `TypeIdentity`. |
| `ADM0009` | Error | An `IsEnum` property must be `string`/`string?`. |
| `ADM0010` | Error | An `ISymbol`/`Type` property is not cacheable; use `TypeIdentity` or a string. |
| `ADM0011` | Warning | A mapped name matches an attribute member only when case is ignored, so it is never read. Fix the case. |
| `ADM0012` | Warning | The mapped named argument is not a public settable member of the attribute, or a bare `[Argument]` implies a constructor parameter it lacks. Map the member that actually carries the value. |
| `ADM0013` | Info | A nested model property reads only names the outer attribute lacks, so it always takes its default. Expected for a deliberately absent value; otherwise rename to match. |
| `ADM0014` | Error | A property has a generated member's name. Rename the parameter. |
| `ADM0015` | Warning | A nested model reads a constructor index that names a different parameter on the outer attribute. Map it by name. |

## Testing a model

Test the attribute side and the model side separately, against what Roslyn actually produces. Use
`AttributeDataFactory` from `Purview.SourceGeneratorFramework.Testing` to compile an application, and the
`AttributeDataAssertions` from the TUnit package to pin each argument by ordinal, parameter name or named
argument, including how it was supplied (`AttributeArgumentForm`):

```csharp
var options = new MyTestOptions { AdditionalNamespaces = ["My.Attributes"] };
var attribute = AttributeDataFactory.FromUsage("[Info(\"custom {Value}\", Name = \"N\")]", options);

await Assert.That(attribute).HasConstructorArgument("messageTemplate", "custom {Value}");
await Assert.That(attribute).HasConstructorArgument("eventId", -1, AttributeArgumentForm.Defaulted);
await Assert.That(attribute).HasNamedArgument("Name", "N");

var model = InfoAttributeData.FromAttributeData(attribute);
await Assert.That(model.Log.MessageTemplate).IsEqualTo("custom {Value}");
await Assert.That(model.Log.Level).IsEqualTo(99);
```

Cover, for each model: positional, by-name and named-argument applications; an application omitting every
optional argument (proves the `DefaultValue`s); for a nested model, an outer attribute that lacks a mapped
member; and the nested target's own `FromAttributeData` returning `Empty` for the outer attribute.
`FromUsage` places the application on a declaration its `AttributeUsage` allows and throws on a compile
error, so a misspelt argument name fails the test. `FromSource` reads applications from a full source.

Generated-code assertions (`Contains("FromAttributeDataUnchecked(attributeData)")`) prove *what* the
generator emits; the tests above prove the model *reads the right values*. Prefer the latter.

## Related skills

`purview-sourcegenerator-testing` for the runner layer, `AttributeDataFactory` and `AttributeArguments`;
`purview-sourcegenerator-tunit-tests` for the assertion extensions; `purview-sourcegenerator-pipeline` for
holding models in an incremental pipeline; `purview-sourcegenerator-diagnostics` for the other analyzer
families.
