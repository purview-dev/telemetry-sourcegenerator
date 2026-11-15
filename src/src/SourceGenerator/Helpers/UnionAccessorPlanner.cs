using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Decides which union accessors a target emits.
/// </summary>
/// <remarks>
/// The generated telemetry class is one <c>partial</c> type spread across the target files
/// (<c>*.Activity.g.cs</c>, <c>*.Logging.g.cs</c>, <c>*.Metric.g.cs</c>), so a private accessor declared by two of them
/// is <c>CS0111</c> for the consumer. The accessor is a shared member like the dependency-injection extension, the class
/// attributes and the constructor, and follows the same ownership rule: the canonical target
/// (Activities &gt; Logging &gt; Metrics) declares it, once per union type. The other targets call it, and a call from
/// another partial resolves because the class is one type.
/// <para>
/// A target cannot decide this from its own methods: the Logging target never sees a metric-only method's parameters,
/// and a parameter excluded from Activities is still unwrapped by the log or metric call site. So for a multi-target
/// interface the list is the interface's own, in declaration order, regardless of which targets consume each union.
/// </para>
/// </remarks>
static class UnionAccessorPlanner
{
	/// <summary>
	/// The accessors <paramref name="target"/> must emit, de-duplicated by accessor name and in declaration order.
	/// Empty when the target does not own them.
	/// </summary>
	/// <param name="interfaceSymbol">The interface being generated.</param>
	/// <param name="generationType">Every target the interface registers.</param>
	/// <param name="target">The target being built.</param>
	/// <param name="ownUnions">The unions the target's own method models carry.</param>
	/// <param name="token">The cancellation token.</param>
	public static EquatableArray<UnionTargetRecord> Plan(
		INamedTypeSymbol interfaceSymbol,
		GenerationType generationType,
		GenerationType target,
		IEnumerable<UnionTargetRecord?> ownUnions,
		CancellationToken token
	)
	{
		token.ThrowIfCancellationRequested();

		if (!SharedHelpers.ShouldEmitUnionAccessors(target, generationType))
			return default;

		// One registered target has one generated file, so its own method list is the whole picture. Keeping that
		// derivation unchanged is what makes a single-target interface's output identical to a per-target one.
		if (generationType == target)
			return Distinct(ownUnions);

		return Distinct(Collect(interfaceSymbol, token));
	}

	/// <summary>
	/// Every union-typed parameter the interface declares, in declaration order.
	/// </summary>
	/// <remarks>
	/// Parameter-level <c>[ExcludeTargets]</c> is deliberately ignored: it only stops a value being recorded by one
	/// target family, and the owning target still declares the accessor the excluded-from target's file calls.
	/// </remarks>
	static EquatableArray<UnionTargetRecord> Collect(INamedTypeSymbol interfaceSymbol, CancellationToken token)
	{
		List<UnionTargetRecord?> unions = [];

		foreach (var method in PipelineHelpers.GetAllInterfaceMethods(interfaceSymbol, token))
		{
			token.ThrowIfCancellationRequested();

			// An excluded method is not implemented by the generated class, so it has no call site for the accessor.
			if (Utilities.ContainsAttribute(method, TypeLibrary.Purview.Telemetry.ExcludeAttribute, token))
				continue;

			foreach (var parameter in method.Parameters)
				unions.Add(UnionHelpers.GetUnionTarget(parameter.Type, token));
		}

		return Distinct(unions);
	}

	static EquatableArray<UnionTargetRecord> Distinct(IEnumerable<UnionTargetRecord?> unions)
	{
		List<UnionTargetRecord> distinct = [];
		HashSet<string> seen = [with(StringComparer.Ordinal)];

		foreach (var union in unions)
		{
			if (union is null || !seen.Add(union.AccessorName))
				continue;

			distinct.Add(union);
		}

		return new([.. distinct]);
	}
}
