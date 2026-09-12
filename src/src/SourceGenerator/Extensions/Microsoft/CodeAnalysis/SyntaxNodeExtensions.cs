using System.ComponentModel;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis;

[EditorBrowsable(EditorBrowsableState.Never)]
static class SyntaxNodeExtensions
{
	extension(SyntaxNode syntax)
	{
		public string Flatten() => syntax.WithoutTrivia().ToString().Flatten();
	}
}
