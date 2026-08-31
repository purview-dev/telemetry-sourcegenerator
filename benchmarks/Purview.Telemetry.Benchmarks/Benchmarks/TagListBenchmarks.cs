using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Purview.Telemetry.Benchmarks.Telemetry;

namespace Purview.Telemetry.Benchmarks.Benchmarks;

/// <summary>
/// Compares performance of metrics recording with few tags vs. many tags.
/// <para>
/// The source generator uses different code paths depending on the number of tags:
/// <list type="bullet">
///   <item>Fewer than 4 tags: tags are passed directly as inline
///   <see cref="System.Collections.Generic.KeyValuePair{TKey,TValue}"/> parameters
///   (no heap allocation for a tag collection).</item>
///   <item>4 or more tags: a <see cref="System.Diagnostics.TagList"/> (a stack-allocated struct)
///   is used to batch the tags before recording, trading struct-population overhead for
///   avoiding a heap allocation.</item>
/// </list>
/// </para>
/// This benchmark demonstrates whether the TagList path has a meaningful impact on throughput
/// and allocations compared to the direct-parameter path.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net47)]
[SimpleJob(RuntimeMoniker.Net48)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class TagListBenchmarks
{
	IMetricsFewTagsTelemetry _fewTags = default!;
	IMetricsManyTagsTelemetry _manyTags = default!;

	[GlobalSetup]
	public void Setup()
	{
		(_fewTags, _manyTags) = BenchmarkHelpers.CreateMetricsTelemetry();
	}

	// --- Few tags (no TagList, direct KeyValuePair parameters) ---

	[Benchmark(Baseline = true, Description = "0 tags (no TagList): histogram record")]
	public void FewTags_Histogram_ZeroTags()
	{
		_fewTags.RecordOperationLatency(latencyMs: 42);
	}

	[Benchmark(Description = "1 tag (no TagList): auto-counter add")]
	public void FewTags_AutoCounter_OneTag()
	{
		_fewTags.CountOperationByType(operationType: "read");
	}

	[Benchmark(Description = "3 tags (no TagList): histogram record")]
	public void FewTags_Histogram_ThreeTags()
	{
		_fewTags.RecordRequestSize(sizeBytes: 1024, endpoint: "/api/data", method: "GET", statusCode: "200");
	}

	// --- Many tags (TagList path) ---

	[Benchmark(Description = "4 tags (TagList): auto-counter add")]
	public void ManyTags_AutoCounter_FourTags()
	{
		_manyTags.CountOperationWithFourTags(endpoint: "/api/data", method: "GET", status: "200", region: "us-east-1");
	}

	[Benchmark(Description = "5 tags (TagList): auto-counter add")]
	public void ManyTags_AutoCounter_FiveTags()
	{
		_manyTags.CountOperationWithFiveTags(
			endpoint: "/api/data",
			method: "GET",
			status: "200",
			region: "us-east-1",
			environment: "production"
		);
	}

	[Benchmark(Description = "6 tags (TagList): histogram record")]
	public void ManyTags_Histogram_SixTags()
	{
		_manyTags.RecordRequestDuration(
			durationMs: 42,
			endpoint: "/api/data",
			method: "GET",
			status: "200",
			region: "us-east-1",
			environment: "production"
		);
	}
}
