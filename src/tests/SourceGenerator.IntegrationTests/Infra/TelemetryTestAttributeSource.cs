namespace Purview.Telemetry.SourceGenerator.Infra;

/// <summary>
/// Minimal definitions of the telemetry attribute types, mirroring the shapes the analyzer's shared
/// rules resolve. A standalone analyzer run does not execute the generator, so these are compiled
/// into the analyzer test compilation via <see cref="TelemetryAnalyzerTestOptions"/>.
/// </summary>
static class TelemetryTestAttributeSource
{
	public const string Attributes = """

		namespace Purview.Telemetry;

		[System.AttributeUsage(System.AttributeTargets.Interface | System.AttributeTargets.Assembly, Inherited = false)]
		public sealed class TelemetryGenerationAttribute : System.Attribute
		{
			public bool GenerateDependencyExtension { get; set; }

			public TelemetryGenerationAttribute(bool generateDependencyExtension = true) =>
				GenerateDependencyExtension = generateDependencyExtension;
		}

		[System.AttributeUsage(System.AttributeTargets.Interface, Inherited = false)]
		public sealed class ActivitySourceAttribute : System.Attribute
		{
			public string? Name { get; set; }

			public ActivitySourceAttribute() { }

			public ActivitySourceAttribute(string? name) => Name = name;
		}

		[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
		public sealed class ActivityAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
		public sealed class EventAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
		public sealed class ContextAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
		public sealed class TagAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
		public sealed class BaggageAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Interface, Inherited = false)]
		public sealed class LoggerAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
		public sealed class LogAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Interface, Inherited = false)]
		public sealed class MeterAttribute : System.Attribute
		{
			public string? Name { get; set; }

			public MeterAttribute() { }

			public MeterAttribute(string? name) => Name = name;
		}

		[System.AttributeUsage(System.AttributeTargets.Method, Inherited = false)]
		public sealed class CounterAttribute : System.Attribute { }

		[System.AttributeUsage(System.AttributeTargets.Parameter, Inherited = false)]
		public sealed class InstrumentMeasurementAttribute : System.Attribute { }

		""";
}
