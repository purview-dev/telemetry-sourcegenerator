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

	public static TemplateInfo[] GetAllTemplates()
	{
		return
		[
			.. Activities.GetTemplates(),
			.. Logging.GetTemplates(),
			.. Metrics.GetTemplates(),
			.. Shared.GetTemplates(),
		];
	}

	public static readonly PurviewTypeInfo Empty = PurviewTypeFactory.Create(
		"Fake.Fake.Fake.Fake.Empty"
	);

	/// <summary>
	/// Naming convention strategies for generated telemetry names.
	/// </summary>
	public enum NamingConvention
	{
		/// <summary>
		/// Legacy naming (pre-v4): Lowercase ActivitySource names, smashed compound words.
		/// </summary>
		Legacy = 0,

		/// <summary>
		/// OpenTelemetry-aligned naming (default from v4): Preserves casing, uses dot.separated.lowercase for metrics,
		/// snake_case for tags, follows OTel semantic conventions.
		/// </summary>
		OpenTelemetry = 1
	}

	public static partial class Shared
	{
		public static readonly TemplateInfo TagAttribute = TemplateInfo.Create(
			"Purview.Telemetry.TagAttribute"
		);
		public static readonly TemplateInfo ExcludeAttribute = TemplateInfo.Create(
			"Purview.Telemetry.ExcludeAttribute"
		);
		public static readonly TemplateInfo TelemetryGenerationAttribute = TemplateInfo.Create(
			"Purview.Telemetry.TelemetryGenerationAttribute"
		);
		public static readonly TemplateInfo TargetsEnum = TemplateInfo.Create(
			"Purview.Telemetry.Targets"
		);
		public static readonly TemplateInfo NamingConventionEnum = TemplateInfo.Create(
			"Purview.Telemetry.NamingConvention"
		);
		public static readonly TemplateInfo ExcludeTargetsAttribute = TemplateInfo.Create(
			"Purview.Telemetry.ExcludeTargetsAttribute"
		);

		public static TemplateInfo[] GetTemplates() =>
			[
				TagAttribute,
				ExcludeAttribute,
				TelemetryGenerationAttribute,
				TargetsEnum,
				NamingConventionEnum,
				ExcludeTargetsAttribute,
			];
	}

	public static partial class DependencyInjection
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
