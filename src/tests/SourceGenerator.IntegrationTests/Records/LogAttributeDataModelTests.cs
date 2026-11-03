using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Infra;
using Purview.Telemetry.SourceGenerator.Logging;

namespace Purview.Telemetry.SourceGenerator.Records;

/// <summary>
/// Reads real applications of the generated logging attributes through the attribute-data models. The
/// level-specific models nest <see cref="LogAttributeData"/>, and 5.0.0 to 5.1.0 read every one of them as
/// <see cref="LogAttributeData.Empty"/>, so these pin each argument the models must carry.
/// </summary>
/// <remarks>
/// The attribute declarations are the generator's own post-initialization output, not a hand-written mirror,
/// so a declaration that cannot be applied (such as a nullable <c>EventId</c>) fails here.
/// </remarks>
public class LogAttributeDataModelTests
{
	static readonly Lazy<TelemetrySourceGeneratorTestOptions> Options = new(CreateOptions);

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributes))]
	public async Task FromAttributeData_GivenPositionalTemplate_ReadsTemplate(string attribute, int level)
	{
		// Arrange
		var application = Apply($"[{attribute}(\"custom {{Value}}\")]");

		// Act
		var model = ReadSpecific(attribute, application);

		// Assert
		await Assert.That(application).HasConstructorArgument("messageTemplate", "custom {Value}");
		await Assert.That(model.MessageTemplate).IsEqualTo("custom {Value}");
		await Assert.That(model.Level).IsEqualTo(level);
		await Assert.That(model.EventIdOrNull).IsNull();
		await Assert.That(model.Name).IsNull();
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributes))]
	public async Task FromAttributeData_GivenConstructorArgumentsByName_ReadsEach(string attribute, int level)
	{
		// Arrange
		var application = Apply($"[{attribute}(eventId: 7, messageTemplate: \"by name {{Value}}\", name: \"Named\")]");

		// Act
		var model = ReadSpecific(attribute, application);

		// Assert
		await Assert.That(application).HasConstructorArgument("eventId", 7, AttributeArgumentForm.NamedParameter);
		await Assert.That(model.EventIdOrNull).IsEqualTo(7);
		await Assert.That(model.MessageTemplate).IsEqualTo("by name {Value}");
		await Assert.That(model.Name).IsEqualTo("Named");
		await Assert.That(model.Level).IsEqualTo(level);
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributes))]
	public async Task FromAttributeData_GivenNamedArguments_ReadsEach(string attribute, int level)
	{
		// Arrange
		var application = Apply(
			$"[{attribute}(MessageTemplate = \"named {{Value}}\", EventId = 42, Name = \"Renamed\", GenerationMode = LoggerGenerationMode.V1)]"
		);

		// Act
		var model = ReadSpecific(attribute, application);

		// Assert
		await Assert.That(application).HasNamedArgument("EventId", 42);
		await Assert.That(model.MessageTemplate).IsEqualTo("named {Value}");
		await Assert.That(model.EventIdOrNull).IsEqualTo(42);
		await Assert.That(model.Name).IsEqualTo("Renamed");
		await Assert.That(model.GenerationMode).IsEqualTo(1);
		await Assert.That(model.Level).IsEqualTo(level);
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributes))]
	public async Task FromAttributeData_GivenNoArguments_TakesDefaults(string attribute, int level)
	{
		// Arrange
		var application = Apply($"[{attribute}]");

		// Act
		var specific = ReadSpecific(attribute, application);

		// Assert
		await Assert.That(application).HasConstructorArgument("messageTemplate", null, AttributeArgumentForm.Defaulted);
		await Assert.That(specific.MessageTemplate).IsNull();
		await Assert.That(specific.EventId).IsEqualTo(-1).Because("an omitted event id falls back to the hashed name");
		await Assert.That(specific.Name).IsNull();
		await Assert.That(specific.GenerationMode).IsEqualTo(0);
		await Assert.That(specific.Level).IsEqualTo(level);
	}

	[Test]
	[MethodDataSource(nameof(SpecificLogAttributes))]
	public async Task FromAttributeData_GivenWhitespaceTemplateAndName_TreatsThemAsUnset(string attribute, int level)
	{
		// Arrange
		var application = Apply($"[{attribute}(\"   \", \"  \")]");

		// Act
		var model = ReadSpecific(attribute, application);

		// Assert
		await Assert.That(model.MessageTemplate).IsNull();
		await Assert.That(model.Name).IsNull();
		await Assert.That(model.Level).IsEqualTo(level);
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorLoggingTests>(
		nameof(TelemetrySourceGeneratorLoggingTests.SpecificLogAttributeTypes)
	)]
	public async Task FromAttributeData_GivenSpecificAttribute_NestedLevelTakesItsDefault(string attribute)
	{
		// Arrange
		var application = Apply($"[{attribute}(\"template\")]");

		// Act
		var nested = ReadNested(attribute, application);

		// Assert
		await Assert.That(application).DoesNotHaveConstructorArgument("level");
		await Assert
			.That(nested.Level)
			.IsEqualTo(99)
			.Because($"[{attribute}] has no level argument; ToLogAttribute supplies the level instead");
		await Assert.That(nested.MessageTemplate).IsEqualTo("template");
	}

	[Test]
	[MethodDataSource<TelemetrySourceGeneratorLoggingTests>(
		nameof(TelemetrySourceGeneratorLoggingTests.SpecificLogAttributeTypes)
	)]
	public async Task FromAttributeData_GivenSpecificAttribute_NestedTargetRejectsIt(string attribute)
	{
		// Arrange
		var application = Apply($"[{attribute}(\"template\")]");

		// Act
		var log = LogAttributeData.FromAttributeData(application);

		// Assert
		await Assert
			.That(log.Exists)
			.IsFalse()
			.Because($"[{attribute}] is not [Log]; only the nested read skips the check");
	}

	[Test]
	public async Task FromAttributeData_GivenLogWithNamedEventId_ReadsIt()
	{
		// Arrange
		var application = Apply("[Log(MessageTemplate = \"log {Value}\", EventId = 9)]");

		// Act
		var model = LogAttributeData.FromAttributeData(application);

		// Assert
		await Assert.That(model.Exists).IsTrue();
		await Assert.That(model.MessageTemplate).IsEqualTo("log {Value}");
		await Assert.That(model.EventIdOrNull).IsEqualTo(9);
	}

	static AttributeData Apply(string usage) => AttributeDataFactory.FromUsage(usage, Options.Value);

	static LogAttributeData ReadSpecific(string attribute, AttributeData application) =>
		attribute switch
		{
			"Trace" => TraceAttributeData.FromAttributeData(application).ToLogAttribute(),
			"Debug" => DebugAttributeData.FromAttributeData(application).ToLogAttribute(),
			"Info" => InfoAttributeData.FromAttributeData(application).ToLogAttribute(),
			"Warning" => WarningAttributeData.FromAttributeData(application).ToLogAttribute(),
			"Error" => ErrorAttributeData.FromAttributeData(application).ToLogAttribute(),
			"Critical" => CriticalAttributeData.FromAttributeData(application).ToLogAttribute(),
			_ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
		};

	// The nested model as read, before ToLogAttribute applies the level and whitespace rules.
	static LogAttributeData ReadNested(string attribute, AttributeData application) =>
		attribute switch
		{
			"Trace" => TraceAttributeData.FromAttributeData(application).Log,
			"Debug" => DebugAttributeData.FromAttributeData(application).Log,
			"Info" => InfoAttributeData.FromAttributeData(application).Log,
			"Warning" => WarningAttributeData.FromAttributeData(application).Log,
			"Error" => ErrorAttributeData.FromAttributeData(application).Log,
			"Critical" => CriticalAttributeData.FromAttributeData(application).Log,
			_ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null),
		};

	static TelemetrySourceGeneratorTestOptions CreateOptions()
	{
		var compilation = CSharpCompilation.Create(
			nameof(LogAttributeDataModelTests),
			[],
			[MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);

		var attributeSources = CSharpGeneratorDriver
			.Create(new TelemetrySourceGenerator())
			.RunGenerators(compilation)
			.GetRunResult()
			.Results.SelectMany(static result => result.GeneratedSources)
			.Select(static source => source.SourceText.ToString());

		return new() { AdditionalSources = [.. attributeSources] };
	}

	public static IEnumerable<(string, int)> SpecificLogAttributes
	{
		get
		{
			List<(string, int)> data = [];

			data.Add(("Trace", 0));
			data.Add(("Debug", 1));
			data.Add(("Info", 2));
			data.Add(("Warning", 3));
			data.Add(("Error", 4));
			data.Add(("Critical", 5));

			return data;
		}
	}
}
