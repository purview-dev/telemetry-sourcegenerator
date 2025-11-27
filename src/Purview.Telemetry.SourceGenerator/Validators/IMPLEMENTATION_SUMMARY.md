# TelemetryMethodValidator Implementation Summary

## Overview

Created a comprehensive, testable validation system for telemetry method return types and parameters that handles complex multi-target generation scenarios.

## Files Created

### 1. `TelemetryMethodValidator.cs`
**Location**: `Purview.Telemetry.SourceGenerator\Validators\TelemetryMethodValidator.cs`

**Purpose**: Core validator class that provides:
- Return type validation for all three target types (Activities, Logging, Metrics)
- Automatic parameter exclusion based on target compatibility
- Support for multi-target scenarios
- Detailed error reporting with specific diagnostic reasons

**Key Methods**:
- `ValidateReturnType()` - Validates return types against target requirements
- `ShouldExcludeParameter()` - Determines if a parameter should be excluded from specific targets

**Key Features**:
- Handles scoped logger validation (IDisposable return type)
- Validates Activity return types (Activity or void)
- Validates async return types (Task, ValueTask)
- Validates observable metrics return types (Func<T>, Measurement<T>, etc.)
- Automatically excludes Activity-related parameters from Logging/Metrics
- Automatically excludes TagList from Logging
- Automatically excludes metrics measurement parameters from Logging

### 2. `TelemetryMethodValidatorTests.cs`
**Location**: `Purview.Telemetry.SourceGenerator.IntegrationTests\Validators\TelemetryMethodValidatorTests.cs`

**Purpose**: Comprehensive test suite covering:
- All return type validation scenarios
- All parameter exclusion scenarios
- Multi-target validation
- Edge cases and error conditions

**Test Coverage**:
- 20+ test cases
- Tests for each target type independently
- Tests for multi-target combinations
- Tests for automatic parameter exclusion
- Tests for observable metrics types
- Parameterized tests for multiple observable return types

### 3. `README.md`
**Location**: `Purview.Telemetry.SourceGenerator\Validators\README.md`

**Purpose**: Complete usage documentation including:
- Basic usage examples
- Validation rules reference table
- Integration examples for emitters
- Multi-target scenario patterns
- Testing guidance
- Future enhancement suggestions

## Updated Files

### `GenericRecords.cs`
- Added `All = Activities | Logging | Metrics` flag to `GenerationType` enum
- Enables easier checking against all target types

## Design Decisions

### 1. Isolated, Testable Architecture
- Validator is a separate class that takes Compilation as a dependency
- All logic is testable without requiring the full source generator infrastructure
- Easy to mock and test in isolation

### 2. Explicit Result Objects
- `ReturnTypeValidationResult` - Contains results for all applicable targets
- `ParameterExclusionResult` - Contains all exclusion reasons
- Results are immutable records with query methods
- Clear separation between validation logic and diagnostic reporting

### 3. Enum-Based Error Types
- `ReturnTypeValidationError` - Specific error types for return validation
- `ParameterExclusionReason` - Specific reasons for parameter exclusion
- Makes it easy to map to diagnostic codes
- Provides type-safe error handling

### 4. Decision Tree Pattern
The validator implements a clear decision tree:

```
ValidateReturnType()
??? For each target in targets
?   ??? If Logging ? ValidateLoggingReturnType()
?   ?   ??? If Scoped ? Must be IDisposable
?   ?   ??? Else ? Must be void, Task, or ValueTask
?   ??? If Activities ? ValidateActivityReturnType()
?   ?   ??? Must be Activity or void
?   ??? If Metrics ? ValidateMetricsReturnType()
?       ??? Must be void or observable types
??? Return combined results

ShouldExcludeParameter()
??? Check parameter type
?   ??? If Activity ? Exclude from Logging and Metrics
?   ??? If ActivityContext ? Exclude from Logging and Metrics
?   ??? If ActivityLink ? Exclude from Logging and Metrics
?   ??? If TagList ? Exclude from Logging
?   ??? If Metrics measurement ? Exclude from Logging
??? Return all exclusions
```

## Integration Points

### Current Usage
The validator is ready to be integrated into:
1. `ActivitySourceTargetClassEmitter` - Validate Activity methods
2. `LoggerGenTargetClassEmitter` - Validate Logger methods and filter parameters
3. `MetricsTargetClassEmitter` - Validate Metrics methods
4. Multi-target generation coordination

### Integration Pattern
```csharp
var validator = new TelemetryMethodValidator(context.Compilation);

// Validate return type
var returnValidation = validator.ValidateReturnType(
    method.ReturnType,
    currentTargetType,
    isScoped: method.IsScoped
);

if (!returnValidation.IsValid)
{
    // Report diagnostics
    foreach (var error in returnValidation.Errors)
    {
        ReportDiagnostic(error.Error, error.Message, method.Locations);
    }
    continue;
}

// Filter parameters
var includedParameters = method.Parameters
    .Where(p =>
    {
        var exclusion = validator.ShouldExcludeParameter(
            p,
            currentTargetType,
            allTargets
        );
        return exclusion.IsIncludedIn(currentTargetType);
    })
    .ToArray();

// Generate method with filtered parameters
EmitMethod(method, includedParameters);
```

## Benefits

### For Development
1. **Centralized Logic**: All validation rules in one place
2. **Easy Testing**: Comprehensive test coverage without needing full source generator
3. **Clear Contracts**: Explicit result types make behavior obvious
4. **Maintainable**: Adding new rules is straightforward

### For Multi-Target Scenarios
1. **Automatic Parameter Filtering**: No manual tracking needed
2. **Consistent Behavior**: Same validation across all emitters
3. **Clear Diagnostics**: Specific error messages for each scenario
4. **Scalable**: Easy to add new target types

### For Users
1. **Better Error Messages**: Specific reasons why something is invalid
2. **Predictable Behavior**: Clear rules about what's excluded
3. **Multi-Target Support**: Works seamlessly across combined targets

## Next Steps

### Immediate Integration
1. Update `ActivitySourceTargetClassEmitter` to use validator
2. Update `LoggerGenTargetClassEmitter` to use validator for parameter filtering
3. Update `MetricsTargetClassEmitter` to use validator
4. Map error enums to actual diagnostic codes

### Future Enhancements
1. Add validation for parameter ordering (Activity must be first)
2. Add validation for attribute compatibility (Tag on correct types)
3. Add custom exclusion rules via attributes
4. Add performance optimizations (type caching)
5. Add more specific metrics observable type validation

## Testing

Run tests with:
```bash
dotnet test --filter "FullyQualifiedName~TelemetryMethodValidatorTests"
```

All 20+ tests should pass, covering:
- ? Return type validation for all targets
- ? Scoped logger validation
- ? Async return type validation
- ? Parameter exclusion for Activity types
- ? Parameter exclusion for ActivityContext
- ? Parameter exclusion for TagList
- ? Parameter exclusion for metrics measurements
- ? Multi-target validation
- ? Observable metrics return types
