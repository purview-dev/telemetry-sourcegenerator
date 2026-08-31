using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Records;

namespace Purview.Telemetry.SourceGenerator.Validators;

/// <summary>
/// Validates return types and parameters for telemetry methods across different generation targets.
/// Handles complex scenarios like multi-target generation and scoped loggers.
/// </summary>
sealed class TelemetryMethodValidator(Compilation compilation)
{
	/// <summary>
	/// Validates the return type for a given method and target generation type.
	/// </summary>
	public ReturnTypeValidationResult ValidateReturnType(
		ITypeSymbol returnType,
		GenerationType targetType,
		bool isScoped = false
	)
	{
		List<ReturnTypeValidation> results = [];
		if (targetType.HasFlag(GenerationType.Logging))
			results.Add(ValidateLoggingReturnType(returnType, isScoped));

		if (targetType.HasFlag(GenerationType.Activities))
			results.Add(ValidateActivityReturnType(returnType));

		if (targetType.HasFlag(GenerationType.Metrics))
			results.Add(ValidateMetricsReturnType(returnType));

		return new(results);
	}

	/// <summary>
	/// Determines if a parameter should be excluded from a specific target generation.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static")]
	public ParameterExclusionResult ShouldExcludeParameter(
		IParameterSymbol parameter,
		GenerationType currentTarget,
		GenerationType allTargets
	)
	{
		var parameterType = parameter.Type;
		List<ParameterExclusion> exclusions = [];
		// Activity parameters should be excluded from Logging and Metrics
		if (Constants.Activities.SystemDiagnostics.Activity.Equals(parameterType))
		{
			if (currentTarget == GenerationType.Logging)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityParameterNotAllowedInLogging,
						$"Parameter '{parameter.Name}' of type Activity is automatically excluded from logging generation."
					)
				);
			}

			if (currentTarget == GenerationType.Metrics)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityParameterNotAllowedInMetrics,
						$"Parameter '{parameter.Name}' of type Activity is automatically excluded from metrics generation."
					)
				);
			}
		}

		// ActivityContext parameters should be excluded from Logging and Metrics
		if (Constants.Activities.SystemDiagnostics.ActivityContext.Equals(parameterType))
		{
			if (currentTarget == GenerationType.Logging)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityContextParameterNotAllowedInLogging,
						$"Parameter '{parameter.Name}' of type ActivityContext is automatically excluded from logging generation."
					)
				);
			}

			if (currentTarget == GenerationType.Metrics)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityContextParameterNotAllowedInMetrics,
						$"Parameter '{parameter.Name}' of type ActivityContext is automatically excluded from metrics generation."
					)
				);
			}
		}

		// ActivityLink parameters should be excluded from Logging and Metrics
		if (IsActivityLinkType(parameterType))
		{
			if (currentTarget == GenerationType.Logging)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityLinkParameterNotAllowedInLogging,
						$"Parameter '{parameter.Name}' of type ActivityLink is automatically excluded from logging generation."
					)
				);
			}

			if (currentTarget == GenerationType.Metrics)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.ActivityLinkParameterNotAllowedInMetrics,
						$"Parameter '{parameter.Name}' of type ActivityLink is automatically excluded from metrics generation."
					)
				);
			}
		}

		// TagList parameters should be excluded from Logging
		if (Constants.System.TagList.Equals(parameterType))
		{
			if (currentTarget == GenerationType.Logging)
			{
				exclusions.Add(
					new ParameterExclusion(
						currentTarget,
						ParameterExclusionReason.TagListParameterNotAllowedInLogging,
						$"Parameter '{parameter.Name}' of type TagList is automatically excluded from logging generation."
					)
				);
			}
		}

		// Check for measurement value parameters in metrics
		// If we're generating for Logging and there's also Metrics target,
		// exclude parameters that look like metric measurement values
		if (currentTarget == GenerationType.Logging && allTargets.HasFlag(GenerationType.Metrics))
		{
			if (IsMetricsMeasurementParameter(parameter, allTargets))
			{
				exclusions.Add(
					new ParameterExclusion(
						GenerationType.Logging,
						ParameterExclusionReason.MetricsMeasurementParameterNotAllowedInLogging,
						$"Parameter '{parameter.Name}' is a metrics measurement value and is automatically excluded from logging generation."
					)
				);
			}
		}

		return new(exclusions);
	}

	ReturnTypeValidation ValidateLoggingReturnType(ITypeSymbol returnType, bool isScoped)
	{
		// Scoped logger must return IDisposable
		if (isScoped)
		{
			return Constants.System.IDisposable.Equals(returnType)
				? ReturnTypeValidation.Valid(GenerationType.Logging, "Scoped logger correctly returns IDisposable.")
				: ReturnTypeValidation.Invalid(
					GenerationType.Logging,
					ReturnTypeValidationError.ScopedLoggerMustReturnIDisposable,
					$"Scoped logger methods must return IDisposable, but found '{returnType.ToDisplayString()}'."
				);
		}

		// Non-scoped logger should return void or Task/ValueTask
		if (returnType.SpecialType == SpecialType.System_Void)
		{
			return ReturnTypeValidation.Valid(GenerationType.Logging, "Logger method correctly returns void.");
		}

		// Check for Task or ValueTask
		return IsTaskType(returnType) || IsValueTaskType(returnType)
			? ReturnTypeValidation.Valid(
				GenerationType.Logging,
				$"Logger method correctly returns async type '{returnType.ToDisplayString()}'."
			)
			: ReturnTypeValidation.Invalid(
				GenerationType.Logging,
				ReturnTypeValidationError.InvalidLoggingReturnType,
				$"Logger methods must return void, Task, ValueTask, or IDisposable (for scoped), but found '{returnType.ToDisplayString()}'."
			);
	}

	static ReturnTypeValidation ValidateActivityReturnType(ITypeSymbol returnType)
	{
		// Activity methods can return Activity or void
		return Constants.Activities.SystemDiagnostics.Activity.Equals(returnType)
				? ReturnTypeValidation.Valid(GenerationType.Activities, "Activity method correctly returns Activity.")
			: returnType.SpecialType == SpecialType.System_Void
				? ReturnTypeValidation.Valid(GenerationType.Activities, "Activity method correctly returns void.")
			: ReturnTypeValidation.Invalid(
				GenerationType.Activities,
				ReturnTypeValidationError.InvalidActivityReturnType,
				$"Activity methods must return Activity or void, but found '{returnType.ToDisplayString()}'."
			);
	}

	static ReturnTypeValidation ValidateMetricsReturnType(ITypeSymbol returnType)
	{
		// Metrics methods should return void or observable types
		if (returnType.SpecialType == SpecialType.System_Void)
		{
			return ReturnTypeValidation.Valid(GenerationType.Metrics, "Metrics method correctly returns void.");
		}

		// Observable metrics can return various types
		return IsObservableMetricsReturnType(returnType)
			? ReturnTypeValidation.Valid(
				GenerationType.Metrics,
				$"Observable metrics method correctly returns '{returnType.ToDisplayString()}'."
			)
			: ReturnTypeValidation.Invalid(
				GenerationType.Metrics,
				ReturnTypeValidationError.InvalidMetricsReturnType,
				$"Metrics methods must return void or observable types, but found '{returnType.ToDisplayString()}'."
			);
	}

	static bool IsActivityLinkType(ITypeSymbol type)
	{
		// Check if it's ActivityLink or IEnumerable<ActivityLink>
		if (Constants.Activities.SystemDiagnostics.ActivityLink.Equals(type))
			return true;

		if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var typeArg = namedType.TypeArguments.FirstOrDefault();
			if (typeArg != null && Constants.Activities.SystemDiagnostics.ActivityLink.Equals(typeArg))
			{
				return true;
			}
		}

		return false;
	}

	bool IsTaskType(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol namedType)
			return false;

		var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
		if (taskType != null && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, taskType))
		{
			return true;
		}

		var genericTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
		return genericTaskType != null
			&& SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, genericTaskType);
	}

	bool IsValueTaskType(ITypeSymbol type)
	{
		if (type is not INamedTypeSymbol namedType)
			return false;

		var valueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
		if (valueTaskType != null && SymbolEqualityComparer.Default.Equals(namedType, valueTaskType))
		{
			return true;
		}

		var genericValueTaskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
		return genericValueTaskType != null
			&& SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, genericValueTaskType);
	}

	static bool IsObservableMetricsReturnType(ITypeSymbol type)
	{
		// Observable metrics can return numeric types or Func<T> where T is numeric
		if (IsNumericType(type))
			return true;

		// Check for Func<T> where T is numeric or Measurement<T>
		if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var funcFullName = $"{namedType.ContainingNamespace?.ToDisplayString()}.{namedType.Name}";
			if (funcFullName == "System.Func")
			{
				var returnType = namedType.TypeArguments.LastOrDefault();
				if (returnType != null && (IsNumericType(returnType) || IsMeasurementType(returnType)))
				{
					return true;
				}
			}

			// Check if the type itself is IEnumerable<Measurement<T>>
			if (
				namedType.Name == "IEnumerable"
				&& namedType.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic"
				&& namedType.TypeArguments.Length == 1
				&& IsMeasurementType(namedType.TypeArguments[0])
			)
			{
				return true;
			}

			// Check for types implementing IEnumerable<Measurement<T>>
			if (
				namedType.AllInterfaces.Any(i =>
					i.Name == "IEnumerable"
					&& i.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic"
					&& i.TypeArguments.Length == 1
					&& IsMeasurementType(i.TypeArguments[0])
				)
			)
			{
				return true;
			}
		}

		return false;
	}

	static bool IsMeasurementType(ITypeSymbol type)
	{
		return type is INamedTypeSymbol namedType
			&& namedType.Name == "Measurement"
			&& namedType.ContainingNamespace?.ToDisplayString() == "System.Diagnostics.Metrics"
			&& namedType.TypeArguments.Length == 1
			&& IsNumericType(namedType.TypeArguments[0]);
	}

	static bool IsNumericType(ITypeSymbol type)
	{
		return type.SpecialType
			is SpecialType.System_Byte
				or SpecialType.System_SByte
				or SpecialType.System_Int16
				or SpecialType.System_UInt16
				or SpecialType.System_Int32
				or SpecialType.System_UInt32
				or SpecialType.System_Int64
				or SpecialType.System_UInt64
				or SpecialType.System_Single
				or SpecialType.System_Double
				or SpecialType.System_Decimal;
	}

	static bool IsMetricsMeasurementParameter(IParameterSymbol parameter, GenerationType allTargets)
	{
		// Check if the parameter name suggests it's a measurement value
#pragma warning disable CA1308 // Normalize strings to uppercase
		var lowerName = parameter.Name.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
		if (lowerName is "countervalue" or "value" or "measurement" or "amount")
			return true;

		// If it's a numeric type and we're in a metrics context
		if (IsNumericType(parameter.Type) && allTargets.HasFlag(GenerationType.Metrics))
		{
			// Check if it's the first parameter (common pattern for metrics)
			if (
				parameter.ContainingSymbol is IMethodSymbol method
				&& method.Parameters.Length > 0
				&& SymbolEqualityComparer.Default.Equals(method.Parameters[0], parameter)
			)
			{
				return true;
			}
		}

		return false;
	}
}

