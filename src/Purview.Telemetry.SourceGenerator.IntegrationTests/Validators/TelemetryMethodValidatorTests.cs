using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator.Records;
using Purview.Telemetry.SourceGenerator.Validators;

namespace Purview.Telemetry.SourceGenerator.IntegrationTests.Validators;

public class TelemetryMethodValidatorTests
{
	static CSharpCompilation CreateCompilation(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
			.Select(a => MetadataReference.CreateFromFile(a.Location))
			.ToList();

		return CSharpCompilation.Create(
			"TestAssembly",
			[syntaxTree],
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
		);
	}

	static IMethodSymbol GetMethodSymbol(Compilation compilation, string methodName)
	{
		var model = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
		var root = compilation.SyntaxTrees.First().GetRoot();
		var methodDeclaration = root.DescendantNodes()
			.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
			.First(m => m.Identifier.Text == methodName);

		return model.GetDeclaredSymbol(methodDeclaration)!;
	}

	[Test]
	public void ValidateReturnType_GivenVoidForLogging_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System;
namespace Test {
	public interface ITest {
		void LogMessage(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "LogMessage");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Logging).ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenIDisposableForScopedLogging_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System;
namespace Test {
	public interface ITest {
		IDisposable BeginScope(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "BeginScope");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging,
			isScoped: true
		);

