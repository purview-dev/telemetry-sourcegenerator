using System.ComponentModel;
using System.Text.RegularExpressions;

namespace System;

[EditorBrowsable(EditorBrowsableState.Never)]
static class StringExtensions
{
	static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(2000));

	extension(string value)
	{
		public string WithComma(bool andSpace = true) => value + ',' + (andSpace ? ' ' : null);

		public string Wrap(char c = '"') => c + value + c;

		public string Flatten() => WhitespaceRegex.Replace(value, " ");
	}
}