/// <summary>
/// Result of return type validation containing results for all applicable targets.
/// </summary>
sealed record ReturnTypeValidationResult(IReadOnlyList<ReturnTypeValidation> Validations)
{
	public bool IsValid => Validations.All(v => v.IsValid);

	public bool IsValidFor(GenerationType target) =>
		Validations.FirstOrDefault(v => v.Target == target)?.IsValid ?? false;

	public IEnumerable<ReturnTypeValidation> Errors => Validations.Where(v => !v.IsValid);
}

/// <summary>
/// Validation result for a specific target's return type.
/// </summary>
sealed record ReturnTypeValidation(
	GenerationType Target,
	bool IsValid,
	ReturnTypeValidationError? Error,
	string Message
)
{
	public static ReturnTypeValidation Valid(GenerationType target, string message) => new(target, true, null, message);

	public static ReturnTypeValidation Invalid(
		GenerationType target,
		ReturnTypeValidationError error,
		string message
	) => new(target, false, error, message);
}

/// <summary>
/// Specific error types for return type validation.
/// </summary>
enum ReturnTypeValidationError
{
	ScopedLoggerMustReturnIDisposable,
	InvalidLoggingReturnType,
	InvalidActivityReturnType,
	InvalidMetricsReturnType,
}

/// <summary>
/// Result indicating if and why a parameter should be excluded from target generation.
/// </summary>
sealed record ParameterExclusionResult(IReadOnlyList<ParameterExclusion> Exclusions)
{
	public bool IsExcludedFrom(GenerationType target) => Exclusions.Any(e => e.Target == target);

	public ParameterExclusion? GetExclusionFor(GenerationType target) =>
		Exclusions.FirstOrDefault(e => e.Target == target);

	public bool IsIncludedIn(GenerationType target) => !IsExcludedFrom(target);
}

/// <summary>
/// Specific exclusion for a parameter from a target.
/// </summary>
sealed record ParameterExclusion(GenerationType Target, ParameterExclusionReason Reason, string Message);

/// <summary>
/// Reasons why a parameter might be excluded from generation.
/// </summary>
enum ParameterExclusionReason
{
	ActivityParameterNotAllowedInLogging,
	ActivityParameterNotAllowedInMetrics,
	ActivityContextParameterNotAllowedInLogging,
	ActivityContextParameterNotAllowedInMetrics,
	ActivityLinkParameterNotAllowedInLogging,
	ActivityLinkParameterNotAllowedInMetrics,
	TagListParameterNotAllowedInLogging,
	MetricsMeasurementParameterNotAllowedInLogging,
}
