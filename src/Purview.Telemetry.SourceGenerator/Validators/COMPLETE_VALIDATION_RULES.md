# Complete Telemetry Method Validation Rules

## Return Type Priority Hierarchy

The validation follows a **strict priority order** from highest to lowest:

```
1. IDisposable (Scoped Logger)     ? HIGHEST PRIORITY - Wins over everything
2. Activity?                        ? Wins over bool and void
3. bool (Standard Metrics)          ? Wins over void, BLOCKED by Observable
4. void (Default)                   ? LOWEST PRIORITY - Universal fallback
```

### Special Rule: Observable Metrics

**Observable metrics have unique constraints:**
- ? Can return: `void` OR `Activity?` (when Activity present)
- ? Cannot return: `bool` (explicitly blocked)
- ? Cannot return: `IDisposable` (incompatible, but Func<T> excluded from Scoped)

**Observable fits BETWEEN Activity and bool in the priority chain:**
- Loses to: IDisposable, Activity
- Blocks: bool returns
- Allows: void as default

---

## Complete Return Type Validation Matrix

### Single Target Scenarios

| Target | Attribute(s) | Valid Returns | Invalid Returns |
|--------|-------------|---------------|-----------------|
| **Scoped Logger** | `[Log(IsScoped = true)]` | `IDisposable` only | `void`, `bool`, `Activity?`, `Task`, `ValueTask` |
| **Non-Scoped Logger** | `[Log]`, `[Trace]`, etc. | `void` only | `bool`, `Activity?`, `IDisposable`, `Task`, `ValueTask`, `Task<T>`, `ValueTask<T>` |
| **Create/Start Activity** | `[Activity]` | `Activity?` (nullable allowed) | `void`, `bool`, `IDisposable`, `Task` |
| **Event/Context** | `[Event]`, `[Context]` | `void` | `Activity?`, `bool`, `IDisposable`, `Task` |
| **Standard Metrics** | `[Counter]`, `[Histogram]` | `void` OR `bool` | `Activity?`, `IDisposable`, `Task` |
| **Observable Metrics** | `[ObservableCounter]`, etc. | `void` only (when alone) | `bool`, `IDisposable`, `Task` |
| **AutoCounter** | `[AutoCounter]` | `void` only | `bool`, `Activity?`, `IDisposable`, `Task` |

---

## Multi-Target Combination Rules

### ? Valid Combinations

#### Scoped Logger Combinations

```csharp
// ? Scoped + Event/Context (Scoped wins - returns IDisposable)
[Log(IsScoped = true)]
[Event]
IDisposable ScopedEvent(Activity? activity, string message);
// Returns: IDisposable
// Note: Activity parameter excluded from logging

// ? Scoped + Void Metrics (Scoped wins - returns IDisposable)
[Log(IsScoped = true)]
[Counter]
IDisposable ScopedCounter(int counterValue, string operation);
// Returns: IDisposable
// Note: counterValue excluded from logging

// ? Scoped + Observable (Scoped wins, Func<T> excluded from logging)
[Log(IsScoped = true)]
[ObservableGauge]
IDisposable ScopedObservable(Func<double> callback, string context);
// Returns: IDisposable
// Note: Func<double> callback excluded from logging
```

#### Activity Combinations

```csharp
// ? Activity + Log (Activity wins - returns Activity?)
[Activity]
[Log]
Activity? TrackAndLog(string operationId, string message);
// Returns: Activity?
// Both targets use all parameters

// ? Activity + Void Metrics (Activity wins - returns Activity?)
[Activity]
[Counter]
Activity? TrackAndCount(int counterValue, string operationId);
// Returns: Activity?
// Note: counterValue excluded from Activity

// ? Activity + Observable (Activity wins - returns Activity?)
[Activity]
[ObservableCounter]
Activity? TrackAndObserve(Func<int> callback, string operationId);
// Returns: Activity?
// Note: Func<int> excluded from Activity, excluded from any Log if present
```

#### Bool Metrics Combinations

```csharp
// ? Bool Metrics + Log (Bool wins - returns bool)
[Counter]
[Log]
bool CountAndLog(int counterValue, string operation);
// Returns: bool
// Note: counterValue excluded from logging

// ? Bool Metrics + Event (Bool wins - returns bool)
[Counter]
[Event]
bool CountAndEvent(Activity? activity, int counterValue, string message);
// Returns: bool
// Note: Activity excluded from metrics, counterValue excluded from event
```

#### Observable Combinations

