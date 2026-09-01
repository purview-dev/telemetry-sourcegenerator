using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class Utilities
{
	static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000));

	public static TargetGeneration IsValidGenerationTarget(
		IMethodSymbol method,
		GenerationType generationType,
		GenerationType requestedType
	)
	{
		var (activityCount, loggingCount, metricsCount) = CountAttributeFamilies(method);

		// Intra-family conflicts (multiple attributes within same family) are always an error.
		var multiGenerationTargetsNotSupported = activityCount > 1 || loggingCount > 1 || metricsCount > 1;

		var interfaceTargetCount = CountFlags(generationType);
		var methodTargets = ToGenerationType(activityCount > 0, loggingCount > 0, metricsCount > 0);
		var methodTargetFamilyCount = CountFlags(methodTargets);
		var isMultiTarget = methodTargetFamilyCount > 1;

		// If the interface has multiple target families, methods need explicit attributes (no inference).
		var inferenceNotSupportedWithMultiTargeting = interfaceTargetCount > 1 && methodTargetFamilyCount == 0;

		// Method has explicit attributes for a family not registered on the interface.
		var raiseMissingInterfaceSource = (methodTargets & ~generationType) != GenerationType.None;

		// Valid for the requested target when there are no errors, and either the interface is
		// single-target (inference applies) or the method targets the requested family explicitly.
#pragma warning disable IDE0072 // Add missing cases
		var isValid =
			!multiGenerationTargetsNotSupported
			&& !inferenceNotSupportedWithMultiTargeting
			&& !raiseMissingInterfaceSource
			&& (
				interfaceTargetCount <= 1
				|| requestedType switch
				{
					GenerationType.Activities => activityCount > 0,
					GenerationType.Logging => loggingCount > 0,
					GenerationType.Metrics => metricsCount > 0,
					_ => false,
				}
			);
#pragma warning restore IDE0072 // Add missing cases

		// Activity parameter without an Activity target is flagged for diagnostics.
		var activityParameterWithoutTarget = activityCount == 0 ? FindActivityParameterWithoutTarget(method) : null;

		return new(
			IsValid: isValid,
			RaiseInferenceNotSupportedWithMultiTargeting: inferenceNotSupportedWithMultiTargeting,
			RaiseMultiGenerationTargetsNotSupported: multiGenerationTargetsNotSupported,
			IsMultiTarget: isMultiTarget,
			MethodTargets: methodTargets,
			ActivityParameterWithoutTarget: activityParameterWithoutTarget,
			RaiseMissingInterfaceSource: raiseMissingInterfaceSource
		);
	}

	static (int ActivityCount, int LoggingCount, int MetricsCount) CountAttributeFamilies(IMethodSymbol method)
	{
		var activityCount = 0;
		var loggingCount = 0;
		var metricsCount = 0;

		foreach (var attribute in method.GetAttributes())
		{
			if (attribute.AttributeClass == null)
				continue;

			var attributeType = TypeReference.Create(attribute.AttributeClass);

			if (IsActivityAttribute(attributeType))
				activityCount++;
			else if (IsLoggingAttribute(attributeType))
				loggingCount++;
			else if (IsMetricsAttribute(attributeType))
				metricsCount++;
		}

		return (activityCount, loggingCount, metricsCount);
	}

	static bool IsActivityAttribute(TypeReference attributeType) =>
		TemplateLibrary.Activities.ActivityAttribute == attributeType
		|| TemplateLibrary.Activities.EventAttribute == attributeType
		|| TemplateLibrary.Activities.ContextAttribute == attributeType;

	static bool IsLoggingAttribute(TypeReference attributeType) =>
		TemplateLibrary.Logging.LogAttribute == attributeType
		|| TemplateLibrary.Logging.TraceAttribute == attributeType
		|| TemplateLibrary.Logging.DebugAttribute == attributeType
		|| TemplateLibrary.Logging.InfoAttribute == attributeType
		|| TemplateLibrary.Logging.WarningAttribute == attributeType
		|| TemplateLibrary.Logging.ErrorAttribute == attributeType
		|| TemplateLibrary.Logging.CriticalAttribute == attributeType;

	static bool IsMetricsAttribute(TypeReference attributeType) =>
		TemplateLibrary.Metrics.CounterAttribute == attributeType
		|| TemplateLibrary.Metrics.AutoCounterAttribute == attributeType
		|| TemplateLibrary.Metrics.UpDownCounterAttribute == attributeType
		|| TemplateLibrary.Metrics.HistogramAttribute == attributeType
		|| TemplateLibrary.Metrics.ObservableCounterAttribute == attributeType
		|| TemplateLibrary.Metrics.ObservableGaugeAttribute == attributeType
		|| TemplateLibrary.Metrics.ObservableUpDownCounterAttribute == attributeType;

	static int CountFlags(GenerationType type)
	{
		var count = 0;
		if (type.HasFlag(GenerationType.Activities))
			count++;
		if (type.HasFlag(GenerationType.Logging))
			count++;
		if (type.HasFlag(GenerationType.Metrics))
			count++;
		return count;
	}

	static GenerationType ToGenerationType(bool activities, bool logging, bool metrics)
	{
		var result = GenerationType.None;
		if (activities)
			result |= GenerationType.Activities;
		if (logging)
			result |= GenerationType.Logging;
		if (metrics)
			result |= GenerationType.Metrics;
		return result;
	}

	static string? FindActivityParameterWithoutTarget(IMethodSymbol method)
	{
		foreach (var param in method.Parameters)
		{
			var paramType = TypeReference.Create(param.Type);
			if (paramType.Identity.Equals(TypeLibrary.Activities.SystemDiagnostics.Activity))
				return param.Name;
		}

		return null;
	}

	public static string WithComma(this string value, bool andSpace = true) => value + ',' + (andSpace ? ' ' : null);

	public static string Wrap(this string value, char c = '"') => c + value + c;

	public static EquatableArray<string> GetParentClasses(TypeDeclarationSyntax classDeclaration)
	{
		var parentClass = classDeclaration.Parent as ClassDeclarationSyntax;

		List<string> parentClassList = [];
		while (parentClass != null)
		{
			parentClassList.Add(parentClass.Identifier.Text);

			parentClass = parentClass.Parent as ClassDeclarationSyntax;
		}

		return parentClassList.Count == 0 ? new EquatableArray<string>([]) : parentClassList.ToImmutableArray();
	}

	public static string? GetParentClassesAsNamespace(TypeDeclarationSyntax classDeclaration)
	{
		var parentClass = classDeclaration.Parent as ClassDeclarationSyntax;

		List<string> parentClasses = [];
		while (parentClass != null)
		{
			parentClasses.Add(parentClass.Identifier.Text);

			parentClass = parentClass.Parent as ClassDeclarationSyntax;
		}

		if (parentClasses.Count == 0)
			return null;

		parentClasses.Reverse();
		return string.Join(".", parentClasses);
	}

	public static string? GetNamespace(TypeDeclarationSyntax typeSymbol)
	{
		// Determine the namespace the type is declared in, if any
		var potentialNamespaceParent = typeSymbol.Parent;
		while (
			potentialNamespaceParent
				is not null
					and not NamespaceDeclarationSyntax
					and not FileScopedNamespaceDeclarationSyntax
		)
		{
			potentialNamespaceParent = potentialNamespaceParent.Parent;
		}

		if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
		{
			var @namespace = namespaceParent.Name.ToString();
			while (namespaceParent.Parent is NamespaceDeclarationSyntax namespaceParentParent)
			{
				namespaceParent = namespaceParentParent;
				@namespace = $"{namespaceParent.Name}.{@namespace}";
			}

			return @namespace;
		}

		return null;
	}

	public static string? GetFullNamespace(TypeDeclarationSyntax type, bool includeTrailingSeparator)
	{
		var typeNamespace = GetNamespace(type);
		var parentClasses = GetParentClassesAsNamespace(type);

		string? fullNamespace = null;
		if (typeNamespace != null)
			fullNamespace = typeNamespace;

		if (parentClasses != null)
		{
			if (fullNamespace != null)
				fullNamespace += ".";

			fullNamespace += parentClasses;

			if (includeTrailingSeparator)
				fullNamespace += ".";
		}
		else if (includeTrailingSeparator && fullNamespace != null)
		{
			fullNamespace += ".";
		}

		return fullNamespace;
	}

	//public static bool IsEnumerableOrArray(string parameterType, string fullTypeName)
	//	=> IsArray(parameterType, fullTypeName)
	//		|| IsEnumerable(parameterType, fullTypeName);

	public static bool IsComplexType(this ITypeSymbol typeSymbol)
	{
		// Check for class, struct, or record types
		if (typeSymbol.TypeKind is TypeKind.Class or TypeKind.Struct)
		{
			// Exclude primitive types and special types like string
			if (typeSymbol.SpecialType is SpecialType.None)
				return true;
		}

		return false;
	}

	public static bool IsArray(this ITypeSymbol typeSymbol) =>
		typeSymbol.SpecialType != SpecialType.System_String && typeSymbol.TypeKind is TypeKind.Array;

	public static bool IsIEnumerable(this ITypeSymbol typeSymbol, Compilation compilation)
	{
		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;

		if (IsIEnumerable(typeSymbol))
			return true;

		// Get the `IEnumerable` symbol from the compilation
		var ienumerableSymbol = compilation.GetTypeByMetadataName(TypeLibrary.System.IEnumerable);

		// Check if the type implements `IEnumerable`
		return ienumerableSymbol != null
			&& typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, ienumerableSymbol));
	}

	static bool IsIEnumerable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;

		// Check for common enumerable types
		return typeSymbol.SpecialType
			is SpecialType.System_Collections_IEnumerable
				or SpecialType.System_Collections_Generic_ICollection_T
				or SpecialType.System_Collections_Generic_IList_T
				or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
				or SpecialType.System_Collections_Generic_IReadOnlyList_T
				or SpecialType.System_Collections_Generic_IEnumerable_T;
	}

	public static bool IsExceptionType(this ITypeSymbol typeSymbol)
	{
		// The type itself, or any base in the hierarchy, derives from System.Exception.
		return TypeLibrary.System.Exception.Equals(typeSymbol)
			|| TypeHelpers.InheritsFrom(typeSymbol, TypeLibrary.System.Exception);
	}

	public static string Flatten(this SyntaxNode syntax) => syntax.WithoutTrivia().ToString().Flatten();

	public static string Flatten(this string value) => WhitespaceRegex.Replace(value, " ");

	public static bool ContainsAttribute(ISymbol symbol, TypeIdentity type, CancellationToken token) =>
		TryContainsAttribute(symbol, type, token, out _);

	public static bool ContainsAttribute(ISymbol symbol, TemplateInfo templateInfo, CancellationToken token) =>
		TryContainsAttribute(symbol, templateInfo, token, out _);

	public static bool ContainsAttribute(ISymbol symbol, TemplateInfo[] templateInfo, CancellationToken token) =>
		TryContainsAttribute(symbol, templateInfo, token, out _, out _);

	public static bool TryContainsAttribute(
		ISymbol symbol,
		TypeIdentity type,
		CancellationToken token,
		out AttributeData? attributeData
	)
	{
		attributeData = null;

		var attributes = symbol.GetAttributes();
		foreach (var attribute in attributes)
		{
			token.ThrowIfCancellationRequested();
			if (attribute.AttributeClass == null)
				continue;

			if (!TypeIdentity.TryCreate(attribute.AttributeClass, out var attributeType))
				continue;

			if (attributeType.Equals(type))
			{
				attributeData = attribute;
				return true;
			}
		}

		return false;
	}

	public static bool TryContainsAttribute(
		ISymbol symbol,
		TemplateInfo templateInfo,
		CancellationToken token,
		out AttributeData? attributeData
	)
	{
		attributeData = null;

		var attributes = symbol.GetAttributes();
		foreach (var attribute in attributes)
		{
			token.ThrowIfCancellationRequested();
			if (attribute.AttributeClass == null)
				continue;

			if (!TypeIdentity.TryCreate(attribute.AttributeClass, out var attributeType))
				continue;

			if (templateInfo.Equals(attributeType))
			{
				attributeData = attribute;
				return true;
			}
		}

		return false;
	}

	public static bool TryContainsAttribute(
		ISymbol symbol,
		TemplateInfo[] templateInfo,
		CancellationToken token,
		out AttributeData? attributeData,
		out TemplateInfo? matchingTemplate
	)
	{
		attributeData = null;
		matchingTemplate = null;

		var attributes = symbol.GetAttributes();
		foreach (var attribute in attributes)
		{
			token.ThrowIfCancellationRequested();

			if (attribute.AttributeClass == null)
				continue;

			if (!TypeIdentity.TryCreate(attribute.AttributeClass, out var attributeType))
				continue;

			foreach (var template in templateInfo)
			{
				if (template.Equals(attributeType))
				{
					attributeData = attribute;
					matchingTemplate = template;

					return true;
				}
			}
		}

		return false;
	}

	public static string LowercaseFirstChar(string value)
	{
		if (value.Length > 0)
		{
			var firstChar = char.ToLowerInvariant(value[0]);
			value = firstChar + value.Substring(1);
		}

		return value;
	}

	public static string UppercaseFirstChar(string value)
	{
		if (value.Length > 0)
		{
			var firstChar = char.ToUpperInvariant(value[0]);
			value = firstChar + value.Substring(1);
		}

		return value;
	}

	/// <summary>
	/// Converts PascalCase or camelCase identifiers to lowercase with separators between words.
	/// E.g., "EntityId" -> "entity.id" (dot separator), "entity_id" (underscore separator)
	/// </summary>
	public static string ConvertToSeparatedLowercase(string pascalCaseName, char separator = '.')
	{
		if (string.IsNullOrEmpty(pascalCaseName))
			return pascalCaseName;

		System.Text.StringBuilder result = new();
		var previousWasUpper = false;
		var previousWasLower = false;

		for (var i = 0; i < pascalCaseName.Length; i++)
		{
			var current = pascalCaseName[i];
			var isUpper = char.IsUpper(current);
			var isLower = char.IsLower(current);

			if (i > 0 && isUpper)
			{
				// Add separator before uppercase if:
				// 1. Previous was lowercase (camelCase boundary: "entityId" -> "entity.id")
				// 2. Next is lowercase and previous was uppercase (acronym boundary: "HTTPSConnection" -> "https.connection")
				var nextIsLower = i + 1 < pascalCaseName.Length && char.IsLower(pascalCaseName[i + 1]);

				if (previousWasLower || (previousWasUpper && nextIsLower))
				{
					result.Append(separator);
				}
			}

			result.Append(char.ToLowerInvariant(current));

			previousWasUpper = isUpper;
			previousWasLower = isLower;
		}

		return result.ToString();
	}

	/// <summary>
	/// Generates an instrument prefix from an interface name by:
	/// 1. Stripping leading 'I' if present
	/// 2. Stripping trailing 'Logs' or 'Telemetry' if present
	/// 3. Converting to snake_case
	/// Example: "IWeatherServiceTelemetry" -> "weather_service"
	/// </summary>
	public static string GenerateInstrumentPrefixFromInterfaceName(string interfaceName)
	{
		if (string.IsNullOrWhiteSpace(interfaceName))
			return string.Empty;

		var name = interfaceName;

		// Strip leading 'I' if present
		if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
			name = name.Substring(1);

		// Strip trailing 'Telemetry' or 'Logs'
		if (name.EndsWith("Telemetry", StringComparison.Ordinal))
			name = name.Substring(0, name.Length - "Telemetry".Length);
		else if (name.EndsWith("Logs", StringComparison.Ordinal))
			name = name.Substring(0, name.Length - "Logs".Length);

		// If we stripped everything, return empty
		if (string.IsNullOrWhiteSpace(name))
			return string.Empty;

		// Convert to snake_case
		return ConvertToSeparatedLowercase(name, '_');
	}

	/// <summary>
	/// Checks if a method has any metrics-related attribute (Counter, AutoCounter, UpDownCounter, Histogram, etc.)
	/// </summary>
	public static bool HasMetricsAttribute(IMethodSymbol method, CancellationToken token)
	{
		return ContainsAttribute(method, TemplateLibrary.Metrics.CounterAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.AutoCounterAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.UpDownCounterAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.HistogramAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.ObservableCounterAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.ObservableGaugeAttribute, token)
			|| ContainsAttribute(method, TemplateLibrary.Metrics.ObservableUpDownCounterAttribute, token);
	}
}
