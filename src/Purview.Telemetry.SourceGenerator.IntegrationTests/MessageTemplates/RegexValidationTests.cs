using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Purview.Telemetry.SourceGenerator.MessageTemplates;

public partial class RegexValidationTests
{
	[Test]
	[MethodDataSource(nameof(OrdinalTests))]
	public void Match_GivenOrdinals_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(NamedTests))]
	public void Match_GivenNames_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(DestructureTests))]
	public void Match_GivenDestructure_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(StringifyTests))]
	public void Match_GivenStringify_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(AlignmentTests))]
	public void Match_GivenAlignment_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(FormattingTests))]
	public void Match_GivenFormatting_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(AMixedBag))]
	public void Match_GivenAMixOfParameters_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	[Test]
	[MethodDataSource(nameof(DoubleCurlyBraces))]
	public void Match_GivenDoubleCurlyBraces_Matches(
		string template,
		[NotNull] TestMessageTemplateHole[] holes
	) => Match(template, holes);

	static void Match(string template, TestMessageTemplateHole[] holes)
	{
		// Arrange/ Act
		var matches = Constants.MessageTemplateMatcher.Matches(template);

		// Assert
		matches.Count.ShouldBe(holes.Length);
		for (var i = 0; i < matches.Count; i++)
		{
			Match match = matches[i];
			match.Success.ShouldBeTrue();

			var hole = holes[i];
			var result = TestMessageTemplateHole.FromMatch(match);

			hole.ShouldBe(result);
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> OrdinalTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				( "Customer with ID {0}", [TestMessageTemplateHole.Create(0)] ),
				(
					"Customer with ID {0} is named {1}",
					[TestMessageTemplateHole.Create(0), TestMessageTemplateHole.Create(1)]
				),
				(
					"Customer with ID {0} is named {1}, and {2}. and {3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1),
						TestMessageTemplateHole.Create(2),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> NamedTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				( "Customer with ID {CustomerId}", [TestMessageTemplateHole.Create("CustomerId")] ),
				(
					"Customer with ID {CustomerId} is named {CustomerName}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName"),
					]
				),
				(
					"Customer with ID {CustomerId} is named {CustomerName}, and {Banana}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName"),
						TestMessageTemplateHole.Create("Banana"),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> DestructureTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {@CustomerId}",
					[TestMessageTemplateHole.Create("CustomerId", destructure: true)]
				),
				(
					"Customer with ID {@CustomerId} is named {@CustomerName}",
					[
						TestMessageTemplateHole.Create("CustomerId", destructure: true),
						TestMessageTemplateHole.Create("CustomerName", destructure: true),
					]
				),
				(
					"Customer with ID {CustomerId} is named {@CustomerName}, and {Banana}. and {@Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", destructure: true),
						TestMessageTemplateHole.Create("Banana"),
						TestMessageTemplateHole.Create("Apple", destructure: true),
					]
				),
				(
					"Customer with ID {CustomerId} is named {@CustomerName}, and {@Banana}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", destructure: true),
						TestMessageTemplateHole.Create("Banana", destructure: true),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {@0}", [TestMessageTemplateHole.Create(0, destructure: true)] ),
				(
					"Customer with ID {@0} is named {@1}",
					[
						TestMessageTemplateHole.Create(0, destructure: true),
						TestMessageTemplateHole.Create(1, destructure: true),
					]
				),
				(
					"Customer with ID {0} is named {@1}, and {2}. and {@3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, destructure: true),
						TestMessageTemplateHole.Create(2),
						TestMessageTemplateHole.Create(3, destructure: true),
					]
				),
				(
					"Customer with ID {0} is named {@1}, and {@2}. and {3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, destructure: true),
						TestMessageTemplateHole.Create(2, destructure: true),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> StringifyTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {$CustomerId}",
					[TestMessageTemplateHole.Create("CustomerId", stringify: true)]
				),
				(
					"Customer with ID {$CustomerId} is named {$CustomerName}",
					[
						TestMessageTemplateHole.Create("CustomerId", stringify: true),
						TestMessageTemplateHole.Create("CustomerName", stringify: true),
					]
				),
				(
					"Customer with ID {CustomerId} is named {$CustomerName}, and {Banana}. and {$Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", stringify: true),
						TestMessageTemplateHole.Create("Banana"),
						TestMessageTemplateHole.Create("Apple", stringify: true),
					]
				),
				(
					"Customer with ID {CustomerId} is named {$CustomerName}, and {$Banana}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", stringify: true),
						TestMessageTemplateHole.Create("Banana", stringify: true),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {$0}", [TestMessageTemplateHole.Create(0, stringify: true)] ),
				(
					"Customer with ID {$0} is named {$1}",
					[
						TestMessageTemplateHole.Create(0, stringify: true),
						TestMessageTemplateHole.Create(1, stringify: true),
					]
				),
				(
					"Customer with ID {0} is named {$1}, and {2}. and {$3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, stringify: true),
						TestMessageTemplateHole.Create(2),
						TestMessageTemplateHole.Create(3, stringify: true),
					]
				),
				(
					"Customer with ID {0} is named {$1}, and {$2}. and {3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, stringify: true),
						TestMessageTemplateHole.Create(2, stringify: true),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> AlignmentTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {CustomerId,10}",
					[TestMessageTemplateHole.Create("CustomerId", alignment: 10)]
				),
				(
					"Customer with ID {CustomerId,-10} is named {CustomerName,10}",
					[
						TestMessageTemplateHole.Create("CustomerId", alignment: -10),
						TestMessageTemplateHole.Create("CustomerName", alignment: 10),
					]
				),
				(
					"Customer with ID {CustomerId} is named {CustomerName,99}, and {Banana}. and {Apple,000}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", alignment: 99),
						TestMessageTemplateHole.Create("Banana"),
						TestMessageTemplateHole.Create("Apple", alignment: 000),
					]
				),
				(
					"Customer with ID {CustomerId} is named {CustomerName,100}, and {Banana,-111}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", alignment: 100),
						TestMessageTemplateHole.Create("Banana", alignment: -111),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {0,-99}", [TestMessageTemplateHole.Create(0, alignment: -99)] ),
				(
					"Customer with ID {0,101} is named {1,-1}",
					[
						TestMessageTemplateHole.Create(0, alignment: 101),
						TestMessageTemplateHole.Create(1, alignment: -1),
					]
				),
				(
					"Customer with ID {0} is named {1,1}, and {2}. and {3,-3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, alignment: 1),
						TestMessageTemplateHole.Create(2),
						TestMessageTemplateHole.Create(3, alignment: -3),
					]
				),
				(
					"Customer with ID {0} is named {1,1111}, and {2,222}. and {3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, alignment: 1111),
						TestMessageTemplateHole.Create(2, alignment: 222),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> FormattingTests
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {CustomerId:ff}",
					[TestMessageTemplateHole.Create("CustomerId", format: "ff")]
				),
				(
					"Customer with ID {CustomerId:-10} is named {CustomerName:pies}",
					[
						TestMessageTemplateHole.Create("CustomerId", format: "-10"),
						TestMessageTemplateHole.Create("CustomerName", format: "pies"),
					]
				),
				(
					"Customer with ID {CustomerId} is named {CustomerName:ice-cream}, and {Banana}. and {Apple:p000p}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", format: "ice-cream"),
						TestMessageTemplateHole.Create("Banana"),
						TestMessageTemplateHole.Create("Apple", format: "p000p"),
					]
				),
				(
					"Customer with ID {CustomerId} is named {CustomerName:100}, and {Banana:-111}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId"),
						TestMessageTemplateHole.Create("CustomerName", format: "100"),
						TestMessageTemplateHole.Create("Banana", format: "-111"),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {0:-99}", [TestMessageTemplateHole.Create(0, format: "-99")] ),
				(
					"Customer with ID {0:101} is named {1:hello-1}",
					[
						TestMessageTemplateHole.Create(0, format: "101"),
						TestMessageTemplateHole.Create(1, format: "hello-1"),
					]
				),
				(
					"Customer with ID {0} is named {1:1}, and {2}. and {3:-3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, format: "1"),
						TestMessageTemplateHole.Create(2),
						TestMessageTemplateHole.Create(3, format: "-3"),
					]
				),
				(
					"Customer with ID {0} is named {1:1111}, and {2:222}. and {3}",
					[
						TestMessageTemplateHole.Create(0),
						TestMessageTemplateHole.Create(1, format: "1111"),
						TestMessageTemplateHole.Create(2, format: "222"),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> AMixedBag
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {CustomerId:ff}",
					[TestMessageTemplateHole.Create("CustomerId", format: "ff")]
				),
				(
					"Customer with ID {CustomerId,0101:-10} is named {$CustomerName:pies}",
					[
						TestMessageTemplateHole.Create(
							"CustomerId",
							alignment: 0101,
							format: "-10"
						),
						TestMessageTemplateHole.Create(
							"CustomerName",
							format: "pies",
							stringify: true
						),
					]
				),
				(
					"Customer with ID {@CustomerId} is named {$CustomerName:ice-cream}, and {$Banana,100:110}. and {@Apple:p000p}",
					[
						TestMessageTemplateHole.Create("CustomerId", destructure: true),
						TestMessageTemplateHole.Create(
							"CustomerName",
							format: "ice-cream",
							stringify: true
						),
						TestMessageTemplateHole.Create(
							"Banana",
							alignment: 100,
							format: "110",
							stringify: true
						),
						TestMessageTemplateHole.Create("Apple", format: "p000p", destructure: true),
					]
				),
				(
					"Customer with ID {$CustomerId} is named {@CustomerName,100:pies}, and {@Banana,1010101:-111}. and {Apple}",
					[
						TestMessageTemplateHole.Create("CustomerId", stringify: true),
						TestMessageTemplateHole.Create(
							"CustomerName",
							alignment: 100,
							format: "pies",
							destructure: true
						),
						TestMessageTemplateHole.Create(
							"Banana",
							alignment: 1010101,
							format: "-111",
							destructure: true
						),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {0:ff}", [TestMessageTemplateHole.Create(0, format: "ff")] ),
				(
					"Customer with ID {0,0101:-10} is named {$1:pies}",
					[
						TestMessageTemplateHole.Create(0, alignment: 0101, format: "-10"),
						TestMessageTemplateHole.Create(1, format: "pies", stringify: true),
					]
				),
				(
					"Customer with ID {@0} is named {$1:ice-cream}, and {$2,100:110}. and {@3:p000p}",
					[
						TestMessageTemplateHole.Create(0, destructure: true),
						TestMessageTemplateHole.Create(1, format: "ice-cream", stringify: true),
						TestMessageTemplateHole.Create(
							2,
							alignment: 100,
							format: "110",
							stringify: true
						),
						TestMessageTemplateHole.Create(3, format: "p000p", destructure: true),
					]
				),
				(
					"Customer with ID {$0} is named {@1,100:pies}, and {@2,1010101:-111}. and {3}",
					[
						TestMessageTemplateHole.Create(0, stringify: true),
						TestMessageTemplateHole.Create(
							1,
							alignment: 100,
							format: "pies",
							destructure: true
						),
						TestMessageTemplateHole.Create(
							2,
							alignment: 1010101,
							format: "-111",
							destructure: true
						),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	public static IEnumerable<(string, TestMessageTemplateHole[])> DoubleCurlyBraces
	{
		get
		{
			List<(string, TestMessageTemplateHole[])> data =
			[
				(
					"Customer with ID {{CustomerId:ff}}",
					[TestMessageTemplateHole.Create("CustomerId", format: "ff")]
				),
				(
					"Customer with ID {{CustomerId,0101:-10}} is named {{$CustomerName:pies}}",
					[
						TestMessageTemplateHole.Create(
							"CustomerId",
							alignment: 0101,
							format: "-10"
						),
						TestMessageTemplateHole.Create(
							"CustomerName",
							format: "pies",
							stringify: true
						),
					]
				),
				(
					"Customer with ID {{@CustomerId}} is named {{$CustomerName:ice-cream}}, and {{$Banana,100:110}}. and {{@Apple:p000p}}",
					[
						TestMessageTemplateHole.Create("CustomerId", destructure: true),
						TestMessageTemplateHole.Create(
							"CustomerName",
							format: "ice-cream",
							stringify: true
						),
						TestMessageTemplateHole.Create(
							"Banana",
							alignment: 100,
							format: "110",
							stringify: true
						),
						TestMessageTemplateHole.Create("Apple", format: "p000p", destructure: true),
					]
				),
				(
					"Customer with ID {{$CustomerId}} is named {{@CustomerName,100:pies}}, and {{@Banana,1010101:-111}}. and {{Apple}}",
					[
						TestMessageTemplateHole.Create("CustomerId", stringify: true),
						TestMessageTemplateHole.Create(
							"CustomerName",
							alignment: 100,
							format: "pies",
							destructure: true
						),
						TestMessageTemplateHole.Create(
							"Banana",
							alignment: 1010101,
							format: "-111",
							destructure: true
						),
						TestMessageTemplateHole.Create("Apple"),
					]
				),
				( "Customer with ID {{0:ff}", [TestMessageTemplateHole.Create(0, format: "ff")] ),
				(
					"Customer with ID {{0,0101:-10}} is named {{$1:pies}}",
					[
						TestMessageTemplateHole.Create(0, alignment: 0101, format: "-10"),
						TestMessageTemplateHole.Create(1, format: "pies", stringify: true),
					]
				),
				(
					"Customer with ID {{@0}} is named {{$1:ice-cream}}, and {{$2,100:110}}. and {{@3:p000p}}",
					[
						TestMessageTemplateHole.Create(0, destructure: true),
						TestMessageTemplateHole.Create(1, format: "ice-cream", stringify: true),
						TestMessageTemplateHole.Create(
							2,
							alignment: 100,
							format: "110",
							stringify: true
						),
						TestMessageTemplateHole.Create(3, format: "p000p", destructure: true),
					]
				),
				(
					"Customer with ID {{$0}} is named {{@1,100:pies}}, and {{@2,1010101:-111}}. and {{3}}",
					[
						TestMessageTemplateHole.Create(0, stringify: true),
						TestMessageTemplateHole.Create(
							1,
							alignment: 100,
							format: "pies",
							destructure: true
						),
						TestMessageTemplateHole.Create(
							2,
							alignment: 1010101,
							format: "-111",
							destructure: true
						),
						TestMessageTemplateHole.Create(3),
					]
				),
			];

			return data;
		}
	}

	[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
	public record struct TestMessageTemplateHole
	{
		public TestMessageTemplateHole(
			string? name,
			int? ordinal,
			int? alignment,
			string? format,
			bool destructure,
			bool stringify
		)
		{
			Name = name;
			Ordinal = ordinal;
			Alignment = alignment;
			Format = format;
			Destructure = destructure;
			Stringify = stringify;

			if (Name == null && Ordinal == null)
				throw new Exception("Name and Ordinal cannot both be null.");
			if (Name != null && Ordinal != null)
				throw new Exception("Name and Ordinal cannot both be set.");
			if (Destructure && Stringify)
				throw new Exception("Destructure and Stringify cannot both be true.");
		}

		public readonly bool IsPositional => Name is null;

		public string? Name { get; private set; }

		public int? Ordinal { get; private set; }

		public int? Alignment { get; private set; }

		public string? Format { get; private set; }

		public bool Destructure { get; private set; }

		public bool Stringify { get; private set; }

		public static TestMessageTemplateHole Create(
			int ordinal,
			int? alignment = null,
			string? format = null,
			bool destructure = false,
			bool stringify = false
		) => new(null, ordinal, alignment, format, destructure, stringify);

		public static TestMessageTemplateHole Create(
			string name,
			int? alignment = null,
			string? format = null,
			bool destructure = false,
			bool stringify = false
		) => new(name, null, alignment, format, destructure, stringify);

		public static TestMessageTemplateHole FromMatch([NotNull] Match match)
		{
			if (!match.Success)
				throw new ArgumentException("Match must be successful.", nameof(match));

			string? name = null;
			string? ordinal = null;
			string? alignment = null;
			string? format = null;
			var destructure = match.Groups["destructure"].Success;
			var stringify = match.Groups["stringify"].Success;

			if (match.Groups["named"].Success)
				name = match.Groups["named"].Value;
			if (match.Groups["ordinal"].Success)
				ordinal = match.Groups["ordinal"].Value;
			if (match.Groups["alignment"].Success)
				alignment = match.Groups["alignment"].Value;
			if (match.Groups["format"].Success)
				format = match.Groups["format"].Value;

			return new(
				name,
				ordinal == null ? null : int.Parse(ordinal, CultureInfo.InvariantCulture),
				alignment == null ? null : int.Parse(alignment, CultureInfo.InvariantCulture),
				format,
				destructure,
				stringify
			);
		}
	}
}