		// Assert
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Logging).ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenVoidForScopedLogging_ReturnsInvalid()
	{
		// Arrange
		const string source = @"
using System;
namespace Test {
	public interface ITest {
		void InvalidScope(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "InvalidScope");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging,
			isScoped: true
		);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.Count().ShouldBe(1);
		result.Errors.First().Error.ShouldBe(
			ReturnTypeValidationError.ScopedLoggerMustReturnIDisposable
		);
	}

	[Test]
	public void ValidateReturnType_GivenActivityForActivities_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		Activity? StartActivity(string name);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "StartActivity");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Activities,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Activities).ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenVoidForActivities_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void RecordEvent(Activity? activity);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "RecordEvent");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Activities,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenStringForActivities_ReturnsInvalid()
	{
		// Arrange
		const string source = @"
namespace Test {
	public interface ITest {
		string InvalidActivity();
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "InvalidActivity");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Activities,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeFalse();
		result.Errors.First().Error.ShouldBe(
			ReturnTypeValidationError.InvalidActivityReturnType
		);
	}

	[Test]
	public void ValidateReturnType_GivenVoidForMetrics_ReturnsValid()
	{
		// Arrange
		const string source = @"
namespace Test {
	public interface ITest {
		void IncrementCounter(int value);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "IncrementCounter");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Metrics,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Metrics).ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenTaskForLogging_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System.Threading.Tasks;
namespace Test {
	public interface ITest {
		Task LogAsync(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "LogAsync");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenValueTaskForLogging_ReturnsValid()
	{
		// Arrange
		const string source = @"
using System.Threading.Tasks;
namespace Test {
	public interface ITest {
		ValueTask LogAsync(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "LogAsync");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
	}

	[Test]
	public void ValidateReturnType_GivenMultiTarget_ValidatesAllTargets()
	{
		// Arrange
		const string source = @"
using System;
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void MultiMethod(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "MultiMethod");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Logging | GenerationType.Activities | GenerationType.Metrics,
			isScoped: false
		);

		// Assert
		result.Validations.Count.ShouldBe(3);
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Logging).ShouldBeTrue();
		result.IsValidFor(GenerationType.Activities).ShouldBeTrue();
		result.IsValidFor(GenerationType.Metrics).ShouldBeTrue();
	}

	[Test]
	public void ShouldExcludeParameter_GivenActivityParameterForLogging_ReturnsExcluded()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(Activity? activity, string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var activityParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			activityParam,
			GenerationType.Logging,
			GenerationType.Logging | GenerationType.Activities
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Logging).ShouldBeTrue();
		result.GetExclusionFor(GenerationType.Logging)?.Reason.ShouldBe(
			ParameterExclusionReason.ActivityParameterNotAllowedInLogging
		);
	}

	[Test]
	public void ShouldExcludeParameter_GivenActivityParameterForMetrics_ReturnsExcluded()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(Activity? activity, int value);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var activityParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			activityParam,
			GenerationType.Metrics,
			GenerationType.Metrics | GenerationType.Activities
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Metrics).ShouldBeTrue();
		result.GetExclusionFor(GenerationType.Metrics)?.Reason.ShouldBe(
			ParameterExclusionReason.ActivityParameterNotAllowedInMetrics
		);
	}

	[Test]
	public void ShouldExcludeParameter_GivenActivityParameterForActivities_ReturnsNotExcluded()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(Activity? activity, string eventName);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var activityParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			activityParam,
			GenerationType.Activities,
			GenerationType.Activities
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Activities).ShouldBeFalse();
	}

	[Test]
	public void ShouldExcludeParameter_GivenActivityContextForLogging_ReturnsExcluded()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(ActivityContext context, string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var contextParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			contextParam,
			GenerationType.Logging,
			GenerationType.Logging | GenerationType.Activities
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Logging).ShouldBeTrue();
		result.GetExclusionFor(GenerationType.Logging)?.Reason.ShouldBe(
			ParameterExclusionReason.ActivityContextParameterNotAllowedInLogging
		);
	}

	[Test]
	public void ShouldExcludeParameter_GivenTagListForLogging_ReturnsExcluded()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(TagList tags, string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var tagsParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			tagsParam,
			GenerationType.Logging,
			GenerationType.Logging | GenerationType.Activities
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Logging).ShouldBeTrue();
		result.GetExclusionFor(GenerationType.Logging)?.Reason.ShouldBe(
			ParameterExclusionReason.TagListParameterNotAllowedInLogging
		);
	}

	[Test]
	public void ShouldExcludeParameter_GivenRegularStringParameter_ReturnsNotExcluded()
	{
		// Arrange
		const string source = @"
namespace Test {
	public interface ITest {
		void Method(string message);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var messageParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			messageParam,
			GenerationType.Logging,
			GenerationType.Logging
		);

		// Assert
		result.IsExcludedFrom(GenerationType.Logging).ShouldBeFalse();
	}

	[Test]
	public void ShouldExcludeParameter_GivenCounterValueForLogging_ReturnsExcluded()
	{
		// Arrange
		const string source = @"
namespace Test {
	public interface ITest {
		void IncrementCounter(int counterValue, string endpoint);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "IncrementCounter");
		var counterValueParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ShouldExcludeParameter(
			counterValueParam,
			GenerationType.Logging,
			GenerationType.Logging | GenerationType.Metrics
		);

		// Assert
		result.Exclusions.Any(e =>
			e.Target == GenerationType.Logging &&
			e.Reason == ParameterExclusionReason.MetricsMeasurementParameterNotAllowedInLogging
		).ShouldBeTrue();
	}

	[Test]
	public void ShouldExcludeParameter_GivenMultipleExclusions_ReturnsAllExclusions()
	{
		// Arrange
		const string source = @"
using System.Diagnostics;
namespace Test {
	public interface ITest {
		void Method(Activity? activity);
	}
}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "Method");
		var activityParam = method.Parameters[0];
		var validator = new TelemetryMethodValidator(compilation);

		// Act - Check against both Logging and Metrics
		var loggingResult = validator.ShouldExcludeParameter(
			activityParam,
			GenerationType.Logging,
			GenerationType.All
		);
		var metricsResult = validator.ShouldExcludeParameter(
			activityParam,
			GenerationType.Metrics,
			GenerationType.All
		);

		// Assert
		loggingResult.IsExcludedFrom(GenerationType.Logging).ShouldBeTrue();
		metricsResult.IsExcludedFrom(GenerationType.Metrics).ShouldBeTrue();
	}

	[Test]
	[MethodDataSource(nameof(GetObservableMetricsReturnTypes))]
	public void ValidateReturnType_GivenObservableMetricsTypes_ReturnsValid(string returnType)
	{
		// Arrange
		var source = $@"
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
namespace Test {{
	public interface ITest {{
		{returnType} ObservableMethod();
	}}
}}";
		var compilation = CreateCompilation(source);
		var method = GetMethodSymbol(compilation, "ObservableMethod");
		var validator = new TelemetryMethodValidator(compilation);

		// Act
		var result = validator.ValidateReturnType(
			method.ReturnType,
			GenerationType.Metrics,
			isScoped: false
		);

		// Assert
		result.IsValid.ShouldBeTrue();
		result.IsValidFor(GenerationType.Metrics).ShouldBeTrue();
	}

	public static IEnumerable<string> GetObservableMetricsReturnTypes
	{
		get
		{
			List<string> types =
			[
				"int",
				"long",
				"double",
				"float",
				"decimal",
				"Func<int>",
				"Func<Measurement<int>>",
				"IEnumerable<Measurement<long>>",
			];

			return types;
		}
	}
}
