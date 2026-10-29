using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

partial class ActivitySourceTargetClassEmitter
{
	static void EmitFields(ActivityOutputContext output, CodeWriter writer, SourceProductionContext context)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		var target = output.Target;
		var activitySourceName = target.ActivitySourceName;
		if (string.IsNullOrWhiteSpace(activitySourceName))
		{
			output.Context.Diagnostic("No activity source specified.");

			activitySourceName = PropertyLibrary.Activities.DefaultActivitySourceName;
		}

		if (output.Context.Capabilities.UseActivitySourceFactory)
		{
			EmitFactoryCreatedActivitySource(output, writer, activitySourceName!);
			return;
		}

		writer.Field(
			new(
				PropertyLibrary.Activities.ActivitySourceFieldName,
				TypeLibrary.System.Diagnostics.ActivitySource.AsTypeReference()
			)
			{
				IsStatic = true,
				IsReadOnly = true,
				Initializer =
					$"new {(string)TypeLibrary.System.Diagnostics.ActivitySource}({activitySourceName!.Wrap()})",
			}
		);
	}

	/// <summary>
	/// .NET 11: the source is created per instance by the injected <c>ActivitySourceFactory</c>, which owns it, so
	/// the generated class never disposes it. The constructor calls the initialisation method before any generated
	/// method can run. For an Activities-only interface this emitter writes that constructor; otherwise the target
	/// that owns the constructor (Logging, then Metrics) adds the factory parameter and the call.
	/// </summary>
	static void EmitFactoryCreatedActivitySource(
		ActivityOutputContext output,
		CodeWriter writer,
		string activitySourceName
	)
	{
		writer.Field(
			new(
				PropertyLibrary.Activities.ActivitySourceFieldName,
				TypeLibrary.System.Diagnostics.ActivitySource.AsTypeReference()
			)
			{
				Initializer = "default!",
			}
		);

		if (output.Target.GenerationType == GenerationType.Activities)
		{
			writer.NewLine();
			using (
				writer.ConstructorScope(
					new ConstructorDeclarationOptions(
						output.Target.ClassNameToGenerate,
						TypeDeclarationAccessibility.Public
					)
					{
						Parameters = [ConstructorEmitter.ActivitySourceFactoryParameter()],
					}
				)
			)
			{
				ConstructorEmitter.EmitActivitySourceInitializationCall(writer);
			}
		}

		var factoryParameter = PropertyLibrary.Activities.ActivitySourceFactoryParameterName;

		writer.NewLine();
		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					PropertyLibrary.Activities.ActivitySourceInitializationMethod,
					TypeLibrary.System.Void.AsTypeReference(),
					TypeDeclarationAccessibility.Private
				)
				{
					Parameters = [ConstructorEmitter.ActivitySourceFactoryParameter()],
				}
			)
		)
		{
			writer.IfBlock(
				factoryParameter + " == null",
				body => body.Throw($"new global::System.ArgumentNullException(nameof({factoryParameter}))")
			);

			writer.NewLine();

			writer
				.Write(PropertyLibrary.Activities.ActivitySourceFieldName)
				.Write(" = ")
				.Write(factoryParameter)
				.Write(".Create(new ")
				.Write(TypeLibrary.System.Diagnostics.ActivitySourceOptions)
				.Write("(")
				.Write(activitySourceName.Wrap())
				.Line("));");
		}
	}
}
