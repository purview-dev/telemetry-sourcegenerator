namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenPartialInterface_GeneratesTelemetry()
	{
		// Arrange
		const string partialInterfaceDef =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
partial interface ITestTelemetry
{
}
";

		const string partialInterfaceActivities =
			@"
using Purview.Telemetry.Activities;

partial interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}
";

		const string partialInterfaceLogging =
			@"
using Purview.Telemetry.Logging;

partial interface ITestTelemetry
{
	[Log]
	void Log([Tag]int intParam, bool boolParam);

	[Log]
	IDisposable? LogScope([Tag]int intParam, bool boolParam);
}
";

		const string partialInterfaceMetric =
			@"
using Purview.Telemetry.Metrics;


partial interface ITestTelemetry
{
	[Counter]
	bool Counter(int counterValue, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			csharpDocuments:
			[
				Text(partialInterfaceDef),
				Text(partialInterfaceActivities),
				Text(partialInterfaceLogging),
				Text(partialInterfaceMetric),
			]
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenNoNamespace_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Log]
	void Log([Tag]int intParam, bool boolParam);

	[Log]
	IDisposable? LogScope([Tag]int intParam, bool boolParam);

	[Counter]
	bool Counter(int counterValue, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicTelemetryGen_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Context]
	void Context(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Log]
	void Log([Tag]int intParam, bool boolParam);

	[Log]
	IDisposable? LogScope([Tag]int intParam, bool boolParam);

	[Counter]
	bool Counter(int counterValue, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithException_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExceptionAndEscape_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExceptionAndDisabledOTelExceptionRulesAndEscape_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(UseRecordExceptionRules = false)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExplicitExceptionAndNamedExceptionAndRulesAreFalse_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(name: ""exception"", UseRecordExceptionRules = false)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExplicitExceptionAndEventIsNamedExceptionAndRulesAreTrue_GeneratesTelemetry()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;

namespace Testing;
	
[ActivitySource(""activity-source"")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(name: ""exception"", UseRecordExceptionRules = true)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(generationResult);
	}

	[Test]
	public async Task Generate_GivenDuplicateTelemetryGen_GeneratesDiagnostics()
	{
		// Arrange
		const string basicTelemetry =
			@"
using Purview.Telemetry.Activities;
using Purview.Telemetry.Logging;
using Purview.Telemetry.Metrics;

namespace Testing;

[ActivitySource(""activity-source"")]
[Logger]
[Meter]
public interface ITestTelemetry
{
	[Activity]
	[Log]
	void Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	[Counter]
	void Event([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Context]
	[Activity]
	void Context([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Log]
	[Counter]
	void Log([Tag]int intParam, bool boolParam);

	[Log]
	[Activity]
	IDisposable? LogScope([Tag]int intParam, bool boolParam);

	[Counter]
	[Event]
	[Log]
	void Counter(int counterValue, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			validationCompilation: false
		);
	}
}
