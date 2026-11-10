; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TSG3023 | Activity.Usage | Warning | [ActivitySourceFactory is not available](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg3023)
TSG3024 | Activity.Usage | Warning | [ActivitySourceOptions require UseActivitySourceFactory](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg3024)
TSG4010 | Metrics.Usage | Warning | [Gauge is not available](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg4010)
TSG4011 | Metrics.Usage | Warning | [HistogramBucketBoundaries is not available](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg4011)
TSG4012 | Metrics.Usage | Warning | [HistogramBucketBoundaries is not a list of numbers](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg4012)
