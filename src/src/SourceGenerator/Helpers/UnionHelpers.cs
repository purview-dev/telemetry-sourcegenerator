using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Recognises C# 15 union types (a type carrying <c>[System.Runtime.CompilerServices.Union]</c>) used as
/// telemetry parameters, and describes how the generated code should read a case value out of one.
/// </summary>
/// <remarks>
/// Detection is symbol based, never target-framework based: a union is a plain type declaration, so the
/// compilation resolves it the same way on net48 as on net11.0. What differs is only whether it declares
/// the non-boxing access pattern, which decides how the generated accessor reads it.
/// </remarks>
static class UnionHelpers
{
	public const string UnionAttributeName = "UnionAttribute";
	public const string CompilerServicesNamespace = "System.Runtime.CompilerServices";
	public const string HasValueMemberName = "HasValue";
	public const string TryGetValueMemberName = "TryGetValue";
	public const string ValueMemberName = "Value";
	public const string UnionMembersInterfaceName = "IUnionMembers";
	public const string UnionMembersCreateMethodName = "Create";

	/// <summary>
	/// Builds the <see cref="UnionTargetRecord"/> for a telemetry parameter, or <see langword="null"/> when
	/// the parameter's type is not a union.
	/// </summary>
	public static UnionTargetRecord? GetUnionTarget(ITypeSymbol parameterType, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();

		var isNullable = false;
		var unionType = parameterType;

		if (
			parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
			&& parameterType is INamedTypeSymbol { TypeArguments.Length: 1 } nullable
		)
		{
			isNullable = true;
			unionType = nullable.TypeArguments[0];
		}
		else if (parameterType.NullableAnnotation == NullableAnnotation.Annotated)
		{
			isNullable = true;
		}

		if (!IsUnion(unionType))
			return null;

		if (unionType is not INamedTypeSymbol namedUnionType)
			return null;

		var caseTypes = GetCaseTypes(namedUnionType);
		if (caseTypes.Count == 0)
			return null;

		return new(
			UnionType: TypeReference.Create(parameterType),
			UnderlyingUnionType: TypeReference.Create(unionType),
			CaseTypes: new([.. caseTypes.Select(static caseType => TypeReference.Create(caseType))]),
			IsValueType: unionType.IsValueType,
			IsNullable: isNullable,
			UsesNonBoxingAccess: UsesNonBoxingAccess(unionType, caseTypes.Count)
		);
	}

	/// <summary>
	/// Whether the type is a union: it carries <c>[System.Runtime.CompilerServices.Union]</c>.
	/// </summary>
	public static bool IsUnion(ITypeSymbol type)
	{
		foreach (var attribute in type.GetAttributes())
		{
			var attributeClass = attribute.AttributeClass;
			if (attributeClass is null)
				continue;

			if (
				attributeClass.Name == UnionAttributeName
				&& attributeClass.ContainingNamespace?.ToDisplayString() == CompilerServicesNamespace
			)
				return true;
		}

		return false;
	}

	/// <summary>
	/// The case types a union can hold: the single-parameter <c>Create</c> members of a nested
	/// <c>IUnionMembers</c> provider when there is one, otherwise the union's own single-parameter
	/// constructors, which is what the compiler generates for a <c>union</c> declaration.
	/// </summary>
	static List<ITypeSymbol> GetCaseTypes(INamedTypeSymbol unionType)
	{
		List<ITypeSymbol> caseTypes = [];

		var provider = unionType.GetTypeMembers(UnionMembersInterfaceName).FirstOrDefault();
		if (provider is not null)
		{
			foreach (
				var method in provider
					.GetMembers(UnionMembersCreateMethodName)
					.OfType<IMethodSymbol>()
					.Where(static m => m.IsStatic && m.Parameters.Length == 1)
			)
				caseTypes.Add(method.Parameters[0].Type);
		}

		if (caseTypes.Count == 0)
		{
			foreach (
				var constructor in unionType.InstanceConstructors.Where(static c =>
					c.DeclaredAccessibility == Accessibility.Public && !c.IsStatic && c.Parameters.Length == 1
				)
			)
				caseTypes.Add(constructor.Parameters[0].Type);
		}

		return caseTypes;
	}

	/// <summary>
	/// Whether the union declares the non-boxing access pattern: a <c>bool HasValue</c> property plus a
	/// <c>bool TryGetValue(out T)</c> overload for every case type. When it does, the generated accessor
	/// reads each case through <c>TryGetValue</c> and the compiler never boxes the union struct.
	/// </summary>
	static bool UsesNonBoxingAccess(ITypeSymbol unionType, int caseTypeCount)
	{
		// A union member provider declares the pattern on the nested interface the union implements.
		var provider = unionType.GetTypeMembers(UnionMembersInterfaceName).FirstOrDefault();
		var searchTargets = provider is null ? [unionType] : new ITypeSymbol[] { unionType, provider };

		var hasHasValue = false;
		var tryGetValueOverloads = 0;

		foreach (var target in searchTargets)
		{
			foreach (var member in target.GetMembers())
			{
				if (
					member is IPropertySymbol { IsStatic: false } property
					&& property.Name == HasValueMemberName
					&& property.Type.SpecialType == SpecialType.System_Boolean
				)
					hasHasValue = true;
				else if (
					member is IMethodSymbol { IsStatic: false, Parameters.Length: 1 } method
					&& method.Name == TryGetValueMemberName
					&& method.ReturnType.SpecialType == SpecialType.System_Boolean
					&& method.Parameters[0].RefKind == RefKind.Out
				)
					tryGetValueOverloads++;
			}
		}

		return hasHasValue && tryGetValueOverloads >= caseTypeCount;
	}

	/// <summary>
	/// The type a log state struct (or a <c>LoggerMessage.Define</c> generic argument) carries for a
	/// parameter. A union is logged as its case value, so the state holds <c>object?</c>; every other
	/// parameter keeps its declared type.
	/// </summary>
	public static string RenderLogStateType(LogParameterTarget parameter, bool useNullable) =>
		parameter.RequiresObjectState
			? (useNullable ? "object?" : "object")
			: parameter.ParameterType.RenderFullNameForNullable(useNullable);
}
