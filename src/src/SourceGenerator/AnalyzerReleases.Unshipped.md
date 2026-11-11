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
TSG2022 | Logging.Usage | Error | [params parameters are not supported](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2022)
TSG2023 | Logging.Usage | Error | [ref, out and ref struct parameters are not supported](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2023)
TSG2024 | Logging.Usage | Error | [TagProvider type could not be resolved](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2024)
TSG2025 | Logging.Usage | Error | [TagProvider method could not be found](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2025)
TSG2026 | Logging.Usage | Error | [TagProvider method has an invalid signature](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2026)
TSG2027 | Logging.Usage | Warning | [TagProvider is only supported on logging parameters](https://purview.dev/docs/telemetry-sourcegenerator/diagnostics/#tsg2027)
