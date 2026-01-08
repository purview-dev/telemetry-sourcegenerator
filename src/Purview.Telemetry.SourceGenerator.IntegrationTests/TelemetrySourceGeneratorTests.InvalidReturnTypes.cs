namespace Purview.Telemetry.SourceGenerator;

/// <summary>
/// Tests for invalid return types that should raise diagnostics.
/// </summary>
public partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenLogMethodReturningString_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	string InvalidReturnType(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningInt_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[Counter]
	int InvalidReturnType(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4001"]
		);
	}

	[Test]
	public async Task Generate_GivenActivityMethodReturningObject_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Activity]
	object InvalidReturnType(string operationId);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3002"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningTaskOfInt_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningTask_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningValueTask_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningTask_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Metrics;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4001"]
		);
	}

	[Test]
	public async Task Generate_GivenActivityMethodReturningTaskOfActivity_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Activities;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3002"]
		);
	}

	[Test]
	public async Task Generate_GivenScopedLogReturningVoid_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log(IsScoped = true)]
	void InvalidScopedReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2020"]
		);
	}

	[Test]
	public async Task Generate_GivenObservableMetricReturningBool_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[ObservableCounter]
	bool InvalidObservableReturn(System.Func<int> callback);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4007"]
		);
	}

	[Test]
	public async Task Generate_GivenAutoCounterReturningBool_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[AutoCounter]
	bool InvalidAutoCounterReturn(string operation);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4008"]
		);
	}

	[Test]
	public async Task Generate_GivenEventMethodReturningActivity_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Event]
	System.Diagnostics.Activity? InvalidEventReturn(
		System.Diagnostics.Activity? activity,
		string eventName
	);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3002"]
		);
	}

	[Test]
	public async Task Generate_GivenContextMethodReturningBool_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Activities;

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface IInvalidTelemetry
{
	[Context]
	bool InvalidContextReturn(
		System.Diagnostics.Activity? activity,
		string key,
		string value
	);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG3002"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningBool_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	bool InvalidBoolReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningActivity_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log]
	System.Diagnostics.Activity? InvalidActivityReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2021"]
		);
	}

	[Test]
	public async Task Generate_GivenScopedLogReturningTask_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;
using System.Threading.Tasks;

namespace Testing;

[Logger]
public interface IInvalidTelemetry
{
	[Log(IsScoped = true)]
	Task InvalidScopedAsyncReturn(string message);
}
";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2020"]
		);
	}

	[Test]
	public async Task Generate_GivenLogMethodReturningValueTaskOfString_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			@"
using Purview.Telemetry.Logging;
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
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG2022"]
		);
	}

	[Test]
	public async Task Generate_GivenMetricMethodReturningIDisposable_RaisesDiagnostic()
	{
		// Arrange
		const string code =
			"""

using Purview.Telemetry.Metrics;

namespace Testing;

[Meter("testing-meter")]
public interface IInvalidTelemetry
{
	[Counter]
	System.IDisposable InvalidDisposableReturn(int value);
}

""";

		// Act
		var generationResult = await GenerateAsync(code);

		// Assert
		await TestHelpers.VerifyAsync(
			generationResult,
			config: s => s.ScrubInlineGuids(),
			expectsDiagnostics: true,
			expectedDiagnosticCodes: ["TSG4001"]
		);
	}
}
