using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Non-type generator configuration and constants.
/// </summary>
static class PropertyLibrary
{
	public const string PurviewTelemetryNamespace = TelemetryAttributeNames.PurviewTelemetryNamespace;
	public const string SystemDiagnosticsNamespace = TelemetryAttributeNames.SystemDiagnosticsNamespace;
	public const string EmbedAttributesHashDefineName = "PURVIEW_TELEMETRY_ATTRIBUTES";

	public static readonly Lazy<Version> Version = new(() => typeof(PropertyLibrary).Assembly.GetName().Version);

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

	public static class System
	{
		public const string NullKeyword = "null";
	}

	// TEMPORARY attribute strings still referenced by emitters; removed during the
	// structured-emission pass.
	public static class Activities
	{
		public const bool UseRecordExceptionRulesDefault = true;
		public const bool RecordExceptionEscapedDefault = true;
		public static string ActivitySourceAttributeShortName =>
			TelemetryAttributeNames.Activities.ActivitySourceAttribute.RenderAttributeTypeName;
		public const string DefaultActivitySourceName = "purview";
		public const int DefaultActivityKind = 0;
		public const string ActivitySourceFieldName = "_activitySource";
		public const string ActivityVariableName = "activity";
		public const string ParentIdParameterName = "parentId";
		public const string StartTimeParameterName = "startTime";
		public const string TimeStampParameterName = "timestamp";
		public const string StatusCode_Key = "otel.status_code";
		public const string StatusDescription_Key = "otel.status_description";
		public const string Tag_ExceptionEventName = "exception";
		public const string Tag_ExceptionEscaped = "exception.escaped";
		public const string Tag_ExceptionType = "exception.type";
		public const string Tag_ExceptionMessage = "exception.message";
		public const string Tag_ExceptionStackTrace = "exception.stacktrace";
		public const string RecordExceptionMethodName = "RecordExceptionInternal";

		public static readonly string ActivityKind_Internal =
			TypeLibrary.Activities.SystemDiagnostics.ActivityKind.StaticMember("Internal");
		public static readonly string ActivityKind_Server =
			TypeLibrary.Activities.SystemDiagnostics.ActivityKind.StaticMember("Server");
		public static readonly string ActivityKind_Client =
			TypeLibrary.Activities.SystemDiagnostics.ActivityKind.StaticMember("Client");
		public static readonly string ActivityKind_Producer =
			TypeLibrary.Activities.SystemDiagnostics.ActivityKind.StaticMember("Producer");
		public static readonly string ActivityKind_Consumer =
			TypeLibrary.Activities.SystemDiagnostics.ActivityKind.StaticMember("Consumer");

		public static readonly string ActivityStatusCode_Unset =
			TypeLibrary.Activities.SystemDiagnostics.ActivityStatusCode.StaticMember("Unset");
		public static readonly string ActivityStatusCode_Ok =
			TypeLibrary.Activities.SystemDiagnostics.ActivityStatusCode.StaticMember("Ok");
		public static readonly string ActivityStatusCode_Error =
			TypeLibrary.Activities.SystemDiagnostics.ActivityStatusCode.StaticMember("Error");

		public static readonly ImmutableDictionary<int, string> ActivityKindTypeMap = new Dictionary<int, string>
		{
			{ 0, ActivityKind_Internal },
			{ 1, ActivityKind_Server },
			{ 2, ActivityKind_Client },
			{ 3, ActivityKind_Producer },
			{ 4, ActivityKind_Consumer },
		}.ToImmutableDictionary();

		public static readonly ImmutableDictionary<int, string> ActivityStatusCodeMap = new Dictionary<int, string>
		{
			{ 0, ActivityStatusCode_Unset },
			{ 1, ActivityStatusCode_Ok },
			{ 2, ActivityStatusCode_Error },
		}.ToImmutableDictionary();
	}

