# Purview Telemetry Source Generator

Purview Telemetry Source Generator is a .NET incremental source generator that generates [`ActivitySource`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource), [`ILogger`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.ilogger), and [`Metrics`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.metrics) based telemetry from methods you define on an interface.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites

Install these exact dependencies in order:

- Install .NET 10.0.100 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.100`
- Install .NET 10.0 runtime: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime dotnet --version 10.0.0`
- Install Bun: `curl -fsSL https://bun.sh/install | bash`
- Set environment variables: `export PATH=$HOME/.bun/bin:$HOME/.dotnet:$PATH && export DOTNET_ROOT=$HOME/.dotnet`

### Bootstrap, Build, and Test

Bootstrap and build the repository:

- `make build` -- builds the main source generator and integration tests. Takes 26 seconds. NEVER CANCEL. Set timeout to 60+ minutes.
- `make test` -- runs 282 integration tests. Takes 42 seconds. NEVER CANCEL. Set timeout to 60+ minutes.
- `make format` -- formats code according to .editorconfig rules. Takes 21 seconds. NEVER CANCEL. Set timeout to 30+ minutes.

Alternative direct commands (use environment variables above):

- `dotnet build ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release`
- `dotnet format ./src/`

### Build and Test Sample Application

The sample application demonstrates the source generator in action:

- `cd samples/SampleApp && dotnet build --configuration Release` -- takes 19 seconds. NEVER CANCEL. Set timeout to 30+ minutes.
- `cd samples/SampleApp && dotnet test --configuration Release` -- runs 8 tests, takes 3 seconds.

### Package Creation

- `make pack` -- creates NuGet package with current version from package.json
- `make version` -- displays current version (currently 3.2.4)

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

### CI Validation

Always run these validation steps before committing:

- `make format` (takes 21 seconds)
- `make build` (takes 26 seconds)
- `make test` (takes 42 seconds)
- Sample app build and test (takes 22 seconds total)

The CI pipeline (`./.github/workflows/ci.yml`) runs the same dotnet restore → build → test workflow.

## Common Tasks

### Project Structure

```
src/
├── Purview.Telemetry.SourceGenerator/          # Main source generator library
├── Purview.Telemetry.SourceGenerator.IntegrationTests/  # 282 integration tests
├── Purview.Telemetry.SourceGenerator.slnx      # Main solution
└── global.json                                 # Pins to .NET 9.0.200

samples/
└── SampleApp/                                  # .NET Aspire demo application
    ├── SampleApp.AppHost/                      # Aspire AppHost
    ├── SampleApp.Host/                         # Main web API
    ├── SampleApp.ServiceDefaults/              # Shared service config
    ├── SampleApp.UnitTests/                    # Sample app tests
    └── SampleApp.slnx                          # Sample solution

.build/
├── common.mk                                   # Shared Makefile targets
└── update-version.ts                           # Version management script

Makefile                                        # Main build automation
package.json                                    # Version 3.2.4, Bun scripts
```

### Key Commands Output

#### make help

```
build          Builds the project.
test           Runs the tests for the project.
release-final  Creates a new release, e.g. v3.0.1.
release-pre    Creates a new pre-release, e.g. v3.0.1-prerelease.1.
pack           Packs the project into a nuget package using PACK_VERSION argument.
format         Formats the code according to the rules of the src/.editorconfig file.
vs             Opens the project in Visual Studio.
code           Opens the project in Visual Studio Code.
vs-s           Opens the sample project in Visual Studio.
version        Displays the current version of the project.
update-version Update related samples and docs to new version.
```

#### Repository Root Files

```
.build/          .config/         .cspell.json     .git/
.gitattributes   .github/         .gitignore       .gitmodules
.husky/          .vscode/         .wiki/           CHANGELOG.md
LICENSE.md       Makefile         README.md        bun.lock
global.json      package.json     samples/         src/
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

### Generated Code Location

When `EmitCompilerGeneratedFiles` is true (as in the sample app), generated files appear in:

- `obj/Debug|Release/generated/` directories
- Integration test snapshots in `src/Purview.Telemetry.SourceGenerator.IntegrationTests/Snapshots/`

### Version Management

- Version is managed in `package.json` (currently 3.2.4)

- `bun .build/update-version.ts` synchronizes version across all files
- `make release-final` and `make release-pre` create new releases using commit-and-tag-version

## CRITICAL Timing and Cancellation Warnings

- **NEVER CANCEL** any build or test command - builds may take up to 60 minutes in some environments
- **Build timeout**: Set 60+ minutes timeout for `make build` and related commands
- **Test timeout**: Set 60+ minutes timeout for `make test` and related commands
- **Format timeout**: Set 30+ minutes timeout for `make format`
- **Expected times**: Build ~26s, Test ~42s, Format ~21s, Sample build ~19s on typical hardware

## Important Development Notes

- Always use conventional commits
- The project uses .slnx solution files (Visual Studio 2022 format)
- Source generator targets netstandard2.0 for broad compatibility
- Integration tests target net9.0
- Sample application is a .NET Aspire application demonstrating telemetry integration
- Always test changes against the sample application to ensure end-to-end functionality
- The integration tests use Verify for snapshot testing of generated code
