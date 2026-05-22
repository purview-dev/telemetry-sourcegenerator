using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Purview.Telemetry.SourceGenerator.Refactorings;

sealed record ILoggerFieldInfo(
	string FieldName,
	FieldDeclarationSyntax? FieldDeclaration,
	PropertyDeclarationSyntax? PropertyDeclaration,
	ITypeSymbol TypeSymbol
);

sealed record LogCallInfo(
	InvocationExpressionSyntax Invocation,
	string ILoggerMethodName,
	string? ExplicitLogLevel,
	string? MessageTemplate,
	IReadOnlyList<LogParameterInfo> Parameters,
	ExpressionSyntax? ExceptionExpression,
	int? ExplicitEventId
);

sealed record LogParameterInfo(
	string Name,
	string TypeDisplayString,
	ExpressionSyntax ArgumentExpression
);

// ─────────────────────────────────────────────────────────────────────────────
// ActivitySource models
// ─────────────────────────────────────────────────────────────────────────────

sealed record ActivitySourceFieldInfo(
	string FieldName,
	FieldDeclarationSyntax? FieldDeclaration,
	PropertyDeclarationSyntax? PropertyDeclaration,
	ITypeSymbol TypeSymbol
);

sealed record ActivitySourceCallInfo(
	InvocationExpressionSyntax Invocation,
	string? ActivityName,
	string? ActivityKind,
	string ReceiverName
);

// ─────────────────────────────────────────────────────────────────────────────
// Metrics models
// ─────────────────────────────────────────────────────────────────────────────

enum MetricsInstrumentKind
{
	Counter,
	AutoCounter,
	UpDownCounter,
	Histogram,
}

sealed record MetricsFieldInfo(
	string FieldName,
	string MeasurementTypeDisplayString,
	MetricsInstrumentKind InstrumentKind,
	FieldDeclarationSyntax? FieldDeclaration,
	PropertyDeclarationSyntax? PropertyDeclaration,
	ITypeSymbol TypeSymbol
);

sealed record MetricsCallInfo(
	InvocationExpressionSyntax Invocation,
	string ReceiverFieldName,
	MetricsInstrumentKind InstrumentKind,
	string MeasurementTypeDisplayString,
	ExpressionSyntax? MeasurementArgument,
	bool IsAutoIncrement
);