```csharp
// ? Observable + Log (both void)
[ObservableGauge]
[Log]
void ObserveAndLog(Func<double> callback, string context);
// Returns: void
// Note: Func<double> excluded from logging

// ? Observable + Event (both void)
[ObservableCounter]
[Event]
void ObserveAndEvent(Activity? activity, Func<int> callback, string message);
// Returns: void
// Note: Activity excluded from metrics, Func<int> excluded from event

// ? Observable + Activity (Activity wins)
[ObservableUpDownCounter]
[Activity]
Activity? ObserveAndTrack(Func<int> callback, string operationId);
// Returns: Activity?
// Note: Func<int> excluded from Activity

// ? Observable + Activity + Log (Activity wins)
[ObservableGauge]
[Activity]
[Log]
Activity? ObserveTrackAndLog(Func<double> callback, string operationId);
// Returns: Activity?
// Note: Func<double> excluded from Activity and Log
```

#### Void Combinations (All Void)

```csharp
// ? Event + Log + Void Metrics (all void)
[Event]
[Log]
[Counter]
void EventLogAndCount(Activity? activity, int counterValue, string message);
// Returns: void
// Note: Activity excluded from metrics/log, counterValue excluded from event/log
```

---

### ? Invalid Combinations

#### Scoped Logger Conflicts

```csharp
// ? Scoped + Create/Start Activity - INCOMPATIBLE
[Log(IsScoped = true)]
[Activity]
??? InvalidCombo(string operationId);
// ERROR: Cannot combine scoped logger (IDisposable) with Activity creation (Activity?)

// ? Scoped + Bool Metrics - INCOMPATIBLE
[Log(IsScoped = true)]
[Counter]
??? InvalidCombo(int counterValue);
// ERROR: Cannot combine scoped logger (IDisposable) with bool metrics (bool)
```

#### Observable Conflicts

```csharp
// ? Observable + Bool Return - BLOCKED
[ObservableCounter]
bool InvalidObservable(Func<int> callback);
// ERROR: Observable metrics cannot return bool

// ? Observable + Bool Metrics (non-observable) - CONFLICT
[ObservableCounter]
[Counter] // This Counter wants bool
bool InvalidCombo(Func<int> observableCallback, int counterValue);
// ERROR: Observable blocks bool return
```

#### Activity Conflicts

```csharp
// ? Activity + Bool Metrics (no Observable) - CONFLICT
[Activity]
[Counter] // This Counter wants bool
??? InvalidCombo(int counterValue, string operationId);
// ERROR: Activity requires Activity?, bool metrics wants bool
```

#### Invalid Return Types

```csharp
// ? Task/ValueTask are not supported
[Log]
Task InvalidAsync(string message);
// ERROR: Task is not a valid return type

[Counter]
ValueTask InvalidAsync(int counterValue);
// ERROR: ValueTask is not a valid return type

// ? Any other return type
[Counter]
string InvalidType(int counterValue);
// ERROR: Only void, bool, Activity?, or IDisposable are valid
```

---

## Parameter Exclusion Rules

### Automatic Parameter Exclusions

| Parameter Type | Excluded From | Reason | Included In |
|---------------|---------------|---------|-------------|
| `Activity` or `Activity?` | Logging, Metrics | Activity-specific | Activities |
| `ActivityContext` | Logging, Metrics | Activity-specific | Activities |
| `ActivityLink` / `IEnumerable<ActivityLink>` | Logging, Metrics | Activity-specific | Activities |
| `TagList` | Logging | Activity/Metrics-specific | Activities, Metrics |
| `DateTimeOffset` (startTime) | Logging, Metrics | Activity-specific | Activities |
| `Func<T>` | Logging, Activities | Metrics observable callback | Metrics (observable) |
| `Func<Measurement<T>>` | Logging, Activities | Metrics observable callback | Metrics (observable) |
| `Func<IEnumerable<Measurement<T>>>` | Logging, Activities | Metrics observable callback | Metrics (observable) |
| First numeric parameter | Logging | Metrics measurement value | Metrics, Activities |

### Parameter Exclusion Examples

```csharp
// Example 1: Activity parameter excluded from Logging
[Event]
[Log]
void RecordEvent(
    Activity? activity,     // ? Excluded from Logging
    string eventName,       // ? Included in both
    int userId              // ? Included in both
);

// Example 2: Func<T> excluded from Logging and Activity
[ObservableCounter]
[Activity]
[Log]
Activity? ObserveTrackAndLog(
    Func<int> callback,     // ? Excluded from Activity and Log
    string operationId,     // ? Included in all
    string context          // ? Included in all
);

// Example 3: Measurement value excluded from Logging
[Counter]
[Log]
bool CountAndLog(
    int counterValue,       // ? Excluded from Logging
    string operation,       // ? Included in both
    string user             // ? Included in both
);
```

---

## Special Rules and Diagnostics

### Event/Context Activity Parameter (TSG3002)

**Event and Context methods generate an INFO diagnostic if missing Activity parameter:**

