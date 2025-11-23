namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Tests for multi-generation targeting scenarios where a single interface
/// supports multiple telemetry types (Activities, Logging, Metrics).
/// </summary>
public partial class TelemetrySourceGeneratorTests
{
	[Fact]
	public async Task Generate_GivenActivitiesAndLogging_GeneratesBothCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	void LogOperation(string operationId, string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenActivitiesAndMetrics_GeneratesBothCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Counter]
	void IncrementCounter(int value);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenLoggingAndMetrics_GeneratesBothCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[Logger]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Log]
	void LogOperation(string operationId, string message);

	[Counter]
	void IncrementCounter(int value);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenAllThreeTypes_GeneratesAllCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	[Log]
	void LogOperation(string operationId, string message);

	[Counter]
	void IncrementCounter(int value);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMethodWithMultipleTargetAttributes_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	[Log]
	void InvalidMethod(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenMethodWithActivityAndMetricAttributes_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Activity]
	[Counter]
	void InvalidMethod(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenMethodWithLoggingAndMetricAttributes_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[Logger]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Log]
	[Counter]
	void InvalidMethod(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenMethodWithAllThreeAttributes_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Activity]
	[Log]
	[Counter]
	void InvalidMethod(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenMethodWithoutAttributeInMultiTarget_RaisesInferenceNotSupportedDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartActivity([Tag]string operationId);

	// This method has no attribute, so inference is not supported in multi-target
	void MethodWithoutAttribute(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenActivitiesLoggingWithExplicitAttributes_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
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
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMetricsWithAllInstrumentTypes_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Meter(""testing-meter"")]
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
	long GetObservableCounter();

	[ObservableGauge]
	double GetObservableGauge();

	[ObservableUpDownCounter]
	int GetObservableUpDownCounter();
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenLoggingMetricsWithVariousTypes_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[Logger]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Log]
	void LogOperation(string operationId, int count, double duration);

	[Counter]
	void IncrementOperationCounter();

	[Histogram]
	void RecordOperationDuration(double milliseconds);

	[Info]
	void InfoLog(string message, int userId);

	[UpDownCounter]
	void UpdateActiveConnections(int delta);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenAllThreeTypesWithComplexParameters_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;
using System;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
[Meter(""testing-meter"")]
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

	[Counter]
	void IncrementRequestCount(string endpoint);

	[Histogram]
	void RecordRequestDuration(double milliseconds, string endpoint);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenActivitiesWithContextAndEvent_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? StartOperation([Tag]string operationId);

	[Context]
	void SetContext(System.Diagnostics.Activity? activity, [Tag]string key, [Tag]string value);

	[Event]
	void RecordEvent(System.Diagnostics.Activity? activity, [Tag]string eventName);

	[Counter]
	void CountOperation(string operationType);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenExclusionInMultiTarget_ExcludesMethodCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
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
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenMultipleMethodsWithSameNames_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
public interface IMultiTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Process([Tag]string id);

	[Log]
	void Process(string id, string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenNullableParametersInMultiTarget_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
[Meter(""testing-meter"")]
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
	void IncrementCounter(int? value);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenAsyncMethodsInMultiTarget_GeneratesCorrectly()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using System.Threading.Tasks;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
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
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(generationResult);
	}

	[Fact]
	public async Task Generate_GivenActivityWithLoggerButNoActivityMethods_GeneratesLoggerOnlyWithInfo()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Logger]
public interface IMultiTelemetry
{
	// Note: No [Activity] method defined, only logging methods
	[Log]
	void LogOperation(string operationId);

	[Info]
	void InfoMessage(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}

	[Fact]
	public async Task Generate_GivenOnlyEventAndContextWithoutActivity_RaisesDiagnostic()
	{
		// Arrange
		const string multiGen =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""testing-activity-source"")]
[Meter(""testing-meter"")]
public interface IMultiTelemetry
{
	// No [Activity] method, only Event and Context
	[Event]
	void RecordEvent(System.Diagnostics.Activity? activity, [Tag]string eventName);

	[Context]
	void SetContext(System.Diagnostics.Activity? activity, [Tag]string key);

	[Counter]
	void IncrementCounter();
}
";

		// Act
		var generationResult = await GenerateAsync(multiGen);

		// Assert
		await TestHelpers.Verify(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true
		);
	}
}
