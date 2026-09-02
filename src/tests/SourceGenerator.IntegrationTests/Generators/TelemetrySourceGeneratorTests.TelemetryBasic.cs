using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenPartialInterface_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string partialInterfaceDef = """


[ActivitySource("activity-source")]
[Logger]
[Meter]
partial interface ITestTelemetry
{
}

""";

		const string partialInterfaceActivities =
			@"

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


partial interface ITestTelemetry
{
	[Counter]
	bool Counter(int counterValue, [Tag]int intParam, bool boolParam);
}
";

		// Act
		var generationResult = await GenerateAsync(
			[partialInterfaceDef, partialInterfaceActivities, partialInterfaceLogging, partialInterfaceMetric],
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenNoNamespace_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """


[ActivitySource("activity-source")]
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

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenBasicTelemetryGen_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
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

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithException_GeneratesTelemetry(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExceptionAndEscape_GeneratesTelemetry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event]
	void Event(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExceptionAndDisabledOTelExceptionRulesAndEscape_GeneratesTelemetry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(UseRecordExceptionRules = false)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await Assert.That(generationResult).HasDiagnostic("TSG3021");
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExplicitExceptionAndNamedExceptionAndRulesAreFalse_GeneratesTelemetry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(name: "exception", UseRecordExceptionRules = false)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenBasicEventWithExplicitExceptionAndEventIsNamedExceptionAndRulesAreTrue_GeneratesTelemetry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
public interface ITestTelemetry
{
	[Activity]
	System.Diagnostics.Activity? Activity();

	[Event(name: "exception", UseRecordExceptionRules = true)]
	void EventMethod(System.Diagnostics.Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam, Exception anException, [Escape]bool escape);
}

""";

		// Act
		var generationResult = await GenerateAsync(basicTelemetry, cancellationToken: cancellationToken);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenDuplicateTelemetryGen_GeneratesDiagnostics(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicTelemetry = """


namespace Testing;

[ActivitySource("activity-source")]
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

""";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(generationResult, expectsDiagnostics: true, cancellationToken: cancellationToken);
	}
}
