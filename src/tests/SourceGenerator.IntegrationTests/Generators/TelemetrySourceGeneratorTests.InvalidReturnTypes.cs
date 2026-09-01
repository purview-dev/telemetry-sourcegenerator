using Purview.Telemetry.SourceGenerator.Infra;

namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Tests for invalid return types that should raise diagnostics.
/// </summary>
public partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenLogMethodReturningString_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	string InvalidReturnType(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningInt_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[Counter]
	int InvalidReturnType(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4001"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenActivityMethodReturningObject_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Activity]
	object InvalidReturnType(string operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG3002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningTaskOfInt_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	Task<int> InvalidAsyncReturnType(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningTask_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	Task InvalidTaskReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningValueTask_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	ValueTask InvalidValueTaskReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningTask_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """

using System.Threading.Tasks;

namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[Counter]
	Task InvalidAsyncCounter(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4001"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenActivityMethodReturningTaskOfActivity_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string code = """

using System.Threading.Tasks;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Activity]
	Task<System.Diagnostics.Activity?> InvalidAsyncActivity(string operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG3002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenScopedLogReturningVoid_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		// Note: Since v4.0, scoped logs are determined by return type only.
		// A method returning void cannot be scoped - it must return IDisposable.
		// This test now verifies that a void-returning method is NOT treated as scoped.
		const string code =
			@"

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	// A log that returns void is not scoped - this is now valid behavior
	// If user wants scoped, they must return IDisposable
	[Log]
	void ValidNonScopedLog(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code, cancellationToken: cancellationToken);

		// Assert - this should succeed since void-returning logs are valid non-scoped logs
		await TestHelpers.VerifyAsync(generationResult, cancellationToken: cancellationToken);
	}

	[Test]
	public async Task Generate_GivenObservableMetricReturningBool_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[ObservableCounter]
	bool InvalidObservableReturn(System.Func<int> callback);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4007"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenAutoCounterReturningBool_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[AutoCounter]
	bool InvalidAutoCounterReturn(string operation);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4008"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenEventMethodReturningActivity_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Activity]
	System.Diagnostics.Activity? ValidActivity(string operationId);

	[Event]
	System.Diagnostics.Activity? InvalidEventReturn(
		System.Diagnostics.Activity? activity,
		string eventName
	);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG3002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenContextMethodReturningBool_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code = """


namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Activity]
	System.Diagnostics.Activity? ValidActivity(string operationId);

	[Context]
	bool InvalidContextReturn(
		System.Diagnostics.Activity? activity,
		string key,
		string value
	);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG3002"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningBool_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	bool InvalidBoolReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningActivity_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		const string code =
			@"

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	System.Diagnostics.Activity? InvalidActivityReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenScopedLogReturningTask_RaisesDiagnostic(CancellationToken cancellationToken)
	{
		// Arrange
		// Note: Since v4.0, scoped logs are determined by return type only (IDisposable).
		// A Task-returning log is invalid because logs don't support async.
		const string code =
			@"
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	Task InvalidScopedAsyncReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert - Task is not a valid return type for logging
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningValueTaskOfString_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string code =
			@"
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	ValueTask<string> InvalidValueTaskReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG2021"],
			cancellationToken: cancellationToken
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningIDisposable_RaisesDiagnostic(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string code = """


namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[Counter]
	System.IDisposable InvalidDisposableReturn(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			code,
			TelemetrySourceGeneratorTestOptions.NoValidation,
			cancellationToken: cancellationToken
		);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			expectsDiagnostics: true,
			validationCompilation: false,
			expectedDiagnosticCodes: ["TSG4001"],
			cancellationToken: cancellationToken
		);
	}
}
