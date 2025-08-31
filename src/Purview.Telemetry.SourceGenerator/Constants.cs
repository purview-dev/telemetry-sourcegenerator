using System.Text.RegularExpressions;
using Purview.Telemetry.SourceGenerator.Templates;

// This as the non-SourceGenerator namespace...
namespace Purview.Telemetry;

static partial class Constants
{
	public const string SystemDiagnosticsNamespace = "System.Diagnostics";
	public const string EmbedAttributesHashDefineName = "PURVIEW_TELEMETRY_ATTRIBUTES";

	public const string MessageTemplateRegex =
		@"\{
    # Optional destructuring (@) or stringify ($)
    (?: (?<destructure>@) | (?<stringify>\$) )?
    # Capture 'identifier' as either an 'ordinal' or a 'named' identifier
    (?<identifier>
        (?<ordinal>\d+)        # e.g. 0, 1, 2
        |                      # OR
        (?<named>[A-Za-z_]\w*) # e.g. CustomerId, name_123
    )
    # Optional alignment (e.g. ,10 or ,-8)
    (?:,(?<alignment>-?\d+))?
    # Optional format specifier (e.g. :C, :0.00)
    (?::(?<format>[^}]+))?
\}";

	public static readonly Regex MessageTemplateMatcher = new(
		MessageTemplateRegex,
		RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
	);

	public static string[] GetEmbeddedFileNames() =>
		[
			"ActivityTypes",
			"LoggingTypes",
			"MetricTypes",
			"SharedTypes",
			"MultiTargetGenerationTypes",
		];

	public static readonly PurviewTypeInfo Empty = PurviewTypeFactory.Create(
		"Fake.Fake.Fake.Fake.Empty"
	);

	public static class VariableNames
	{
		public const string MeterFieldName = "_meter";
		public const string LoggerFieldName = "_logger";
		public const string ActivitySourceFieldName = "_activitySource";
	}

	public static class Shared
	{
		public static readonly PurviewTypeInfo TagAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.TagAttribute"
		);
		public static readonly PurviewTypeInfo ExcludeAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.ExcludeAttribute"
		);
		public static readonly PurviewTypeInfo TelemetryGenerationAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.TelemetryGenerationAttribute");
		public static readonly PurviewTypeInfo TelemetryAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.TelemetryAttribute"
		);
		public static readonly PurviewTypeInfo EnableMultiTargetGenerationAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.EnableMultiTargetGenerationAttribute");
		public static readonly PurviewTypeInfo ExcludeFromActivityAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.ExcludeFromActivityAttribute");
		public static readonly PurviewTypeInfo ExcludeFromLoggingAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.ExcludeFromLoggingAttribute");
		public static readonly PurviewTypeInfo ExcludeFromMetricsAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.ExcludeFromMetricsAttribute");
	}

	public static class DependencyInjection
	{
		public const string DependencyInjectionNamespace =
			"Microsoft.Extensions.DependencyInjection";

		public static readonly PurviewTypeInfo IServiceCollection = PurviewTypeFactory.Create(
			DependencyInjectionNamespace + ".IServiceCollection"
		);
		public static readonly PurviewTypeInfo ServiceDescriptor = PurviewTypeFactory.Create(
			DependencyInjectionNamespace + ".ServiceDescriptor"
		);
		public static readonly PurviewTypeInfo ServiceLifetime = PurviewTypeFactory.Create(
			DependencyInjectionNamespace + ".ServiceLifetime"
		);

		public static readonly string Singleton = ServiceLifetime + "." + nameof(Singleton);
	}

	public static class Diagnostics
	{
		public const string Usage = nameof(Usage);

		public static class Activity
		{
			public const string Usage = nameof(Activity) + "." + nameof(Usage);
		}

		public static class Logging
		{
			public const string Usage = nameof(Logging) + "." + nameof(Usage);
			public const string Performance = nameof(Logging) + "." + nameof(Performance);
		}

		public static class Metrics
		{
			public const string Usage = nameof(Metrics) + "." + nameof(Usage);
		}
	}
}
