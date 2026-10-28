namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class TypeLibraryGenerator
{
	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity TagList = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity Activity = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivitySource = default;

	/// <summary>
	/// .NET 11 (System.Diagnostics.DiagnosticSource 11): the dependency-injected creator of activity sources,
	/// the tracing counterpart of <c>IMeterFactory</c>. Emitted only when the compilation can resolve it.
	/// </summary>
	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivitySourceFactory = default;

	/// <summary>.NET 10 (System.Diagnostics.DiagnosticSource 10): the options an activity source is created from.</summary>
	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivitySourceOptions = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityEvent = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityContext = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityKind = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityStatusCode = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityTagsCollection = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	static readonly TypeIdentity ActivityLink = default;

	[TypeRef(SystemDiagnosticsNamespace)]
	internal static readonly TypeReference ActivityTagIEnumerable =
		TypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(
			TypeLibrary.System.Collections.Generic.KeyValuePair.MakeGeneric(
				TypeLibrary.System.String,
				TypeLibrary.System.Object
			)
		);

	[TypeRef(SystemDiagnosticsNamespace)]
	internal static readonly TypeReference ActivityLinkIEnumerable =
		TypeLibrary.System.Collections.Generic.IEnumerable.MakeGeneric(ActivityLink);

	[TypeRef(SystemDiagnosticsNamespace)]
	internal static readonly TypeReference ActivityLinkArray = new TypeReference(ActivityLink).MakeArray();
}
