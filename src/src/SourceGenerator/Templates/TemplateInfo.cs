namespace Purview.Telemetry.SourceGenerator.Templates;

sealed record TemplateInfo(TypeIdentity TypeInfo, string? Source, string TemplateData) : IEquatable<TypeIdentity>
{
	public string Name => TypeInfo.Name;

	public string GetGeneratedFilename() => $"{Name}.g.cs";

	public bool Equals(TypeIdentity other) => other.Equals(TypeInfo);

	public static TemplateInfo Create(string fullTypeName)
	{
		var lastDotIndex = fullTypeName.LastIndexOf('.');
		var typeName = fullTypeName.Substring(lastDotIndex + 1);
		var @namespace = fullTypeName.Substring(0, lastDotIndex);

		return Create(new TypeIdentity(typeName, @namespace));
	}

	public static TemplateInfo Create(TypeIdentity type)
	{
		var source = type.Namespace?.Split('.') ?? [];
		var isRootSources = source.Length == 2;
		var sourceToUse = isRootSources ? null : source.LastOrDefault();

		var template = SourceEmitter.Emit(type.Name);

		return new(type, sourceToUse, template);
	}

	public static implicit operator string(TemplateInfo templateInfo) => templateInfo.TypeInfo.RenderFullName;
}
