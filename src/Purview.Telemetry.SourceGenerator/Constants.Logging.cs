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

		public static readonly PurviewTypeInfo LoggerGenerationAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.Logging.LoggerGenerationAttribute");
		public static readonly PurviewTypeInfo LoggerAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.LoggerAttribute"
		);
		public static readonly PurviewTypeInfo LogAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.LogAttribute"
		);
		public static readonly PurviewTypeInfo LogPrefixType = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.LogPrefixType"
		);

		public static readonly PurviewTypeInfo ExpandEnumerableAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.Logging.ExpandEnumerableAttribute");

		public static readonly PurviewTypeInfo TraceAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.TraceAttribute"
		);
		public static readonly PurviewTypeInfo DebugAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.DebugAttribute"
		);
		public static readonly PurviewTypeInfo InfoAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.InfoAttribute"
		);
		public static readonly PurviewTypeInfo WarningAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.WarningAttribute"
		);
		public static readonly PurviewTypeInfo ErrorAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.ErrorAttribute"
		);
		public static readonly PurviewTypeInfo CriticalAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Logging.CriticalAttribute"
		);

		public static readonly PurviewTypeInfo[] SpecificLogAttributes =
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

		public static readonly ImmutableDictionary<
			PurviewTypeInfo,
			int
		> SpecificLogAttributesToLevel = new Dictionary<PurviewTypeInfo, int>
		{
			{ TraceAttribute, 0 },
			{ DebugAttribute, 1 },
			{ InfoAttribute, 2 },
			{ WarningAttribute, 3 },
			{ ErrorAttribute, 4 },
			{ CriticalAttribute, 5 },
		}.ToImmutableDictionary();

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
