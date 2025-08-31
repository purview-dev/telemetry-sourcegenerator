using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class PipelineHelpers
{
	public static bool HasMultiTargetAttribute(SyntaxNode node, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		return node is InterfaceDeclarationSyntax;
	}

	public static MultiTargetInterface? BuildMultiTargetTransform(
		GeneratorAttributeSyntaxContext context,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (context.TargetSymbol is not INamedTypeSymbol interfaceSymbol)
		{
			logger?.Warning("MultiTarget transform called on non-interface symbol");
			return null;
		}

		if (context.TargetNode is not InterfaceDeclarationSyntax interfaceSyntax)
		{
			logger?.Warning("MultiTarget transform called on non-interface syntax");
			return null;
		}

		var assembly = context.SemanticModel.Compilation.Assembly;
		
		// Check if multi-target generation is enabled at the assembly level
		var enableMultiTargetAttr = assembly
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass != null
				&& PurviewTypeFactory.Create(attr.AttributeClass)
					== Constants.Shared.EnableMultiTargetGenerationAttribute
			);

		if (enableMultiTargetAttr == null)
		{
			logger?.Debug($"Multi-target generation not enabled at assembly level");
			return null;
		}

		// Get TelemetryGeneration attribute from interface
		var telemetryGeneration = SharedHelpers.GetTelemetryGenerationAttribute(
			interfaceSymbol,
			context.SemanticModel,
			logger,
			cancellationToken
		);

		// Get interface information
		var interfaceName = interfaceSymbol.Name;
		var namespaceName = interfaceSymbol.ContainingNamespace?.ToDisplayString() ?? "";
		var parentClasses = Utilities.GetParentClasses(interfaceSyntax);

		// Find methods with Telemetry attribute
		var multiTargetMethods = ProcessMultiTargetMethods(
			interfaceSymbol,
			context.SemanticModel.Compilation,
			logger,
			cancellationToken
		);

		if (multiTargetMethods.IsEmpty)
		{
			logger?.Debug($"No multi-target methods found in interface {interfaceName}");
			return null;
		}

		// Determine what generation types are needed
		var generationType = GetGenerationTypeFromMethods(multiTargetMethods);

		var location = interfaceSyntax.GetLocation();

		return new MultiTargetInterface(
			InterfaceName: interfaceName,
			FullyQualifiedInterfaceName: interfaceSymbol.ToDisplayString(),
			InterfaceSymbol: interfaceSymbol,
			Namespace: namespaceName,
			ParentClasses: parentClasses,
			TelemetryGeneration: telemetryGeneration,
			Methods: multiTargetMethods,
			GenerationType: generationType,
			Location: location
		);
	}

	static ImmutableArray<MultiTargetMethod> ProcessMultiTargetMethods(
		INamedTypeSymbol interfaceSymbol,
		Compilation compilation,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		var result = ImmutableArray.CreateBuilder<MultiTargetMethod>();

		foreach (var method in GetAllInterfaceMethods(interfaceSymbol, compilation, cancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Look for Telemetry attribute on method
			var telemetryAttribute = method
				.GetAttributes()
				.FirstOrDefault(attr =>
					attr.AttributeClass != null
					&& PurviewTypeFactory.Create(attr.AttributeClass)
						== Constants.Shared.TelemetryAttribute
				);

			if (telemetryAttribute == null)
			{
				logger?.Debug($"Method {method.Name} does not have Telemetry attribute");
				continue;
			}

			// Parse the telemetry configuration from the attribute
			var config = ParseTelemetryConfiguration(telemetryAttribute);

			if (!config.IsMultiTargetEnabled)
			{
				logger?.Debug($"Method {method.Name} has Telemetry attribute but no targets enabled");
				continue;
			}

			// Process parameters
			var parameters = ProcessMultiTargetParameters(
				method.Parameters,
				logger,
				cancellationToken
			);

			var multiTargetMethod = new MultiTargetMethod(
				MethodName: method.Name,
				FullyQualifiedMethodName: method.ToDisplayString(),
				MethodSymbol: method,
				Configuration: config,
				Parameters: parameters,
				Location: method.Locations.FirstOrDefault() ?? Location.None
			);

			result.Add(multiTargetMethod);
		}

		return result.ToImmutable();
	}

	static MultiTargetConfiguration ParseTelemetryConfiguration(AttributeData telemetryAttribute)
	{
		var targetTypes = GenerationType.None;
		string? activityName = null;
		string? logMessage = null;
		string? logLevel = null;
		int? logEventId = null;

		// Check GenerateActivity property
		var generateActivity = GetAttributeProperty<bool>(telemetryAttribute, "GenerateActivity");
		if (generateActivity == true)
		{
			targetTypes |= GenerationType.Activities;
			activityName = GetAttributeProperty<string>(telemetryAttribute, "ActivityName");
		}

		// Check GenerateLogging property
		var generateLogging = GetAttributeProperty<bool>(telemetryAttribute, "GenerateLogging");
		if (generateLogging == true)
		{
			targetTypes |= GenerationType.Logging;
			logMessage = GetAttributeProperty<string>(telemetryAttribute, "LogMessage");
			
			// Handle LogLevel enum property
			var logLevelValue = GetAttributeProperty<object>(telemetryAttribute, "LogLevel");
			if (logLevelValue != null)
			{
				logLevel = logLevelValue.ToString();
			}
			
			logEventId = GetAttributeProperty<int?>(telemetryAttribute, "LogEventId");
		}

		// Check GenerateMetrics property
		var generateMetrics = GetAttributeProperty<bool>(telemetryAttribute, "GenerateMetrics");
		if (generateMetrics == true)
		{
			targetTypes |= GenerationType.Metrics;
		}

		return new MultiTargetConfiguration(
			IsMultiTargetEnabled: targetTypes != GenerationType.None,
			TargetTypes: targetTypes,
			ActivityName: activityName,
			LogMessage: logMessage,
			LogLevel: logLevel,
			LogEventId: logEventId
		);
	}

	static GenerationType GetGenerationTypeFromMethods(ImmutableArray<MultiTargetMethod> methods)
	{
		var generationType = GenerationType.None;

		foreach (var method in methods)
		{
			generationType |= method.Configuration.TargetTypes;
		}

		return generationType;
	}

	static ImmutableArray<MultiTargetParameter> ProcessMultiTargetParameters(
		ImmutableArray<IParameterSymbol> parameters,
		GenerationLogger? logger,
		CancellationToken cancellationToken
	)
	{
		var result = ImmutableArray.CreateBuilder<MultiTargetParameter>();

		foreach (var parameter in parameters)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var exclusions = Utilities.GetParameterExclusions(parameter);
			var (isTag, tagName) = GetTagInfo(parameter);
			var (isBaggage, baggageName) = GetBaggageInfo(parameter);

			logger?.Debug(
				$"Found parameter '{parameter.Name}': IsTag={isTag} (TagName={tagName}), IsBaggage={isBaggage} (BaggageName={baggageName}), Exclusions={exclusions}"
			);

			if (isTag && isBaggage)
			{
				logger?.Warning(
					$"Parameter {parameter.Name} cannot be both a Tag and Baggage. It will be treated as a Tag."
				);
				isBaggage = false;
				baggageName = null;
			}

			MultiTargetParameter multiTargetParam = new(
				Name: parameter.Name,
				TypeName: parameter.Type.ToDisplayString(),
				ParameterSymbol: parameter,
				Exclusions: exclusions,
				IsTag: isTag,
				IsBaggage: isBaggage,
				TagName: tagName,
				BaggageName: baggageName
			);

			result.Add(multiTargetParam);
		}

		return result.ToImmutable();
	}

	static (bool IsTag, string? TagName) GetTagInfo(IParameterSymbol parameter)
	{
		var tagAttribute = parameter
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass != null
				&& PurviewTypeFactory.Create(attr.AttributeClass) == Constants.Shared.TagAttribute
			);

		if (tagAttribute == null)
			return (false, null);

		// Extract tag name from attribute
		string? tagName = null;
		if (tagAttribute.ConstructorArguments.Length > 0)
		{
			var nameValue = tagAttribute.ConstructorArguments[0].Value;
			if (nameValue is string name && !string.IsNullOrWhiteSpace(name))
				tagName = name;
		}

		// If no name specified, use parameter name
		tagName ??= parameter.Name;

		return (true, tagName);
	}

	static (bool IsBaggage, string? BaggageName) GetBaggageInfo(IParameterSymbol parameter)
	{
		// Check for Activities.BaggageAttribute
		var baggageAttribute = parameter
			.GetAttributes()
			.FirstOrDefault(attr =>
				attr.AttributeClass != null
				&& PurviewTypeFactory.Create(attr.AttributeClass)
					== Constants.Activities.BaggageAttribute
			);

		if (baggageAttribute == null)
			return (false, null);

		// Extract baggage name from attribute
		string? baggageName = null;
		if (baggageAttribute.ConstructorArguments.Length > 0)
		{
			var nameValue = baggageAttribute.ConstructorArguments[0].Value;
			if (nameValue is string name && !string.IsNullOrWhiteSpace(name))
				baggageName = name;
		}

		// If no name specified, use parameter name
		baggageName ??= parameter.Name;

		return (true, baggageName);
	}

	/// <summary>
	/// Helper method to extract property values from attributes.
	/// </summary>
	static T? GetAttributeProperty<T>(AttributeData attribute, string propertyName)
	{
		var namedArgument = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == propertyName);
		if (namedArgument.Key != null && namedArgument.Value.Value is T value)
		{
			return value;
		}

		return default(T);
	}
}
