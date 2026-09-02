using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class DependencyInjectionClassEmitter
{
	public static void GenerateImplementation(
		CodeWriter writer,
		GenerationType requestingType,
		TelemetryGenerationAttributeData attribute,
		GenerationType generationType,
		string implementationClassName,
		TypeReference interfaceType,
		SourceProductionContext context,
		GenerationContext<TelemetryCapabilities> generationContext
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (!attribute.GenerateDependencyExtension)
		{
			generationContext.Debug("Skipping dependency injection emit.");
			return;
		}

		if (!SharedHelpers.ShouldEmitDIExtension(requestingType, generationType))
		{
			generationContext.Debug($"Skipping dependency injection emit for {requestingType} ({generationType}).");
			return;
		}

		var classNameToGenerate = attribute.DependencyInjectionClassName;
		if (string.IsNullOrWhiteSpace(classNameToGenerate))
			classNameToGenerate = implementationClassName + "DIExtension";

		var classAccessibility = attribute.DependencyInjectionClassIsPublic
			? TypeDeclarationAccessibility.Public
			: TypeDeclarationAccessibility.Internal;

		generationContext.Debug(
			$"Generating service dependency class {classNameToGenerate} for: {interfaceType.RenderFullName}"
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		// When the DI class is placed in a custom namespace (TelemetryNamesNamespace), the
		// AddSingleton extension method is no longer in scope, so import it explicitly.
		if (attribute.TelemetryNamesNamespace != null)
			writer.WriteUsing(PropertyLibrary.DependencyInjection.DependencyInjectionNamespace);

		using (
			writer.WriteBlockNamespaceScope(
				attribute.TelemetryNamesNamespace ?? PropertyLibrary.DependencyInjection.DependencyInjectionNamespace
			)
		)
		{
			using (
				writer.WriteClassScope(
					new(classNameToGenerate!, classAccessibility)
					{
						IsStatic = true,
						IncludeGeneratedAttributes = false,
						Attributes = [EmitterHelpers.EditorBrowsableAttribute()],
					}
				)
			)
			{
				EmitMethod(
					writer,
					implementationClassName,
					interfaceType,
					attribute.TelemetryNamesNamespace,
					generationContext,
					context.CancellationToken
				);
			}
		}

		var hintName =
			$"{BuildImplQualifiedName(attribute.TelemetryNamesNamespace, interfaceType, classNameToGenerate!)}.DependencyInjection.g.cs";
		context.AddSource(hintName, writer);
	}

	static void EmitMethod(
		CodeWriter writer,
		string className,
		TypeReference interfaceType,
		string? telemetryNamesNamespace,
		GenerationContext<TelemetryCapabilities> generationContext,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		var interfaceName = interfaceType.Identity.Name;
		var methodName = interfaceName;
		if (methodName[0] == 'I')
			methodName = methodName.Substring(1);

		generationContext.Debug($"Emitting DI method for {interfaceName}.");

		using (
			writer.WriteMethodScope(
				new MethodDeclarationOptions(
					"Add" + methodName,
					TypeLibrary.DependencyInjection.IServiceCollection,
					TypeDeclarationAccessibility.Public
				)
				{
					IsStatic = true,
					Parameters =
					[
						new ParameterDeclarationOptions("services", TypeLibrary.DependencyInjection.IServiceCollection)
						{
							IsThis = true,
						},
					],
					IncludeGeneratedAttributes = false,
				}
			)
		)
		{
			writer
				.Write("return services.AddSingleton<")
				.Write(interfaceType.RenderFullName)
				.Write(", ")
				.Write("global::")
				.Write(BuildImplQualifiedName(telemetryNamesNamespace, interfaceType, className))
				.WriteLine(">();");
		}
	}

	/// <summary>
	/// Builds the fully-qualified (non <c>global::</c>-prefixed) name of the generated implementation
	/// class from the interface identity's namespace and containing-type chain plus the class name.
	/// When <paramref name="telemetryNamesNamespace"/> is supplied, the implementation class lives in
	/// that namespace instead.
	/// </summary>
	static string BuildImplQualifiedName(string? telemetryNamesNamespace, TypeReference interfaceType, string className)
	{
		var identity = interfaceType.Identity;

		var builder = new StringBuilder();
		var ns = telemetryNamesNamespace ?? identity.Namespace;
		if (ns != null)
			builder.Append(ns).Append('.');

		if (telemetryNamesNamespace == null)
		{
			foreach (var containingType in identity.ContainingTypes)
				builder.Append(containingType.Name).Append('.');
		}

		builder.Append(className);

		return builder.ToString();
	}
}
