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
	static readonly Dictionary<TypeIdentity, Action<CodeWriter, TypeIdentity>> Emitters = new()
	{
		// Telemetry Shared
		[TypeLibrary.TelemetryShared.TagAttribute] = WriteTagLikeAttribute,
		[TypeLibrary.TelemetryShared.ExcludeAttribute] = (writer, type) =>
			WriteSimpleAttribute(writer, type, AttributeTargets.Method, includeSuppressMessage: false),
		[TypeLibrary.TelemetryShared.TelemetryGenerationAttribute] = WriteTelemetryGenerationAttribute,
		[TypeLibrary.TelemetryShared.Targets] = WriteTargetsEnum,
		[TypeLibrary.TelemetryShared.NamingConvention] = WriteNamingConventionEnum,
		[TypeLibrary.TelemetryShared.ExcludeTargetsAttribute] = WriteExcludeTargetsAttribute,
		// Activities
		[TypeLibrary.Activities.BaggageAttribute] = WriteTagLikeAttribute,
		[TypeLibrary.Activities.ActivitySourceGenerationAttribute] = WriteActivitySourceGenerationAttribute,
		[TypeLibrary.Activities.ActivitySourceAttribute] = WriteActivitySourceAttribute,
		[TypeLibrary.Activities.ActivityAttribute] = WriteActivityAttribute,
		[TypeLibrary.Activities.EventAttribute] = WriteEventAttribute,
		[TypeLibrary.Activities.ContextAttribute] = (writer, type) =>
			WriteSimpleAttribute(writer, type, AttributeTargets.Method, includeSuppressMessage: false),
		[TypeLibrary.Activities.EscapeAttribute] = (writer, type) =>
			WriteSimpleAttribute(writer, type, AttributeTargets.Parameter, includeSuppressMessage: false),
		[TypeLibrary.Activities.StatusDescriptionAttribute] = (writer, type) =>
			WriteSimpleAttribute(writer, type, AttributeTargets.Parameter, includeSuppressMessage: false),
		// Logging
		[TypeLibrary.Logging.LoggerGenerationAttribute] = WriteLoggerGenerationAttribute,
		[TypeLibrary.Logging.LoggerAttribute] = WriteLoggerAttribute,
		[TypeLibrary.Logging.LogAttribute] = WriteLogAttribute,
		[TypeLibrary.Logging.LogPrefixType] = WriteLogPrefixTypeEnum,
		[TypeLibrary.Logging.LoggerGenerationMode] = WriteLoggerGenerationModeEnum,
		[TypeLibrary.Logging.ExpandEnumerableAttribute] = WriteExpandEnumerableAttribute,
		[TypeLibrary.Logging.TraceAttribute] = WriteSpecificLogAttribute,
		[TypeLibrary.Logging.DebugAttribute] = WriteSpecificLogAttribute,
		[TypeLibrary.Logging.InfoAttribute] = WriteSpecificLogAttribute,
		[TypeLibrary.Logging.WarningAttribute] = WriteSpecificLogAttribute,
		[TypeLibrary.Logging.ErrorAttribute] = WriteSpecificLogAttribute,
		[TypeLibrary.Logging.CriticalAttribute] = WriteSpecificLogAttribute,
		// Metrics
		[TypeLibrary.Metrics.MeterGenerationAttribute] = WriteMeterGenerationAttribute,
		[TypeLibrary.Metrics.MeterAttribute] = WriteMeterAttribute,
		[TypeLibrary.Metrics.MeterNameGenerationType] = WriteMeterNameGenerationTypeEnum,
		[TypeLibrary.Metrics.InstrumentMeasurementAttribute] = (writer, type) =>
			WriteSimpleAttribute(writer, type, AttributeTargets.Parameter, includeSuppressMessage: false),
		[TypeLibrary.Metrics.AutoCounterAttribute] = WriteAutoCounterAttribute,
		[TypeLibrary.Metrics.CounterAttribute] = WriteCounterLikeAttribute,
		[TypeLibrary.Metrics.UpDownCounterAttribute] = WriteCounterLikeAttribute,
		[TypeLibrary.Metrics.HistogramAttribute] = WriteCounterLikeAttribute,
		[TypeLibrary.Metrics.ObservableCounterAttribute] = WriteObservableCounterLikeAttribute,
		[TypeLibrary.Metrics.ObservableUpDownCounterAttribute] = WriteObservableCounterLikeAttribute,
		[TypeLibrary.Metrics.ObservableGaugeAttribute] = WriteObservableCounterLikeAttribute,
	};

	public static void EmitAll(IncrementalGeneratorPostInitializationContext context)
	{
		// Adds Microsoft.CodeAnalysis.EmbeddedAttribute to the compilation so generated marker
		// types (decorated with [Microsoft.CodeAnalysis.Embedded]) are invisible to downstream
		// assemblies, preventing CS0436 conflicts when multiple projects reference this generator.
		context.AddEmbeddedAttributeDefinition();

		var settings = GenerationSettings.Create<TelemetrySourceGenerator>();
		foreach (var type in TypeLibrary.GetAllGeneratedTypes())
		{
			CodeWriter writer = new(settings);
			WriteMarkerFileHeader(writer);

			Emit(writer, type);

			context.AddSource($"{type.MetadataFullName}.g.cs", writer);
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
		writer.WriteAutoGeneratedHeader(nullableDirective: NullableDirectiveMode.Disable);
		writer
			.WriteLine("#if !NET48_OR_GREATER && !PURVIEW_TELEMETRY_NON_NULLABLE")
			.WriteLine("#nullable enable")
			.WriteLine("#endif")
			.NewLine()
			.WriteLine("#pragma warning disable CS8625")
			.NewLine();
	}

	static void Emit(CodeWriter writer, TypeIdentity type)
	{
		if (!Emitters.TryGetValue(type, out var emit))
			throw new ArgumentOutOfRangeException(nameof(type), type.Name, "Unknown generation type requested.");

		emit(writer, type);
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
		bool includeSuppressMessage = true
	)
	{
		if (wrapInExcludeLoggingGuard)
			writer.WriteLine("#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING").NewLine();

		var attributes = ImmutableArray<AttributeDeclarationOptions>.Empty;
		attributes = attributes.Add(ConditionalAttribute());
		if (includeSuppressMessage)
			attributes = attributes.Add(SuppressMessageAttribute());

		writer
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.WriteAttributeClass(
				new(type.Name, TypeDeclarationAccessibility.Internal) { Attributes = attributes },
				targets,
				body
			);

		if (wrapInExcludeLoggingGuard)
			writer.WriteLine("#endif");
	}

	static void WriteSimpleAttribute(
		CodeWriter writer,
		TypeIdentity type,
		AttributeTargets targets,
		bool includeSuppressMessage
	)
	{
		EmitAttribute(writer, type, targets, static _ => { }, includeSuppressMessage: includeSuppressMessage);
	}

	static AttributeDeclarationOptions ConditionalAttribute() =>
		new(new TypeIdentity("ConditionalAttribute", "System.Diagnostics"))
		{
			Arguments = [new("\"PURVIEW_TELEMETRY_ATTRIBUTES\"")],
		};

	static AttributeDeclarationOptions SuppressMessageAttribute() =>
		new(new TypeIdentity("SuppressMessageAttribute", "System.Diagnostics.CodeAnalysis"))
		{
			Arguments = [new("\"Design\""), new("\"CA1019:Define accessors for attribute arguments\"")],
		};

	// -------------------------------------------------------------------------------------------
	// Members
	// -------------------------------------------------------------------------------------------

	static void WriteEmptyConstructor(CodeWriter writer, TypeIdentity type) =>
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public),
			static _ => { }
		);

	static void WriteNameConstructor(CodeWriter writer, TypeIdentity type) =>
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters =
				[
					new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference()),
				],
			},
			ctor => ctor.WriteAssignment("Name", "name")
		);

	static void WriteMessageTemplateConstructor(CodeWriter writer, TypeIdentity type) =>
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters =
				[
					new ParameterDeclarationOptions(
						"messageTemplate",
						PurviewTypeLibrary.System.String.AsTypeReference()
					),
				],
			},
			ctor => ctor.WriteAssignment("MessageTemplate", "messageTemplate")
		);

	static void WriteEventIdConstructor(CodeWriter writer, TypeIdentity type) =>
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = [new("eventId", PurviewTypeLibrary.System.Int32.AsTypeReference())],
			},
			ctor => ctor.WriteAssignment("EventId", "eventId")
		);

	/// <summary>Writes a public property with generated attributes and an optional initializer.</summary>
	static void WritePublicProperty(CodeWriter writer, string name, TypeReference type, string? initializer = null)
	{
		writer.WriteProperty(
			new PropertyDeclarationOptions(name, type, TypeDeclarationAccessibility.Public)
			{
				HasSetter = true,
				Initializer = initializer,
			}
		);
	}

	/// <summary>
	/// Writes a public nullable-capable string property inside the <c>NET48_OR_GREATER</c>/
	/// <c>PURVIEW_TELEMETRY_NON_NULLABLE</c> preprocessor guard used by the marker attributes.
	/// </summary>
	static void WriteNullableStringProperty(CodeWriter writer, string name)
	{
		writer.WriteLine("#if NET48_OR_GREATER || PURVIEW_TELEMETRY_NON_NULLABLE");
		writer.WriteProperty(
			new PropertyDeclarationOptions(
				name,
				PurviewTypeLibrary.System.String.AsTypeReference(),
				TypeDeclarationAccessibility.Public
			)
			{
				HasSetter = true,
				IncludeGeneratedAttributes = false,
			}
		);
		writer.WriteLine("#else");
		writer.WriteProperty(
			new(name, PurviewTypeLibrary.System.String.MakeNullable(writer), TypeDeclarationAccessibility.Public)
			{
				HasSetter = true,
				IncludeGeneratedAttributes = false,
			}
		);
		writer.WriteLine("#endif");
	}

	/// <summary>Writes a public non-nullable string property (used for defaults that always have a value).</summary>
	static void WritePlainStringProperty(CodeWriter writer, string name, string? initializer = null)
	{
		writer.WriteProperty(
			new PropertyDeclarationOptions(
				name,
				PurviewTypeLibrary.System.String.AsTypeReference(),
				TypeDeclarationAccessibility.Public
			)
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

	static void WriteTagLikeAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Parameter,
			body =>
			{
				WriteEmptyConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"skipOnNullOrEmpty",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							),
						],
					},
					ctor => ctor.WriteAssignment("SkipOnNullOrEmpty", "skipOnNullOrEmpty")
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new ParameterDeclarationOptions(
								"skipOnNullOrEmpty",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "false",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("Name", "name");
						ctor.WriteAssignment("SkipOnNullOrEmpty", "skipOnNullOrEmpty");
					}
				);

				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(body, "SkipOnNullOrEmpty", PurviewTypeLibrary.System.Boolean.AsTypeReference());
			}
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
				WriteEmptyConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"generateDependencyExtension",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							),
							new ParameterDeclarationOptions(
								"className",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions(
								"dependencyInjectionClassName",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("GenerateDependencyExtension", "generateDependencyExtension");
						ctor.WriteAssignment("ClassName", "className");
						ctor.WriteAssignment("DependencyInjectionClassName", "dependencyInjectionClassName");
					}
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"className",
								PurviewTypeLibrary.System.String.AsTypeReference()
							),
							new ParameterDeclarationOptions(
								"dependencyInjectionClassName",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("ClassName", "className");
						ctor.WriteAssignment("DependencyInjectionClassName", "dependencyInjectionClassName");
					}
				);

				WritePublicProperty(
					body,
					"GenerateDependencyExtension",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WriteNullableStringProperty(body, "ClassName");
				WriteNullableStringProperty(body, "DependencyInjectionClassName");
				WritePublicProperty(
					body,
					"DependencyInjectionClassIsPublic",
					PurviewTypeLibrary.System.Boolean.AsTypeReference()
				);
				WritePublicProperty(
					body,
					"NamingConvention",
					namingConvention.AsTypeReference(),
					$"{namingConvention.RenderFullName}.OpenTelemetry"
				);
				WritePublicProperty(
					body,
					"GenerateTelemetryNamesClass",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WriteNullableStringProperty(body, "TelemetryNamesClassName");
				WriteNullableStringProperty(body, "TelemetryNamesNamespace");
			}
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
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new ParameterDeclarationOptions("targets", targets.AsTypeReference())],
					},
					ctor => ctor.WriteAssignment("ExcludedTargets", "targets")
				);

				body.XmlSummary("Gets or sets the targets to exclude for this parameter.");
				WritePublicProperty(body, "ExcludedTargets", targets.AsTypeReference());
			}
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
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new ParameterDeclarationOptions(
								"defaultToTags",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
							new ParameterDeclarationOptions(
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
						ctor.WriteLine(
							"if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentNullException(nameof(name));"
						);
						ctor.WriteAssignment("Name", "name");
						ctor.WriteAssignment("DefaultToTags", "defaultToTags");
						ctor.WriteAssignment(
							"GenerateDiagnosticsForMissingActivity",
							"generateDiagnosticsForMissingActivity"
						);
					}
				);

				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(body, "DefaultToTags", PurviewTypeLibrary.System.Boolean.AsTypeReference(), "true");
				WriteNullableStringProperty(body, "BaggageAndTagPrefix");
				WritePlainStringProperty(body, "BaggageAndTagSeparator", "\".\"");
				WritePublicProperty(
					body,
					"LowercaseBaggageAndTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(
					body,
					"GenerateDiagnosticsForMissingActivity",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
			}
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

				WriteNullableStringProperty(body, "Name");
				body.XmlSummary("Specifies the default when inferring between tag or baggage.");
				WritePublicProperty(body, "DefaultToTags", PurviewTypeLibrary.System.Boolean.AsTypeReference(), "true");
				WriteNullableStringProperty(body, "BaggageAndTagPrefix");
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
			}
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
				WriteEmptyConstructor(body, type);
				WriteNameConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new ParameterDeclarationOptions("kind", activityKind.AsTypeReference())],
					},
					ctor => ctor.WriteAssignment("Kind", "kind")
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new ParameterDeclarationOptions("kind", activityKind.AsTypeReference())
							{
								DefaultValue = $"{activityKind.RenderFullName}.Internal",
							},
							new ParameterDeclarationOptions(
								"createOnly",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "false",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("Name", "name");
						ctor.WriteAssignment("Kind", "kind");
						ctor.WriteAssignment("CreateOnly", "createOnly");
					}
				);

				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(body, "Kind", activityKind.AsTypeReference());
				WritePublicProperty(body, "CreateOnly", PurviewTypeLibrary.System.Boolean.AsTypeReference());
			}
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
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("statusCode", statusCode.AsTypeReference())
							{
								DefaultValue = $"{statusCode.RenderFullName}.Unset",
							},
						],
					},
					ctor => ctor.WriteAssignment("StatusCode", "statusCode")
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference()),
							new ParameterDeclarationOptions(
								"useRecordExceptionRules",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
							new ParameterDeclarationOptions(
								"recordExceptionAsEscaped",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
							new ParameterDeclarationOptions("statusCode", statusCode.AsTypeReference())
							{
								DefaultValue = $"{statusCode.RenderFullName}.Unset",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("Name", "name");
						ctor.WriteAssignment("UseRecordExceptionRules", "useRecordExceptionRules");
						ctor.WriteAssignment("RecordExceptionAsEscaped", "recordExceptionAsEscaped");
						ctor.WriteAssignment("StatusCode", "statusCode");
					}
				);

				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(
					body,
					"UseRecordExceptionRules",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(
					body,
					"RecordExceptionAsEscaped",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(body, "StatusCode", statusCode.AsTypeReference());
				WriteNullableStringProperty(body, "StatusDescription");
			}
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
				WriteEmptyConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters = [new ParameterDeclarationOptions("defaultLevel", logLevel.AsTypeReference())],
					},
					ctor => ctor.WriteAssignment("DefaultLevel", "defaultLevel")
				);

				WritePublicProperty(
					body,
					"DefaultLevel",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information"
				);
				WritePublicProperty(body, "GenerationMode", TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference());
				WritePublicProperty(body, "DefaultPrefixType", TypeLibrary.Logging.LogPrefixType.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true
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
				WriteEmptyConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("defaultLevel", logLevel.AsTypeReference()),
							new ParameterDeclarationOptions(
								"customPrefix",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("DefaultLevel", "defaultLevel");
						ctor.WriteAssignment("CustomPrefix", "customPrefix");
						ctor.WriteIfBlock(
							"!string.IsNullOrWhiteSpace(CustomPrefix)",
							block => block.WriteAssignment("PrefixType", $"{logPrefixType.RenderFullName}.Custom")
						);
					}
				);

				WritePublicProperty(
					body,
					"DefaultLevel",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information"
				);
				WriteNullableStringProperty(body, "CustomPrefix");
				WritePublicProperty(body, "PrefixType", logPrefixType.AsTypeReference());
				WritePublicProperty(body, "GenerationMode", TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true
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
				WriteEmptyConstructor(body, type);
				WriteMessageTemplateConstructor(body, type);
				WriteEventIdConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions("level", logLevel.AsTypeReference()),
							new ParameterDeclarationOptions(
								"messageTemplate",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("Level", "level");
						ctor.WriteAssignment("MessageTemplate", "messageTemplate");
						ctor.WriteAssignment("Name", "name");
					}
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"eventId",
								PurviewTypeLibrary.System.Int32.AsTypeReference()
							),
							new ParameterDeclarationOptions("level", logLevel.AsTypeReference()),
							new ParameterDeclarationOptions(
								"messageTemplate",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("Level", "level");
						ctor.WriteAssignment("MessageTemplate", "messageTemplate");
						ctor.WriteAssignment("EventId", "eventId");
						ctor.WriteAssignment("Name", "name");
					}
				);

				WritePublicProperty(
					body,
					"Level",
					logLevel.AsTypeReference(),
					$"{logLevel.RenderFullName}.Information"
				);
				WriteNullableStringProperty(body, "MessageTemplate");
				WritePublicProperty(body, "EventId", PurviewTypeLibrary.System.Int32.MakeNullable(writer));
				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(body, "GenerationMode", TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true
		);
	}

	static void WriteSpecificLogAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteMessageTemplateConstructor(body, type);
				WriteEventIdConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"messageTemplate",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("MessageTemplate", "messageTemplate");
						ctor.WriteAssignment("Name", "name");
					}
				);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"eventId",
								PurviewTypeLibrary.System.Int32.AsTypeReference()
							),
							new ParameterDeclarationOptions(
								"messageTemplate",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference())
							{
								DefaultValue = "null",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("MessageTemplate", "messageTemplate");
						ctor.WriteAssignment("EventId", "eventId");
						ctor.WriteAssignment("Name", "name");
					}
				);

				WriteNullableStringProperty(body, "MessageTemplate");
				WritePublicProperty(body, "EventId", PurviewTypeLibrary.System.Int32.MakeNullable(writer));
				WriteNullableStringProperty(body, "Name");
				WritePublicProperty(body, "GenerationMode", TypeLibrary.Logging.LoggerGenerationMode.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true
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
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"maximumValueCount",
								PurviewTypeLibrary.System.Int32.AsTypeReference()
							)
							{
								DefaultValue = "5",
							},
						],
					},
					ctor => ctor.WriteAssignment("MaximumValueCount", "maximumValueCount")
				);

				body.XmlSummary("Gets or sets the maximum number of values to include when expanding an enumerable.");
				WritePublicProperty(body, "MaximumValueCount", PurviewTypeLibrary.System.Int32.AsTypeReference());
			},
			wrapInExcludeLoggingGuard: true
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
				WriteEmptyConstructor(body, type);

				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"meterName",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions("nameGenerationType", nameGenerationType.AsTypeReference())
							{
								DefaultValue = $"{nameGenerationType.RenderFullName}.DotNet",
							},
							new ParameterDeclarationOptions(
								"instrumentPrefix",
								PurviewTypeLibrary.System.String.AsTypeReference()
							)
							{
								DefaultValue = "null",
							},
							new ParameterDeclarationOptions(
								"lowercaseInstrumentName",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
							new ParameterDeclarationOptions(
								"lowercaseTagKeys",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							)
							{
								DefaultValue = "true",
							},
						],
					},
					ctor =>
					{
						ctor.WriteAssignment("MeterName", "meterName");
						ctor.WriteAssignment("MeterNameGenerationType", "nameGenerationType");
						ctor.WriteAssignment("InstrumentPrefix", "instrumentPrefix");
						ctor.WriteAssignment("LowercaseInstrumentName", "lowercaseInstrumentName");
						ctor.WriteAssignment("LowercaseTagKeys", "lowercaseTagKeys");
					}
				);

				WriteNullableStringProperty(body, "MeterName");
				WritePublicProperty(
					body,
					"MeterNameGenerationType",
					nameGenerationType.AsTypeReference(),
					$"{nameGenerationType.RenderFullName}.DotNet"
				);
				WriteNullableStringProperty(body, "InstrumentPrefix");
				WritePlainStringProperty(body, "InstrumentSeparator", "\".\"");
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
			}
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
				WriteEmptyConstructor(body, type);
				WriteNameConstructor(body, type);

				WriteNullableStringProperty(body, "Name");
				WriteNullableStringProperty(body, "InstrumentPrefix");
				WritePublicProperty(
					body,
					"IncludeAssemblyInstrumentPrefix",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseInstrumentName",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
				WritePublicProperty(
					body,
					"LowercaseTagKeys",
					PurviewTypeLibrary.System.Boolean.AsTypeReference(),
					"true"
				);
			}
		);
	}

	static void WriteAutoCounterAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type);
				WriteNameUnitDescriptionConstructor(body, type);

				WriteNullableStringProperty(body, "Name");
				WriteNullableStringProperty(body, "Unit");
				WriteNullableStringProperty(body, "Description");
			}
		);
	}

	static void WriteCounterLikeAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type);
				body.WriteConstructor(
					new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
					{
						Parameters =
						[
							new ParameterDeclarationOptions(
								"autoIncrement",
								PurviewTypeLibrary.System.Boolean.AsTypeReference()
							),
						],
					},
					ctor => ctor.WriteAssignment("AutoIncrement", "autoIncrement")
				);
				WriteNameUnitDescriptionConstructor(body, type, appendAutoIncrement: true);

				WritePublicProperty(body, "AutoIncrement", PurviewTypeLibrary.System.Boolean.AsTypeReference());
				WriteNullableStringProperty(body, "Name");
				WriteNullableStringProperty(body, "Unit");
				WriteNullableStringProperty(body, "Description");
			}
		);
	}

	static void WriteObservableCounterLikeAttribute(CodeWriter writer, TypeIdentity type)
	{
		EmitAttribute(
			writer,
			type,
			AttributeTargets.Method,
			body =>
			{
				WriteEmptyConstructor(body, type);
				WriteNameUnitDescriptionConstructor(body, type, appendThrowOnAlreadyInitialized: true);

				WritePublicProperty(body, "AutoIncrement", PurviewTypeLibrary.System.Boolean.AsTypeReference());
				WriteNullableStringProperty(body, "Name");
				WriteNullableStringProperty(body, "Unit");
				WriteNullableStringProperty(body, "Description");
				WritePublicProperty(
					body,
					"ThrowOnAlreadyInitialized",
					PurviewTypeLibrary.System.Boolean.AsTypeReference()
				);
			}
		);
	}

	static void WriteNameUnitDescriptionConstructor(
		CodeWriter writer,
		TypeIdentity type,
		bool appendAutoIncrement = false,
		bool appendThrowOnAlreadyInitialized = false
	)
	{
		writer.WriteConstructor(
			new ConstructorDeclarationOptions(type.Name, TypeDeclarationAccessibility.Public)
			{
				Parameters = BuildNameUnitDescriptionParameters(appendAutoIncrement, appendThrowOnAlreadyInitialized),
			},
			ctor =>
			{
				ctor.WriteAssignment("Name", "name");
				ctor.WriteAssignment("Unit", "unit");
				ctor.WriteAssignment("Description", "description");
				if (appendAutoIncrement)
					ctor.WriteAssignment("AutoIncrement", "autoIncrement");
				if (appendThrowOnAlreadyInitialized)
					ctor.WriteAssignment("ThrowOnAlreadyInitialized", "throwOnAlreadyInitialized");
			}
		);
	}

	static ImmutableArray<ParameterDeclarationOptions> BuildNameUnitDescriptionParameters(
		bool appendAutoIncrement,
		bool appendThrowOnAlreadyInitialized
	)
	{
		var parameters = ImmutableArray<ParameterDeclarationOptions>.Empty;
		parameters = parameters.Add(
			new ParameterDeclarationOptions("name", PurviewTypeLibrary.System.String.AsTypeReference())
		);
		parameters = parameters.Add(
			new ParameterDeclarationOptions("unit", PurviewTypeLibrary.System.String.AsTypeReference())
			{
				DefaultValue = "null",
			}
		);
		parameters = parameters.Add(
			new ParameterDeclarationOptions("description", PurviewTypeLibrary.System.String.AsTypeReference())
			{
				DefaultValue = "null",
			}
		);
		if (appendAutoIncrement)
			parameters = parameters.Add(
				new ParameterDeclarationOptions("autoIncrement", PurviewTypeLibrary.System.Boolean.AsTypeReference())
				{
					DefaultValue = "false",
				}
			);
		if (appendThrowOnAlreadyInitialized)
			parameters = parameters.Add(
				new ParameterDeclarationOptions(
					"throwOnAlreadyInitialized",
					PurviewTypeLibrary.System.Boolean.AsTypeReference()
				)
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
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines which telemetry targets a parameter is excluded from.")
			.WriteEnum(
				new(type.Name, TypeDeclarationAccessibility.Public)
				{
					Attributes = [new(new TypeIdentity("FlagsAttribute", "System"))],
				},
				new("None", 0, "No telemetry targets are excluded."),
				new("Activities", 1, "Excludes activity (tracing) targets."),
				new("Logging", 2, "Excludes logging targets."),
				new("Metrics", 4, "Excludes metrics targets."),
				new("All", "Activities | Logging | Metrics")
			);
	}

	static void WriteNamingConventionEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines the naming convention used for generated telemetry names.")
			.WriteEnum(
				new(type.Name, TypeDeclarationAccessibility.Public),
				new("Legacy", 0, "Uses the legacy naming convention for generated telemetry names."),
				new("OpenTelemetry", 1, "Uses the OpenTelemetry naming convention for generated telemetry names.")
			);
	}

	static void WriteLogPrefixTypeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer.WriteLine("#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING").NewLine();
		writer
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines the mode used to generate or override the prefix for the log entry.")
			.WriteEnum(
				new(type.Name, TypeDeclarationAccessibility.Public),
				new("Default", 0, "Uses the default log prefix."),
				new("Interface", 1, "Uses the interface name as the log prefix."),
				new("Class", 2, "Uses the class name as the log prefix."),
				new("Custom", 3, "Uses a custom log prefix."),
				new("TrimmedClassName", 4, "Uses the trimmed class name as the log prefix.")
			);

		writer.WriteLine("#endif");
	}

	static void WriteLoggerGenerationModeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer.WriteLine("#if !EXCLUDE_PURVIEW_TELEMETRY_LOGGING").NewLine();

		writer
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Controls the generation mode used for log methods.")
			.WriteEnum(
				new(type.Name, TypeDeclarationAccessibility.Public),
				new("Auto", 0, "Automatically selects the log generation mode."),
				new("V1", 1, "Uses the first-generation log implementation."),
				new("V2", 2, "Uses the second-generation log implementation.")
			);

		writer.WriteLine("#endif");
	}

	static void WriteMeterNameGenerationTypeEnum(CodeWriter writer, TypeIdentity type)
	{
		writer
			.WriteFileScopedNamespace(TypeLibrary.PurviewTelemetryNamespace)
			.XmlSummary("Determines how meter names are generated when not explicitly specified.")
			.WriteEnum(
				new(type.Name, TypeDeclarationAccessibility.Public),
				new("OpenTelemetry", 0, "Generates meter names using the OpenTelemetry convention."),
				new("DotNet", 1, "Generates meter names using the .NET convention.")
			);
	}
}
