# Invalid Return Type Validation - Implementation Complete ?

## Summary

Successfully implemented comprehensive return type validation for all telemetry targets with 5 new diagnostic codes and 15 integration tests.

---

## ?? What Was Implemented

### 1. New Diagnostic Codes Added

#### Logging Diagnostics (TSG2020-2022)
- **TSG2020**: `ScopedLogMustReturnIDisposable`
  - **Severity**: Error
  - **When**: Scoped logger doesn't return IDisposable
  - **Message**: "Scoped logging methods must return IDisposable to properly manage the log scope lifetime."

- **TSG2021**: `LogMustReturnVoidOrAsync`
  - **Severity**: Error
  - **When**: Non-scoped logger returns invalid type
  - **Message**: "Non-scoped logging methods can only return void, Task, or ValueTask. Other return types like string, int, bool, Activity, or IDisposable are not supported."

- **TSG2022**: `InvalidAsyncReturnType`
  - **Severity**: Error
  - **When**: Log method returns `Task<T>` or `ValueTask<T>`
  - **Message**: "Async logging methods must return Task or ValueTask (non-generic). Generic Task<T> or ValueTask<T> are not supported."

#### Metrics Diagnostics (TSG4007-4008)
- **TSG4007**: `ObservableCannotReturnBool`
  - **Severity**: Error
  - **When**: Observable metric returns bool
  - **Message**: "Observable metrics can only return void or Activity? (when combined with Activity attribute). Boolean returns are not supported for observables."

- **TSG4008**: `AutoCounterMustReturnVoid`
  - **Severity**: Error
  - **When**: AutoCounter returns non-void
  - **Message**: "AutoCounter methods must return void. Boolean or other return types are not supported."

### 2. New Constants Added

Added Task and ValueTask type constants to `Constants.System`:
```csharp
public static readonly PurviewTypeInfo Task = PurviewTypeFactory.Create("System.Threading.Tasks.Task");
public static readonly PurviewTypeInfo ValueTask = PurviewTypeFactory.Create("System.Threading.Tasks.ValueTask");
```

### 3. Validation Logic Implemented

#### Logging Validation (`PipelineHelpers.Logger.cs`)
Added `ValidateLogReturnType()` method that:
- ? Detects scoped logs by IDisposable return type
- ? Validates non-scoped logs return void, Task, or ValueTask
- ? Detects and rejects generic `Task<T>` / `ValueTask<T>`
- ? Reports specific error diagnostics (TSG2020-2022)

#### Metrics Validation (`PipelineHelpers.Metrics.cs`)
Enhanced existing validation to:
- ? Specifically detect Observable + bool return (TSG4007)
- ? Specifically detect AutoCounter + non-void return (TSG4008)
- ? Fall back to generic validation (TSG4001) for other cases

### 4. Comprehensive Integration Tests

Created `TelemetrySourceGeneratorTests.InvalidReturnTypes.cs` with **15 test cases**:

| Test | Target | Invalid Return | Diagnostic |
|------|--------|---------------|------------|
| 1 | Log | `string` | TSG2021 |
| 2 | Counter | `int` | TSG4001 |
| 3 | Activity | `object` | TSG3002 |
| 4 | Log | `Task<int>` | TSG2022 |
| 5 | Counter | `Task` | TSG4001 |
| 6 | Activity | `Task<Activity?>` | TSG3002 |
| 7 | Log (Scoped) | `void` | TSG2020 |
| 8 | ObservableCounter | `bool` | TSG4007 |
| 9 | AutoCounter | `bool` | TSG4008 |
| 10 | Event | `Activity?` | TSG3002 |
| 11 | Context | `bool` | TSG3002 |
| 12 | Log | `bool` | TSG2021 |
| 13 | Log | `Activity?` | TSG2021 |
| 14 | Log (Scoped) | `Task` | TSG2020 |
| 15 | Log | `ValueTask<string>` | TSG2022 |
| 16 | Counter | `IDisposable` | TSG4001 |

---

## ?? Valid Return Types Reference

### Activities
| Attribute | Valid Returns | Notes |
|-----------|--------------|-------|
| `[Activity]` | `Activity`, `Activity?` | Nullable allowed |
| `[Event]` | `void` | Only |
| `[Context]` | `void` | Only |

### Logging
| Attribute | Valid Returns | Notes |
|-----------|--------------|-------|
| `[Log]` | `void`, `Task`, `ValueTask` | Non-scoped |
| `[Log(IsScoped = true)]` | `IDisposable` | Scoped only |
| All levels | Same as `[Log]` | Trace, Debug, Info, etc. |

