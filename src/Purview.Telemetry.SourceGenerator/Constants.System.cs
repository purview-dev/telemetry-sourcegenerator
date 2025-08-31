using System.Globalization;
using Microsoft.CodeAnalysis;
using Purview.Telemetry.SourceGenerator.Templates;

namespace Purview.Telemetry;

partial class Constants
{
	public static Lazy<Version> Version = new(() => typeof(Constants).Assembly.GetName().Version!);

	public static class System
	{
		public const string VoidKeyword = "void";
		public const string NullKeyword = "null";
		public const string DefaultKeyword = "default";

		public static readonly PurviewTypeInfo DateTimeOffset =
			PurviewTypeFactory.Create<DateTimeOffset>();
		public static readonly PurviewTypeInfo Func = PurviewTypeFactory.Create("System.Func"); // <T>
		public static readonly PurviewTypeInfo Action = PurviewTypeFactory.Create("System.Action"); // <T>

		public static readonly PurviewTypeInfo IEnumerable = PurviewTypeFactory.Create(
			"System.Collections.IEnumerable"
		);
		public static readonly PurviewTypeInfo GenericIEnumerable = PurviewTypeFactory.Create(
			"System.Collections.Generic.IEnumerable"
		); // <>
		public static readonly PurviewTypeInfo List = PurviewTypeFactory.Create(
			"System.Collections.Generic.List"
		); // <>
		public static readonly PurviewTypeInfo Dictionary = PurviewTypeFactory.Create(
			"System.Collections.Generic.Dictionary"
		); // <>
		public static readonly PurviewTypeInfo ConcurrentDictionary = PurviewTypeFactory.Create(
			"System.Collections.Concurrent.ConcurrentDictionary"
		); // <>

		public static readonly PurviewTypeInfo IDisposable =
			PurviewTypeFactory.Create<IDisposable>();
		public static readonly PurviewTypeInfo Exception = PurviewTypeFactory.Create<Exception>();

		public static readonly PurviewTypeInfo TagList = PurviewTypeFactory.Create(
			SystemDiagnosticsNamespace + ".TagList"
		);

		public const string AggressiveInlining =
			"[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";

		public const string ExcludeFromCodeCoverageConstant =
			"[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

		public const string EditorBrowsableConstant =
			"[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]";

		public const string CompilerGeneratedConstant =
			"[global::System.Runtime.CompilerServices.CompilerGenerated]";

		const string GeneratedCodeConstant =
			"[global::System.CodeDom.Compiler.GeneratedCode(\"{0}\", \"{1}\")]";

		public static readonly Lazy<string> GeneratedCode = new(() =>
			string.Format(
				CultureInfo.InvariantCulture,
				GeneratedCodeConstant,
				"Purview.Telemetry.SourceGenerator",
				Version.Value
			)
		);

		public static class BuiltInTypes
		{
			// Reference types
			public const string StringKeyword = "string";
			public const string ObjectKeyword = "object";

			// Boolean types
			public const string BoolKeyword = "bool";

			// Signed integers
			public const string ByteKeyword = "byte";
			public const string ShortKeyword = "short";
			public const string IntKeyword = "int";
			public const string LongKeyword = "long";

			// Unsigned integers
			public const string SByteKeyword = "sbyte";
			public const string UShortKeyword = "ushort";
			public const string UIntKeyword = "uint";
			public const string ULongKeyword = "ulong";

			// Floating‑point & decimal
			public const string FloatKeyword = "float";
			public const string DoubleKeyword = "double";
			public const string DecimalKeyword = "decimal";

			// Other types
			public const string CharKeyword = "char";

			// Reference types
			public static readonly PurviewTypeInfo String = PurviewTypeFactory.Create(
				SpecialType.System_String
			);
			public static readonly PurviewTypeInfo Object = PurviewTypeFactory.Create(
				SpecialType.System_Object
			);

			// Boolean types
			public static readonly PurviewTypeInfo Boolean = PurviewTypeFactory.Create(
				SpecialType.System_Boolean
			);

			// Signed integers
			public static readonly PurviewTypeInfo Byte = PurviewTypeFactory.Create(
				SpecialType.System_Byte
			);
			public static readonly PurviewTypeInfo Int16 = PurviewTypeFactory.Create(
				SpecialType.System_Int16
			);
			public static readonly PurviewTypeInfo Int32 = PurviewTypeFactory.Create(
				SpecialType.System_Int32
			);
			public static readonly PurviewTypeInfo Int64 = PurviewTypeFactory.Create(
				SpecialType.System_Int64
			);

			// Unsigned integers
			public static readonly PurviewTypeInfo SByte = PurviewTypeFactory.Create(
				SpecialType.System_SByte
			);
			public static readonly PurviewTypeInfo UInt16 = PurviewTypeFactory.Create(
				SpecialType.System_UInt16
			);
			public static readonly PurviewTypeInfo UInt32 = PurviewTypeFactory.Create(
				SpecialType.System_UInt32
			);
			public static readonly PurviewTypeInfo UInt64 = PurviewTypeFactory.Create(
				SpecialType.System_UInt64
			);

			// Floating‑point & decimal
			public static readonly PurviewTypeInfo Single = PurviewTypeFactory.Create(
				SpecialType.System_Single
			);
			public static readonly PurviewTypeInfo Double = PurviewTypeFactory.Create(
				SpecialType.System_Double
			);
			public static readonly PurviewTypeInfo Decimal = PurviewTypeFactory.Create(
				SpecialType.System_Decimal
			);

			// Other types
			public static readonly PurviewTypeInfo Char = PurviewTypeFactory.Create(
				SpecialType.System_Char
			);
		}
	}
}
