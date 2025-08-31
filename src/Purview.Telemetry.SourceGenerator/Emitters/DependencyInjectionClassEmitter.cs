using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static class DependencyInjectionClassEmitter
{
	public static void GenerateImplementation(
		GenerationType requestingType,
		TelemetryGenerationAttributeRecord attribute,
		GenerationType generationType,
		string implementationClassName,
		string sourceInterfaceName,
		string? fullyQualifiedNamespace,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (attribute.GenerateDependencyExtension.Value == false)
		{
			logger?.Debug("Skipping dependency injection emit.");
			return;
		}

		if (!SharedHelpers.ShouldEmit(requestingType, generationType))
		{
			logger?.Debug(
				$"Skipping dependency injection emit for {requestingType} ({generationType})."
			);
			return;
		}

		StringBuilder builder = new();

		var classNameToGenerate = attribute.DependencyInjectionClassName.Value;
		if (string.IsNullOrWhiteSpace(classNameToGenerate))
			classNameToGenerate = implementationClassName + "DIExtension";

		var classAccessModifier =
			(attribute.DependencyInjectionClassIsPublic.Value ?? false)
				? "public static"
				: "static";

		logger?.Debug(
			$"Generating service dependency class {classNameToGenerate} for: {fullyQualifiedNamespace}{sourceInterfaceName}"
		);

		context.CancellationToken.ThrowIfCancellationRequested();

		builder
			.Append("namespace ")
			.AppendLine(Constants.DependencyInjection.DependencyInjectionNamespace)
			.AppendLine('{');

		builder
			.ClassAttributes(1)
			.Append(1, $"{classAccessModifier} class ", withNewLine: false)
			.Append(classNameToGenerate)
			.AppendLine()
			.Append(1, '{');

		EmitMethod(
			builder,
			2,
			implementationClassName,
			sourceInterfaceName,
			fullyQualifiedNamespace,
			logger,
			context.CancellationToken
		);

		EmitHelpers.EmitClassEnd(builder, 1);

		builder.AppendLine('}');

		EmbeddedResources.Instance.AddHeader(builder);
		var hintName = $"{fullyQualifiedNamespace}{classNameToGenerate}.DependencyInjection.g.cs";

		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(builder.ToString(), Encoding.UTF8)
		);
	}

	static void EmitMethod(
		StringBuilder builder,
		int indent,
		string className,
		string interfaceName,
		string? fullyQualifiedNamespace,
		GenerationLogger? logger,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		logger?.Debug($"Emitting DI method for {interfaceName}.");

		var methodName = interfaceName;
		if (methodName[0] == 'I')
			methodName = methodName.Substring(1);

		methodName = $" Add{methodName}";

		builder
			.CodeGen(indent)
			.AggressiveInlining(indent)
			.Append(indent, "public static ", withNewLine: false)
			.Append(Constants.DependencyInjection.IServiceCollection)
			.Append(methodName)
			.Append("(this ")
			.Append(Constants.DependencyInjection.IServiceCollection)
			.AppendLine(" services)")
			.Append(indent, '{')
			.Append(indent + 1, "return services.AddSingleton<", withNewLine: false)
			// serviceType...
			.Append("global::")
			.Append(fullyQualifiedNamespace!)
			.Append(interfaceName)
			.Append(", ")
			// implementationType
			.Append("global::")
			.Append(fullyQualifiedNamespace!)
			.Append(className)
			.AppendLine(">();")
			.Append(indent, '}');
	}
}
