# Telemetry Validation Rules - Complete Index

This directory contains comprehensive documentation for the telemetry method validation system.

## ?? Documentation Files

### Core Documentation
1. **[COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md)** ?
   - Complete return type validation matrix
   - All valid and invalid combinations
   - Parameter exclusion rules
   - Special rules and diagnostics
   - **START HERE** for complete reference

2. **[DECISION_TREE.md](DECISION_TREE.md)** ??
   - Visual decision tree diagrams
   - Priority ladder visualization
   - Quick compatibility matrix
   - Parameter exclusion flow
   - **USE THIS** for quick visual reference

3. **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** ?
   - Quick lookup tables
   - Common scenarios
   - Error codes
   - Testing patterns
   - **USE THIS** for day-to-day development

### Implementation Documentation
4. **[README.md](README.md)** ??
   - Usage guide and examples
   - Integration patterns
   - API documentation
   - **USE THIS** for integration

5. **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** ???
   - Architecture overview
   - Design decisions
   - Files created
   - Integration points
   - **USE THIS** for understanding the system

## ?? Quick Navigation

### By Use Case

| I want to... | Read this file |
|--------------|----------------|
| **Understand all validation rules** | [COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md) |
| **See visual flow diagrams** | [DECISION_TREE.md](DECISION_TREE.md) |
| **Quick rule lookup** | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| **Integrate the validator** | [README.md](README.md) |
| **Understand the architecture** | [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) |

### By Developer Role

| Role | Recommended Reading Order |
|------|---------------------------|
| **New Developer** | 1. QUICK_REFERENCE.md<br>2. DECISION_TREE.md<br>3. COMPLETE_VALIDATION_RULES.md |
| **Integrating Validator** | 1. README.md<br>2. IMPLEMENTATION_SUMMARY.md<br>3. COMPLETE_VALIDATION_RULES.md |
| **Writing Tests** | 1. QUICK_REFERENCE.md<br>2. COMPLETE_VALIDATION_RULES.md |
| **Debugging Issues** | 1. DECISION_TREE.md<br>2. COMPLETE_VALIDATION_RULES.md |

## ?? Validation Rule Summary

### Return Type Priority (Highest ? Lowest)
1. **IDisposable** (Scoped Logger) - Wins over everything
2. **Activity?** (Create/Start Activity) - Wins over bool and void
3. **Observable void enforcement** - Blocks bool returns
4. **bool** (Standard Metrics) - Wins over default void
5. **void** (Default) - Universal fallback

### Valid Return Types by Target

| Target | Valid Returns |
|--------|---------------|
| Scoped Log | `IDisposable` only |
| Non-Scoped Log | `void` only |
| Create/Start Activity | `Activity?` |
| Event/Context | `void` |
| Standard Metrics | `void` or `bool` |
| Observable Metrics | `void` (or `Activity?` if Activity present) |
| AutoCounter | `void` only |

### Key Rules

? **Always Valid Combinations:**
- Scoped Log + Event/Context
- Scoped Log + Void Metrics
- Scoped Log + Observable (Func<T> excluded from log)
- Activity + Log
- Activity + Void Metrics
- Activity + Observable
- Bool Metrics + Log/Event/Context
- Observable + Log/Event (both void)

? **Always Invalid Combinations:**
- Scoped Log + Create/Start Activity
- Scoped Log + Bool Metrics
- Activity + Bool Metrics (unless Observable present)
- Observable + Bool return
- **Task/ValueTask with any target** (async not supported)

### Parameter Exclusions

**Excluded from Logging:**
- `Activity`, `ActivityContext`, `ActivityLink`
- `TagList`
- `Func<T>` (Observable callbacks)
- `DateTimeOffset` (startTime)
- First numeric parameter (measurement value)

**Excluded from Metrics:**
- `Activity`, `ActivityContext`, `ActivityLink`
- `DateTimeOffset` (startTime)

**Excluded from Activities:**
- (None - Activities can use all parameters)

## ?? Finding Specific Information

