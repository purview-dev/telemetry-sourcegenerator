set quiet

root_folder := "./src/"
solution_file := root_folder + "Telemetry.SourceGenerator.slnx"
test_solution := solution_file
build_configuration := "Release"

pipeline_solution := "build/Pipeline.slnx"
pipeline_project := "build/PipelineCLI/PipelineCLI.csproj"

sample_solution_file := "./samples/SampleApp/SampleApp.slnx"
artifact_folder := "./artifacts/"
benchmark_solution := "./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj"

# Displays the list of available commands

[private]
default:
    just --list

# Run the PR pipeline (restore, build, lint, tests)
[group('Pipeline')]
pipeline-pr *args:
    echo "Running PR pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} {{ args }}

# Run the build pipeline (restore, build, lint)
[group('Pipeline')]
pipeline-build *args:
    echo "Running build pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=false --Release:Mode=None {{ args }} 

# Run the release pipeline (restore, build, lint, tests, pack, publish, GitHub release)
[group('Pipeline')]
pipeline-release *args:
    echo "Running release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=NuGet {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, local nuget publish)
# Note: `just` runs recipes through the shell, which strips backslashes from unquoted arguments.
# Always use forward slashes for the feed path, e.g.
# just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
[group('Pipeline')]
pipeline-local-release *args:
    echo "Running local release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=LocalNuGet {{ args }}

# Run the pipeline with tests enabled
[group('Pipeline')]
pipeline-tests *args:
    echo "Running tests pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=true --Release:Mode=None {{ args }}

# -----------------------------------------------------------------------------
# Build and Test
# -----------------------------------------------------------------------------

# Builds the solution with the specified configuration (default: Release)

[group('Build and Test')]
build:
    echo "Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet build "{{ solution_file }}" --configuration "{{ build_configuration }}"

# Runs tests for the solution with the specified configuration (default: Release)

[group('Build and Test')]
test:
    echo "Running tests for {{ BLUE }}{{ test_solution }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet test --solution "{{ test_solution }}" --configuration "{{ build_configuration }}"

# Packs the source generator into a NuGet package

[group('Build and Test')]
pack: update-version build-pack

# Packs the source generator into a NuGet package and outputs it to the
# specified folder, including version, branch, commit, and copyright year
# information in the package metadata.

[group('Build and Test')]
build-pack:
    bun -e "const version = require('./package.json').version; console.log('Packing {{ BLUE }}Source Generator{{ NORMAL }} with {{ YELLOW }}' + version + '{{ NORMAL }}...');"
    echo "  Configuration:   {{ GREEN }}{{ build_configuration }}{{ NORMAL }}"
    bun -e "const { execFileSync } = require('child_process'); console.log('  Branch:          {{ GREEN }}' + execFileSync('git', ['rev-parse', '--abbrev-ref', 'HEAD'], { encoding: 'utf8' }).trim() + '{{ NORMAL }}');"
    bun -e "const { execFileSync } = require('child_process'); console.log('  Commit:          {{ GREEN }}' + execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim() + '{{ NORMAL }}');"
    bun -e "console.log('  Copyright Year:  {{ GREEN }}' + new Date().getFullYear() + '{{ NORMAL }}');"
    echo "  Output Folder:   {{ GREEN }}{{ artifact_folder }}{{ NORMAL }}"
    bun .build/build-pack.ts "{{ root_folder }}" "{{ build_configuration }}" "{{ artifact_folder }}"

# -----------------------------------------------------------------------------
# Samples
# -----------------------------------------------------------------------------

# Builds the sample solution with the specified configuration (default: Release)

[group('Samples - Build and Test')]
build-s:
    echo "Building {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet build "{{ sample_solution_file }}" --configuration "{{ build_configuration }}"

# Runs tests for the sample solution with the specified configuration

[group('Samples - Build and Test')]
test-s:
    echo "Running tests for {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet test --solution "{{ sample_solution_file }}" --configuration "{{ build_configuration }}"

# -----------------------------------------------------------------------------
# Formatting
# -----------------------------------------------------------------------------

# Formats the code in the root folder

format:
    echo "Formatting {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    dotnet format "{{ root_folder }}"

