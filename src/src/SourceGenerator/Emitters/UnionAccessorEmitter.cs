using System.Globalization;
using Purview.Telemetry.SourceGenerator.Helpers;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Emitters;

/// <summary>
/// Emits the per-union accessors a generated class uses to read a union parameter's case value.
/// </summary>
/// <remarks>
/// One accessor is emitted per distinct union type in the class, so the read logic exists once no matter how
/// many methods and targets use the union, and the emitted method is small enough for the JIT to inline into
/// every call site. Nothing here reflects over the union: every case is a compile-time <c>TryGetValue</c>
/// overload or the union's own <c>Value</c> property, so the generated code stays trim- and AOT-safe.
/// </remarks>
static class UnionAccessorEmitter
{
	public static void EmitAccessors(CodeWriter writer, IEnumerable<UnionTargetRecord?> unions)
	{
		List<UnionTargetRecord> distinct = [];
		HashSet<string> emitted = [with(StringComparer.Ordinal)];

		foreach (var union in unions)
		{
			if (union is null || !emitted.Add(union.AccessorName))
				continue;

			distinct.Add(union);
		}

		if (distinct.Count == 0)
			return;

		foreach (var union in distinct)
			EmitAccessor(writer, union);
	}

	static void EmitAccessor(CodeWriter writer, UnionTargetRecord union)
	{
		var useNullable = writer.IsNullableContextEnabled is null or true;

		writer.NewLine();

		using (
			writer.MethodScope(
				new MethodDeclarationOptions(
					union.AccessorName,
					TypeLibrary.System.Object.AsTypeReference().Nullable(writer),
					TypeDeclarationAccessibility.Private
				)
				{
					IsStatic = true,
					Attributes = [EmitterHelpers.AggressiveInliningAttribute()],
					Parameters = [new ParameterDeclarationOptions("value", union.UnionType)],
				}
			)
		)
		{
			var value = "value";

			if (union.IsNullable)
			{
				// A nullable union is either a Nullable<T> around a struct union or a nullable reference to a
				// class union. Both mean "no value", which unwraps to null.
				if (union.IsValueType)
				{
					writer.IfBlock("!value." + UnionHelpers.HasValueMemberName, static body =>
						body.Return(PropertyLibrary.System.NullKeyword)
					);

					writer
						.NewLine()
						.Write(union.UnderlyingUnionType.RenderFullNameForNullable(useNullable))
						.Write(" unionValue = value.GetValueOrDefault();")
						.NewLine();

					value = "unionValue";
				}
				else
				{
					writer.IfBlock("value is null", static body => body.Return(PropertyLibrary.System.NullKeyword));

					writer.NewLine();
				}
			}

			if (!union.UsesNonBoxingAccess)
			{
				// A union declared with the `union` keyword stores object? and exposes the case value through
				// Value, which is already boxed. Null contents mean the union holds no value.
				writer.Return(value + "." + UnionHelpers.ValueMemberName);
				return;
			}

			writer.IfBlock(value + "." + UnionHelpers.HasValueMemberName, body =>
			{
				for (var i = 0; i < union.CaseTypes.Count; i++)
				{
					var caseVariableName = "caseValue" + i.ToString(CultureInfo.InvariantCulture);

					// The out argument is the case type itself rather than `var`: a union declares one
					// TryGetValue overload per case, so an inferred type would be ambiguous.
					body.IfBlock(
						value
							+ "."
							+ UnionHelpers.TryGetValueMemberName
							+ "(out "
							+ union.CaseTypes[i].RenderFullNameForNullable(useNullable)
							+ " "
							+ caseVariableName
							+ ")",
						caseBody => caseBody.Return(caseVariableName)
					);
				}
			});

			writer.NewLine().Return(PropertyLibrary.System.NullKeyword);
		}
	}
}
