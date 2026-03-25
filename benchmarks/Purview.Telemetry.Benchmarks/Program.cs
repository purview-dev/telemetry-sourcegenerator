using BenchmarkDotNet.Running;
using Purview.Telemetry.Benchmarks.Benchmarks;

// Run all benchmarks when no arguments are provided, or use BenchmarkSwitcher
// for interactive selection (pass --filter or class names via args).
var switcher = BenchmarkSwitcher.FromAssembly(typeof(ActivityBenchmarks).Assembly);

if (args.Length == 0)
{
    // Run all benchmarks when launched without arguments.
    switcher.RunAll();
}
else
{
    switcher.Run(args);
}
