namespace Purview.Telemetry.SourceGenerator.Refactorings;

/// <summary>
/// Snapshot tests for <see cref="ConvertActivitySourceToTelemetryRefactoringProvider"/>.
/// Each test defines a <em>before</em> scenario and the snapshot captures the <em>after</em> output.
/// To regenerate snapshots: run <c>dotnet test</c>; <c>*.received.txt</c> files are auto-accepted.
/// </summary>
public sealed class ConvertActivitySourceToTelemetryRefactoringProviderSnapshotTests
	: CodeRefactoringTestBase
{
	static readonly ConvertActivitySourceToTelemetryRefactoringProvider Provider = new();

	// ─────────────────────────────────────────────────────────────────────────
	// Basic StartActivity scenarios
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_SingleStartActivity_LiteralName(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$OrderProcessor
			{
				static readonly ActivitySource _activitySource = new("OrderProcessor");

				public void Process(string orderId)
				{
					using var activity = _activitySource.StartActivity("ProcessOrder");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_SingleStartActivity_NoName(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$DataLoader
			{
				static readonly ActivitySource _activitySource = new("DataLoader");

				public void Load()
				{
					using var activity = _activitySource.StartActivity();
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_SingleStartActivity_WithActivityKind(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$HttpClient
			{
				static readonly ActivitySource _activitySource = new("HttpClient");

				public void SendRequest(string url)
				{
					using var activity = _activitySource.StartActivity("SendRequest", ActivityKind.Client);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_MultipleActivities_DifferentKinds(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$ApiGateway
			{
				static readonly ActivitySource _activitySource = new("ApiGateway");

				public void HandleIncoming(string path)
				{
					using var activity = _activitySource.StartActivity("HandleIncoming", ActivityKind.Server);
				}

				public void CallUpstream(string service)
				{
					using var activity = _activitySource.StartActivity("CallUpstream", ActivityKind.Client);
				}

				public void ProcessBackground(string jobId)
				{
					using var activity = _activitySource.StartActivity("ProcessBackground", ActivityKind.Internal);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Injection styles
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_PrimaryConstructor_ActivitySource(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$SearchService(ActivitySource activitySource)
			{
				public void Search(string query)
				{
					using var activity = activitySource.StartActivity("Search");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_InstanceField_ExpressionBodyCtor(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$EmailService
			{
				readonly ActivitySource _tracer;

				public EmailService(ActivitySource tracer) => _tracer = tracer;

				public void Send(string to, string subject)
				{
					using var activity = _tracer.StartActivity("SendEmail");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Activity name derivation
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_ActivityName_FromLiteral_CamelCase(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$CacheService
			{
				static readonly ActivitySource _activitySource = new("CacheService");

				public void FetchFromCache(string key)
				{
					using var activity = _activitySource.StartActivity("fetchFromCache");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_ActivityName_FromLiteral_WithSeparator(
		CancellationToken cancellationToken
	)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$WarehouseService
			{
				static readonly ActivitySource _activitySource = new("WarehouseService");

				public void PickItem(string itemId)
				{
					using var activity = _activitySource.StartActivity("warehouse.pick-item");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Multiple activities in same method / across methods
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_SameActivityName_TwoMethods(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$QueueService
			{
				static readonly ActivitySource _activitySource = new("QueueService");

				public void Enqueue(string item)
				{
					using var activity = _activitySource.StartActivity("QueueOperation");
				}

				public void Dequeue()
				{
					using var activity = _activitySource.StartActivity("QueueOperation");
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	[Test]
	public async Task Verify_StaticFieldInit_FullNewExpression(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$MetadataService
			{
				static readonly ActivitySource _source = new ActivitySource("MetadataService", "1.0.0");

				public void Fetch(string id)
				{
					using var activity = _source.StartActivity("FetchMetadata", ActivityKind.Internal);
				}
			}
			""";

		await VerifyRefactoringAsync(code, Provider, cancellationToken);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// Document scope
	// ─────────────────────────────────────────────────────────────────────────

	[Test]
	public async Task Verify_DocumentScope_TwoClassesInSameFile(CancellationToken cancellationToken)
	{
		const string code = """
			using System.Diagnostics;

			namespace Testing;

			public class $$RequestTracker
			{
				readonly ActivitySource _source = new("RequestTracker");

				public void TrackRequest(string path)
				{
					using var activity = _source.StartActivity("TrackRequest");
				}
			}

			public class QueryTracker
			{
				readonly ActivitySource _source = new("QueryTracker");

				public void TrackQuery(string query)
				{
					using var activity = _source.StartActivity("TrackQuery");
				}
			}
			""";

		await VerifyRefactoringAsync(
			code,
			Provider,
			"Purview.Telemetry.ConvertActivitySourceToTelemetry.Document",
			cancellationToken
		);
	}
}
