using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Purview.SourceGeneratorFramework;

namespace Purview.Telemetry.SourceGenerator.Activities;

partial class TelemetrySourceGeneratorActivityTests
{
	[Test]
	public async Task Generate_GivenAssemblyEnableDI_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();

		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method with its parameter signature");
		await Assert
			.That(
				implClass.HasMethodReturnType(
					query,
					"Activity",
					TypeReference.Create<Activity>().Nullable(GenerationSettings.Create<TelemetrySourceGenerator>())
				)
			)
			.IsTrue()
			.Because("the activity method must return a nullable Activity");

		var diClass = query.GetClass("TestActivitiesCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestActivities", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must register the implementation via AddTestActivities");
	}

	[Test]
	public async Task Generate_GivenInterfaceEnableDI_GeneratesActivity(CancellationToken cancellationToken)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = true)]
[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		var diClass = query.GetClass("TestActivitiesCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestActivities", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must register the implementation via AddTestActivities");
	}

	[Test]
	public async Task Generate_GivenDIDisabledAtAssemblyAndInterfaceEnableDI_GeneratesActivity(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = false)]

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = true)]
[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		var diClass = query.GetClass("TestActivitiesCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestActivities", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the DI extension must be generated when the interface opts in");
	}

	[Test]
	public async Task Generate_GivenDIEnabledAtAssemblyAndInterfaceDisableDI_GeneratesActivity(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true)]

namespace Testing;

[TelemetryGeneration(GenerateDependencyExtension = false)]
[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		await Assert
			.That(query.HasClass("TestActivitiesCoreDIExtension", "Microsoft.Extensions.DependencyInjection"))
			.IsFalse()
			.Because("the DI extension must not be generated when the interface opts out");
	}

	[Test]
	public async Task Generate_GivenAssemblyEnableDIAndClassIsPublic_GeneratesActivity(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicActivity = """

using System.Diagnostics;

[assembly: TelemetryGeneration(GenerateDependencyExtension = true, DependencyInjectionClassIsPublic = true)]

namespace Testing;

[ActivitySource("testing-activity-source")]
public interface ITestActivities {
	[Activity]
	Activity? Activity([Baggage]string stringParam, [Tag]int intParam, bool boolParam);

	[Event]
	void Event(Activity? activity, [Baggage]string stringParam, [Tag]int intParam, bool boolParam);
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicActivity,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		var query = generationResult.Generated();
		var implClass = query.GetClass("TestActivitiesCore", "Testing");
		await Assert
			.That(
				implClass.HasMethod(
					query,
					"Activity",
					TypeReference.Create<string>(),
					TypeReference.Create<int>(),
					TypeReference.Create<bool>()
				)
			)
			.IsTrue()
			.Because("the generated implementation must contain the activity method");
		var diClass = query.GetClass("TestActivitiesCoreDIExtension", "Microsoft.Extensions.DependencyInjection");
		await Assert
			.That(diClass.HasMethod(query, "AddTestActivities", TypeReference.Create<IServiceCollection>()))
			.IsTrue()
			.Because("the public DI extension must register the implementation via AddTestActivities");
	}
}
