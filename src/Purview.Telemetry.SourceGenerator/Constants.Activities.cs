using System.Collections.Immutable;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry;

partial class Constants
{
	public static partial class Activities
	{
		public const bool UseRecordExceptionRulesDefault = true;
		public const bool RecordExceptionEscapedDefault = true;

		public const string DefaultActivitySourceName = "purview";
		public const int DefaultActivityKind = 0;

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

		public static readonly PurviewTypeInfo ActivitySourceGenerationAttribute =
			PurviewTypeFactory.Create(
				"Purview.Telemetry.Activities.ActivitySourceGenerationAttribute"
			);
		public static readonly PurviewTypeInfo ActivitySourceAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.ActivitySourceAttribute"
		);
		public static readonly PurviewTypeInfo ActivityAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.ActivityAttribute"
		);
		public static readonly PurviewTypeInfo EventAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.EventAttribute"
		);
		public static readonly PurviewTypeInfo ContextAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.ContextAttribute"
		);
		public static readonly PurviewTypeInfo BaggageAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.BaggageAttribute"
		);
		public static readonly PurviewTypeInfo EscapeAttribute = PurviewTypeFactory.Create(
			"Purview.Telemetry.Activities.EscapeAttribute"
		);
		public static readonly PurviewTypeInfo StatusDescriptionAttribute =
			PurviewTypeFactory.Create("Purview.Telemetry.Activities.StatusDescriptionAttribute");

		public static readonly Lazy<ImmutableDictionary<int, string>> ActivityKindTypeMap = new(
			CreateActivityKindTypeMap
		);
		public static readonly Lazy<ImmutableDictionary<int, string>> ActivityStatusCodeMap = new(
			CreateActivityStatusCodeMap
		);

		static ImmutableDictionary<int, string> CreateActivityKindTypeMap()
		{
			var activityKindBuilder = ImmutableDictionary.CreateBuilder<int, string>();
			activityKindBuilder.Add(0, SystemDiagnostics.ActivityKind_Internal);
			activityKindBuilder.Add(1, SystemDiagnostics.ActivityKind_Server);
			activityKindBuilder.Add(2, SystemDiagnostics.ActivityKind_Client);
			activityKindBuilder.Add(3, SystemDiagnostics.ActivityKind_Producer);
			activityKindBuilder.Add(4, SystemDiagnostics.ActivityKind_Consumer);

			return activityKindBuilder.ToImmutable();
		}

		static ImmutableDictionary<int, string> CreateActivityStatusCodeMap()
		{
			var activityStatusCodeBuilder = ImmutableDictionary.CreateBuilder<int, string>();
			activityStatusCodeBuilder.Add(0, SystemDiagnostics.ActivityStatusCode_Unset);
			activityStatusCodeBuilder.Add(1, SystemDiagnostics.ActivityStatusCode_Ok);
			activityStatusCodeBuilder.Add(2, SystemDiagnostics.ActivityStatusCode_Error);
			return activityStatusCodeBuilder.ToImmutable();
		}

		public static class SystemDiagnostics
		{
			public static readonly PurviewTypeInfo Activity = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".Activity"
			);

			public static readonly PurviewTypeInfo ActivitySource = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivitySource"
			);

			public static readonly PurviewTypeInfo ActivityEvent = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityEvent"
			);

			public static readonly PurviewTypeInfo ActivityContext = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityContext"
			);

			public static readonly PurviewTypeInfo ActivityKind = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityKind"
			);

			public static readonly PurviewTypeInfo ActivityStatusCode = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityStatusCode"
			);

			public static readonly PurviewTypeInfo ActivityTagsCollection =
				PurviewTypeFactory.Create(SystemDiagnosticsNamespace + ".ActivityTagsCollection");

			public static readonly PurviewTypeInfo ActivityTagIEnumerable =
				System.IEnumerable.MakeGeneric(
					PurviewTypeFactory.Create(
						"System.Collections.Generic.KeyValuePair<string, object?>"
					)
				);

			public static readonly PurviewTypeInfo ActivityLink = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityLink"
			);

			public static readonly PurviewTypeInfo ActivityLinkIEnumerable =
				PurviewTypeFactory.Create(
					$"System.Collections.Generic.IEnumerable<{ActivityLink}>"
				);

			public static readonly PurviewTypeInfo ActivityLinkArray = PurviewTypeFactory.Create(
				SystemDiagnosticsNamespace + ".ActivityLink[]"
			);

			public static readonly string ActivityKind_Internal = ActivityKind + ".Internal";
			public static readonly string ActivityKind_Server = ActivityKind + ".Server";
			public static readonly string ActivityKind_Client = ActivityKind + ".Client";
			public static readonly string ActivityKind_Producer = ActivityKind + ".Producer";
			public static readonly string ActivityKind_Consumer = ActivityKind + ".Consumer";

			public static readonly string ActivityStatusCode_Unset = ActivityStatusCode + ".Unset";
			public static readonly string ActivityStatusCode_Ok = ActivityStatusCode + ".Ok";
			public static readonly string ActivityStatusCode_Error = ActivityStatusCode + ".Error";
		}
	}
}