	public static class Logging
	{
		public const int UnboundedIEnumerableMaxCountBeforeDiagnostic = 5;
		public const int MaxNonExceptionParameters = 6;
		public const string DefaultLogLevelConstantName = "DEFAULT_LOGLEVEL";
		public static string LoggerAttributeShortName =>
			TelemetryAttributeNames.Logging.LoggerAttribute.RenderAttributeTypeName;
		public const string LoggerFieldName = "_logger";
		public const int DefaultLevel = 2;

		public static string ILoggerOfTMetadataName => TelemetryAttributeNames.Logging.ILoggerOfT.MetadataFullName;

		public static readonly string LogLevel_Trace = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"Trace"
		);
		public static readonly string LogLevel_Debug = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"Debug"
		);
		public static readonly string LogLevel_Information =
			TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember("Information");
		public static readonly string LogLevel_Warning = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"Warning"
		);
		public static readonly string LogLevel_Error = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"Error"
		);
		public static readonly string LogLevel_Critical = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"Critical"
		);
		public static readonly string LogLevel_None = TypeLibrary.Logging.MicrosoftExtensions.LogLevel.StaticMember(
			"None"
		);

		public static readonly ImmutableDictionary<int, string> LogLevelTypeMap = new Dictionary<int, string>
		{
			{ 0, LogLevel_Trace },
			{ 1, LogLevel_Debug },
			{ 2, LogLevel_Information },
			{ 3, LogLevel_Warning },
			{ 4, LogLevel_Error },
			{ 5, LogLevel_Critical },
			{ 6, LogLevel_None },
		}.ToImmutableDictionary();
	}

	public static class Metrics
	{
		public const int MinimumParamsForTagList = 4;
		public static string MeterAttributeShortName =>
			TelemetryAttributeNames.Metrics.MeterAttribute.RenderAttributeTypeName;
		public static string AutoCounterAttributeShortName =>
			TelemetryAttributeNames.Metrics.AutoCounterAttribute.RenderAttributeTypeName;
		public static string CounterAttributeShortName =>
			TelemetryAttributeNames.Metrics.CounterAttribute.RenderAttributeTypeName;
		public static string HistogramAttributeShortName =>
			TelemetryAttributeNames.Metrics.HistogramAttribute.RenderAttributeTypeName;
		public static string UpDownCounterAttributeShortName =>
			TelemetryAttributeNames.Metrics.UpDownCounterAttribute.RenderAttributeTypeName;
		public const string MeterInitializationMethod = "InitializeMeters";
		public const string MeterFactoryParameterName = "meterFactory";
		public const string InstrumentSeparatorDefault = ".";
		public const bool LowercaseInstrumentNameDefault = true;
		public const bool LowercaseTagKeysDefault = true;

		public const string ObservableCounterNoun = "ObservableCounter";
		public const string ObservableGaugeNoun = "ObservableGauge";
		public const string ObservableUpDownCounterNoun = "ObservableUpDownCounter";

		public static string CounterMetadataName => TelemetryAttributeNames.Metrics.Counter.MetadataFullName;
		public static string HistogramMetadataName => TelemetryAttributeNames.Metrics.Histogram.MetadataFullName;
		public static string UpDownCounterMetadataName =>
			TelemetryAttributeNames.Metrics.UpDownCounter.MetadataFullName;

		public static readonly TypeMapping[] ValidMeasurementKeywordTypes =
		[
			KnownLangTypes.Get(SpecialType.System_Byte),
			KnownLangTypes.Get(SpecialType.System_Int16),
			KnownLangTypes.Get(SpecialType.System_Int32),
			KnownLangTypes.Get(SpecialType.System_Int64),
			KnownLangTypes.Get(SpecialType.System_Double),
			KnownLangTypes.Get(SpecialType.System_Single),
			KnownLangTypes.Get(SpecialType.System_Decimal),
		];
	}

	public static class DependencyInjection
	{
		public const string DependencyInjectionNamespace = "Microsoft.Extensions.DependencyInjection";
		public static readonly string Singleton = TypeLibrary.DependencyInjection.ServiceLifetime.StaticMember(
			"Singleton"
		);
	}
}
