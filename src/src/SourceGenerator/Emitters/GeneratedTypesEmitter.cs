using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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
			TypeLibrary.Purview.Telemetry.TagAttribute,
			(writer, type) =>
				WriteTagLikeAttribute(writer, type, "Marks a parameter as a tag for an activity, event or instrument.")
		);

		yield return (
			TypeLibrary.Purview.Telemetry.ExcludeAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Method,
					includeSuppressMessage: false,
					"Marks a method to be excluded from telemetry generation."
				)
		);
		yield return (TypeLibrary.Purview.Telemetry.TelemetryGenerationAttribute, WriteTelemetryGenerationAttribute);
		yield return (TypeLibrary.Purview.Telemetry.Targets, WriteTargetsEnum);
		yield return (TypeLibrary.Purview.Telemetry.NamingConvention, WriteNamingConventionEnum);
		yield return (TypeLibrary.Purview.Telemetry.ExcludeTargetsAttribute, WriteExcludeTargetsAttribute);

		// Activities
		yield return (
			TypeLibrary.Purview.Telemetry.BaggageAttribute,
			(writer, type) =>
				WriteTagLikeAttribute(writer, type, "Marks a parameter as baggage to be attached to an activity.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.ActivitySourceGenerationAttribute,
			WriteActivitySourceGenerationAttribute
		);
		yield return (TypeLibrary.Purview.Telemetry.ActivitySourceAttribute, WriteActivitySourceAttribute);
		yield return (TypeLibrary.Purview.Telemetry.ActivityAttribute, WriteActivityAttribute);
		yield return (TypeLibrary.Purview.Telemetry.EventAttribute, WriteEventAttribute);
		yield return (
			TypeLibrary.Purview.Telemetry.ContextAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Method,
					includeSuppressMessage: false,
					"Marks a parameter as an activity context."
				)
		);
		yield return (
			TypeLibrary.Purview.Telemetry.EscapeAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					"Marks a parameter as the escape flag for a recorded exception."
				)
		);
		yield return (
			TypeLibrary.Purview.Telemetry.StatusDescriptionAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					"Marks a parameter as the status description of an activity or event."
				)
		);

		// Logging
		yield return (TypeLibrary.Purview.Telemetry.LoggerGenerationAttribute, WriteLoggerGenerationAttribute);
		yield return (TypeLibrary.Purview.Telemetry.LoggerAttribute, WriteLoggerAttribute);
		yield return (TypeLibrary.Purview.Telemetry.LogAttribute, WriteLogAttribute);
		yield return (TypeLibrary.Purview.Telemetry.LogPrefixType, WriteLogPrefixTypeEnum);
		yield return (TypeLibrary.Purview.Telemetry.LoggerGenerationMode, WriteLoggerGenerationModeEnum);
		yield return (TypeLibrary.Purview.Telemetry.ExpandEnumerableAttribute, WriteExpandEnumerableAttribute);
		yield return (
			TypeLibrary.Purview.Telemetry.TraceAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a trace-level log method.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.DebugAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a debug-level log method.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.InfoAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as an informational log method.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.WarningAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a warning-level log method.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.ErrorAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as an error-level log method.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.CriticalAttribute,
			(writer, type) => WriteSpecificLogAttribute(writer, type, "Marks a method as a critical-level log method.")
		);

		// Metrics
		yield return (TypeLibrary.Purview.Telemetry.MeterGenerationAttribute, WriteMeterGenerationAttribute);
		yield return (TypeLibrary.Purview.Telemetry.MeterAttribute, WriteMeterAttribute);
		yield return (TypeLibrary.Purview.Telemetry.MeterNameGenerationType, WriteMeterNameGenerationTypeEnum);
		yield return (
			TypeLibrary.Purview.Telemetry.InstrumentMeasurementAttribute,
			(writer, type) =>
				WriteSimpleAttribute(
					writer,
					type,
					AttributeTargets.Parameter,
					includeSuppressMessage: false,
					"Marks a parameter as the measurement value of an instrument."
				)
		);
		yield return (
			TypeLibrary.Purview.Telemetry.AutoCounterAttribute,
			(writer, type) =>
				WriteAutoCounterAttribute(writer, type, "Marks a method as an auto-incrementing counter instrument.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.CounterAttribute,
			(writer, type) => WriteCounterLikeAttribute(writer, type, "Marks a method as a counter instrument.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.UpDownCounterAttribute,
			(writer, type) =>
				WriteCounterLikeAttribute(writer, type, "Marks a method as an up-down counter instrument.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.HistogramAttribute,
			(writer, type) => WriteCounterLikeAttribute(writer, type, "Marks a method as a histogram instrument.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.ObservableCounterAttribute,
			(writer, type) =>
				WriteObservableCounterLikeAttribute(writer, type, "Marks a method as an observable counter instrument.")
		);
		yield return (
			TypeLibrary.Purview.Telemetry.ObservableUpDownCounterAttribute,
			(writer, type) =>
				WriteObservableCounterLikeAttribute(
					writer,
					type,
					"Marks a method as an observable up-down counter instrument."
				)
		);
		yield return (
			TypeLibrary.Purview.Telemetry.ObservableGaugeAttribute,
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

			context.AddSource($"{emitter.Type.Name}.g.cs", writer);
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
		string summary,
		bool wrapInExcludeLoggingGuard = false,
		bool includeSuppressMessage = true
	)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		using var scope = wrapInExcludeLoggingGuard
			? writer.HashDefinesScope("!EXCLUDE_PURVIEW_TELEMETRY_LOGGING")
			: writer.EmptyScope();

		var attributes = ImmutableArray<AttributeDeclarationOptions>.Empty;
		attributes = attributes.Add(ConditionalAttribute());
		if (includeSuppressMessage)
			attributes = attributes.Add(SuppressMessageAttribute());

		writer.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace);

		writer
			.XmlSummary(summary)
			.AttributeClass(
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
		string summary
	)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

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

	static void WriteEmptyConstructor(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer.XmlSummary(summary).Constructor(new(type.Name, TypeDeclarationAccessibility.Public), static _ => { });
	}

	static void WriteNameConstructor(
		CodeWriter writer,
		TypeIdentity type,
		string summary,
		Action<CodeWriter>? xmlBody = null
	)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.XmlParam("name", "The name of the telemetry entry.")
			.Constructor(
				new(type.Name, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("name", TypeLibrary.System.String)],
				},
				ctor => ctor.Assignment("Name", "name")
			);
	}

	static void WriteMessageTemplateConstructor(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.XmlParam("messageTemplate", "The message template used to generate the log message.")
			.Constructor(
				new(type.Name, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("messageTemplate", TypeLibrary.System.String)],
				},
				ctor => ctor.Assignment("MessageTemplate", "messageTemplate")
			);
	}

	static void WriteEventIdConstructor(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.XmlParam("eventId", "The event identifier of the log entry.")
			.Constructor(
				new(type.Name, TypeDeclarationAccessibility.Public)
				{
					Parameters = [new("eventId", TypeLibrary.System.Int32)],
				},
				ctor => ctor.Assignment("EventId", "eventId")
			);
	}

	/// <summary>Writes a public property with generated attributes and an optional initializer.</summary>
	static void WritePublicProperty(
		CodeWriter writer,
		string name,
		TypeReference type,
		string summary,
		string? initializer = null
	)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.Property(
				new(name, type, TypeDeclarationAccessibility.Public) { HasSetter = true, Initializer = initializer }
			);
	}

	/// <summary>
	/// Writes a public nullable-capable string property inside the <c>NET48_OR_GREATER</c>/
	/// <c>PURVIEW_TELEMETRY_NON_NULLABLE</c> preprocessor guard used by the marker attributes.
	/// </summary>
	static void WriteNullableStringProperty(CodeWriter writer, string name, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer.HashDefines(
			"NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE",
			hashWriter =>
				hashWriter
					.XmlSummary(summary)
					.Property(
						new(name, TypeLibrary.System.String, TypeDeclarationAccessibility.Public) { HasSetter = true }
					)
					.HashElse()
					.Property(
						new(name, TypeLibrary.System.String.MakeNullable(writer), TypeDeclarationAccessibility.Public)
						{
							HasSetter = true,
						}
					)
		);
	}

	/// <summary>Writes a public non-nullable string property (used for defaults that always have a value).</summary>
	static void WritePlainStringProperty(CodeWriter writer, string name, string summary, string? initializer = null)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.Property(
				new(name, TypeLibrary.System.String, TypeDeclarationAccessibility.Public)
				{
					HasSetter = true,

					Initializer = initializer,
				}
			);
	}

	// -------------------------------------------------------------------------------------------
	// Shared templates
	// -------------------------------------------------------------------------------------------

	static void WriteTagLikeAttribute(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

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
						Parameters = [new("skipOnNullOrEmpty", TypeLibrary.System.Boolean)],
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
							new("name", TypeLibrary.System.String),
							new("skipOnNullOrEmpty", TypeLibrary.System.Boolean) { DefaultValue = "false" },
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
					TypeLibrary.System.Boolean,
					"Determines whether the value is skipped when it is null or empty."
				);
			},
			summary
		);
	}

	static void WriteTelemetryGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		var namingConvention = TypeLibrary.Purview.Telemetry.NamingConvention;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly | AttributeTargets.Interface,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
						"Constructs a new instance specifying whether a dependency-injection extension is generated, the generated class name and the dependency-injection class name."
					)
					.XmlParam("generateDependencyExtension", "Whether to generate a dependency-injection extension.")
					.XmlParam("className", "The name of the generated telemetry class.")
					.XmlParam("dependencyInjectionClassName", "The name of the generated dependency-injection class.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("generateDependencyExtension", TypeLibrary.System.Boolean),
								new("className", TypeLibrary.System.String) { DefaultValue = "null" },
								new("dependencyInjectionClassName", TypeLibrary.System.String)
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
					)
					.XmlParam("className", "The name of the generated telemetry class.")
					.XmlParam("dependencyInjectionClassName", "The name of the generated dependency-injection class.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("className", TypeLibrary.System.String),
								new("dependencyInjectionClassName", TypeLibrary.System.String)
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
					TypeLibrary.System.Boolean,
					"Determines whether a dependency-injection extension method is generated.",
					"true"
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
					TypeLibrary.System.Boolean,
					"Determines whether the dependency-injection class is generated as public."
				);
				WritePublicProperty(
					body,
					"NamingConvention",
					namingConvention,
					"Determines the naming convention used for generated telemetry names.",
					$"{namingConvention.RenderFullName}.OpenTelemetry"
				);
				WritePublicProperty(
					body,
					"GenerateTelemetryNamesClass",
					TypeLibrary.System.Boolean,
					"Determines whether a telemetry names class is generated.",
					"true"
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
			"Specifies the telemetry generation behaviour for an interface or assembly."
		);
	}

	static void WriteExcludeTargetsAttribute(CodeWriter writer, TypeIdentity type)
	{
		var targets = TypeLibrary.Purview.Telemetry.Targets;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Parameter,
			body =>
			{
				body.XmlSummary("Constructs a new instance with the specified targets to exclude.")
					.XmlParam("targets", $"The {XmlSee("ExcludedTargets")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public) { Parameters = [new("targets", targets)] },
						ctor => ctor.Assignment("ExcludedTargets", "targets")
					);

				WritePublicProperty(
					body,
					"ExcludedTargets",
					targets,
					"Gets or sets the targets to exclude for this parameter."
				);
			},
			"Marks a parameter as excluded from the specified telemetry targets."
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
				body.XmlSummary("Constructs a new instance specifying the activity source name and default behaviour.")
					.XmlParam("name", "The name of the activity source.")
					.XmlParam("defaultToTags", "Whether parameters are inferred as tags by default.")
					.XmlParam(
						"generateDiagnosticsForMissingActivity",
						"Whether diagnostics are generated for missing activity definitions."
					)
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("defaultToTags", TypeLibrary.System.Boolean) { DefaultValue = "true" },
								new("generateDiagnosticsForMissingActivity", TypeLibrary.System.Boolean)
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
					TypeLibrary.System.Boolean,
					"Determines whether parameters are inferred as tags by default.",
					"true"
				);
				WriteNullableStringProperty(
					body,
					"BaggageAndTagPrefix",
					"The prefix applied to generated baggage and tag names."
				);
				WritePlainStringProperty(
					body,
					"BaggageAndTagSeparator",
					"The separator used between baggage and tag name parts.",
					"\".\""
				);
				WritePublicProperty(
					body,
					"LowercaseBaggageAndTagKeys",
					TypeLibrary.System.Boolean,
					"Determines whether baggage and tag keys are lowercased.",
					"true"
				);
				WritePublicProperty(
					body,
					"GenerateDiagnosticsForMissingActivity",
					TypeLibrary.System.Boolean,
					"Determines whether diagnostics are generated for missing activity definitions.",
					"true"
				);
			},
			"Specifies the default activity source generation behaviour for an assembly."
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
				WriteEmptyConstructor(
					body,
					type,
					$"Constructs a new instance of the {XmlSee("ActivitySourceAttribute")}."
				);

				WriteNameConstructor(
					body,
					type,
					$"Constructs a new instance specifying the {XmlSee("Name")}.",
					w => w.XmlParam("name", $"The {XmlSee("Name")}.")
				);

				WriteNullableStringProperty(body, "Name", "Optional. Gets the name of the activity source.");

				WritePublicProperty(
					body,
					"DefaultToTags",
					TypeLibrary.System.Boolean,
					"Specifies the default when inferring between tag or baggage.",
					"true"
				);
				WriteNullableStringProperty(
					body,
					"BaggageAndTagPrefix",
					"The prefix applied to generated baggage and tag names."
				);

				WritePublicProperty(
					body,
					"IncludeActivitySourcePrefix",
					TypeLibrary.System.Boolean,
					"Determines if the name is used as a prefix.",
					"true"
				);

				WritePublicProperty(
					body,
					"LowercaseBaggageAndTagKeys",
					TypeLibrary.System.Boolean,
					"Determines if tag/ baggage names are lowercased.",
					"true"
				);
			},
			"Marks an interface as an activity source."
		);
	}

	static void WriteActivityAttribute(CodeWriter writer, TypeIdentity type)
	{
		var activityKind = TypeLibrary.System.Diagnostics.ActivityKind;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee("ActivityAttribute")}.");

				WriteNameConstructor(
					body,
					type,
					$"Constructs a new instance specifying the {XmlSee("Name")}.",
					w => w.XmlParam("name", $"The {XmlSee("Name")}.")
				);

				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("Kind")}.")
					.XmlParam("kind", $"The {XmlSee("Kind")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters = [new("kind", activityKind)],
						},
						ctor => ctor.Assignment("Kind", "kind")
					);

				body.XmlSummary(
						$"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Kind")} and whether the activity is created without starting it."
					)
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.XmlParam("kind", $"The {XmlSee("Kind")}.")
					.XmlParam(
						"createOnly",
						$"Whether the activity is created without starting it ({XmlSee("CreateOnly")})."
					)
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("kind", activityKind) { DefaultValue = $"{activityKind.RenderFullName}.Internal" },
								new("createOnly", TypeLibrary.System.Boolean) { DefaultValue = "false" },
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
				WritePublicProperty(body, "Kind", activityKind, "Gets the kind of the activity.");
				WritePublicProperty(
					body,
					"CreateOnly",
					TypeLibrary.System.Boolean,
					"Determines whether the activity is created without starting it."
				);
			},
			"Marks a method as an activity."
		);
	}

	static void WriteEventAttribute(CodeWriter writer, TypeIdentity type)
	{
		var statusCode = TypeLibrary.System.Diagnostics.ActivityStatusCode;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				body.XmlSummary($"Constructs a new instance specifying the {XmlSee("StatusCode")}.")
					.XmlParam("statusCode", $"The {XmlSee("StatusCode")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("statusCode", statusCode) { DefaultValue = $"{statusCode.RenderFullName}.Unset" },
							],
						},
						ctor => ctor.Assignment("StatusCode", "statusCode")
					);

				body.XmlSummary(
						$"Constructs a new instance specifying the {XmlSee("Name")}, exception handling behaviour and {XmlSee("StatusCode")}."
					)
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.XmlParam(
						"useRecordExceptionRules",
						$"Whether to use record exception rules ({XmlSee("UseRecordExceptionRules")})."
					)
					.XmlParam(
						"recordExceptionAsEscaped",
						$"Whether a recorded exception is escaped ({XmlSee("RecordExceptionAsEscaped")})."
					)
					.XmlParam("statusCode", $"The {XmlSee("StatusCode")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("name", TypeLibrary.System.String),
								new("useRecordExceptionRules", TypeLibrary.System.Boolean) { DefaultValue = "true" },
								new("recordExceptionAsEscaped", TypeLibrary.System.Boolean) { DefaultValue = "true" },
								new("statusCode", statusCode) { DefaultValue = $"{statusCode.RenderFullName}.Unset" },
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
					TypeLibrary.System.Boolean,
					"Determines whether the default exception-handling rules are used.",
					"true"
				);
				WritePublicProperty(
					body,
					"RecordExceptionAsEscaped",
					TypeLibrary.System.Boolean,
					"Determines whether a recorded exception is escaped.",
					"true"
				);
				WritePublicProperty(body, "StatusCode", statusCode, "Gets the status code of the event.");
				WriteNullableStringProperty(
					body,
					"StatusDescription",
					"Optional. Gets the status description of the event."
				);
			},
			"Marks a method as an activity event."
		);
	}

	// -------------------------------------------------------------------------------------------
	// Logging templates
	// -------------------------------------------------------------------------------------------

	static void WriteLoggerGenerationAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Microsoft.Extensions.Logging.LogLevel;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary($"Constructs a new instance specifying the default {XmlSee("DefaultLevel")}.")
					.XmlParam("defaultLevel", $"The default {XmlSee("DefaultLevel")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters = [new("defaultLevel", logLevel)],
						},
						ctor => ctor.Assignment("DefaultLevel", "defaultLevel")
					);

				WritePublicProperty(
					body,
					"DefaultLevel",
					logLevel,
					"Gets or sets the default log level used by generated log methods.",
					$"{logLevel.RenderFullName}.Information"
				);
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Purview.Telemetry.LoggerGenerationMode,
					"Gets or sets the log generation mode used for generated log methods."
				);
				WritePublicProperty(
					body,
					"DefaultPrefixType",
					TypeLibrary.Purview.Telemetry.LogPrefixType,
					"Gets or sets the default log prefix type used by generated log methods."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Specifies the default logging generation behaviour for an assembly."
		);
	}

	static void WriteLoggerAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Microsoft.Extensions.Logging.LogLevel;
		var logPrefixType = TypeLibrary.Purview.Telemetry.LogPrefixType;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Interface,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
						$"Constructs a new instance specifying the default {XmlSee("DefaultLevel")} and an optional custom prefix."
					)
					.XmlParam("defaultLevel", $"The default {XmlSee("DefaultLevel")}.")
					.XmlParam("customPrefix", $"The custom log prefix ({XmlSee("CustomPrefix")}).")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("defaultLevel", logLevel),
								new("customPrefix", TypeLibrary.System.String) { DefaultValue = "null" },
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
					logLevel,
					"Gets or sets the default log level used by generated log methods.",
					$"{logLevel.RenderFullName}.Information"
				);
				WriteNullableStringProperty(
					body,
					"CustomPrefix",
					"Gets or sets the custom log prefix used by generated log methods."
				);
				WritePublicProperty(
					body,
					"PrefixType",
					logPrefixType,
					"Gets or sets the log prefix type used by generated log methods."
				);
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Purview.Telemetry.LoggerGenerationMode,
					"Gets or sets the log generation mode used for generated log methods."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Marks an interface as a logger."
		);
	}

	static void WriteLogAttribute(CodeWriter writer, TypeIdentity type)
	{
		var logLevel = TypeLibrary.Microsoft.Extensions.Logging.LogLevel;

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
					)
					.XmlParam("level", $"The {XmlSee("Level")}.")
					.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.")
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("level", logLevel),
								new("messageTemplate", TypeLibrary.System.String) { DefaultValue = "null" },
								new("name", TypeLibrary.System.String) { DefaultValue = "null" },
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
					)
					.XmlParam("eventId", $"The {XmlSee("EventId")}.")
					.XmlParam("level", $"The {XmlSee("Level")}.")
					.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.")
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("eventId", TypeLibrary.System.Int32),
								new("level", logLevel),
								new("messageTemplate", TypeLibrary.System.String) { DefaultValue = "null" },
								new("name", TypeLibrary.System.String) { DefaultValue = "null" },
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
					logLevel,
					"Gets or sets the log level of the log entry.",
					$"{logLevel.RenderFullName}.Information"
				);
				WriteNullableStringProperty(
					body,
					"MessageTemplate",
					"Gets or sets the message template used for the log entry."
				);
				WritePublicProperty(
					body,
					"EventId",
					TypeLibrary.System.Int32.MakeNullable(writer),
					"Gets or sets the event identifier of the log entry."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the log entry.");
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Purview.Telemetry.LoggerGenerationMode,
					"Gets or sets the log generation mode used for the log entry."
				);
			},
			wrapInExcludeLoggingGuard: true,
			summary: "Marks a method as a log method."
		);
	}

	static void WriteSpecificLogAttribute(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary must be provided for public properties.", nameof(summary));
#endif

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
					)
					.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.")
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("messageTemplate", TypeLibrary.System.String) { DefaultValue = "null" },
								new("name", TypeLibrary.System.String) { DefaultValue = "null" },
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
					)
					.XmlParam("eventId", $"The {XmlSee("EventId")}.")
					.XmlParam("messageTemplate", $"The {XmlSee("MessageTemplate")}.")
					.XmlParam("name", $"The {XmlSee("Name")}.")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("eventId", TypeLibrary.System.Int32),
								new("messageTemplate", TypeLibrary.System.String) { DefaultValue = "null" },
								new("name", TypeLibrary.System.String) { DefaultValue = "null" },
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
					TypeLibrary.System.Int32.MakeNullable(writer),
					"Gets or sets the event identifier of the log entry."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the log entry.");
				WritePublicProperty(
					body,
					"GenerationMode",
					TypeLibrary.Purview.Telemetry.LoggerGenerationMode,
					"Gets or sets the log generation mode used for the log entry."
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
					)
					.XmlParam(
						"maximumValueCount",
						$"The maximum number of values to include ({XmlSee("MaximumValueCount")})."
					)
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters = [new("maximumValueCount", TypeLibrary.System.Int32) { DefaultValue = "5" }],
						},
						ctor => ctor.Assignment("MaximumValueCount", "maximumValueCount")
					);

				WritePublicProperty(
					body,
					"MaximumValueCount",
					TypeLibrary.System.Int32,
					"Gets or sets the maximum number of values to include when expanding an enumerable."
				);
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
		var nameGenerationType = TypeLibrary.Purview.Telemetry.MeterNameGenerationType;

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Assembly,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
						"Constructs a new instance specifying the meter name, name-generation type, instrument prefix and name casing defaults."
					)
					.XmlParam("meterName", $"The {XmlSee("MeterName")}.")
					.XmlParam("nameGenerationType", $"The {XmlSee("MeterNameGenerationType")}.")
					.XmlParam("instrumentPrefix", $"The {XmlSee("InstrumentPrefix")}.")
					.XmlParam(
						"lowercaseInstrumentName",
						$"Whether instrument names are lowercased ({XmlSee("LowercaseInstrumentName")})."
					)
					.XmlParam("lowercaseTagKeys", $"Whether tag keys are lowercased ({XmlSee("LowercaseTagKeys")}).")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters =
							[
								new("meterName", TypeLibrary.System.String) { DefaultValue = "null" },
								new("nameGenerationType", nameGenerationType)
								{
									DefaultValue = $"{nameGenerationType.RenderFullName}.DotNet",
								},
								new("instrumentPrefix", TypeLibrary.System.String) { DefaultValue = "null" },
								new("lowercaseInstrumentName", TypeLibrary.System.Boolean) { DefaultValue = "true" },
								new("lowercaseTagKeys", TypeLibrary.System.Boolean) { DefaultValue = "true" },
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
					nameGenerationType,
					"Gets or sets how meter names are generated when not explicitly specified.",
					$"{nameGenerationType.RenderFullName}.DotNet"
				);
				WriteNullableStringProperty(
					body,
					"InstrumentPrefix",
					"Gets or sets the prefix applied to instrument names."
				);
				WritePlainStringProperty(
					body,
					"InstrumentSeparator",
					"Gets or sets the separator used between instrument name parts.",
					"\".\""
				);
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					TypeLibrary.System.Boolean,
					"Determines whether instrument names are lowercased.",
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					TypeLibrary.System.Boolean,
					"Determines whether tag keys are lowercased.",
					"true"
				);
			},
			"Specifies the default meter generation behaviour for an assembly."
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
					TypeLibrary.System.Boolean,
					"Determines whether the assembly-level instrument prefix is included.",
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					TypeLibrary.System.Boolean,
					"Determines whether instrument names are lowercased.",
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					TypeLibrary.System.Boolean,
					"Determines whether tag keys are lowercased.",
					"true"
				);
			},
			"Marks an interface as a meter."
		);
	}

	static void WriteAutoCounterAttribute(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEUBG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary cannot be null or whitespace.", nameof(summary));
#endif

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
					$"Constructs a new instance specifying the {XmlSee("Name")}, {XmlSee("Unit")} and {XmlSee("Description")}."
				);

				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
			},
			summary
		);
	}

	static void WriteCounterLikeAttribute(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEUBG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary cannot be null or whitespace.", nameof(summary));
#endif

		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type, $"Constructs a new instance of the {XmlSee(type.Name)}.");
				body.XmlSummary(
						$"Constructs a new instance specifying whether the counter auto-increments ({XmlSee("AutoIncrement")})."
					)
					.XmlParam("autoIncrement", $"Whether the counter auto-increments ({XmlSee("AutoIncrement")}).")
					.Constructor(
						new(type.Name, TypeDeclarationAccessibility.Public)
						{
							Parameters = [new("autoIncrement", TypeLibrary.System.Boolean)],
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
					TypeLibrary.System.Boolean,
					"Determines whether the counter auto-increments."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
			},
			summary
		);
	}

	static void WriteObservableCounterLikeAttribute(CodeWriter writer, TypeIdentity type, string summary)
	{
#if DEUBG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary cannot be null or whitespace.", nameof(summary));
#endif

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
					TypeLibrary.System.Boolean,
					"Determines whether the counter auto-increments."
				);
				WriteNullableStringProperty(body, "Name", "Gets or sets the name of the instrument.");
				WriteNullableStringProperty(body, "Unit", "Gets or sets the measurement unit of the instrument.");
				WriteNullableStringProperty(body, "Description", "Gets or sets the description of the instrument.");
				WritePublicProperty(
					body,
					"ThrowOnAlreadyInitialized",
					TypeLibrary.System.Boolean,
					"Determines whether an exception is thrown when the instrument is initialized more than once."
				);
			},
			summary
		);
	}

	static void WriteNameUnitDescriptionConstructor(
		CodeWriter writer,
		TypeIdentity type,
		string summary,
		bool appendAutoIncrement = false,
		bool appendThrowOnAlreadyInitialized = false
	)
	{
#if DEBUG
		if (string.IsNullOrWhiteSpace(summary))
			throw new ArgumentException("Summary cannot be null or whitespace.", nameof(summary));
#endif

		writer
			.XmlSummary(summary)
			.XmlParam("name", "The name of the instrument.")
			.XmlParam("unit", "The measurement unit of the instrument.")
			.XmlParam("description", "The description of the instrument.");

		if (appendAutoIncrement)
			writer.XmlParam("autoIncrement", "Whether the counter should auto-increment.");
		if (appendThrowOnAlreadyInitialized)
		{
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
		parameters = parameters.Add(new("name", TypeLibrary.System.String));
		parameters = parameters.Add(new("unit", TypeLibrary.System.String) { DefaultValue = "null" });
		parameters = parameters.Add(new("description", TypeLibrary.System.String) { DefaultValue = "null" });
		if (appendAutoIncrement)
			parameters = parameters.Add(new("autoIncrement", TypeLibrary.System.Boolean) { DefaultValue = "false" });
		if (appendThrowOnAlreadyInitialized)
			parameters = parameters.Add(
				new("throwOnAlreadyInitialized", TypeLibrary.System.Boolean) { DefaultValue = "false" }
			);

		return parameters;
	}

	// -------------------------------------------------------------------------------------------
	// Enum templates
	// -------------------------------------------------------------------------------------------

	static void WriteTargetsEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace)
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
			.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace)
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
					.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace)
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
					.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace)
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
			.FileScopedNamespace(TypeLibrary.Purview.Telemetry.Namespace)
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
