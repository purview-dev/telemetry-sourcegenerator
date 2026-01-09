using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Purview.Telemetry.SourceGenerator.BuildTools;

// these types borrowed from Roslyn's internal implementations of the abstract types
sealed class InMemoryAdditionalText(
	string path,
	string content,
	(string key, string value)[]? options = default
) : AdditionalText
{
	readonly SourceText _content = SourceText.From(content, Encoding.UTF8);

	public AnalyzerConfigOptions GetOptions()
	{
		if (options is null || options.Length == 0)
			return InMemoryConfigOptions.Empty;

		var builder = ImmutableDictionary.CreateBuilder<string, string>(
			AnalyzerConfigOptions.KeyComparer
		);
		foreach ((var key, var value) in options)
			builder.Add(Literals.AdditionalFileMetadataPrefix + key, value);

		return new InMemoryConfigOptions(builder.ToImmutable());
	}

	public override string Path { get; } = path;

	public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

	private class InMemoryConfigOptions(ImmutableDictionary<string, string> values)
		: AnalyzerConfigOptions
	{
		public static AnalyzerConfigOptions Empty { get; } = new InMemoryConfigOptions([]);

		public override bool TryGetValue(
			string key,
			[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value
		) => values.TryGetValue(key, out value);
	}
}

internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
	readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

	public static TestAnalyzerConfigOptionsProvider Empty { get; } =
		new([], TestAnalyzerConfigOptions.Empty);

	internal TestAnalyzerConfigOptionsProvider(
		ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
		AnalyzerConfigOptions globalOptions
	)
	{
		_treeDict = treeDict;
		GlobalOptions = globalOptions;
	}

	public override AnalyzerConfigOptions GlobalOptions { get; }

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
		_treeDict.TryGetValue(tree, out var options) ? options : TestAnalyzerConfigOptions.Empty;

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
		_treeDict.TryGetValue(textFile, out var options)
			? options
			: TestAnalyzerConfigOptions.Empty;

	internal TestAnalyzerConfigOptionsProvider WithAdditionalTreeOptions(
		ImmutableDictionary<object, AnalyzerConfigOptions> treeDict
	) => new(_treeDict.AddRange(treeDict), GlobalOptions);

	internal TestAnalyzerConfigOptionsProvider WithGlobalOptions(
		AnalyzerConfigOptions globalOptions
	) => new(_treeDict, globalOptions);
}

internal sealed class TestAnalyzerConfigOptions(ImmutableDictionary<string, string> properties)
	: AnalyzerConfigOptions
{
	public static TestAnalyzerConfigOptions Empty { get; } =
		new(ImmutableDictionary.Create<string, string>(KeyComparer));

	public override bool TryGetValue(
		string key,
		[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value
	) => properties.TryGetValue(key, out value);
}
