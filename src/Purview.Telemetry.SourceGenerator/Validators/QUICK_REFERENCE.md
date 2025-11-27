# Telemetry Method Validation Rules - Quick Reference

## Return Type Rules

### ? Valid Return Types

| Target | Scoped | Valid Types |
|--------|--------|-------------|
| **Logging** | No | `void` only |
| **Logging** | Yes | `IDisposable` only |
| **Activities** | N/A | `Activity`, `Activity?` (create/start), `void` (event/context) |
| **Metrics** | N/A | `void`, `bool` (standard only) |
| **Observable Metrics** | N/A | `void` only |

### ? Invalid Examples

```csharp
// ? Logging returning Task/ValueTask
[Log]
Task InvalidAsync(string message);  // ERROR: Task not supported

// ? Scoped logger returning void
[Log(IsScoped = true)]
void InvalidScope(string message);  // ERROR: Must return IDisposable

// ? Activity returning string
[Activity]
string InvalidActivity();  // ERROR: Must return Activity or Activity?

// ? Observable returning bool
[ObservableCounter]
bool InvalidObservable(Func<int> callback);  // ERROR: Observable cannot return bool

// ? AutoCounter returning bool
[AutoCounter]
bool InvalidAutoCounter();  // ERROR: AutoCounter must return void
```

## Parameter Exclusion Rules

### Automatic Exclusions

| Parameter Type | ? Excluded From | ? Included In | Reason |
|---------------|-----------------|---------------|---------|
| `Activity` | Logging, Metrics | Activities | Activity-specific parameter |
| `Activity?` | Logging, Metrics | Activities | Activity-specific parameter |
| `ActivityContext` | Logging, Metrics | Activities | Activity-specific parameter |
| `ActivityLink` | Logging, Metrics | Activities | Activity-specific parameter |
| `IEnumerable<ActivityLink>` | Logging, Metrics | Activities | Activity-specific parameter |
| `TagList` | Logging | Activities, Metrics | Activity/Metrics-specific parameter |
| First numeric param (metrics) | Logging | Metrics | Measurement value |

### Examples

```csharp
// Multi-target interface
[ActivitySource("source")]
[Logger]
public interface IMultiTelemetry
{
    // Event method
    [Event]
    void RecordEvent(
        Activity? activity,     // ? Excluded from Logging
        string eventName,       // ? Included in both
        int userId              // ? Included in both
    );
    
    // The generated logging method will only have: eventName, userId
    // The generated activity method will have: activity, eventName, userId
}

// Metrics + Logging
[Meter("meter")]
[Logger]
public interface IMetricsLogger
{
    [Counter]
    [Log]
    void CountOperation(
        int counterValue,       // ? Excluded from Logging (metrics measurement)
        string operation,       // ? Included in both
        string user             // ? Included in both
    );
    
    // The generated logging method will have: operation, user
    // The generated metrics method will have: counterValue, operation, user
}
```

## Decision Flow

### Return Type Validation Flow

```
Method Declaration
    ?
    ? Check Target(s)
    ?   ?
    ?   ? Logging?
    ?   ?   ? Scoped? ? Must be IDisposable
    ?   ?   ? Not Scoped ? Must be void
    ?   ?
    ?   ? Activities?
    ?   ?   ? Create/Start ? Must be Activity or Activity?
    ?   ?   ? Event/Context ? Must be void
    ?   ?
    ?   ? Metrics?
    ?       ? Observable ? Must be void
    ?       ? Standard ? Must be void or bool
    ?       ? AutoCounter ? Must be void
    ?
    ? Return Combined Results
```

### Parameter Exclusion Flow

```
For Each Parameter
    ?
    ?? Check Type
    ?   ?
    ?   ?? Activity/ActivityContext/ActivityLink?
    ?   ?   ?? Current Target = Logging ? ? Exclude
    ?   ?   ?? Current Target = Metrics ? ? Exclude
    ?   ?   ?? Current Target = Activities ? ? Include
    ?   ?
    ?   ?? TagList?
    ?   ?   ?? Current Target = Logging ? ? Exclude
    ?   ?   ?? Otherwise ? ? Include
    ?   ?
    ?   ?? Metrics Measurement Value?
    ?   ?   ?? Current Target = Logging ? ? Exclude
    ?   ?   ?? Otherwise ? ? Include
    ?   ?
    ?   ?? Other Type ? ? Include
    ?
    ?? Return Exclusion Result
```

## Common Scenarios

### ? Valid Multi-Target Methods

```csharp
// Different methods for different targets - RECOMMENDED
[ActivitySource("source")]
[Logger]
public interface IMulti
{
    [Activity]
    Activity? StartOperation(string id);
    
    [Log]
    void LogOperation(string id, string message);
}

// Same method, different targets - automatic filtering applies
[ActivitySource("source")]
[Logger]
public interface IMulti
{
    [Event]
    [Log]
    void RecordEvent(
        Activity? activity,  // Auto-excluded from Logging
        string eventName
    );
}
```

### ? Invalid Scenarios

```csharp
// ? Multiple telemetry attributes on one method
[Activity]
[Log]
void Invalid(string message);  // Error: TSG1002

// ? No attribute in multi-target (inference not supported)
[ActivitySource("source")]
[Logger]
public interface IMulti
{
    void NoAttribute(string msg);  // Error: TSG1001
}

// ? Wrong return type for scoped logger
[Log(IsScoped = true)]
void InvalidScope(string msg);  // Error: Must return IDisposable
```

## Validation in Code

### Quick Check Pattern

```csharp
var validator = new TelemetryMethodValidator(compilation);

// 1. Validate return type
var returnOk = validator
    .ValidateReturnType(method.ReturnType, targetType, isScoped)
    .IsValid;

if (!returnOk) 
{
    // Report error
    return;
}

// 2. Filter parameters
var parameters = method.Parameters
    .Where(p => validator
        .ShouldExcludeParameter(p, currentTarget, allTargets)
        .IsIncludedIn(currentTarget))
    .ToArray();

// 3. Generate with filtered parameters
EmitMethod(method, parameters);
```

## Error Codes

| Code | Error | Fix |
|------|-------|-----|
| `ScopedLoggerMustReturnIDisposable` | Scoped logger not returning IDisposable | Change return type to IDisposable |
| `InvalidLoggingReturnType` | Invalid logging return type | Use void, Task, or ValueTask |
| `InvalidActivityReturnType` | Invalid activity return type | Use Activity or void |
| `InvalidMetricsReturnType` | Invalid metrics return type | Use void or observable types |
| `ActivityParameterNotAllowedInLogging` | Activity param in logging | Parameter auto-excluded (informational) |
| `ActivityParameterNotAllowedInMetrics` | Activity param in metrics | Parameter auto-excluded (informational) |

## Testing

### Test Your Validation

```csharp
[Test]
public void MyValidator_Test()
{
    const string source = @"
        using System;
        namespace Test {
            public interface ITest {
                void MyMethod(string message);
            }
        }";
    
    var compilation = CreateCompilation(source);
    var method = GetMethodSymbol(compilation, "MyMethod");
    var validator = new TelemetryMethodValidator(compilation);
    
    var result = validator.ValidateReturnType(
        method.ReturnType,
        GenerationType.Logging
    );
    
    Assert.IsTrue(result.IsValid);
}
