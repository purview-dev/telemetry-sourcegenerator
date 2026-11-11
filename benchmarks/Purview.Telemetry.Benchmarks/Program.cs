using BenchmarkDotNet.Running;
using Purview.Telemetry.Benchmarks.Benchmarks;

// BenchmarkSwitcher.Run prompts for a selection when the arguments select no benchmark, and
// `--artifacts <path>` or `--job short` alone is exactly that: they configure the run, they do not choose
// what to run. Appending a `*` filter when nothing selects a benchmark keeps those options working and
// never prompts. A --filter or --list is passed through untouched, so filtering and listing keep working.
var switcher = BenchmarkSwitcher.FromAssembly(typeof(ActivityBenchmarks).Assembly);

if (args.Any(static arg => IsSelection(arg)))
{
	switcher.Run(args);
}
else
{
	switcher.Run([.. args, "--filter", "*"]);
}

static bool IsSelection(string arg) =>
	arg is "--list" or "--filter" || arg.StartsWith("--filter=", StringComparison.Ordinal);
