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
	ExpressionSyntax? ExceptionExpression
);

sealed record LogParameterInfo(
	string Name,
	string TypeDisplayString,
	ExpressionSyntax ArgumentExpression
);
