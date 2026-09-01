namespace Purview.Telemetry.SourceGenerator.Templates;

sealed record TemplateInfo(TypeIdentity TypeInfo, string? Source, string TemplateData) : IEquatable<TypeIdentity>
{
	public string Name => TypeInfo.Name;

	public string GetGeneratedFilename() => $"{Name}.g.cs";

	public bool Equals(TypeIdentity other) => other.Equals(TypeInfo);

	public static TemplateInfo Create(string fullTypeName, bool attachHeader = true)
	{
		var lastDotIndex = fullTypeName.LastIndexOf('.');
		var typeName = fullTypeName.Substring(lastDotIndex + 1);
		var @namespace = fullTypeName.Substring(0, lastDotIndex);

		return Create(new TypeIdentity(typeName, @namespace), attachHeader);
	}

	public static TemplateInfo Create(TypeIdentity type, bool attachHeader = true)
	{
		var source = type.Namespace?.Split('.') ?? [];
		var isRootSources = source.Length == 2;
		var sourceToUse = isRootSources ? null : source.LastOrDefault();

		var template = EmbeddedResources.Instance.LoadTemplateForEmitting(sourceToUse, type.Name, attachHeader);

		return new(type, sourceToUse, template);
	}

	public static implicit operator string(TemplateInfo templateInfo) => templateInfo.TypeInfo.RenderFullName;
}