```csharp
// INFO: TSG3002 - Activity parameter recommended
[Event]
void RecordEvent(string eventName);
// INFO: Event/Context method should have Activity? parameter.
//       Consider adding it or use Activity.Current internally.

// No diagnostic - Activity parameter present
[Event]
void RecordEvent(Activity? activity, string eventName);
// ? Activity parameter present (best practice)
```

**Key Points:**
- Activity parameter is **not required** (can use `Activity.Current`)
- Generates **INFO** diagnostic (TSG3002), not an error
- User can ignore and continue with Activity.Current

### AutoCounter Rules

```csharp
// ? AutoCounter - always returns void
[AutoCounter]
void IncrementCounter(string operation);
// Returns: void
// No measurement parameter needed

// ? AutoCounter with bool - INVALID
[AutoCounter]
bool InvalidAutoCounter(string operation);
// ERROR: AutoCounter must return void

// ? AutoCounter with measurement param - INVALID
[AutoCounter]
void InvalidAutoCounter(int counterValue, string operation);
// ERROR: AutoCounter cannot have measurement parameter
```

---

## Return Type Decision Algorithm

```
VALIDATE_RETURN_TYPE(method, allTargets):

1. CHECK: Has Scoped Log?
   YES ? 
     IF returnType == IDisposable: VALID
     IF hasActivity OR hasBoolMetrics: ERROR (Incompatible)
     ELSE: ERROR (Must be IDisposable)
   NO ? Continue...

2. CHECK: Has Create/Start Activity?
   YES ?
     IF returnType == Activity OR Activity?: 
       IF hasObservable: VALID (Observable allows Activity)
       IF hasBoolMetrics AND NOT hasObservable: ERROR (Conflict)
       ELSE: VALID
     ELSE: ERROR (Must return Activity?)
   NO ? Continue...

3. CHECK: Has Observable Metrics?
   YES ?
     IF returnType == void: VALID
     IF returnType == Activity?: 
       IF hasActivity: VALID (Already validated above)
       ELSE: ERROR (Activity? requires [Activity] attribute)
     IF returnType == bool: ERROR (Observable blocks bool)
     IF returnType == IDisposable: ERROR (Incompatible)
     ELSE: ERROR (Must be void or Activity?)
   NO ? Continue...

4. CHECK: Has Bool Metrics (non-observable)?
   YES ?
     IF returnType == bool: VALID
     ELSE: ERROR (Must return bool)
   NO ? Continue...

5. DEFAULT: Check void only
   IF returnType == void: VALID
   ELSE: ERROR (Invalid return type - only void, bool, Activity?, or IDisposable allowed)
```

---

## Complete Validation Checklist

### For Scoped Loggers
- ? Returns `IDisposable`
- ? Not combined with `[Activity]` (Create/Start)
- ? Not combined with bool-returning metrics
- ? Can combine with `[Event]`, `[Context]`
- ? Can combine with void-returning metrics
- ? Can combine with Observable (Func<T> excluded from log)

### For Activities (Create/Start)
- ? Returns `Activity` or `Activity?`
- ? Not combined with Scoped Log
- ? Not combined with bool metrics (unless Observable present)
- ? Can combine with non-scoped Log
- ? Can combine with void metrics
- ? Can combine with Observable metrics

### For Event/Context
- ? Returns `void`
- ?? Should have `Activity?` parameter (TSG3002 if missing)
- ? Can combine with anything that returns void
- ? Can combine with bool metrics (bool wins)
- ? Can combine with Scoped Log (IDisposable wins)

### For Observable Metrics
- ? Returns `void` (default) OR `Activity?` (with Activity)
- ? Cannot return `bool`
- ? Cannot return `IDisposable` (but Func<T> excluded from Scoped Log)
- ? Requires `Func<T>` parameter
- ? Can combine with Activity (Activity? wins)
- ? Can combine with Log (void default)
- ? Can combine with Event/Context (void default)

### For Standard Metrics
- ? Returns `void` OR `bool`
- ? Cannot return with Observable (Observable blocks bool)
- ? Can combine with Log (bool wins if present)
- ? Can combine with Event/Context (bool wins if present)
- ? Cannot combine with Activity if bool (conflict)

### For Logging
- ? Returns `void` (non-scoped)
- ? Returns `IDisposable` (scoped)
- ? Can combine with virtually anything (flexible return)
- ? Activity parameters excluded
- ? Func<T> callbacks excluded
- ? Measurement values excluded

---

## Priority Summary

**Return Type Priority (Highest to Lowest):**
1. **IDisposable** (Scoped) - Absolute winner
2. **Activity?** (Activities) - Wins over metrics
3. **Observable void enforcement** - Blocks bool
4. **bool** (Metrics) - Wins over default void
5. **void** (Default) - Universal fallback

**When checking return types, validate in this exact order to ensure correct precedence.**
