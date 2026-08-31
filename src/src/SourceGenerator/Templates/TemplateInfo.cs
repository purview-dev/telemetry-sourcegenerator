namespace Purview.Telemetry.SourceGenerator.Templates;

record TemplateInfo(PurviewTypeInfo TypeInfo, string? Source, string TemplateData) : IEquatable<PurviewTypeInfo>
{
	public string Name => TypeInfo.TypeName;

	public string GetGeneratedFilename() => $"{Name}.g.cs";

	public bool Equals(PurviewTypeInfo? other) => other != null && other == TypeInfo;

	public static TemplateInfo Create(string fullTypeName, bool attachHeader = true)
	{
		var purviewType = PurviewTypeFactory.Create(fullTypeName);
		var source = purviewType.Namespace!.Split('.');
		var isRootSources = source.Length == 2;
		var sourceToUse = isRootSources ? null : source.Last();

		var template = EmbeddedResources.Instance.LoadTemplateForEmitting(
			sourceToUse,
			purviewType.TypeName,
			attachHeader
		);
		TemplateInfo templateInfo = new(purviewType, sourceToUse, template);

		return templateInfo;
	}

	public static implicit operator string(TemplateInfo templateInfo) => templateInfo.TypeInfo;
}
