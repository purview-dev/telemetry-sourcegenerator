namespace Purview.Telemetry.SourceGenerator.Templates;

/// <summary>
/// Identity of a marker-attribute template. The template source is not held in memory;
/// it is emitted with a <see cref="CodeWriter"/> during <c>RegisterPostInitializationOutput</c>
/// (see <c>MarkerAttributeTemplateEmitter</c>).
/// </summary>
sealed record TemplateInfo(TypeIdentity TypeInfo) : IEquatable<TypeIdentity>
{
	public string Name => TypeInfo.Name;

	public string GetGeneratedFilename() => $"{Name}.g.cs";

	public bool Equals(TypeIdentity other) => other.Equals(TypeInfo);

	public static TemplateInfo Create(TypeIdentity type) => new(type);

	public static implicit operator string(TemplateInfo templateInfo) => templateInfo.TypeInfo.RenderFullName;
}
