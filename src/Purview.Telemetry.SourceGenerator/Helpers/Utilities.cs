using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class Utilities
{
	private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000));

	public static TargetGeneration IsValidGenerationTarget(
		IMethodSymbol method,
		GenerationType generationType,
		GenerationType requestedType
	)
	{
		// Optimized: Count in single pass instead of multiple enumerations
		var activityCount = 0;
		var loggingCount = 0;
		var metricsCount = 0;

		foreach (var attribute in method.GetAttributes())
		{
			if (attribute.AttributeClass == null)
				continue;

			var attributeType = PurviewTypeFactory.Create(attribute.AttributeClass);

			// Check activities
			if (Constants.Activities.ActivityAttribute == attributeType
				|| Constants.Activities.EventAttribute == attributeType
				|| Constants.Activities.ContextAttribute == attributeType)
			{
				activityCount++;
			}
			// Check logging
			else if (Constants.Logging.LogAttribute == attributeType
				|| Constants.Logging.TraceAttribute == attributeType
				|| Constants.Logging.DebugAttribute == attributeType
				|| Constants.Logging.InfoAttribute == attributeType
				|| Constants.Logging.WarningAttribute == attributeType
				|| Constants.Logging.ErrorAttribute == attributeType
				|| Constants.Logging.CriticalAttribute == attributeType)
			{
				loggingCount++;
			}
			// Check metrics
			else if (Constants.Metrics.CounterAttribute == attributeType
				|| Constants.Metrics.AutoCounterAttribute == attributeType
				|| Constants.Metrics.UpDownCounterAttribute == attributeType
				|| Constants.Metrics.HistogramAttribute == attributeType
				|| Constants.Metrics.ObservableCounterAttribute == attributeType
				|| Constants.Metrics.ObservableGaugeAttribute == attributeType
				|| Constants.Metrics.ObservableUpDownCounterAttribute == attributeType)
			{
				metricsCount++;
			}
		}

		var inferenceNotSupportedWithMultiTargeting = false;
		var multiGenerationTargetsNotSupported = false;

		// Check for intra-family conflicts (multiple attributes within same family)
		// This is always an error - can only have one activity/event/context, one log level, one instrument
		if (activityCount > 1 || loggingCount > 1 || metricsCount > 1)
			multiGenerationTargetsNotSupported = true;

		// Count how many families are present on the interface
		var interfaceTargetCount = 0;
		if (generationType.HasFlag(GenerationType.Activities))
			interfaceTargetCount++;
		if (generationType.HasFlag(GenerationType.Logging))
			interfaceTargetCount++;
		if (generationType.HasFlag(GenerationType.Metrics))
			interfaceTargetCount++;

		// Determine which target families this method has explicit attributes for
		var methodTargets = GenerationType.None;
		if (activityCount > 0)
			methodTargets |= GenerationType.Activities;
		if (loggingCount > 0)
			methodTargets |= GenerationType.Logging;
		if (metricsCount > 0)
			methodTargets |= GenerationType.Metrics;

		// Count how many target families this method targets
		var methodTargetFamilyCount = 0;
		if (methodTargets.HasFlag(GenerationType.Activities))
			methodTargetFamilyCount++;
		if (methodTargets.HasFlag(GenerationType.Logging))
			methodTargetFamilyCount++;
		if (methodTargets.HasFlag(GenerationType.Metrics))
			methodTargetFamilyCount++;

		// This method is multi-target if it has attributes from more than one family
		var isMultiTarget = methodTargetFamilyCount > 1;

		// If interface has multiple target families, methods need explicit attributes (no inference)
		if (interfaceTargetCount > 1)
		{
			// If no explicit attribute for any target, that's the inference error
			if (methodTargetFamilyCount == 0)
				inferenceNotSupportedWithMultiTargeting = true;
		}

		// Determine if this method is valid for the requested target type
		var isValid =
			!multiGenerationTargetsNotSupported && !inferenceNotSupportedWithMultiTargeting;
		if (isValid)
		{
			// Method is valid for this target if it has an explicit attribute for this target,
			// OR if it's single-target generation and can use inference
			if (interfaceTargetCount > 1)
			{
				// Multi-target interface: must have explicit attribute for this target
				isValid = requestedType switch
				{
					GenerationType.Activities => activityCount > 0,
					GenerationType.Logging => loggingCount > 0,
					GenerationType.Metrics => metricsCount > 0,
					_ => false,
				};
			}
			// Single-target interface: original inference logic applies
		}

		// Check for Activity parameter without Activity target
		string? activityParameterWithoutTarget = null;
		if (activityCount == 0)
		{
			// No Activity attribute, check if there are Activity parameters
			foreach (var param in method.Parameters)
			{
				var paramType = PurviewTypeFactory.Create(param.Type);
				if (Constants.Activities.SystemDiagnostics.Activity.Equals(paramType))
				{
					activityParameterWithoutTarget = param.Name;
					break;
				}
			}
		}

		return new(
			IsValid: isValid,
			RaiseInferenceNotSupportedWithMultiTargeting: inferenceNotSupportedWithMultiTargeting,
			RaiseMultiGenerationTargetsNotSupported: multiGenerationTargetsNotSupported,
			IsMultiTarget: isMultiTarget,
			MethodTargets: methodTargets,
			ActivityParameterWithoutTarget: activityParameterWithoutTarget
		);
	}

	public static string WithComma(this string value, bool andSpace = true) =>
		value + ',' + (andSpace ? ' ' : null);

	public static string Wrap(this string value, char c = '"') => c + value + c;

	public static string[] GetParentClasses(TypeDeclarationSyntax classDeclaration)
	{
		var parentClass = classDeclaration.Parent as ClassDeclarationSyntax;

		List<string> parentClassList = [];
		while (parentClass != null)
		{
			parentClassList.Add(parentClass.Identifier.Text);

			parentClass = parentClass.Parent as ClassDeclarationSyntax;
		}

		return [.. parentClassList];
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

	public static string? GetFullNamespace(
		TypeDeclarationSyntax type,
		bool includeTrailingSeparator
	)
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

	public static object? GetTypedConstantValue(TypedConstant arg) =>
		arg.Kind == TypedConstantKind.Array ? arg.Values : arg.Value;

	public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(
		this IncrementalValuesProvider<TSource> source
	) => source.Where(static m => m is not null);

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
		typeSymbol.SpecialType != SpecialType.System_String
		&& typeSymbol.TypeKind is TypeKind.Array;

	public static bool IsIEnumerable(this ITypeSymbol typeSymbol, Compilation compilation)
	{
		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;

		if (IsIEnumerable(typeSymbol))
			return true;

		// Get the `IEnumerable` symbol from the compilation
		var ienumerableSymbol = compilation.GetTypeByMetadataName(Constants.System.IEnumerable);

		// Check if the type implements `IEnumerable`
		return ienumerableSymbol != null
			&& typeSymbol.AllInterfaces.Any(i =>
				SymbolEqualityComparer.Default.Equals(i, ienumerableSymbol)
			);
	}

	static bool IsIEnumerable(ITypeSymbol typeSymbol)
	{
#pragma warning disable IDE0046 // Convert to conditional expression
		if (typeSymbol.SpecialType == SpecialType.System_String)
			return false;
#pragma warning restore IDE0046 // Convert to conditional expression

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
		var localTypeSymbol = typeSymbol;
		while (localTypeSymbol != null)
		{
			if (Constants.System.Exception.Equals(localTypeSymbol))
				return true;

			localTypeSymbol = localTypeSymbol.BaseType;
		}

		return false;
	}

	public static string Flatten(this SyntaxNode syntax) =>
		syntax.WithoutTrivia().ToString().Flatten();

	public static string Flatten(this string value) =>
		WhitespaceRegex.Replace(value, " ");

	public static bool ContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo type,
		CancellationToken token
	) => TryContainsAttribute(symbol, type, token, out _);

	public static bool ContainsAttribute(
		ISymbol symbol,
		TemplateInfo templateInfo,
		CancellationToken token
	) => TryContainsAttribute(symbol, templateInfo, token, out _);

	public static bool ContainsAttribute(
		ISymbol symbol,
		TemplateInfo[] templateInfo,
		CancellationToken token
	) => TryContainsAttribute(symbol, templateInfo, token, out _, out _);

	public static bool TryContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo type,
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

			var attributeType = PurviewTypeFactory.Create(attribute.AttributeClass);
			if (attributeType == type)
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

			var attributeType = PurviewTypeFactory.Create(attribute.AttributeClass);
			if (attributeType == templateInfo)
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

			var attributeType = PurviewTypeFactory.Create(attribute.AttributeClass);
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
		bool previousWasUpper = false;
		bool previousWasLower = false;

		for (int i = 0; i < pascalCaseName.Length; i++)
		{
			char current = pascalCaseName[i];
			bool isUpper = char.IsUpper(current);
			bool isLower = char.IsLower(current);

			if (i > 0 && isUpper)
			{
				// Add separator before uppercase if:
				// 1. Previous was lowercase (camelCase boundary: "entityId" -> "entity.id")
				// 2. Next is lowercase and previous was uppercase (acronym boundary: "HTTPSConnection" -> "https.connection")
				bool nextIsLower = i + 1 < pascalCaseName.Length && char.IsLower(pascalCaseName[i + 1]);

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
	/// Detects if a lowercase string appears to be a compound word without separators.
	/// E.g., "entityid", "requestcount", "httpconnection" (likely compounds)
	/// Returns true if the string is likely multiple words smashed together.
	/// </summary>
	public static bool IsLikelyCompoundWord(string lowercaseName)
	{
		if (string.IsNullOrEmpty(lowercaseName) || lowercaseName.Length < 6)
			return false;

		// If it contains separators already, it's not a compound
		if (lowercaseName.Contains('.') || lowercaseName.Contains('_') || lowercaseName.Contains('-'))
			return false;

		// Common compound patterns (heuristic)
		string[] commonSuffixes = ["id", "key", "name", "type", "count", "value", "time", "date", "code", "number"];
		string[] commonPrefixes = ["get", "set", "is", "has", "can", "should", "will"];

		foreach (var suffix in commonSuffixes)
		{
			if (lowercaseName.EndsWith(suffix, StringComparison.Ordinal) && lowercaseName.Length > suffix.Length + 2)
				return true;
		}

		foreach (var prefix in commonPrefixes)
		{
			if (lowercaseName.StartsWith(prefix, StringComparison.Ordinal) && lowercaseName.Length > prefix.Length + 2)
				return true;
		}

		return false;
	}

	/// <summary>
	/// Checks if a name is a generic or reserved term that provides little semantic value.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
	public static bool IsGenericOrReservedName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return false;

		string[] genericTerms =
		[
			"activity", "event", "error", "exception", "start", "stop", "begin", "end",
			"task", "action", "func", "method", "operation", "process", "handler"
		];

		string lowerName = name.ToLowerInvariant();
		return genericTerms.Contains(lowerName);
	}

	/// <summary>
	/// Checks if a method has any metrics-related attribute (Counter, AutoCounter, UpDownCounter, Histogram, etc.)
	/// </summary>
	public static bool HasMetricsAttribute(IMethodSymbol method, CancellationToken token)
	{
		return ContainsAttribute(method, Constants.Metrics.CounterAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.AutoCounterAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.UpDownCounterAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.HistogramAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.ObservableCounterAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.ObservableGaugeAttribute, token)
			|| ContainsAttribute(method, Constants.Metrics.ObservableUpDownCounterAttribute, token);
	}
}