### Return Type Questions
- **"Can I return X?"** ? [COMPLETE_VALIDATION_RULES.md - Return Type Matrix](COMPLETE_VALIDATION_RULES.md#complete-return-type-validation-matrix)
- **"Why can't I combine X and Y?"** ? [COMPLETE_VALIDATION_RULES.md - Invalid Combinations](COMPLETE_VALIDATION_RULES.md#-invalid-combinations)
- **"What wins: X or Y?"** ? [DECISION_TREE.md - Priority Ladder](DECISION_TREE.md#simplified-priority-ladder)

### Parameter Questions
- **"Is this parameter excluded?"** ? [COMPLETE_VALIDATION_RULES.md - Parameter Exclusion Rules](COMPLETE_VALIDATION_RULES.md#parameter-exclusion-rules)
- **"Where can I use this parameter?"** ? [DECISION_TREE.md - Parameter Exclusion Flow](DECISION_TREE.md#parameter-exclusion-flow)

### Implementation Questions
- **"How do I integrate?"** ? [README.md - Integration Examples](README.md#integration-examples)
- **"How does it work?"** ? [IMPLEMENTATION_SUMMARY.md - Design Decisions](IMPLEMENTATION_SUMMARY.md#design-decisions)
- **"How do I test?"** ? [QUICK_REFERENCE.md - Testing](QUICK_REFERENCE.md#testing)

## ?? Testing

All validation rules are covered by tests in:
- `TelemetryMethodValidatorTests.cs` - Unit tests for the validator
- `TelemetrySourceGeneratorTests.MultiGeneration.cs` - Integration tests for multi-target scenarios

## ?? Quick Start

### For Developers

```csharp
// 1. Create validator
var validator = new TelemetryMethodValidator(compilation);

// 2. Validate return type
var result = validator.ValidateReturnType(
    method.ReturnType,
    GenerationType.Activities | GenerationType.Logging,
    isScoped: false
);

if (!result.IsValid)
{
    // Report errors
    foreach (var error in result.Errors)
    {
        ReportDiagnostic(error.Error, error.Message);
    }
}

// 3. Filter parameters
var includedParams = method.Parameters
    .Where(p => validator
        .ShouldExcludeParameter(p, currentTarget, allTargets)
        .IsIncludedIn(currentTarget))
    .ToArray();
```

### For Validation Rule Lookups

```
Question: "Can Observable return bool?"
Answer: See COMPLETE_VALIDATION_RULES.md ? Observable Rules ? NO ?

Question: "What's the priority order?"
Answer: See DECISION_TREE.md ? Priority Ladder

Question: "Is Activity excluded from Logging?"
Answer: See QUICK_REFERENCE.md ? Parameter Exclusion Rules ? YES ?
```

## ?? Rule Statistics

- **Total Return Type Combinations**: 50+ documented
- **Valid Combinations**: 30+
- **Invalid Combinations**: 20+
- **Parameter Exclusion Rules**: 9
- **Diagnostic Codes**: 20+ (including invalid return types)
- **Test Cases**: 20+ (validator) + 35+ (integration, including 19 invalid return type tests)

## ?? Updates and Maintenance

When validation rules change:
1. Update [COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md) first
2. Update [DECISION_TREE.md](DECISION_TREE.md) visual diagrams
3. Update [QUICK_REFERENCE.md](QUICK_REFERENCE.md) lookup tables
4. Update implementation in `TelemetryMethodValidator.cs`
5. Add tests in `TelemetryMethodValidatorTests.cs`
6. Update this INDEX.md if structure changes

## ?? Document Relationships

```
INDEX.md (You are here)
    ?
    ???? COMPLETE_VALIDATION_RULES.md ? Complete reference
    ?       ?
    ?       ???? All return type rules
    ?       ???? All parameter rules
    ?       ???? All combination rules
    ?       ???? Decision algorithm
    ?
    ???? DECISION_TREE.md ? Visual reference
    ?       ?
    ?       ???? Decision flow diagrams
    ?       ???? Priority ladder
    ?       ???? Compatibility matrix
    ?
    ???? QUICK_REFERENCE.md ? Daily reference
    ?       ?
    ?       ???? Lookup tables
    ?       ???? Common scenarios
    ?       ???? Error codes
    ?
    ???? README.md ? Implementation guide
    ?       ?
    ?       ???? Usage examples
    ?       ???? Integration patterns
    ?       ???? API documentation
    ?
    ???? IMPLEMENTATION_SUMMARY.md ? Architecture
            ?
            ???? Design decisions
            ???? Files created
            ???? Integration points
```

## ?? Pro Tips

1. **Visual Learner?** Start with [DECISION_TREE.md](DECISION_TREE.md)
2. **Need Quick Answer?** Check [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
3. **Implementing Feature?** Read [README.md](README.md) + [COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md)
4. **Debugging Error?** Use [DECISION_TREE.md](DECISION_TREE.md) to trace validation flow
5. **Writing Tests?** Reference [COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md) for all cases

## ?? Learning Path

### Beginner
1. Read [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Get familiar with basic rules
2. Review [DECISION_TREE.md](DECISION_TREE.md) - Understand the flow
3. Practice with common scenarios from QUICK_REFERENCE

### Intermediate
1. Study [COMPLETE_VALIDATION_RULES.md](COMPLETE_VALIDATION_RULES.md) - Learn all rules
2. Read [README.md](README.md) - Understand integration
3. Review [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Understand architecture

### Advanced
1. Deep dive into `TelemetryMethodValidator.cs` - Study implementation
2. Review test cases in `TelemetryMethodValidatorTests.cs` - See edge cases
3. Contribute new validation rules with full documentation

---

**Last Updated**: 2024
**Maintainer**: Purview Telemetry Source Generator Team
**Version**: 3.2.4
