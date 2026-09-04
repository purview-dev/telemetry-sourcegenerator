using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Emitters;

/// <summary>
/// Emits the marker-attribute templates injected into consuming compilations. Each template
/// previously shipped as a static embedded resource; it is now built in memory with a
/// <see cref="CodeWriter"/> inside <c>RegisterPostInitializationOutput</c>.
/// </summary>
static class GeneratedTypesEmitter
{
	static IEnumerable<(TypeIdentity Type, Action<CodeWriter, TypeIdentity> Emitter)> GetEmitters()
	{
		// Telemetry Shared
		yield return (
			TypeLibrary.TelemetryShared.TagAttribute,
			(writer, type) =>
				WriteTagLikeAttribute(writer, type, "Marks a parameter as a tag for an activity, event or instrument.")
		);

		yield return (
			TypeLibrary.TelemetryShared.ExcludeAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Method,
					includeSuppressMessage: false,
					summary: "Marks a method to be excluded from telemetry generation."
				)
		);
		yield return (TypeLibrary.TelemetryShared.TelemetryGenerationAttribute, WriteTelemetryGenerationAttribute);
		yield return (TypeLibrary.TelemetryShared.Targets, WriteTargetsEnum);
		yield return (TypeLibrary.TelemetryShared.NamingConvention, WriteNamingConventionEnum);
		yield return (TypeLibrary.TelemetryShared.ExcludeTargetsAttribute, WriteExcludeTargetsAttribute);
		// Activities
		yield return (
			TypeLibrary.Activities.BaggageAttribute,
			(writer, type) =>
				WriteTagLikeAttribute(writer, type, "Marks a parameter as baggage to be attached to an activity.")
		);
		yield return (TypeLibrary.Activities.ActivitySourceGenerationAttribute, WriteActivitySourceGenerationAttribute);
		yield return (TypeLibrary.Activities.ActivitySourceAttribute, WriteActivitySourceAttribute);
		yield return (TypeLibrary.Activities.ActivityAttribute, WriteActivityAttribute);
		yield return (TypeLibrary.Activities.EventAttribute, WriteEventAttribute);
		yield return (
			TypeLibrary.Activities.ContextAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Method,
					includeSuppressMessage: false,
					summary: "Marks a parameter as an activity context."
				)
		);
		yield return (
			TypeLibrary.Activities.EscapeAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					summary: "Marks a parameter as the escape flag for a recorded exception."
				)
		);
		yield return (
			TypeLibrary.Activities.StatusDescriptionAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					summary: "Marks a parameter as the status description of an activity or event."
				)
		);
		// Logging
		yield return (TypeLibrary.Logging.LoggerGenerationAttribute, WriteLoggerGenerationAttribute);
		yield return (TypeLibrary.Logging.LoggerAttribute, WriteLoggerAttribute);
		yield return (TypeLibrary.Logging.LogAttribute, WriteLogAttribute);
		yield return (TypeLibrary.Logging.LogPrefixType, WriteLogPrefixTypeEnum);
		yield return (TypeLibrary.Logging.LoggerGenerationMode, WriteLoggerGenerationModeEnum);
		yield return (TypeLibrary.Logging.ExpandEnumerableAttribute, WriteExpandEnumerableAttribute);
		yield return (
			TypeLibrary.Logging.TraceAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a trace-level log method.")
		);
		yield return (
			TypeLibrary.Logging.DebugAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a debug-level log method.")
		);
		yield return (
			TypeLibrary.Logging.InfoAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as an informational log method.")
		);
		yield return (
			TypeLibrary.Logging.WarningAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a warning-level log method.")
		);
		yield return (
			TypeLibrary.Logging.ErrorAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as an error-level log method.")
		);
		yield return (
			TypeLibrary.Logging.CriticalAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a critical-level log method.")
		);
		// Metrics
		yield return (TypeLibrary.Metrics.MeterGenerationAttribute, WriteMeterGenerationAttribute);
		yield return (TypeLibrary.Metrics.MeterAttribute, WriteMeterAttribute);
		yield return (TypeLibrary.Metrics.MeterNameGenerationType, WriteMeterNameGenerationTypeEnum);
		yield return (
			TypeLibrary.Metrics.InstrumentMeasurementAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					summary: "Marks a parameter as the measurement value of an instrument."
				)
		);
		yield return (
			TypeLibrary.Metrics.AutoCounterAttribute,
			(writer, type) =>
				WriteAutoCounterAttribute(writer, type, "Marks a method as an auto-incrementing counter instrument.")
		);
		yield return (
			TypeLibrary.Metrics.CounterAttribute,
			(writer, type) => WriteCounterLikeAttribute(writer, type, "Marks a method as a counter instrument.")
		);
		yield return (
			TypeLibrary.Metrics.UpDownCounterAttribute,
			(writer, type) =>
				WriteCounterLikeAttribute(writer, type, "Marks a method as an up-down counter instrument.")
		);
		yield return (
			TypeLibrary.Metrics.HistogramAttribute,
			(writer, type) => WriteCounterLikeAttribute(writer, type, "Marks a method as a histogram instrument.")
		);
		yield return (
			TypeLibrary.Metrics.ObservableCounterAttribute,
			(writer, type) =>
				WriteObservableCounterLikeAttribute(writer, type, "Marks a method as an observable counter instrument.")
		);
		yield return (
			TypeLibrary.Metrics.ObservableUpDownCounterAttribute,
			(writer, type) =>
				WriteObservableCounterLikeAttribute(
					writer,
					type,
					"Marks a method as an observable up-down counter instrument."
				)
		);
		yield return (
			TypeLibrary.Metrics.ObservableGaugeAttribute,
			(writer, type) =>
				WriteObservableCounterLikeAttribute(writer, type, "Marks a method as an observable gauge instrument.")
		);
	}

	public static void EmitAll(IncrementalGeneratorPostInitializationContext context)
	{
		// Adds Microsoft.CodeAnalysis.EmbeddedAttribute to the compilation so generated marker
		// types (decorated with [Microsoft.CodeAnalysis.Embedded]) are invisible to downstream
		// assemblies, preventing CS0436 conflicts when multiple projects reference this generator.
		context.AddEmbeddedAttributeDefinition();

		var settings = GenerationSettings.Create<TelemetrySourceGenerator>();
		foreach (var emitter in GetEmitters())
		{
			CodeWriter writer = new(settings);
			WriteMarkerFileHeader(writer);

			emitter.Emitter(writer, emitter.Type);

			context.AddSource($"{emitter.Type.MetadataFullName}.g.cs", writer);
		}
	}

	/// <summary>
	/// Writes the header for marker-attribute files. Nullable annotations are only enabled on modern
	/// targets — the injected templates declare plain <c>string</c> members for
	/// <c>NET48_OR_GREATER</c>/<c>PURVIEW_TELEMETRY_NON_NULLABLE</c> consumers — and CS8625 is
	/// suppressed because optional string parameters intentionally default to <see langword="null"/>.
	/// </summary>
	static void WriteMarkerFileHeader(CodeWriter writer)
	{
		writer.AutoGeneratedHeader(nullableDirective: NullableDirectiveMode.Disable);
		writer
			.HashDefines(
				"!NET48_OR_GREATER && !PURVIEW_TELEMETRY_NON_NULLABLE",
				hashWriter => hashWriter.Line("#nullable enable")
			)
			.PragmaDisable("CS8625");
	}

	// -------------------------------------------------------------------------------------------
	// Attribute templates
	// -------------------------------------------------------------------------------------------

	/// <summary>Writes a complete attribute-template file.</summary>
	static void EmitAttribute(
		CodeWriter writer,
		TypeIdentity type,
		AttributeTargets targets,
		Action<CodeWriter> body,
		bool wrapInExcludeLoggingGuard = false,
		bool includeSuppressMessage = true,
		string? summary = null
	)
	{
		using var scope = wrapInExcludeLoggingGuard
			? writer.HashDefinesScope("!EXCLUDE_PURVIEW_TELEMETRY_LOGGING")
			: writer.EmptyScope();

		var attributes = ImmutableArray<AttributeDeclarationOptions>.Empty;
		attributes = attributes.Add(ConditionalAttribute());
		if (includeSuppressMessage)
			attributes = attributes.Add(SuppressMessageAttribute());

		writer.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace);
		if (summary != null)
			writer.XmlSummary(summary);
		writer.AttributeClass(
			new(type.Name, TypeDeclarationAccessibility.Internal) { Attributes = attributes },
			targets,
			body
		);
	}

	static void WriteSimpleAttribute(
		CodeWriter writer,
		TypeIdentity type,
		AttributeTargets targets,
		bool includeSuppressMessage,
		string? summary = null
	)
	{
		EmitAttribute(
			writer,
			type,
			targets,
			static _ => { },
			includeSuppressMessage: includeSuppressMessage,
			summary: summary
		);
	}

	static AttributeDeclarationOptions ConditionalAttribute() =>
		new(new TypeIdentity("ConditionalAttribute", "System.Diagnostics"))
		{
			Arguments = [new("PURVIEW_TELEMETRY_ATTRIBUTES".Surround())],
		};

	static AttributeDeclarationOptions SuppressMessageAttribute() =>
		new(new TypeIdentity("SuppressMessageAttribute", "System.Diagnostics.CodeAnalysis"))
		{
			Arguments = [new("Design".Surround()), new("CA1019:Define accessors for attribute arguments".Surround())],
		};

	// -------------------------------------------------------------------------------------------
	// Members
	// -------------------------------------------------------------------------------------------

	static void WriteEmptyConstructor(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		if (summary != null)
			writer.XmlSummary(summary);
		writer.Constructor(new(type.Name, TypeDeclarationAccessibility.Public), static _ => { });
	}

	static void WriteNameConstructor(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		if (summary != null)
		{
			writer.XmlSummary(summary);
			writer.XmlParam("name", "The name of the telemetry entry.");
		}
		writer.Constructor(
			new(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = [new("name", PurviewTypeLibrary.System.String.AsTypeReference())],
			},
			ctor => ctor.Assignment("Name", "name")
		);
	}

	static void WriteMessageTemplateConstructor(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		if (summary != null)
		{
			writer.XmlSummary(summary);
			writer.XmlParam("messageTemplate", "The message template used to generate the log message.");
		}
		writer.Constructor(
			new(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = [new("messageTemplate", PurviewTypeLibrary.System.String.AsTypeReference())],
			},
			ctor => ctor.Assignment("MessageTemplate", "messageTemplate")
		);
	}

	static void WriteEventIdConstructor(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		if (summary != null)
		{
			writer.XmlSummary(summary);
			writer.XmlParam("eventId", "The event identifier of the log entry.");
		}
		writer.Constructor(
			new(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = [new("eventId", PurviewTypeLibrary.System.Int32.AsTypeReference())],
			},
			ctor => ctor.Assignment("EventId", "eventId")
		);
	}

	/// <summary>Writes a public property with generated attributes and an optional initializer.</summary>
	static void WritePublicProperty(
		CodeWriter writer,
		string name,
		TypeReference type,
		string? initializer = null,
		string? summary = null
	)
	{
		if (summary != null)
			writer.XmlSummary(summary);
		writer.Property(
			new(name, type, TypeDeclarationAccessibility.Public) { HasSetter = true, Initializer = initializer }
		);
	}

	/// <summary>
	/// Writes a public nullable-capable string property inside the <c>NET48_OR_GREATER</c>/
	/// <c>PURVIEW_TELEMETRY_NON_NULLABLE</c> preprocessor guard used by the marker attributes.
	/// </summary>
	static void WriteNullableStringProperty(CodeWriter writer, string name, string? summary = null)
	{
		writer.HashDefines(
			"NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE",
			hashWriter =>
			{
				if (summary != null)
					hashWriter.XmlSummary(summary);
				hashWriter
					.Property(
						new(
							name,
							PurviewTypeLibrary.System.String.AsTypeReference(),
							TypeDeclarationAccessibility.Public
						)
						{
							HasSetter = true,
							IncludeGeneratedAttributes = false,
						}
					)
					.HashElse()
					.Property(
						new(
							name,
							PurviewTypeLibrary.System.String.MakeNullable(writer),
							TypeDeclarationAccessibility.Public
						)
						{
							HasSetter = true,
							IncludeGeneratedAttributes = false,
						}
					);
			}
		);
	}

	/// <summary>Writes a public non-nullable string property (used for defaults that always have a value).</summary>
	static void WritePlainStringProperty(
		CodeWriter writer,
		string name,
		string? initializer = null,
		string? summary = null
	)
	{
		if (summary != null)
			writer.XmlSummary(summary);
		writer.Property(
			new(name, PurviewTypeLibrary.System.String.AsTypeReference(), TypeDeclarationAccessibility.Public)
			{
				HasSetter = true,
				IncludeGeneratedAttributes = false,
				Initializer = initializer,
			}
		);
	}

	// -------------------------------------------------------------------------------------------
	// Shared templates
	// -------------------------------------------------------------------------------------------

	static void WriteTagLikeAttribute(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Parameter,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new("skipOnNullOrEmpty", PurviewTypeLibrary.System.Boolean.AsTypeReference())],
					},
					ctor => ctor.Assignment("SkipOnNullOrEmpty", "skipOnNullOrEmpty")
				);
				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("Name")} and whether empty values are skipped."
				);
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.XmlParam("skipOnNullOrEmpty", "Whether to skip the value when it is null or empty.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new("skipOnNullOrEmpty", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "false",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("Name", "name");
						ctor.Assignment("SkipOnNullOrEmpty", "skipOnNullOrEmpty");
					}
				);

				WriteNullableStringProperty(body, "Name", "Optional. Gets the name of the tag or baggage value.");
				WritePublicProperty(
					body,
					"SkipOnNullOrEmpty",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether the value is skipped when it is null or empty."
				);
			},
			summary: summary
		);
	}

	static void WriteTelemetryGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		var namingConvention = TypeLibrary.TelemetryShared.NamingConvention;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly | AttributeTargets.Interface,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
					"Constructs a new instance specifying whether a dependency-injection extension is generated, the generated class name and the dependency-injection class name."
				);
				body.XmlParam("generateDependencyExtension", "Whether to generate a dependency-injection extension.");
				body.XmlParam("className", "The name of the generated telemetry class.");
				body.XmlParam("dependencyInjectionClassName", "The name of the generated dependency-injection class.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("generateDependencyExtension", PurviewTypeLibrary.System.Boolean.AsTypeReference()),
							new("className", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("dependencyInjectionClassName", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("GenerateDependencyExtension", "generateDependencyExtension");
						ctor.Assignment("ClassName", "className");
						ctor.Assignment("DependencyInjectionClassName", "dependencyInjectionClassName");
					}
				);
				body.XmlSummary(
					"Constructs a new instance specifying the generated class name and the dependency-injection class name."
				);
				body.XmlParam("className", "The name of the generated telemetry class.");
				body.XmlParam("dependencyInjectionClassName", "The name of the generated dependency-injection class.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("className", PurviewTypeLibrary.System.String.AsTypeReference()),
							new("dependencyInjectionClassName", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("ClassName", "className");
						ctor.Assignment("DependencyInjectionClassName", "dependencyInjectionClassName");
					}
				);

				WritePublicProperty(
					body,
					"GenerateDependencyExtension",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether a dependency-injection extension method is generated."
				);
				WriteNullableStringProperty(body, "ClassName", "The name of the generated telemetry class.");
				WriteNullableStringProperty(
					body,
					"DependencyInjectionClassName",
					"The name of the generated dependency-injection class."
				);
				WritePublicProperty(
					body,
					"DependencyInjectionClassIsPublic",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether the dependency-injection class is generated as public."
				);
				WritePublicProperty(
					body,
					"NamingConvention",
					namingConvention.AsTypeReference(),
					$"{namingConvention.RenderFullName}.OpenTelemetry",
					"Determines the naming convention used for generated telemetry names."
				);
				WritePublicProperty(
					body,
					"GenerateTelemetryNamesClass",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether a telemetry names class is generated."
				);
				WriteNullableStringProperty(
					body,
					"TelemetryNamesClassName",
					"The name of the generated telemetry names class."
				);
				WriteNullableStringProperty(
					body,
					"TelemetryNamesNamespace",
					"The namespace of the generated telemetry names class."
				);
			},
			summary: "Specifies the telemetry generation behaviour for an interface or assembly."
		);
	}

	static void WriteExcludeTargetsAttribute(CodeWriter writer, TypeIdentity type)
	{
		var targets = TypeLibrary.TelemetryShared.Targets;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Parameter,
			body =>
			{
				body.XmlSummary("Constructs a new instance with the specified targets to exclude.");
				body.XmlParam("targets", $"The {XmlSee("ExcludedTargets")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new("targets", targets.AsTypeReference())],
					},
					ctor => ctor.Assignment("ExcludedTargets", "targets")
				);

				body.XmlSummary("Gets or sets the targets to exclude for this parameter.");
				WritePublicProperty(body, "ExcludedTargets", targets.AsTypeReference());
			},
			summary: "Marks a parameter as excluded from the specified telemetry targets."
		);
	}

	// -------------------------------------------------------------------------------------------
	// Activity templates
	// -------------------------------------------------------------------------------------------

	static void WriteActivitySourceGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly,
			body =>
			{
				body.XmlSummary("Constructs a new instance specifying the activity source name and default behaviour.");
				body.XmlParam("name", "The name of the activity source.");
				body.XmlParam("defaultToTags", "Whether parameters are inferred as tags by default.");
				body.XmlParam(
					"generateDiagnosticsForMissingActivity",
					"Whether diagnostics are generated for missing activity definitions."
				);
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new("defaultToTags", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "true",
							},
							new(
								"generateDiagnosticsForMissingActivity",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
						],
					},
					ctor =>
					{
						ctor.IfBlock(
							"string.IsNullOrWhiteSpace(name)",
							static body => body.Throw("new System.ArgumentNullException(nameof(name))")
						);
						ctor.Assignment("Name", "name");
						ctor.Assignment("DefaultToTags", "defaultToTags");
						ctor.Assignment(
							"GenerateDiagnosticsForMissingActivity",
							"generateDiagnosticsForMissingActivity"
						);
					}
				);

				WriteNullableStringProperty(body, "Name", "The name of the activity source.");
				WritePublicProperty(
					body,
					"DefaultToTags",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether parameters are inferred as tags by default."
				);
				WriteNullableStringProperty(
					body,
					"BaggageAndTagPrefix",
					"The prefix applied to generated baggage and tag names."
				);
				WritePlainStringProperty(
					body,
					"BaggageAndTagSeparator",
					"\".\"",
					"The separator used between baggage and tag name parts."
				);
				WritePublicProperty(
					body,
					"LowercaseBaggageAndTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether baggage and tag keys are lowercased."
				);
				WritePublicProperty(
					body,
					"GenerateDiagnosticsForMissingActivity",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether diagnostics are generated for missing activity definitions."
				);
			},
			summary: "Specifies the default activity source generation behaviour for an assembly."
		);
	}

	static void WriteActivitySourceAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Interface,
			body =>
			{
				body.XmlSummary($"Constructs a new instance of the {XmlSee("ActivitySourceAttribute")}.");
				WriteEmptyConstructor(body, type);

				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("Name")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				WriteNameConstructor(body, type);

				WriteNullableStringProperty(body, "Name", "Optional. Gets the name of the activity source.");
				body.XmlSummary("Specifies the default when inferring between tag or baggage.");
				WritePublicProperty(body, "DefaultToTags", PurviewTypeLibrary.System.Boolean.AsTypeReference(), "true");
				WriteNullableStringProperty(
					body,
					"BaggageAndTagPrefix",
					"The prefix applied to generated baggage and tag names."
				);
				body.XmlSummary("Determines if the name is used as a prefix.");
				WritePublicProperty(
					body,
					"IncludeActivitySourcePrefix",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				body.XmlSummary("Determines if tag/ baggage names are lowercased.");
				WritePublicProperty(
					body,
					"LowercaseBaggageAndTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
			},
			summary: "Marks an interface as an activity source."
		);
	}

	static void WriteActivityAttribute(CodeWriter writer, TypeIdentity type)
	{
		var activityKind = TypeLibrary.Activities.SystemDiagnostics.ActivityKind;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				body.XmlSummary($"Constructs a new instance of the {XmlSee("ActivityAttribute")}.");
				WriteEmptyConstructor(body, type);

				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("Name")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				WriteNameConstructor(body, type);

				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("Kind")}.");
				body.XmlParam("kind", $"The {XmlSee("Kind")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new("kind", activityKind.AsTypeReference())],
					},
					ctor => ctor.Assignment("Kind", "kind")
				);

				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Kind")} and whether the activity is created without starting it."
				);
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.XmlParam("kind", $"The {XmlSee("Kind")}.");
				body.XmlParam(
					"createOnly",
					$"Whether the activity is created without starting it ({XmlSee("CreateOnly")})."
				);
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new("kind", activityKind.AsTypeReference())
							{
								DefaultValue = $"{activityKind.RenderFullName}.Internal",
							},
							new("createOnly", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "false",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("Name", "name");
						ctor.Assignment("Kind", "kind");
						ctor.Assignment("CreateOnly", "createOnly");
					}
				);

				WriteNullableStringProperty(body, "Name", "Optional. Gets the name of the activity.");
				WritePublicProperty(
					body,
					"Kind",
					activityKind.AsTypeReference(),
					summary: "Gets the kind of the activity."
				);
				WritePublicProperty(
					body,
					"CreateOnly",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether the activity is created without starting it."
				);
			},
			summary: "Marks a method as an activity."
		);
	}

	static void WriteEventAttribute(CodeWriter writer, TypeIdentity type)
	{
		var statusCode = TypeLibrary.Activities.SystemDiagnostics.ActivityStatusCode;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("StatusCode")}.");
				body.XmlParam("statusCode", $"The {XmlSee("StatusCode")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("statusCode", statusCode.AsTypeReference())
							{
								DefaultValue = $"{statusCode.RenderFullName}.Unset",
							},
						],
					},
					ctor => ctor.Assignment("StatusCode", "statusCode")
				);
				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("Name")}, exception handling behaviour and {XmlSee("StatusCode")}."
				);
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.XmlParam(
					"useRecordExceptionRules",
					$"Whether to use record exception rules ({XmlSee("UseRecordExceptionRules")})."
				);
				body.XmlParam(
					"recordExceptionAsEscaped",
					$"Whether a recorded exception is escaped ({XmlSee("RecordExceptionAsEscaped")})."
				);
				body.XmlParam("statusCode", $"The {XmlSee("StatusCode")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new("useRecordExceptionRules", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "true",
							},
							new("recordExceptionAsEscaped", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "true",
							},
							new("statusCode", statusCode.AsTypeReference())
							{
								DefaultValue = $"{statusCode.RenderFullName}.Unset",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("Name", "name");
						ctor.Assignment("UseRecordExceptionRules", "useRecordExceptionRules");
						ctor.Assignment("RecordExceptionAsEscaped", "recordExceptionAsEscaped");
						ctor.Assignment("StatusCode", "statusCode");
					}
				);

				WriteNullableStringProperty(body, "Name", "Optional. Gets the name of the event.");
				WritePublicProperty(
					body,
					"UseRecordExceptionRules",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether the default exception-handling rules are used."
				);
				WritePublicProperty(
					body,
					"RecordExceptionAsEscaped",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether a recorded exception is escaped."
				);
				WritePublicProperty(
					body,
					"StatusCode",
					statusCode.AsTypeReference(),
					summary: "Gets the status code of the event."
				);
				WriteNullableStringProperty(
					body,
					"StatusDescription",
					"Optional. Gets the status description of the event."
				);
			},
			summary: "Marks a method as an activity event."
		);
	}

	// -------------------------------------------------------------------------------------------
	// Logging templates
	// -------------------------------------------------------------------------------------------

	static void WriteLoggerGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Logging.MicrosoftExtensions.LogLevel;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary($"Constructs a new instance specifying the default {XmlSee("DefaultLevel")}.");
				body.XmlParam("defaultLevel", $"The default {XmlSee("DefaultLevel")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new("defaultLevel", logLevel.AsTypeReference())],
					},
					ctor => ctor.Assignment("DefaultLevel", "defaultLevel")
				);

				WritePublicProperty(
					body,
					"DefaultLevel",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information",
					"Gets or sets the default log level used by generated log methods."
				);
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference(),
					summary: "Gets or sets the log generation mode used for generated log methods."
				);
				WritePublicProperty(
					body,
					"DefaultPrefixType",
					TypeLibrary.Logging.LogPrefixType.AsTypeReference(),
					summary: "Gets or sets the default log prefix type used by generated log methods."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Specifies the default logging generation behaviour for an assembly."
		);
	}

	static void WriteLoggerAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Logging.MicrosoftExtensions.LogLevel;
		var logPrefixType = TypeLibrary.Logging.LogPrefixType;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Interface,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
					$"Constructs a new instance specifying the default {XmlSee("DefaultLevel")} and an optional custom prefix."
				);
				body.XmlParam("defaultLevel", $"The default {XmlSee("DefaultLevel")}.");
				body.XmlParam("customPrefix", $"The custom log prefix ({XmlSee("CustomPrefix")}).");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("defaultLevel", logLevel.AsTypeReference()),
							new("customPrefix", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("DefaultLevel", "defaultLevel");
						ctor.Assignment("CustomPrefix", "customPrefix");
						ctor.IfBlock(
							"!string.IsNullOrWhiteSpace(CustomPrefix)",
							block => block.Assignment("PrefixType", $"{logPrefixType.RenderFullName}.Custom")
						);
					}
				);

				WritePublicProperty(
					body,
					"DefaultLevel",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information",
					"Gets or sets the default log level used by generated log methods."
				);
				WriteNullableStringProperty(
					body,
					"CustomPrefix",
					"Gets or sets the custom log prefix used by generated log methods."
				);
				WritePublicProperty(
					body,
					"PrefixType",
					logPrefixType.AsTypeReference(),
					summary: "Gets or sets the log prefix type used by generated log methods."
				);
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference(),
					summary: "Gets or sets the log generation mode used for generated log methods."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Marks an interface as a logger."
		);
	}

	static void WriteLogAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Logging.MicrosoftExtensions.LogLevel;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				WriteMessageTemplateConstructor(
					body,
					type,
					$"Constructs a new instance specifying the {XmlSee("MessageTemplate")}."
				);
				WriteEventIdConstructor(body, type, $"Constructs a new instance specifying the {XmlSee("EventId")}.");
				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("Level")}, optional {XmlSee("MessageTemplate")} and {XmlSee("Name")}."
				);
				body.XmlParam("level", $"The {XmlSee("Level")}.");
				body.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("level", logLevel.AsTypeReference()),
							new("messageTemplate", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" },
						],
					},
					ctor =>
					{
						ctor.Assignment("Level", "level");
						ctor.Assignment("MessageTemplate", "messageTemplate");
						ctor.Assignment("Name", "name");
					}
				);
				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("EventId")}, {XmlSee("Level")}, optional {XmlSee("MessageTemplate")} and {XmlSee("Name")}."
				);
				body.XmlParam("eventId", $"The {XmlSee("EventId")}.");
				body.XmlParam("level", $"The {XmlSee("Level")}.");
				body.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("eventId", PurviewTypeLibrary.System.Int32.AsTypeReference()),
							new("level", logLevel.AsTypeReference()),
							new("messageTemplate", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" },
						],
					},
					ctor =>
					{
						ctor.Assignment("Level", "level");
						ctor.Assignment("MessageTemplate", "messageTemplate");
						ctor.Assignment("EventId", "eventId");
						ctor.Assignment("Name", "name");
					}
				);

				WritePublicProperty(
					body,
					"Level",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information",
					"Gets or sets the log level of the log entry."
				);
				WriteNullableStringProperty(
					body,
					"MessageTemplate",
					"Gets or sets the message template used for the log entry."
				);
				WritePublicProperty(
					body,
					"EventId",
					PurviewTypeLibrary.System.Int32.MakeNullable(writer),
					summary: "Gets or sets the event identifier of the log entry."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the log entry.");
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference(),
					summary: "Gets or sets the log generation mode used for the log entry."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Marks a method as a log method."
		);
	}

	static void WriteSpecificLogAttribute(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteMessageTemplateConstructor(
					body,
					type,
					$"Constructs a new instance specifying the {XmlSee("MessageTemplate")}."
				);
				WriteEventIdConstructor(body, type, $"Constructs a new instance specifying the {XmlSee("EventId")}.");
				body.XmlSummary(
					$"Constructs a new instance specifying an optional {XmlSee("MessageTemplate")} and {XmlSee("Name")}."
				);
				body.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("messageTemplate", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" },
						],
					},
					ctor =>
					{
						ctor.Assignment("MessageTemplate", "messageTemplate");
						ctor.Assignment("Name", "name");
					}
				);
				body.XmlSummary(
					$"Constructs a new instance specifying the {XmlSee("EventId")}, optional {XmlSee("MessageTemplate")} and {XmlSee("Name")}."
				);
				body.XmlParam("eventId", $"The {XmlSee("EventId")}.");
				body.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.");
				body.XmlParam("name", $"The {XmlSee("Name")}.");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("eventId", PurviewTypeLibrary.System.Int32.AsTypeReference()),
							new("messageTemplate", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("name", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" },
						],
					},
					ctor =>
					{
						ctor.Assignment("MessageTemplate", "messageTemplate");
						ctor.Assignment("EventId", "eventId");
						ctor.Assignment("Name", "name");
					}
				);

				WriteNullableStringProperty(
					body,
					"MessageTemplate",
					"Gets or sets the message template used for the log entry."
				);
				WritePublicProperty(
					body,
					"EventId",
					PurviewTypeLibrary.System.Int32.MakeNullable(writer),
					summary: "Gets or sets the event identifier of the log entry."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the log entry.");
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference(),
					summary: "Gets or sets the log generation mode used for the log entry."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: summary
		);
	}

	static void WriteExpandEnumerableAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Parameter,
			body =>
			{
				body.XmlSummary(
					$"Constructs a new instance specifying the maximum number of values to expand ({XmlSee("MaximumValueCount")})."
				);
				body.XmlParam(
					"maximumValueCount",
					$"The maximum number of values to include ({XmlSee("MaximumValueCount")})."
				);
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("maximumValueCount", PurviewTypeLibrary.System.Int32.AsTypeReference())
							{
								DefaultValue = "5",
							},
						],
					},
					ctor => ctor.Assignment("MaximumValueCount", "maximumValueCount")
				);

				body.XmlSummary("Gets or sets the maximum number of values to include when expanding an enumerable.");
				WritePublicProperty(body, "MaximumValueCount", PurviewTypeLibrary.System.Int32.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Marks an enumerable parameter to be expanded into multiple log entries."
		);
	}

	// -------------------------------------------------------------------------------------------
	// Metrics templates
	// -------------------------------------------------------------------------------------------

	static void WriteMeterGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		var nameGenerationType = TypeLibrary.Metrics.MeterNameGenerationType;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
					"Constructs a new instance specifying the meter name, name-generation type, instrument prefix and name casing defaults."
				);
				body.XmlParam("meterName", $"The {XmlSee("MeterName")}.");
				body.XmlParam("nameGenerationType", $"The {XmlSee("MeterNameGenerationType")}.");
				body.XmlParam("instrumentPrefix", $"The {XmlSee("InstrumentPrefix")}.");
				body.XmlParam(
					"lowercaseInstrumentName",
					$"Whether instrument names are lowercased ({XmlSee("LowercaseInstrumentName")})."
				);
				body.XmlParam("lowercaseTagKeys", $"Whether tag keys are lowercased ({XmlSee("LowercaseTagKeys")}).");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new("meterName", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("nameGenerationType", nameGenerationType.AsTypeReference())
							{
								DefaultValue = $"{nameGenerationType.RenderFullName}.DotNet",
							},
							new("instrumentPrefix", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
							new("lowercaseInstrumentName", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "true",
							},
							new("lowercaseTagKeys", PurviewTypeLibrary.System.Boolean.AsTypeReference())
							{
								DefaultValue = "true",
							},
						],
					},
					ctor =>
					{
						ctor.Assignment("MeterName", "meterName");
						ctor.Assignment("MeterNameGenerationType", "nameGenerationType");
						ctor.Assignment("InstrumentPrefix", "instrumentPrefix");
						ctor.Assignment("LowercaseInstrumentName", "lowercaseInstrumentName");
						ctor.Assignment("LowercaseTagKeys", "lowercaseTagKeys");
					}
				);

				WriteNullableStringProperty(body, "MeterName", "Gets or sets the name of the meter.");
				WritePublicProperty(
					body,
					"MeterNameGenerationType",
					nameGenerationType.AsTypeReference(),
					$"{nameGenerationType.RenderFullName}.DotNet",
					"Gets or sets how meter names are generated when not explicitly specified."
				);
				WriteNullableStringProperty(
					body,
					"InstrumentPrefix",
					"Gets or sets the prefix applied to instrument names."
				);
				WritePlainStringProperty(
					body,
					"InstrumentSeparator",
					"\".\"",
					"Gets or sets the separator used between instrument name parts."
				);
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether instrument names are lowercased."
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether tag keys are lowercased."
				);
			},
			summary: "Specifies the default meter generation behaviour for an assembly."
		);
	}

	static void WriteMeterAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Interface,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				WriteNameConstructor(body, type, $"Constructs a new instance specifying the {XmlSee("Name")}.");

				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the meter.");
				WriteNullableStringProperty(
					body,
					"InstrumentPrefix",
					"Gets or sets the prefix applied to instrument names."
				);
				WritePublicProperty(
					body,
					"IncludeAssemblyInstrumentPrefix",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether the assembly-level instrument prefix is included."
				);
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether instrument names are lowercased."
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true",
					"Determines whether tag keys are lowercased."
				);
			},
			summary: "Marks an interface as a meter."
		);
	}

	static void WriteAutoCounterAttribute(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				WriteNameUnitDescriptionConstructor(
					body,
					type,
					summary: $"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Unit")} and {XmlSee("Description")}."
				);

				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
			},
			summary: summary
		);
	}

	static void WriteCounterLikeAttribute(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
					$"Constructs a new instance specifying whether the counter auto-increments ({XmlSee("AutoIncrement")})."
				);
				body.XmlParam("autoIncrement", $"Whether the counter auto-increments ({XmlSee("AutoIncrement")}).");
				body.Constructor(
					new(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new("autoIncrement", PurviewTypeLibrary.System.Boolean.AsTypeReference())],
					},
					ctor => ctor.Assignment("AutoIncrement", "autoIncrement")
				);
				WriteNameUnitDescriptionConstructor(
					body,
					type,
					appendAutoIncrement: true,
					summary: $"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Unit")}, {XmlSee("Description")} and whether the counter auto-increments."
				);

				WritePublicProperty(
					body,
					"AutoIncrement",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether the counter auto-increments."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
			},
			summary: summary
		);
	}

	static void WriteObservableCounterLikeAttribute(CodeWriter writer, TypeIdentity type, string? summary = null)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				WriteNameUnitDescriptionConstructor(
					body,
					type,
					appendThrowOnAlreadyInitialized: true,
					summary: $"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Unit")}, {XmlSee("Description")} and whether initializing twice throws."
				);
				WritePublicProperty(
					body,
					"AutoIncrement",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether the counter auto-increments."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
				WritePublicProperty(
					body,
					"ThrowOnAlreadyInitialized",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					summary: "Determines whether an exception is thrown when the instrument is initialized more than once."
				);
			},
			summary: summary
		);
	}

	static void WriteNameUnitDescriptionConstructor(
		CodeWriter writer,
		TypeIdentity type,
		bool appendAutoIncrement = false,
		bool appendThrowOnAlreadyInitialized = false,
		string? summary = null
	)
	{
		if (summary != null)
		{
			writer.XmlSummary(summary);
			writer.XmlParam("name", "The name of the instrument.");
			writer.XmlParam("unit", "The measurement unit of the instrument.");
			writer.XmlParam("description", "The description of the instrument.");
			if (appendAutoIncrement)
				writer.XmlParam("autoIncrement", "Whether the counter should auto-increment.");
			if (appendThrowOnAlreadyInitialized)
				writer.XmlParam(
					"throwOnAlreadyInitialized",
					"Whether to throw if the instrument has already been initialized."
				);
		}
		writer.Constructor(
			new(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = BuildNameUnitDescriptionParameters(appendAutoIncrement, appendThrowOnAlreadyInitialized),
			},
			ctor =>
			{
				ctor.Assignment("Name", "name");
				ctor.Assignment("Unit", "unit");
				ctor.Assignment("Description", "description");
				if (appendAutoIncrement)
					ctor.Assignment("AutoIncrement", "autoIncrement");
				if (appendThrowOnAlreadyInitialized)
					ctor.Assignment("ThrowOnAlreadyInitialized", "throwOnAlreadyInitialized");
			}
		);
	}

	static ImmutableArray<ParameterDeclarationOptions> BuildNameUnitDescriptionParameters(
		bool appendAutoIncrement,
		bool appendThrowOnAlreadyInitialized
	)
	{
		var parameters = ImmutableArray<ParameterDeclarationOptions>.Empty;
		parameters = parameters.Add(new("name", PurviewTypeLibrary.System.String.AsTypeReference()));
		parameters = parameters.Add(
			new("unit", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" }
		);
		parameters = parameters.Add(
			new("description", PurviewTypeLibrary.System.String.AsTypeReference()) { DefaultValue = "null" }
		);
		if (appendAutoIncrement)
			parameters = parameters.Add(
				new("autoIncrement", PurviewTypeLibrary.System.Boolean.AsTypeReference()) { DefaultValue = "false" }
			);
		if (appendThrowOnAlreadyInitialized)
			parameters = parameters.Add(
				new("throwOnAlreadyInitialized", PurviewTypeLibrary.System.Boolean.AsTypeReference())
				{
					DefaultValue = "false",
				}
			);

		return parameters;
	}

	// -------------------------------------------------------------------------------------------
	// Enum templates
	// -------------------------------------------------------------------------------------------

	static void WriteTargetsEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines which telemetry targets a parameter is excluded from.")
			.Enum(
				type.Name,
				TypeDeclarationAccessibility.Public,
				fields:
				[
					new("None", 0, "No telemetry targets are excluded."),
					new("Activities", 1, "Excludes activity (tracing) targets."),
					new("Logging", 2, "Excludes logging targets."),
					new("Metrics", 4, "Excludes metrics targets."),
					new("All", "Activities | Logging | Metrics"),
				],
				configure: options => options with { Attributes = [new(new TypeIdentity("FlagsAttribute", "System"))] }
			);
	}

	static void WriteNamingConventionEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines the naming convention used for generated telemetry names.")
			.Enum(
				type.Name,
				TypeDeclarationAccessibility.Public,
				fields:
				[
					new("Legacy", 0, "Uses the legacy naming convention for generated telemetry names."),
					new("OpenTelemetry", 1, "Uses the OpenTelemetry naming convention for generated telemetry names."),
				]
			);
	}

	static void WriteLogPrefixTypeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer.HashDefines(
			"!EXCLUDE_PURVIEW_TELEMETRY_LOGGING",
			hashWriter =>
				hashWriter
					.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
					.XmlSummary("Determines the mode used to generate or override the prefix for the log entry.")
					.Enum(
						type.Name,
						TypeDeclarationAccessibility.Public,
						fields:
						[
							new("Default", 0, "Uses the default log prefix."),
							new("Interface", 1, "Uses the interface name as the log prefix."),
							new("Class", 2, "Uses the class name as the log prefix."),
							new("Custom", 3, "Uses a custom log prefix."),
							new("TrimmedClassName", 4, "Uses the trimmed class name as the log prefix."),
						]
					)
		);
	}

	static void WriteLoggerGenerationModeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer.HashDefines(
			"!EXCLUDE_PURVIEW_TELEMETRY_LOGGING",
			hashWriter =>
				hashWriter
					.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
					.XmlSummary("Controls the generation mode used for log methods.")
					.Enum(
						type.Name,
						TypeDeclarationAccessibility.Public,
						fields:
						[
							new("Auto", 0, "Automatically selects the log generation mode."),
							new("V1", 1, "Uses the first-generation log implementation."),
							new("V2", 2, "Uses the second-generation log implementation."),
						]
					)
		);
	}

	static void WriteMeterNameGenerationTypeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.FileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines how meter names are generated when not explicitly specified.")
			.Enum(
				type.Name,
				TypeDeclarationAccessibility.Public,
				fields:
				[
					new("OpenTelemetry", 0, "Generates meter names using the OpenTelemetry convention."),
					new("DotNet", 1, "Generates meter names using the .NET convention."),
				]
			);
	}
}
