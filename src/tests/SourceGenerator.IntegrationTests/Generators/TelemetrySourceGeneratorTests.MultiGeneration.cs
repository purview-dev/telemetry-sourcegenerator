using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Tests for multi-generation targeting scenarios where a single interface
/// supports multiple telemetry types (Activities, Logging, Metrics).
/// </summary>
public partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenActivitiesAndLogging_GeneratesBothCorrectly(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	void LogOperation(string operationId, string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
	}

	[Test]
	public async Task Generate_GivenActivitiesAndMetrics_GeneratesBothCorrectly(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Counter]
	void IncrementCounter(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("IncrementCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
	}

	[Test]
	public async Task Generate_GivenLoggingAndMetrics_GeneratesBothCorrectly(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Log]
	void LogOperation(string operationId, string message);

	[Counter]
	void IncrementCounter(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("IncrementCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
	}

	[Test]
	public async Task Generate_GivenAllThreeTypes_GeneratesAllCorrectly(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	void LogOperation(string operationId, string message);

	[Counter]
	void IncrementCounter(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("IncrementCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
	}

	[Test]
	public async Task Generate_GivenMethodWithMultipleTargetAttributes_GeneratesAllTargets(
		CancellationToken cancellationToken
	)
	{
		// Arrange - v4.0 supports multi-targeting: a single method can have multiple telemetry attributes
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	[Log]
	System.Diagnostics.Activity? TraceAndLogMethod(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert - should generate both activity and logging for this method
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("TraceAndLogMethod"))
			.IsTrue()
			.Because("the generated class must contain the multi-target method");
	}

	[Test]
	public async Task Generate_GivenMethodWithActivityAndMetricAttributes_GeneratesBothTargets(
		CancellationToken cancellationToken
	)
	{
		// Arrange - v4.0 supports multi-targeting
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	[Counter]
	System.Diagnostics.Activity? TraceAndCountMethod(int counterValue, [Tag]string operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert - should generate both activity and counter for this method
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("TraceAndCountMethod"))
			.IsTrue()
			.Because("the generated class must contain the multi-target method");
	}

	[Test]
	public async Task Generate_GivenMethodWithLoggingAndMetricAttributes_GeneratesBothTargets(
		CancellationToken cancellationToken
	)
	{
		// Arrange - v4.0 supports multi-targeting
		const string multiGen = """


namespace Testing;

[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Log]
	[Counter]
	void LogAndCountMethod(int value, string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert - should generate both logging and counter for this method
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("LogAndCountMethod"))
			.IsTrue()
			.Because("the generated class must contain the multi-target method");
	}

	[Test]
	public async Task Generate_GivenMethodWithAllThreeAttributes_GeneratesAllTargets(
		CancellationToken cancellationToken
	)
	{
		// Arrange - v4.0 supports multi-targeting
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	[Log]
	[Counter]
	System.Diagnostics.Activity? FullTelemetryMethod(int counterValue, [Tag]string operationId, string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert - should generate activity, logging, and counter for this method
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("FullTelemetryMethod"))
			.IsTrue()
			.Because("the generated class must contain the multi-target method");
	}

	[Test]
	public async Task Generate_GivenMethodWithoutAttributeInMultiTarget_RaisesInferenceNotSupportedDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """

#nullable enable

namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	// This method has no attribute, so inference is not supported in multi-target
	void MethodWithoutAttribute(string message);
}

partial class MultiTelemetryCore
{
	public void MethodWithoutAttribute(string message)
	{
		// User must implement methods that don't have telemetry attributes
	}
}

""";

		// Act
		var generationResult = await GenerateAsync(
			multiGen,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1001");
	}

	[Test]
	public async Task Generate_GivenActivitiesLoggingWithExplicitAttributes_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Event]
	void RecordEvent(System.Diagnostics.Activity? activity, [Tag]string eventType);

	[Trace]
	void TraceMessage(string message);

	[Debug]
	void DebugMessage(string message);

	[Info]
	void InfoMessage(string message);

	[Warning]
	void WarnMessage(string message);

	[Error]
	void ErrorMessage(string message);

	[Critical]
	void CriticalMessage(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("RecordEvent"))
			.IsTrue()
			.Because("the generated class must contain the event method");
		await Assert
			.That(query.HasMethod("TraceMessage"))
			.IsTrue()
			.Because("the generated class must contain the trace log method");
		await Assert
			.That(query.HasMethod("DebugMessage"))
			.IsTrue()
			.Because("the generated class must contain the debug log method");
		await Assert
			.That(query.HasMethod("InfoMessage"))
			.IsTrue()
			.Because("the generated class must contain the info log method");
		await Assert
			.That(query.HasMethod("WarnMessage"))
			.IsTrue()
			.Because("the generated class must contain the warning log method");
		await Assert
			.That(query.HasMethod("ErrorMessage"))
			.IsTrue()
			.Because("the generated class must contain the error log method");
		await Assert
			.That(query.HasMethod("CriticalMessage"))
			.IsTrue()
			.Because("the generated class must contain the critical log method");
	}

	[Test]
	public async Task Generate_GivenMetricsWithAllInstrumentTypes_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Counter]
	void IncrementCounter(int value);

	[AutoCounter]
	void AutoIncrement();

	[UpDownCounter]
	void UpdateUpDownCounter(int value);

	[Histogram]
	void RecordHistogram(double value);

	[ObservableCounter]
	void GetObservableCounter(System.Func<long> valueFunc);

	[ObservableGauge]
	void GetObservableGauge(System.Func<double> valueFunc);

	[ObservableUpDownCounter]
	void GetObservableUpDownCounter(System.Func<int> valueFunc);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("IncrementCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
		await Assert
			.That(query.HasMethod("AutoIncrement"))
			.IsTrue()
			.Because("the generated class must contain the auto-counter method");
		await Assert
			.That(query.HasMethod("UpdateUpDownCounter"))
			.IsTrue()
			.Because("the generated class must contain the up-down counter method");
		await Assert
			.That(query.HasMethod("RecordHistogram"))
			.IsTrue()
			.Because("the generated class must contain the histogram method");
		await Assert
			.That(query.HasMethod("GetObservableCounter"))
			.IsTrue()
			.Because("the generated class must contain the observable counter method");
		await Assert
			.That(query.HasMethod("GetObservableGauge"))
			.IsTrue()
			.Because("the generated class must contain the observable gauge method");
		await Assert
			.That(query.HasMethod("GetObservableUpDownCounter"))
			.IsTrue()
			.Because("the generated class must contain the observable up-down counter method");
	}

	[Test]
	public async Task Generate_GivenLoggingMetricsWithVariousTypes_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Log]
	void LogOperation(string operationId, int count, double duration);

	[Counter]
	void IncrementOperationCounter(int value);

	[Histogram]
	void RecordOperationDuration(double milliseconds);

	[Info]
	void InfoLog(string message, int userId);

	[UpDownCounter]
	void UpdateActiveConnections(int delta);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("IncrementOperationCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
		await Assert
			.That(query.HasMethod("RecordOperationDuration"))
			.IsTrue()
			.Because("the generated class must contain the histogram method");
		await Assert
			.That(query.HasMethod("InfoLog"))
			.IsTrue()
			.Because("the generated class must contain the info log method");
		await Assert
			.That(query.HasMethod("UpdateActiveConnections"))
			.IsTrue()
			.Because("the generated class must contain the up-down counter method");
	}

	[Test]
	public async Task Generate_GivenAllThreeTypesWithComplexParameters_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """

using System;

namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? ProcessRequest(
		[Tag]string requestId,
		[Tag]int userId,
		[Baggage]string? correlationId
	);

	[Log]
	void LogProcessing(
		string requestId,
		int userId,
		DateTime timestamp,
		string? correlationId
	);

	[AutoCounter]
	void IncrementRequestCount(string endpoint);

	[Histogram]
	void RecordRequestDuration(double milliseconds, string endpoint);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("ProcessRequest"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("LogProcessing"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("IncrementRequestCount"))
			.IsTrue()
			.Because("the generated class must contain the auto-counter method");
		await Assert
			.That(query.HasMethod("RecordRequestDuration"))
			.IsTrue()
			.Because("the generated class must contain the histogram method");
	}

	[Test]
	public async Task Generate_GivenActivitiesWithContextAndEvent_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartOperation([Tag]string operationId);

	[Context]
	void SetContext(System.Diagnostics.Activity? activity, [Tag]string key, [Tag]string value);

	[Event]
	void RecordEvent(System.Diagnostics.Activity? activity, [Tag]string eventName);

	[Counter]
	void CountOperation(int counterValue, string operationType);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartOperation"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("SetContext"))
			.IsTrue()
			.Because("the generated class must contain the context method");
		await Assert
			.That(query.HasMethod("RecordEvent"))
			.IsTrue()
			.Because("the generated class must contain the event method");
		await Assert
			.That(query.HasMethod("CountOperation"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
	}

	[Test]
	public async Task Generate_GivenExclusionInMultiTarget_ExcludesMethodCorrectly(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	void LogOperation(string operationId);

	[Exclude]
	void ExcludedMethod(string message);
}

partial class MultiTelemetryCore
{
	public void ExcludedMethod(string message)
	{
		// This method should be excluded from the generated telemetry implementation.
	}
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("ExcludedMethod"))
			.IsFalse()
			.Because("the excluded method must not be generated in the telemetry implementation");
	}

	[Test]
	public async Task Generate_GivenMultipleMethodsWithSameNames_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Process([Tag]string id);

	[Log]
	void Process(string id, string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			multiGen,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1003");
	}

	[Test]
	public async Task Generate_GivenNullableParametersInMultiTarget_GeneratesCorrectly(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity(
		[Tag]string? operationId,
		[Tag]int? userId
	);

	[Log]
	void LogOperation(string? operationId, int? userId, string? message);

	[Counter]
	void IncrementCounter(int value, [Tag]string? operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("StartActivity"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("IncrementCounter"))
			.IsTrue()
			.Because("the generated class must contain the counter method");
	}

	[Test]
	public async Task Generate_GivenAsyncMethodsInMultiTarget_RaisesDiagnostics(CancellationToken cancellationToken)
	{
		// Arrange
		const string multiGen = """

using System.Threading.Tasks;

namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	Task LogOperationAsync(string operationId, string message);

	[Info]
	ValueTask InfoAsync(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			multiGen,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert - Task and ValueTask are not valid return types for logging
		await Assert.That(generationResult).HasDiagnostic("TSG2021"); // Async return types are invalid
	}

	[Test]
	public async Task Generate_GivenActivityWithLoggerButNoActivityMethods_GeneratesLoggerOnlyWithInfo(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Logger]
public interface IMultiTelemetry
{
	// Note: No [Activity] method defined, only logging methods
	[Log]
	void LogOperation(string operationId);

	[Info]
	void InfoMessage(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(multiGen, cancellationToken: cancellationToken);

		// Assert
		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("LogOperation"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("InfoMessage"))
			.IsTrue()
			.Because("the generated class must contain the info log method");
	}

	[Test]
	public async Task Generate_GivenOnlyEventAndContextWithoutActivity_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string multiGen = """


namespace Testing;

[ActivitySource("testing-activity-source")]
[Meter("testing-meter")]
public interface IMultiTelemetry
{
	// No [Activity] method, only Event and Context
	[Event]
	void RecordEvent(System.Diagnostics.Activity? activity, [Tag]string eventName);

	[Context]
	void SetContext(System.Diagnostics.Activity? activity, [Tag]string key);

	[Counter]
	void IncrementCounter(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			multiGen,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3012");
	}

	[Test]
	public async Task Generate_GivenLoggerOnlyInterfaceWithMetricsMethod_RaisesMissingInterfaceSourceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange - interface has [Logger] but method has [AutoCounter], missing [Meter]
		const string code = """


namespace Testing;

[Logger]
public interface ITelemetry
{
	[Warning]
	void WarnOperation(string message);

	[AutoCounter]
	void CountOperation();
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1010");
	}

	[Test]
	public async Task Generate_GivenMeterOnlyInterfaceWithActivityMethod_RaisesMissingInterfaceSourceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange - interface has [Meter] but method has [Activity], missing [ActivitySource]
		const string code = """


namespace Testing;

[Meter("testing-meter")]
public interface ITelemetry
{
	[Counter]
	void IncrementCounter(int value);

	[Activity]
	System.Diagnostics.Activity? StartOperation(string operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1010");
	}

	[Test]
	public async Task Generate_GivenActivitySourceOnlyInterfaceWithLogMethod_RaisesMissingInterfaceSourceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange - interface has [ActivitySource] but method has [Warning], missing [Logger]
		const string code = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Warning]
	void WarnOperation(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1010");
	}

	[Test]
	public async Task Generate_GivenLoggerOnlyInterfaceWithMethodHavingBothLoggingAndMetricsAttributes_RaisesMissingInterfaceSourceDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange - interface has [Logger] only, method has both [Warning] and [AutoCounter]
		const string code = """


namespace Testing;

[Logger]
public interface ITelemetry
{
	[Warning]
	[AutoCounter]
	void WarnAndCount(string message);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG1010");
	}
}
