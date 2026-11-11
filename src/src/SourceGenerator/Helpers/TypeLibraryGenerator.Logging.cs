namespace Purview.Telemetry.SourceGenerator.Helpers;

partial class TypeLibraryGenerator
{
	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity ILogger = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity LoggerMessage = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity LogLevel = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity EventId = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity LoggerMessageHelper = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity LogPropertiesAttribute = default;

	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity LogPropertyIgnoreAttribute = default;

	/// <summary>
	/// <c>Microsoft.Extensions.Logging.TagProviderAttribute</c>, shipped by Microsoft.Extensions.Telemetry.Abstractions.
	/// The generator reads that type rather than declaring a Purview duplicate: a second
	/// <c>Purview.Telemetry.TagProviderAttribute</c> would make every <c>[TagProvider]</c> ambiguous in a consumer that
	/// imports both namespaces for <c>ILogger</c> and <c>[Logger]</c> (CS0104).
	/// </summary>
	[TypeRef(LoggingNamespace)]
	static readonly TypeIdentity TagProviderAttribute = default;
}
