using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry.SourceGenerator.Helpers;

/// <summary>
/// Ultra-high-performance code writer optimized for zero allocations and minimal overhead.
/// Uses direct char[] buffer manipulation with ArrayPool for maximum throughput.
/// </summary>
public sealed class CodeWriter : IDisposable
{
	static readonly string[] IndentCache = CreateIndentCache();
	static readonly string[] CommonStrings = CreateCommonStringCache();
	static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;

	char[] _buffer;
	int _position;
	bool _disposed;

	public CodeWriter(int initialCapacity = 8192)
	{
		_buffer = CharPool.Rent(initialCapacity);
		_position = 0;
		_disposed = false;
	}

	static string[] CreateIndentCache()
	{
		var arr = new string[33]; // up to 32 levels should be plenty
		arr[0] = string.Empty;
		for (var i = 1; i < arr.Length; i++)
			arr[i] = new string('\t', i);
		return arr;
	}

	static string[] CreateCommonStringCache()
	{
		return new[]
		{
			"",
			" ",
			"  ",
			"   ", // 0-3: common spaces
			"return",
			"if",
			"else",
			"using", // 4-7: keywords
			"namespace",
			"class",
			"public",
			"private", // 8-11: more keywords
			"void",
			"string",
			"int",
			"bool", // 12-15: common types
			"{0}",
			"{1}",
			"{2}",
			"{3}", // 16-19: common format placeholders
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Indent(int level)
	{
		if (level > 0 && level < IndentCache.Length)
			WriteString(IndentCache[level]);
		else if (level >= IndentCache.Length)
			WriteString(IndentCache[IndentCache.Length - 1]);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Write(string value)
	{
		if (value != null)
			WriteStringOptimized(value);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Write(char value)
	{
		EnsureCapacity(1);
		_buffer[_position++] = value;
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Write(int value)
	{
		// Fast path for common small integers
		if (value >= 0 && value <= 9999)
		{
			WriteIntFast(value);
		}
		else
		{
			WriteString(value.ToString());
		}
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteLine()
	{
		Write('\n');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteLine(string value)
	{
		if (value != null)
			WriteString(value);
		Write('\n');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteLine(char value)
	{
		Write(value);
		Write('\n');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Space()
	{
		Write(' ');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Dot()
	{
		Write('.');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Comma()
	{
		Write(',');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Semicolon()
	{
		Write(';');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter OpenParen()
	{
		Write('(');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter CloseParen()
	{
		Write(')');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter OpenBrace()
	{
		Write('{');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter CloseBrace()
	{
		Write('}');
		WriteLine();
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter CommaSpace()
	{
		Write(',');
		Write(' ');
		return this;
	}

	// High-level method generation APIs
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter BeginMethod(
		int indent,
		string returnType,
		string methodName,
		string? parameters = null
	)
	{
		Indent(indent);
		Write(returnType);
		Space();
		Write(methodName);
		OpenParen();
		if (parameters != null)
			Write(parameters);
		CloseParen();
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter BeginMethodBody(int indent)
	{
		WriteLine();
		Indent(indent);
		WriteLine('{');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter EndMethodBody(int indent)
	{
		Indent(indent);
		WriteLine('}');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteMethodCall(
		int indent,
		string? target,
		string methodName,
		string? arguments = null
	)
	{
		Indent(indent);
		if (target != null)
		{
			Write(target);
			Dot();
		}
		Write(methodName);
		OpenParen();
		if (arguments != null)
			Write(arguments);
		CloseParen();
		Semicolon();
		WriteLine();
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteReturn(int indent, string? value = null)
	{
		Indent(indent);
		Write("return");
		if (value != null)
		{
			Space();
			Write(value);
		}
		Semicolon();
		WriteLine();
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteIf(int indent, string condition)
	{
		Indent(indent);
		Write("if (");
		Write(condition);
		WriteLine(")");
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteUsing(string namespaceName)
	{
		Write("using ");
		Write(namespaceName);
		Semicolon();
		WriteLine();
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteNamespace(string namespaceName)
	{
		Write("namespace ");
		WriteLine(namespaceName);
		WriteLine('{');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteClass(
		int indent,
		string className,
		string? baseClass = null,
		string? interfaces = null
	)
	{
		Indent(indent);
		Write("class ");
		Write(className);

		if (baseClass != null || interfaces != null)
		{
			Write(" : ");
			if (baseClass != null)
			{
				Write(baseClass);
				if (interfaces != null)
					CommaSpace();
			}
			if (interfaces != null)
				Write(interfaces);
		}

		WriteLine();
		Indent(indent);
		WriteLine('{');
		return this;
	}

	// Zero-allocation string formatting for common patterns
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteFormatted(string format, string arg1)
	{
		// Zero-allocation interpolation for single argument
		var openBrace = format.IndexOf('{');
		if (openBrace >= 0)
		{
			var closeBrace = format.IndexOf('}', openBrace);
			if (closeBrace > openBrace)
			{
				// Write prefix directly to buffer
				if (openBrace > 0)
				{
					EnsureCapacity(openBrace);
					format.CopyTo(0, _buffer, _position, openBrace);
					_position += openBrace;
				}

				// Write argument
				WriteString(arg1);

				// Write suffix directly to buffer
				var suffixLength = format.Length - closeBrace - 1;
				if (suffixLength > 0)
				{
					EnsureCapacity(suffixLength);
					format.CopyTo(closeBrace + 1, _buffer, _position, suffixLength);
					_position += suffixLength;
				}
				return this;
			}
		}
		WriteString(format);
		return this;
	}

	// Batch operations for reducing method call overhead
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteSequence(params string[] values)
	{
		if (values == null)
			return this;
		for (var i = 0; i < values.Length; i++)
			WriteString(values[i]);
		return this;
	}

	// Zero-allocation overloads for common cases
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteSequence(string value1, string value2)
	{
		WriteString(value1);
		WriteString(value2);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteSequence(string value1, string value2, string value3)
	{
		WriteString(value1);
		WriteString(value2);
		WriteString(value3);
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteSequence(string value1, string value2, string value3, string value4)
	{
		WriteString(value1);
		WriteString(value2);
		WriteString(value3);
		WriteString(value4);
		return this;
	}

	// Core high-performance buffer manipulation
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void WriteStringOptimized(string value)
	{
		if (string.IsNullOrEmpty(value))
			return;

		// Fast path for single characters and very short strings
		if (value.Length == 1)
		{
			EnsureCapacity(1);
			_buffer[_position++] = value[0];
			return;
		}

		// Check if it's a cached common string
		for (var i = 0; i < CommonStrings.Length; i++)
		{
			if (ReferenceEquals(value, CommonStrings[i]))
			{
				WriteString(value);
				return;
			}
		}

		WriteString(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void WriteString(string value)
	{
		if (string.IsNullOrEmpty(value))
			return;

		EnsureCapacity(value.Length);
		value.CopyTo(0, _buffer, _position, value.Length);
		_position += value.Length;
	}

	// Memory-optimized helpers for common patterns
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteKeyword(string keyword)
	{
		// Use cached strings for common keywords to reduce string interning overhead
		switch (keyword)
		{
			case "return":
				WriteString(CommonStrings[4]);
				break;
			case "if":
				WriteString(CommonStrings[5]);
				break;
			case "else":
				WriteString(CommonStrings[6]);
				break;
			case "using":
				WriteString(CommonStrings[7]);
				break;
			case "namespace":
				WriteString(CommonStrings[8]);
				break;
			case "class":
				WriteString(CommonStrings[9]);
				break;
			case "public":
				WriteString(CommonStrings[10]);
				break;
			case "private":
				WriteString(CommonStrings[11]);
				break;
			case "void":
				WriteString(CommonStrings[12]);
				break;
			case "string":
				WriteString(CommonStrings[13]);
				break;
			case "int":
				WriteString(CommonStrings[14]);
				break;
			case "bool":
				WriteString(CommonStrings[15]);
				break;
			default:
				WriteString(keyword);
				break;
		}
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteSpaces(int count)
	{
		// Use cached strings for common space counts
		if (count >= 0 && count < 4)
		{
			WriteString(CommonStrings[count]);
		}
		else if (count > 0)
		{
			// For larger counts, write efficiently
			EnsureCapacity(count);
			for (var i = 0; i < count; i++)
				_buffer[_position++] = ' ';
		}
		return this;
	}

	// Zero-allocation method builder patterns
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteMethodSignature(string returnType, string methodName)
	{
		WriteString(returnType);
		Write(' ');
		WriteString(methodName);
		Write('(');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteParameterList(string parameters)
	{
		WriteString(parameters);
		Write(')');
		return this;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void WriteIntFast(int value)
	{
		if (value == 0)
		{
			EnsureCapacity(1);
			_buffer[_position++] = '0';
			return;
		}

		// Fast integer conversion without allocation
		EnsureCapacity(10); // Max digits for int32
		var start = _position;

		if (value < 0)
		{
			_buffer[_position++] = '-';
			value = -value;
		}

		var digits = _position;
		do
		{
			_buffer[_position++] = (char)('0' + (value % 10));
			value /= 10;
		} while (value > 0);

		// Reverse the digits
		var end = _position - 1;
		while (digits < end)
		{
			var temp = _buffer[digits];
			_buffer[digits] = _buffer[end];
			_buffer[end] = temp;
			digits++;
			end--;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	void EnsureCapacity(int additionalLength)
	{
		if (_position + additionalLength > _buffer.Length)
		{
			// Optimized growth strategy - less aggressive for large buffers
			var currentSize = _buffer.Length;
			var requiredSize = _position + additionalLength;

			int newSize;
			if (currentSize < 8192)
			{
				// Aggressive growth for small buffers
				newSize = Math.Max(currentSize * 2, requiredSize);
			}
			else if (currentSize < 65536)
			{
				// Moderate growth for medium buffers
				newSize = Math.Max(currentSize + currentSize / 2, requiredSize);
			}
			else
			{
				// Conservative growth for large buffers
				newSize = Math.Max(currentSize + 32768, requiredSize);
			}

			var newBuffer = CharPool.Rent(newSize);
			Array.Copy(_buffer, 0, newBuffer, 0, _position);
			CharPool.Return(_buffer);
			_buffer = newBuffer;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		if (_buffer != null)
		{
			CharPool.Return(_buffer);
			_buffer = null!;
		}

		_disposed = true;
	}

	public override string ToString()
	{
		if (_disposed || _buffer == null)
			return string.Empty;

		return new string(_buffer, 0, _position);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string ToStringAndDispose()
	{
		var result = ToString();
		Dispose();
		return result;
	}

	// Legacy compatibility methods (minimal allocation)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Append(string value) => Write(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter Append(char value) => Write(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter AppendLine() => WriteLine();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter AppendLine(string value) => WriteLine(value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public CodeWriter WriteIndent(int level) => Indent(level);

	// Compatibility with existing code patterns
	public CodeWriter Append(int indent, char value, bool withNewLine = true)
	{
		Indent(indent);
		Write(value);
		if (withNewLine)
			WriteLine();
		return this;
	}

	public CodeWriter Append(int indent, string value, bool withNewLine = true)
	{
		Indent(indent);
		Write(value);
		if (withNewLine)
			WriteLine();
		return this;
	}

	public CodeWriter Append(int indent, int value, bool withNewLine = true)
	{
		Indent(indent);
		Write(value);
		if (withNewLine)
			WriteLine();
		return this;
	}

	public CodeWriter Append(object? value)
	{
		if (value is null)
			return this;
		Write(value.ToString() ?? string.Empty);
		return this;
	}

	internal CodeWriter Append(int indent, PurviewTypeInfo typeInfo, bool withNewLine = true)
	{
		Indent(indent);
		Write(typeInfo.ToString());
		if (withNewLine)
			WriteLine();
		return this;
	}

	public CodeWriter AppendLine(char ch)
	{
		Write(ch);
		WriteLine();
		return this;
	}

	public CodeWriter AggressiveInlining(int indent) =>
		Append(indent, Constants.System.AggressiveInlining);

	public CodeWriter CodeGen(int indent) => Append(indent, Constants.System.GeneratedCode.Value);

	public CodeWriter ClassAttributes(int indent) =>
		Append(indent, Utilities.GetClassAttributesString(true, indent));

	public CodeWriter WithIndent(int indent) => Indent(indent);
}
