using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

static partial class Utilities
{
	public static TargetGeneration IsValidGenerationTarget(
		IMethodSymbol method,
		GenerationType generationType,
		GenerationType requestedType
	)
	{
		var attributes = method
			.GetAttributes()
			.Where(m => m.AttributeClass != null)
			.Select(m => PurviewTypeFactory.Create(m.AttributeClass!))
			.ToArray();
		var activityCount = attributes.Count(static m =>
			Constants.Activities.ActivityAttribute == m
			|| Constants.Activities.EventAttribute == m
			|| Constants.Activities.ContextAttribute == m
		);
		var loggingCount = attributes.Count(static m =>
			Constants.Logging.LogAttribute == m
			|| Constants.Logging.TraceAttribute == m
			|| Constants.Logging.DebugAttribute == m
			|| Constants.Logging.InfoAttribute == m
			|| Constants.Logging.WarningAttribute == m
			|| Constants.Logging.ErrorAttribute == m
			|| Constants.Logging.CriticalAttribute == m
		);
		var metricsCount = attributes.Count(static m =>
			Constants.Metrics.CounterAttribute == m
			|| Constants.Metrics.AutoCounterAttribute == m
			|| Constants.Metrics.UpDownCounterAttribute == m
			|| Constants.Metrics.HistogramAttribute == m
			|| Constants.Metrics.ObservableCounterAttribute == m
			|| Constants.Metrics.ObservableGaugeAttribute == m
			|| Constants.Metrics.ObservableUpDownCounterAttribute == m
		);

		var count = activityCount + loggingCount + metricsCount;
		var inferenceNotSupportedWithMultiTargeting = false;
		var multiGenerationTargetsNotSupported = false;
		if (generationType != requestedType)
		{
			// This means it's multi-target generation so we need everything to be explicit.
			if (count == 0)
				inferenceNotSupportedWithMultiTargeting = true;
		}

		if (count > 1)
			multiGenerationTargetsNotSupported = true;

		var isValid =
			!multiGenerationTargetsNotSupported && !inferenceNotSupportedWithMultiTargeting;
		if (isValid)
		{
			if (
				generationType.HasFlag(GenerationType.Activities)
				&& requestedType == GenerationType.Activities
			)
			{
				isValid = loggingCount == 0 && metricsCount == 0;
			}

			if (
				generationType.HasFlag(GenerationType.Logging)
				&& requestedType == GenerationType.Logging
			)
			{
				isValid = activityCount == 0 && metricsCount == 0;
			}

			if (
				generationType.HasFlag(GenerationType.Metrics)
				&& requestedType == GenerationType.Metrics
			)
			{
				isValid = activityCount == 0 && loggingCount == 0;
			}
		}

		return new(
			IsValid: isValid,
			RaiseInferenceNotSupportedWithMultiTargeting: inferenceNotSupportedWithMultiTargeting,
			RaiseMultiGenerationTargetsNotSupported: multiGenerationTargetsNotSupported
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
			parentClasses.Insert(0, parentClass.Identifier.Text);

			parentClass = parentClass.Parent as ClassDeclarationSyntax;
		}

		return parentClasses.Count == 0 ? null : string.Join(".", parentClasses);
	}

	public static string? GetNamespace(TypeDeclarationSyntax typeSymbol)
	{
		// Determine the namespace the type is declared in, if any
		var potentialNamespaceParent = typeSymbol.Parent;
		while (
			potentialNamespaceParent != null
			&& potentialNamespaceParent is not NamespaceDeclarationSyntax
			&& potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax
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
		if (typeSymbol.TypeKind == TypeKind.Class || typeSymbol.TypeKind == TypeKind.Struct)
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
		ITypeSymbol? localTypeSymbol = typeSymbol;
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
		Regex.Replace(value, @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(2000));

	public static bool ContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo typeInfo,
		CancellationToken token
	) => TryContainsAttribute(symbol, typeInfo, token, out _);

	public static bool ContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo[] typeInfo,
		CancellationToken token
	) => TryContainsAttribute(symbol, typeInfo, token, out _, out _);

	public static bool TryContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo typeInfo,
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
			if (attributeType == typeInfo)
			{
				attributeData = attribute;
				return true;
			}
		}

		return false;
	}

	public static bool TryContainsAttribute(
		ISymbol symbol,
		PurviewTypeInfo[] typeInfo,
		CancellationToken token,
		out AttributeData? attributeData,
		out PurviewTypeInfo? matchingTemplate
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
			foreach (var template in typeInfo)
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

	public static string LowerCaseFirstChar(string value)
	{
		if (value.Length > 0)
		{
			var firstChar = char.ToLowerInvariant(value[0]);
			value = firstChar + value.Substring(1);
		}

		return value;
	}

	public static string UpperCaseFirstChar(string value)
	{
		if (value.Length > 0)
		{
			var firstChar = char.ToUpperInvariant(value[0]);
			value = firstChar + value.Substring(1);
		}

		return value;
	}

	public static string GetClassAttributesString(bool includeEditorBrowsable, int indent = 0)
	{
		var indentString = new string('\t', indent);
		return GetEnumAttributesString(includeEditorBrowsable, indent)
			+ $"\n{indentString}{Constants.System.ExcludeFromCodeCoverageConstant}";
	}

	public static string GetEnumAttributesString(bool includeEditorBrowsable, int indent = 0)
	{
		var indentString = new string('\t', indent);
		var result = string.Join(
			"\n",
			indentString + Constants.System.CompilerGeneratedConstant,
			indentString + Constants.System.GeneratedCode.Value
		);

		if (includeEditorBrowsable)
			result += $"\n{indentString}{Constants.System.EditorBrowsableConstant}";

		return result;
	}
}