### Metrics
| Attribute | Valid Returns | Notes |
|-----------|--------------|-------|
| `[Counter]` | `void`, `bool` | Standard |
| `[AutoCounter]` | `void` | Only |
| `[Histogram]` | `void`, `bool` | Standard |
| `[UpDownCounter]` | `void`, `bool` | Standard |
| `[ObservableCounter]` | `void` | Cannot return bool |
| `[ObservableGauge]` | `void` | Cannot return bool |
| `[ObservableUpDownCounter]` | `void` | Cannot return bool |

---

## ?? Invalid Return Types (Examples)

All of these now raise appropriate diagnostics:

```csharp
// ? TSG2021 - Log returning string
[Log]
string InvalidLog(string message);

// ? TSG2022 - Log returning Task<int>
[Log]
Task<int> InvalidAsync(string message);

// ? TSG2020 - Scoped log returning void
[Log(IsScoped = true)]
void InvalidScoped(string message);

// ? TSG4007 - Observable returning bool
[ObservableCounter]
bool InvalidObservable(Func<int> callback);

// ? TSG4008 - AutoCounter returning bool
[AutoCounter]
bool InvalidAutoCounter();

// ? TSG4001 - Counter returning Task
[Counter]
Task InvalidAsync(int value);

// ? TSG3002 - Activity returning object
[Activity]
object InvalidActivity(string name);
```

---

## ?? Testing

### Run All Tests
```bash
dotnet test Purview.Telemetry.SourceGenerator.IntegrationTests
```

### Run Just Invalid Return Type Tests
```bash
dotnet test --filter "FullyQualifiedName~InvalidReturnTypes"
```

### Expected Results
- **16 new tests** in `TelemetrySourceGeneratorTests.InvalidReturnTypes.cs`
- All tests validate correct diagnostic codes are raised
- All tests verify error messages are helpful

---

## ?? Implementation Details

### Validation Flow

```
Method Detected
    ?
Validate Multi-Target (TSG1001, TSG1002)
    ?
Validate Return Type ? NEW!
    ?? Logging: ValidateLogReturnType()
    ?   ?? Scoped: Must be IDisposable (TSG2020)
    ?   ?? Non-Scoped: Must be void/Task/ValueTask (TSG2021)
    ?   ?? Generic Task<T>: Rejected (TSG2022)
    ?
    ?? Metrics: Enhanced validation
        ?? Observable + bool: Rejected (TSG4007)
        ?? AutoCounter + non-void: Rejected (TSG4008)
        ?? Other: Generic check (TSG4001)
    ?
Generate Code or Skip on Error
```

### Key Design Decisions

1. **Scoped Detection**: Determined by return type (`IDisposable`), not attribute property
2. **Specific Diagnostics**: Observable and AutoCounter get their own error codes for clarity
3. **Generic Task Detection**: Check `ConstructedFrom` to identify `Task<T>` vs `Task`
4. **Early Validation**: Happens before parameter validation to fail fast

---

## ?? Files Modified

### Source Generator
1. `TelemetryDiagnostics.Logging.cs` - Added TSG2020-2022
2. `TelemetryDiagnostics.Metrics.cs` - Added TSG4007-4008
3. `Constants.System.cs` - Added Task and ValueTask constants
4. `PipelineHelpers.Logger.cs` - Added `ValidateLogReturnType()` method
5. `PipelineHelpers.Metrics.cs` - Enhanced return type validation

### Tests
6. `TelemetrySourceGeneratorTests.InvalidReturnTypes.cs` - **NEW** 16 comprehensive tests

### Documentation
7. `INVALID_RETURN_TYPES_STATUS.md` - Implementation status (created earlier)
8. `COMPLETE_VALIDATION_RULES.md` - Updated (created earlier)

---

## ? Validation Checklist

- [x] Diagnostic codes added (5 new codes)
- [x] Constants added (Task, ValueTask)
- [x] Logging validation implemented
- [x] Metrics validation enhanced
- [x] Integration tests created (16 tests)
- [x] Build successful
- [x] All existing tests pass
- [x] Documentation updated

---

## ?? Summary

**All invalid return types now properly diagnosed with clear, actionable error messages!**

The implementation provides:
- **5 new diagnostic codes** (TSG2020-2022, TSG4007-4008)
- **16 comprehensive integration tests**
- **Early validation** before code generation
- **Specific error messages** for common mistakes
- **Full coverage** of all telemetry targets

Users will now get immediate, clear feedback when they use unsupported return types, making the source generator more robust and user-friendly!

---

**Implementation Date**: 2024
**Status**: ? Complete
**Build**: ? Passing
**Tests**: ? 16 new tests passing
