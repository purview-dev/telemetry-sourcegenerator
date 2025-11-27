# TelemetryMethodValidator Usage Guide

## Overview

The `TelemetryMethodValidator` class provides a centralized, testable way to validate return types and parameters across different telemetry generation targets (Activities, Logging, Metrics). It handles complex multi-target scenarios and automatic parameter exclusion.

## Key Features

1. **Return Type Validation**: Validates that return types are appropriate for each target type
2. **Parameter Exclusion**: Automatically determines which parameters should be excluded from specific targets
3. **Multi-Target Support**: Handles validation across combined generation targets
4. **Detailed Diagnostics**: Provides specific error reasons and messages

## Basic Usage

### Creating a Validator

```csharp
var validator = new TelemetryMethodValidator(compilation);
```

### Validating Return Types

```csharp
// Single target
var result = validator.ValidateReturnType(
    method.ReturnType,
    GenerationType.Logging,
    isScoped: false
);

if (!result.IsValid)
{
    foreach (var error in result.Errors)
    {
        // Report diagnostic: error.Message
    }
}

// Multi-target
var multiResult = validator.ValidateReturnType(
    method.ReturnType,
    GenerationType.Activities | GenerationType.Logging,
    isScoped: false
);

// Check specific target validity
if (multiResult.IsValidFor(GenerationType.Logging))
{
    // Proceed with logging generation
}
```

### Checking Parameter Exclusions

```csharp
foreach (var parameter in method.Parameters)
{
    var exclusion = validator.ShouldExcludeParameter(
        parameter,
        currentTarget,
        allTargets
    );

    if (exclusion.IsExcludedFrom(currentTarget))
    {
        // Skip this parameter for the current target
        var reason = exclusion.GetExclusionFor(currentTarget);
        // Optionally log: reason.Message
        continue;
    }

    // Include parameter in generation
}
```

## Validation Rules

### Return Type Rules

#### Logging
- **Non-Scoped**: `void`, `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`
- **Scoped**: `IDisposable` only

#### Activities
- `Activity`, `Activity?`, or `void`
- Event/Context methods: `void`

#### Metrics
- Standard metrics: `void`
- Observable metrics: numeric types, `Func<T>`, `Func<Measurement<T>>`, `IEnumerable<Measurement<T>>`

### Parameter Exclusion Rules

| Parameter Type | Excluded From | Reason |
|---------------|---------------|---------|
| `Activity` | Logging, Metrics | Activity parameters are only valid for Activity generation |
| `ActivityContext` | Logging, Metrics | ActivityContext parameters are only valid for Activity generation |
| `ActivityLink`, `IEnumerable<ActivityLink>` | Logging, Metrics | ActivityLink parameters are only valid for Activity generation |
| `TagList` | Logging | TagList is Activity/Metrics specific |
| Metrics measurement values (first numeric parameter) | Logging | Metrics counter/histogram values shouldn't be logged |

## Integration Examples

### In Activity Emitter

```csharp
var validator = new TelemetryMethodValidator(context.Compilation);

foreach (var method in activityMethods)
{
    // Validate return type
    var returnValidation = validator.ValidateReturnType(
        method.ReturnType,
        GenerationType.Activities
    );

    if (!returnValidation.IsValid)
    {
        TelemetryDiagnostics.Report(
            context.ReportDiagnostic,
            TelemetryDiagnostics.Activities.InvalidReturnType,
            method.Locations,
            returnValidation.Errors.First().Message
        );
        continue;
    }

    // Filter parameters
    var includedParams = method.Parameters
        .Where(p =>
        {
            var exclusion = validator.ShouldExcludeParameter(
                p,
                GenerationType.Activities,
                targetType
            );
            return exclusion.IsIncludedIn(GenerationType.Activities);
        })
        .ToArray();

    // Generate with filtered parameters
    EmitMethod(method, includedParams);
}
```

### In Multi-Target Scenario

```csharp
var allTargets = GenerationType.Activities | GenerationType.Logging;
var validator = new TelemetryMethodValidator(context.Compilation);

foreach (var method in methods)
{
    // Validate for all targets
    var returnValidation = validator.ValidateReturnType(
        method.ReturnType,
        allTargets
    );

    // Generate for each valid target
    if (returnValidation.IsValidFor(GenerationType.Activities))
    {
        var activityParams = FilterParameters(
            method.Parameters,
            GenerationType.Activities,
            allTargets,
            validator
        );
        EmitActivityMethod(method, activityParams);
    }

    if (returnValidation.IsValidFor(GenerationType.Logging))
    {
        var loggingParams = FilterParameters(
            method.Parameters,
            GenerationType.Logging,
            allTargets,
            validator
        );
        EmitLoggingMethod(method, loggingParams);
    }
}

IParameterSymbol[] FilterParameters(
    ImmutableArray<IParameterSymbol> parameters,
    GenerationType currentTarget,
    GenerationType allTargets,
    TelemetryMethodValidator validator)
{
    return parameters
        .Where(p =>
        {
            var exclusion = validator.ShouldExcludeParameter(
                p,
                currentTarget,
                allTargets
            );
            return exclusion.IsIncludedIn(currentTarget);
        })
        .ToArray();
}
```

## Testing

The validator is designed to be easily testable. See `TelemetryMethodValidatorTests.cs` for comprehensive test examples:

```csharp
[Test]
public void ValidateReturnType_GivenActivityParameterForLogging_ReturnsExcluded()
{
    const string source = @"
using System.Diagnostics;
namespace Test {
    public interface ITest {
        void Method(Activity? activity, string message);
    }
}";
    var compilation = CreateCompilation(source);
    var method = GetMethodSymbol(compilation, "Method");
    var activityParam = method.Parameters[0];
    var validator = new TelemetryMethodValidator(compilation);

    var result = validator.ShouldExcludeParameter(
        activityParam,
        GenerationType.Logging,
        GenerationType.Logging | GenerationType.Activities
    );

    result.IsExcludedFrom(GenerationType.Logging).ShouldBeTrue();
}
```

## Future Enhancements

Potential areas for extension:

1. **Custom Exclusion Rules**: Allow user-defined exclusion patterns via attributes
2. **Parameter Ordering Validation**: Ensure Activity parameters come first in Event/Context methods
3. **Type Compatibility Checks**: Validate that Tag/Baggage attributes are on compatible types
4. **Performance Optimization**: Cache compilation type lookups
5. **Diagnostic Codes**: Map each error to a specific diagnostic code for better tooling support
