using System.Collections.Immutable;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry;

partial class Constants
{
	public static class Logging
	{
		public const int UnboundedIEnumerableMaxCountBeforeDiagnostic = 5;

		public const int MaxNonExceptionParameters = 6;
		public const string DefaultLogLevelConstantName = "DEFAULT_LOGLEVEL";

		public const string LoggerFieldName = "_logger";

		public const int DefaultLevel = 2;

		public static readonly TemplateInfo LoggerGenerationAttribute = TemplateInfo.Create(
			"Purview.Telemetry.LoggerGenerationAttribute"
		);
		public static readonly TemplateInfo LoggerAttribute = TemplateInfo.Create(
			"Purview.Telemetry.LoggerAttribute"
		);
		public static readonly TemplateInfo LogAttribute = TemplateInfo.Create(
			"Purview.Telemetry.LogAttribute"
		);
		public static readonly TemplateInfo LogPrefixType = TemplateInfo.Create(
			"Purview.Telemetry.LogPrefixType"
		);

		public static readonly TemplateInfo ExpandEnumerableAttribute = TemplateInfo.Create(
			"Purview.Telemetry.ExpandEnumerableAttribute"
		);

		public static readonly TemplateInfo TraceAttribute = TemplateInfo.Create(
			"Purview.Telemetry.TraceAttribute"
		);
		public static readonly TemplateInfo DebugAttribute = TemplateInfo.Create(
			"Purview.Telemetry.DebugAttribute"
		);
		public static readonly TemplateInfo InfoAttribute = TemplateInfo.Create(
			"Purview.Telemetry.InfoAttribute"
		);
		public static readonly TemplateInfo WarningAttribute = TemplateInfo.Create(
			"Purview.Telemetry.WarningAttribute"
		);
		public static readonly TemplateInfo ErrorAttribute = TemplateInfo.Create(
			"Purview.Telemetry.ErrorAttribute"
		);
		public static readonly TemplateInfo CriticalAttribute = TemplateInfo.Create(
			"Purview.Telemetry.CriticalAttribute"
		);

		public static readonly TemplateInfo[] SpecificLogAttributes =
		[
			TraceAttribute,
			DebugAttribute,
			InfoAttribute,
			WarningAttribute,
			ErrorAttribute,
			CriticalAttribute,
		];

		public static readonly ImmutableDictionary<int, PurviewTypeInfo> LogLevelTypeMap =
			new Dictionary<int, PurviewTypeInfo>
			{
				{ 0, MicrosoftExtensions.LogLevel_Trace },
				{ 1, MicrosoftExtensions.LogLevel_Debug },
				{ 2, MicrosoftExtensions.LogLevel_Information },
				{ 3, MicrosoftExtensions.LogLevel_Warning },
				{ 4, MicrosoftExtensions.LogLevel_Error },
				{ 5, MicrosoftExtensions.LogLevel_Critical },
				{ 6, MicrosoftExtensions.LogLevel_None },
			}.ToImmutableDictionary();

		public static readonly ImmutableDictionary<TemplateInfo, int> SpecificLogAttributesToLevel =
			new Dictionary<TemplateInfo, int>
			{
				{ TraceAttribute, 0 },
				{ DebugAttribute, 1 },
				{ InfoAttribute, 2 },
				{ WarningAttribute, 3 },
				{ ErrorAttribute, 4 },
				{ CriticalAttribute, 5 },
			}.ToImmutableDictionary();

		public static TemplateInfo[] GetTemplates() =>
			[
				LoggerGenerationAttribute,
				LoggerAttribute,
				LogAttribute,
				LogPrefixType,
				ExpandEnumerableAttribute,
				TraceAttribute,
				DebugAttribute,
				InfoAttribute,
				WarningAttribute,
				ErrorAttribute,
				CriticalAttribute,
			];

		public static class MicrosoftExtensions
		{
			public const string Namespace = "Microsoft.Extensions.Logging";

			public static readonly PurviewTypeInfo ILogger = PurviewTypeFactory.Create(
				Namespace + '.' + nameof(ILogger)
			);
			public static readonly PurviewTypeInfo LoggerMessage = PurviewTypeFactory.Create(
				Namespace + '.' + nameof(LoggerMessage)
			);
			public static readonly PurviewTypeInfo LogLevel = PurviewTypeFactory.Create(
				Namespace + '.' + nameof(LogLevel)
			);
			public static readonly PurviewTypeInfo EventId = PurviewTypeFactory.Create(
				Namespace + '.' + nameof(EventId)
			);

			// Log Telemetry Gen.
			public static readonly PurviewTypeInfo LoggerMessageHelper = PurviewTypeFactory.Create(
				Namespace + '.' + nameof(LoggerMessageHelper)
			);
			public static readonly PurviewTypeInfo LogPropertiesAttribute =
				PurviewTypeFactory.Create(Namespace + '.' + nameof(LogPropertiesAttribute));
			public static readonly PurviewTypeInfo LogPropertyIgnoreAttribute =
				PurviewTypeFactory.Create(Namespace + '.' + nameof(LogPropertyIgnoreAttribute));

			public static readonly PurviewTypeInfo LogLevel_Trace = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Trace"
			);
			public static readonly PurviewTypeInfo LogLevel_Debug = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Debug"
			);
			public static readonly PurviewTypeInfo LogLevel_Information = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Information"
			);
			public static readonly PurviewTypeInfo LogLevel_Warning = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Warning"
			);
			public static readonly PurviewTypeInfo LogLevel_Error = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Error"
			);
			public static readonly PurviewTypeInfo LogLevel_Critical = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".Critical"
			);
			public static readonly PurviewTypeInfo LogLevel_None = PurviewTypeFactory.Create(
				LogLevel.FullyQualifiedName + ".None"
			);
		}
	}
}