lint:
    echo "Linting checking {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    dotnet csharpier check .

lint-fix:
    echo "Linting fixing {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    dotnet csharpier format .

# -----------------------------------------------------------------------------
# Versioning and Release
# -----------------------------------------------------------------------------

# Creates a new changeset to describe the changes in the current branch.
# Requires Bun.

[group('Versioning and Release')]
changeset:
    bun changeset

# Shows pending changesets and the version bump they imply.
# Requires Bun.

[group('Versioning and Release')]
changeset-status:
    bun changeset status

# Displays the current version of the project.
# Requires Bun.

[group('Versioning and Release')]
version:
    bun -e "console.log('Current Version: {{ GREEN }}' + require('./package.json').version + '{{ NORMAL }}')"

# Updates related samples and documentation to the new version.
# Requires Bun.

[group('Versioning and Release')]
update-version:
    bun -e "console.log('Update related samples and docs to new version: {{ GREEN }}' + require('./package.json').version + '{{ NORMAL }}')"
    git submodule update --init --recursive
    bun .build/update-version.ts

# -----------------------------------------------------------------------------
# System / Shell
# -----------------------------------------------------------------------------

# Opens the solution in the default associated application

[group('System/ Shell')]
vs:
    echo "Opening {{ BLUE }}{{ solution_file }}{{ NORMAL }}..."
    open "{{ solution_file }}"

# Open the solution in Visual Studio/ Registered application
[group('Utilities')]
vs-pipeline:
    open {{ pipeline_solution }}

# Opens the root folder in Visual Studio Code

[group('System/ Shell')]
code:
    echo "Opening {{ BLUE }}Visual Studio Code{{ NORMAL }}..."
    code "{{ root_folder }}"

# Opens the sample solution in the default associated application

[group('System/ Shell')]
vs-s:
    echo "Opening {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }}..."
    open "{{ sample_solution_file }}"

# -----------------------------------------------------------------------------
# Benchmarking
# -----------------------------------------------------------------------------

# Runs the full benchmark suite

[group('Benchmarking')]
benchmark:
    echo "Running benchmarks for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet run --project "{{ benchmark_solution }}" --configuration "{{ build_configuration }}" --framework net10.0

# Runs a quick single-runtime (.NET 10.0) benchmark with reduced iterations
# for development validation.
#
# Uses BenchmarkDotNet's short job:
#   - 1 launch
#   - 3 warmups
#   - 3 iterations
#
# Use `just benchmark` for the full multi-runtime suite.

[group('Benchmarking')]
benchmark-quick:
    echo "Running quick benchmarks (net10.0, short job) for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet run --project "{{ benchmark_solution }}" --configuration "{{ build_configuration }}" --framework net10.0 -- --job short --runtimes net10.0

# Runs benchmarks and reminds you to update performance documentation

[group('Benchmarking')]
benchmark-docs: benchmark
    echo ""
    echo "{{ GREEN }}Benchmarks complete.{{ NORMAL }} Results are in {{ BLUE }}BenchmarkDotNet.Artifacts/results/{{ NORMAL }}."
    echo ""
    echo "{{ YELLOW }}Next steps - update performance documentation:{{ NORMAL }}"
    echo "  1. Open {{ BLUE }}README.md{{ NORMAL }} and update the {{ YELLOW }}## Performance{{ NORMAL }} section:"
    echo "     - Activities:   use {{ BLUE }}*ActivityBenchmarks*-report-github.md{{ NORMAL }}"
    echo "     - Logging:      use {{ BLUE }}*LoggerBenchmarks*-report-github.md{{ NORMAL }}"
    echo "     - Multi-target: use {{ BLUE }}*MultiTarget*-report-github.md{{ NORMAL }}"
    echo "     - Metrics:      use {{ BLUE }}*MetricsBenchmarks*-report-github.md{{ NORMAL }}"
    echo "  2. Regenerate {{ BLUE }}PERFORMANCE.md{{ NORMAL }} from all six *-report-github.md files."
    echo "  3. Update the environment header (machine / SDK / runtime versions)."
    echo ""
    echo "  See {{ BLUE }}.github/copilot-instructions.md{{ NORMAL }} - Benchmarking for full instructions."
