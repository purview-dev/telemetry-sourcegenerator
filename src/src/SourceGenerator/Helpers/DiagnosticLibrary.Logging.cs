using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

partial class DiagnosticLibrary
{
	// Start at 2000
	public static class Logging
	{
		public static readonly ReportableDiagnostic MultipleExceptionsDefined = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2000",
				title: "Too many exception parameters",
				messageFormat: "Only a single exceptions parameter is permitted.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic MaximumLogEntryParametersExceeded = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2001",
				title: "More than 6 parameters",
				messageFormat: $"The maximum number of parameters (excluding optional Exception) is {PropertyLibrary.Logging.MaxNonExceptionParameters}",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic InferringErrorLogLevel = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2002",
				title: "Inferring error log level",
				messageFormat: "Because an exception parameter was defined and no log level was defined the level was inferred to be Error. Consider explicitly defining the required level.",
				defaultSeverity: DiagnosticSeverity.Info,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic MSLoggingNotReferenced = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2003",
				title: "Could not find a reference to Microsoft.Extensions.Logging.ILogger, skipping log generation",
				messageFormat: "No reference was found for the ILogger type, no log generation is possible so no logging attributes will be added. Add a reference to the appropriate NuGet package, such as Microsoft.Extensions.Logging.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: true
		);

		public static readonly ReportableDiagnostic MixedOrdinalAndNamedProperties = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2004",
				title: "Cannot mix ordinal and named property placeholders",
				messageFormat: "The message template for log method '{0}' mixes ordinal and named property placeholders which is not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic OrdinalsExceedParameters = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2005",
				title: "Ordinal values exceed parameter count",
				messageFormat: "The maximum ordinal value for log method '{0}' exceeds the number of provided parameters.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic ExpandEnumerableAndLogPropertiesNotSupported =
			ReportableDiagnostic.Create(
				new DiagnosticDescriptor(
					id: "TSG2006",
					title: "Using LogPropertiesAttribute and ExpandEnumerableAttribute on the same parameter is not supported",
					messageFormat: "Expanding an array/ IEnumerable, and the expanding the complex type of the items in the array are not supported.",
					defaultSeverity: DiagnosticSeverity.Error,
					category: Categories.Logging.Usage,
					isEnabledByDefault: true
				),
				isBlocking: false
			);

		public static readonly ReportableDiagnostic ScopedMethodShouldNotHaveLevel = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2007",
				title: "A scoped log shouldn't have a LogLevel, this will be ignored.",
				messageFormat: "Scoped log entries do not support having a log level set.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic UnboundedIEnumerableMaxCount = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2008",
				title: "Unbounded enumeration possible",
				messageFormat: $"The limit on unbounded enumeration is higher than the recommended default ({PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic}). This may cause performance issues, make sure you understand the consequences and test thoroughly.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Performance,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic LogMustReturnVoidOrAsync = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2021",
				title: "Log method must return void or IDisposable",
				messageFormat: "Logging methods can only return void (non-scoped) or IDisposable (scoped). Other return types like string, int, bool, Activity, Task, or ValueTask are not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic ParamsParameterNotSupported = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2022",
				title: "params parameters are not supported",
				messageFormat: "A logging parameter cannot be declared 'params'. The generated method re-declares the parameter without the modifier, so the params call form would stop compiling; take an array and mark it [ExpandEnumerable] instead.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic ByRefParameterNotSupported = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2023",
				title: "ref, out and ref struct parameters are not supported",
				messageFormat: "A logging parameter cannot be 'ref', 'out', 'in', 'scoped', or a ref struct such as Span<T>. The generated state struct cannot capture it, and LoggerMessage.Define cannot carry it.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic TagProviderTypeNotFound = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2024",
				title: "TagProvider type could not be resolved",
				messageFormat: "The [TagProvider] on parameter '{0}' names the type '{1}', which cannot be resolved. The parameter is logged without the extra tags.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic TagProviderMethodNotFound = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2025",
				title: "TagProvider method could not be found",
				messageFormat: "The [TagProvider] on parameter '{0}' names the method '{1}' on '{2}', which does not exist. The parameter is logged without the extra tags.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic TagProviderMethodInvalidSignature = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2026",
				title: "TagProvider method has an invalid signature",
				messageFormat: "The [TagProvider] method '{0}' on '{1}' must be a visible static method taking one '{2}' parameter and returning IEnumerable<KeyValuePair<string, object?>> or IEnumerable<KeyValuePair<string, string>>. The parameter is logged without the extra tags.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);

		public static readonly ReportableDiagnostic TagProviderNotOnLoggingMethod = ReportableDiagnostic.Create(
			new DiagnosticDescriptor(
				id: "TSG2027",
				title: "TagProvider is only supported on logging parameters",
				messageFormat: "The [TagProvider] on parameter '{0}' is on a method that generates no log entry, so the provider would never run. Move it to a logging method, or remove it.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			),
			isBlocking: false
		);
	}
}
