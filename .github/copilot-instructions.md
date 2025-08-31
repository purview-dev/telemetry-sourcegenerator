# Purview Telemetry Source Generator

Purview Telemetry Source Generator is a .NET incremental source generator that generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap, Build, and Test

Direct commands to run from repository root:

- `dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet format ./src/`

### Build and Test Sample Application

The sample application demonstrates the source generator in action:

- `cd samples/SampleApp && dotnet build --configuration Release` -- takes 19 seconds. NEVER CANCEL. Set timeout to 30+ minutes.
- `cd samples/SampleApp && dotnet test --configuration Release` -- runs 8 tests, takes 3 seconds.

## Validation

### Manual Validation Requirements

Always manually validate changes to the source generator:

- ALWAYS run `make build && make test` after making any changes to the source generator code
- ALWAYS build and test the sample application: `cd samples/SampleApp && dotnet build --configuration Release && dotnet test --configuration Release`
- ALWAYS run `make format` before committing to ensure code formatting compliance
- Test actual source generator functionality by examining generated files in the sample project (EmitCompilerGeneratedFiles is enabled)

### Functional Testing Scenarios

Test these scenarios when modifying the source generator:

- **Interface to Implementation Generation**: Modify an interface in `samples/SampleApp/SampleApp.Host/APIs/` and verify generated telemetry code appears
- **Activity Generation**: Test ActivitySource generation by adding methods with activity attributes
- **Logging Generation**: Test ILogger generation by adding methods with logging attributes
- **Metrics Generation**: Test metrics generation by adding methods with metrics attributes
- **Integration Test Coverage**: Verify new functionality is covered by tests in `src/Purview.Telemetry.SourceGenerator.IntegrationTests/`
- **Multi-Target Generation**: Test multi-target generation by ensuring the rules in this document are followed and verifying generated code

#### Multi-Target Rules

These are **must** be followed:

- Ensure all generated code is covered by integration tests
- Follow naming conventions for generated types and members
- Maintain consistent formatting and style in generated code
- Certain combinations of generation types cannot be combined:
  - Activity and Logging Scopes
  - Activities, Events, and Context
  - Non-multi-targeted generations and multi-targeted generations
- Support all the existing features of the non-multi-targeted generations

## Common Tasks

### Project Structure

```
src/
├── Purview.Telemetry.SourceGenerator/          # Main source generator library
├── Purview.Telemetry.SourceGenerator.IntegrationTests/  # Integration tests
├── Purview.Telemetry.SourceGenerator.slnx      # Main solution
└── global.json

samples/
└── SampleApp/                                  # .NET Aspire demo application
    ├── SampleApp.AppHost/                      # Aspire AppHost
    ├── SampleApp.Host/                         # Main web API
    ├── SampleApp.ServiceDefaults/              # Shared service config
    ├── SampleApp.UnitTests/                    # Sample app tests
    └── SampleApp.slnx                          # Sample solution

.build/
└── update-version.ts                           # Version management script
```

### Source Generator Architecture

The source generator processes interface definitions and generates three types of telemetry code:

- **Activities**: Distributed tracing using ActivitySource
- **Logging**: Structured logging using ILogger
- **Metrics**: Performance metrics using .NET metrics APIs

Generated code includes:

- Implementation classes with telemetry instrumentation
- Dependency injection registration helpers
- Configuration and initialization code

### Version Management

- Version is managed in `package.json`

- `bun .build/update-version.ts` synchronizes version across all files
- `make release-final` and `make release-pre` create new releases using commit-and-tag-version

## Important Development Notes

- Always use conventional commits
- The project uses .slnx solution files (Visual Studio 2022 format)
- Source generator targets netstandard2.0 for broad compatibility
- Integration tests target net9.0
- Sample application is a .NET Aspire application demonstrating telemetry integration
- Always test changes against the sample application to ensure end-to-end functionality
- The integration tests use `Verify` for snapshot testing of generated code
  - Never alter generated code manually
  - Snapshots are automatically generated in the `./src/Purview.Telemetry.SourceGenerator.IntegrationTests/Snapshots/` folder.
