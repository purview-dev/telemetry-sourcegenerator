namespace Purview.Telemetry
{

/// <summary>
/// Determines how meter names are generated when not explicitly specified.
/// </summary>
{CodeGen}
enum MeterNameGenerationType
{
	/// <summary>
	/// OpenTelemetry convention: Assembly name lowercased with dots separating namespaces.
	/// Example: "MyCompany.MyApp" becomes "mycompany.myapp".
	/// Follows OpenTelemetry semantic conventions for meter naming.
	/// </summary>
	OpenTelemetry = 0,

	/// <summary>
	/// .NET convention (default): Assembly name preserved as-is without case transformation.
	/// Example: "MyCompany.MyApp" remains "MyCompany.MyApp".
	/// </summary>
	DotNet = 1
}
}
