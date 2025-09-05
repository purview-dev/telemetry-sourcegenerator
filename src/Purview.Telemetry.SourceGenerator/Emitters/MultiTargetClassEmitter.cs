using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Emitters;

static partial class MultiTargetClassEmitter
{
	public static void GenerateImplementation(
		MultiTargetInterface targetInterface,
		SourceProductionContext context,
		GenerationLogger? logger
	)
	{
		var interfaceName = targetInterface.InterfaceName;
		var classNamespace = targetInterface.Namespace;

		var classNameToGenerate = targetInterface.TelemetryGeneration.ClassName.Or(
			GenerateClassName(interfaceName)
		);

		// Create a target record that follows the same pattern as other targets
		var target = new MultiTargetGenerationTarget(
			TelemetryGeneration: targetInterface.TelemetryGeneration,
			GenerationType: targetInterface.GenerationType,
			ClassNameToGenerate: classNameToGenerate,
			ClassNamespace: classNamespace,
			ParentClasses: targetInterface.ParentClasses,
			FullNamespace: string.IsNullOrEmpty(classNamespace) ? null : classNamespace + ".",
			FullyQualifiedName: (string.IsNullOrEmpty(classNamespace) ? "" : classNamespace + ".")
				+ classNameToGenerate,
			InterfaceType: PurviewTypeFactory.Create(targetInterface.InterfaceSymbol),
			Methods: targetInterface.Methods,
			DuplicateMethods: ImmutableDictionary<string, Location[]>.Empty,
			Failures: null
		);

		logger?.Debug($"Generating multi-target implementation for: {interfaceName}");

		StringBuilder builder = new();

		// Add header
		EmbeddedResources.Instance.AddHeader(builder);

		var indent = EmitHelpers.EmitNamespaceStart(
			target.ClassNamespace,
			target.ParentClasses,
			builder,
			context.CancellationToken
		);

		indent = EmitHelpers.EmitClassStart(
			target.GenerationType,
			target.GenerationType,
			target.ClassNameToGenerate,
			target.InterfaceType.FullyQualifiedName,
			builder,
			indent,
			context.CancellationToken
		);

		EmitFields(target, builder, indent, context, logger);
		EmitConstructor(target, builder, indent, context, logger);
		EmitMethods(target, builder, indent, context, logger);

		EmitHelpers.EmitClassEnd(builder, indent);

		EmitHelpers.EmitNamespaceEnd(
			target.ClassNamespace,
			target.ParentClasses,
			indent,
			builder,
			context.CancellationToken
		);

		var hintName = $"{target.FullyQualifiedName}.MultiTarget.g.cs";
		context.AddSource(
			hintName,
			Microsoft.CodeAnalysis.Text.SourceText.From(builder.ToString(), Encoding.UTF8)
		);

		// Emit DI helper for multi-target implementations as well
		DependencyInjectionClassEmitter.GenerateImplementation(
			/* requestingType */target.GenerationType,
			/* attribute */target.TelemetryGeneration,
			/* generationType */target.GenerationType,
			/* implementationClassName */target.ClassNameToGenerate,
			/* sourceInterfaceName */target.InterfaceType.TypeName,
			/* fullyQualifiedNamespace */target.FullNamespace,
			context,
			logger
		);
	}

	static string GenerateClassName(string name)
	{
		if (name[0] == 'I')
			name = name.Substring(1);

		return name + "Core";
	}
}
