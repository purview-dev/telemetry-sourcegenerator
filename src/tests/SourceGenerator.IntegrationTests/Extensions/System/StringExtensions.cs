using System.ComponentModel;

namespace System;

[EditorBrowsable(EditorBrowsableState.Never)]
static class StringExtensions
{
	extension(string value)
	{
		/// <summary>Replaces all occurrences using ordinal semantics (net48 lacks the StringComparison overload).</summary>
		public string ReplaceOrdinal(string oldValue, string newValue) =>
#if NET48
			value.Replace(oldValue, newValue);
#else
			value.Replace(oldValue, newValue, StringComparison.Ordinal);
#endif
	}
}
