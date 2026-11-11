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
			writer.IfBlock(factoryParameter + " == null", body =>
				body.Throw($"new global::System.ArgumentNullException(nameof({factoryParameter}))")
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
				.Write(')');

			// .NET 11's ActivitySourceOptions carries the source's version, telemetry schema URL and tags. They are
			// emitted only when set, so a source whose assembly set none keeps the single-argument constructor.
			// The version falls back to the assembly version, resolved once per compilation on the capabilities.
			var version = output.Target.ActivitySourceGenerationAttribute?.Version
				?? output.Context.Capabilities.AssemblyVersion;
			var telemetrySchemaUrl = output.Target.ActivitySourceGenerationAttribute?.TelemetrySchemaUrl;
			var tags = SharedHelpers.MergeTags(output.Target.ActivitySourceGenerationAttribute?.Tags, null);

			if (!string.IsNullOrWhiteSpace(version) || !string.IsNullOrWhiteSpace(telemetrySchemaUrl) || !tags.IsEmpty)
			{
				writer
					.Write(' ')
					.Line("{")
					.Indented(body =>
					{
						if (!string.IsNullOrWhiteSpace(version))
							body.Write("Version = ").Line(version!.Wrap() + ",");

						if (!string.IsNullOrWhiteSpace(telemetrySchemaUrl))
							body.Write("TelemetrySchemaUrl = ").Line(telemetrySchemaUrl!.Wrap() + ",");

						if (!tags.IsEmpty)
						{
							body.Write(
								"Tags = new global::System.Collections.Generic.KeyValuePair<string, object?>[] {"
							);
							var first = true;
							foreach (var tag in tags)
							{
								var separator = tag.IndexOf('=');
								if (separator <= 0)
									continue;

								if (!first)
									body.Write(", ");

								first = false;
								body.Write("new(")
									.Write(tag.Substring(0, separator).Wrap())
									.Write(", ")
									.Write(tag.Substring(separator + 1).Wrap())
									.Write(')');
							}

							body.Line(" },");
						}
					})
					.Line("});");
			}
			else
			{
				writer.Line(");");
			}
		}
	}
}
