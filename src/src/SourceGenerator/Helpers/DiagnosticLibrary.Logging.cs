using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator;

partial class DiagnosticLibrary
{
	// Start at 2000
	public static class Logging
	{
		public static readonly DiagnosticInfo MultipleExceptionsDefined = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2000",
				title: "Too many exception parameters",
				messageFormat: "Only a single exceptions parameter is permitted.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MaximumLogEntryParametersExceeded = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2001",
				title: "More than 6 parameters",
				messageFormat: $"The maximum number of parameters (excluding optional Exception) is {PropertyLibrary.Logging.MaxNonExceptionParameters}",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo InferringErrorLogLevel = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2002",
				title: "Inferring error log level",
				messageFormat: "Because an exception parameter was defined and no log level was defined the level was inferred to be Error. Consider explicitly defining the required level.",
				defaultSeverity: DiagnosticSeverity.Info,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MSLoggingNotReferenced = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2003",
				title: "Could not find a reference to Microsoft.Extensions.Logging.ILogger, skipping log generation",
				messageFormat: "No reference was found for the ILogger type, no log generation is possible so no logging attributes will be added. Add a reference to the appropriate NuGet package, such as Microsoft.Extensions.Logging.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo MixedOrdinalAndNamedProperties = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2004",
				title: "Cannot mix ordinal and named property placeholders",
				messageFormat: "The message template for log method '{0}' mixes ordinal and named property placeholders which is not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo OrdinalsExceedParameters = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2005",
				title: "Ordinal values exceed parameter count",
				messageFormat: "The maximum ordinal value for log method '{0}' exceeds the number of provided parameters.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ExpandEnumerableAndLogPropertiesNotSupported = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2006",
				title: "Using LogPropertiesAttribute and ExpandEnumerableAttribute on the same parameter is not supported",
				messageFormat: "Expanding an array/ IEnumerable, and the expanding the complex type of the items in the array are not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo ScopedMethodShouldNotHaveLevel = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2007",
				title: "A scoped log shouldn't have a LogLevel, this will be ignored.",
				messageFormat: "Scoped log entries do not support having a log level set.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo UnboundedIEnumerableMaxCount = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2008",
				title: "Unbounded enumeration possible",
				messageFormat: $"The limit on unbounded enumeration is higher than the recommended default ({PropertyLibrary.Logging.UnboundedIEnumerableMaxCountBeforeDiagnostic}). This may cause performance issues, make sure you understand the consequences and test thoroughly.",
				defaultSeverity: DiagnosticSeverity.Warning,
				category: Categories.Logging.Performance,
				isEnabledByDefault: true
			)
		);

		public static readonly DiagnosticInfo LogMustReturnVoidOrAsync = DiagnosticInfo.Create(
			new DiagnosticDescriptor(
				id: "TSG2021",
				title: "Log method must return void or IDisposable",
				messageFormat: "Logging methods can only return void (non-scoped) or IDisposable (scoped). Other return types like string, int, bool, Activity, Task, or ValueTask are not supported.",
				defaultSeverity: DiagnosticSeverity.Error,
				category: Categories.Logging.Usage,
				isEnabledByDefault: true
			)
		);
	}
}
