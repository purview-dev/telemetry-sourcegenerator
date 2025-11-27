# Task/ValueTask Removed as Valid Return Types ?

## Summary

Removed `Task` and `ValueTask` as valid return types for telemetry methods. They are now properly rejected as invalid types.

---

## ?? What Was Changed

### 1. Removed Constants
**File**: `Constants.System.cs`

Removed:
```csharp
public static readonly PurviewTypeInfo Task = PurviewTypeFactory.Create("System.Threading.Tasks.Task");
public static readonly PurviewTypeInfo ValueTask = PurviewTypeFactory.Create("System.Threading.Tasks.ValueTask");
```

### 2. Simplified Logging Validation
**File**: `PipelineHelpers.Logger.cs`

**Before** (incorrect):
```csharp
// Allowed void, Task, ValueTask, IDisposable
var isTask = Constants.System.Task.Equals(method.ReturnType);
var isValueTask = Constants.System.ValueTask.Equals(method.ReturnType);
if (!isVoid && !isTask && !isValueTask) { /* error */ }
```

**After** (correct):
```csharp
// Only allow void and IDisposable - everything else is invalid
var isVoid = method.ReturnsVoid;
var isIDisposable = Constants.System.IDisposable.Equals(method.ReturnType);

if (isVoid || isIDisposable) {
    return null; // Valid
}

// Everything else (including Task/ValueTask) is invalid
return diagnostic;
```

### 3. Updated Diagnostic Messages
**File**: `TelemetryDiagnostics.Logging.cs`

- **TSG2021**: Updated to clarify Task/ValueTask are **not** supported
  - Old: "must return void, Task, or ValueTask"
  - New: "can only return void (non-scoped) or IDisposable (scoped)"

- **TSG2022**: Clarified async types are not supported
  - New: "Logging methods cannot return Task or ValueTask"

### 4. Added Integration Tests
**File**: `TelemetrySourceGeneratorTests.InvalidReturnTypes.cs`

Added 3 new tests:
```csharp
// Test Task being rejected
[Log]
Task InvalidTaskReturn(string message);  // ? TSG2021

// Test ValueTask being rejected  
[Log]
ValueTask InvalidValueTaskReturn(string message);  // ? TSG2021

// Test Task<T> being rejected
[Log]
Task<int> InvalidAsyncReturnType(string message);  // ? TSG2021
```

### 5. Updated Existing Tests
**File**: `TelemetrySourceGeneratorTests.MultiGeneration.cs`

Changed test from expecting success to expecting diagnostics:
```csharp
// Old name: Generate_GivenAsyncMethodsInMultiTarget_GeneratesCorrectly
// New name: Generate_GivenAsyncMethodsInMultiTarget_RaisesDiagnostics

[Log]
Task LogOperationAsync(...);    // ? TSG2021

[Info]  
ValueTask InfoAsync(...);       // ? TSG2021
```

### 6. Updated Documentation
**File**: `COMPLETE_VALIDATION_RULES.md`

Updated validation matrix to show Task/ValueTask as **invalid**:

| Target | Valid Returns | Invalid Returns |
|--------|--------------|-----------------|
| **Non-Scoped Logger** | `void` only | Task, ValueTask, Task<T>, ValueTask<T>, ... |
| **Scoped Logger** | `IDisposable` only | void, Task, ValueTask, ... |

---

## ? Valid Return Types (Final)

### Logging
- **Non-scoped**: `void` ?
- **Scoped**: `IDisposable` ?

### Activities
- `Activity`, `Activity?` ?
- `void` (for Event/Context) ?

### Metrics
- `void` ?
- `bool` (standard metrics only) ?

---

## ? Invalid Return Types

**All of these now correctly raise TSG2021**:
- `Task` ?
- `ValueTask` ?
- `Task<T>` ?
- `ValueTask<T>` ?
- `string` ?
- `int` ?
- `bool` (for logging) ?
- `object` ?
- Any custom type ?

---

## ?? Verification

### Build Status
? **Build successful**

### Tests Updated
- Added 3 new tests for Task/ValueTask
- Updated 1 existing test to expect diagnostics
- Total: **19 tests** covering invalid return types

### Validation Logic
The new approach is simpler and more correct:

```csharp
// Allowlist approach - only these are valid:
if (isVoid || isIDisposable) {
    return null; // ? Valid
}

// Everything else is invalid
return diagnostic; // ? Invalid (including Task/ValueTask)
```

---

## ?? Key Takeaways

1. ? **Task/ValueTask are NOT valid** - They are now properly rejected
2. ? **Simpler validation** - Allowlist approach makes intent clear
3. ? **Better diagnostics** - Clear messages that async types aren't supported
4. ? **Comprehensive tests** - All async types tested and rejected
5. ? **Updated documentation** - Clearly shows what's valid/invalid

---

## ?? Result

**Telemetry methods are now strictly synchronous**:
- Logging: `void` or `IDisposable` only
- Activities: `Activity?` or `void` (Event/Context)
- Metrics: `void` or `bool` (standard only)

Any attempt to use `Task`, `ValueTask`, or any other async type will raise **TSG2021** with a clear error message.

---

**Implementation Date**: 2024
**Status**: ? Complete
**Build**: ? Passing
**Tests**: ? 19 tests covering invalid return types
