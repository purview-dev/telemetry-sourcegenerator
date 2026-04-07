set quiet := true
set windows-shell := ["pwsh", "-NoProfile", "-Command"]

root_folder := "./src/"
solution_file := root_folder + "Purview.Telemetry.SourceGenerator.slnx"
test_solution := solution_file
configuration := "Release"
sample_solution_file := "./samples/SampleApp/SampleApp.slnx"
artifact_folder := "p:/sync-projects/.local-nuget/"
benchmark_solution := "./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj"

# Displays the list of available commands
[private]
default:
    @just --list

# Builds the solution with the specified configuration (default: Release)
[group('Build and Test')]
build:
    @Write-Host "Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet build "{{ solution_file }}" --configuration "{{ configuration }}"

# Runs tests for the solution with the specified configuration (default: Release)
[group('Build and Test')]
test:
    @Write-Host "Running tests for {{ BLUE }}{{ test_solution }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet test --solution "{{ test_solution }}" --configuration "{{ configuration }}"

# Builds the sample solution with the specified configuration (default: Release)
[group('Samples - Build and Test')]
build-s:
    @Write-Host "Building {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet build "{{ sample_solution_file }}" --configuration "{{ configuration }}"

# Runs tests for the sample solution with the specified configuration (default: Release)
[group('Samples - Build and Test')]
test-s:
    @Write-Host "Running tests for {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet test --solution "{{ sample_solution_file }}" --configuration "{{ configuration }}"

# Creates a new release (final or prerelease) using bun
[group('Versioning and Release')]
release-final:
    @Write-Host "Committing the changes and creating a new release..."
    @bun release

# Creates a new prerelease using bun
[group('Versioning and Release')]
release-pre:
    @Write-Host "Committing the changes and creating a new release..."
    @bun release -- --prerelease prerelease

# Packs the source generator into a NuGet package and outputs it to the specified folder
[group('Build and Test')]
pack: update-version build-pack

# Formats the code in the root folder
format:
    @Write-Host "Formatting {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    @dotnet format "{{ root_folder }}"

# Opens the solution file in Visual Studio
[group('System/ Shell')]
vs:
    @Write-Host "Opening {{ BLUE }}{{ solution_file }}{{ NORMAL }} in {{ YELLOW }}Visual Studio{{ NORMAL }}..."
    @start "{{ solution_file }}"

# Opens the root folder in Visual Studio Code
[group('System/ Shell')]
code:
    @Write-Host "Opening {{ BLUE }}Visual Studio Code{{ NORMAL }}..."
    @code "{{ root_folder }}"

# Opens the sample solution file in Visual Studio
[group('System/ Shell')]
vs-s:
    @Write-Host "Opening {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} in {{ YELLOW }}Visual Studio{{ NORMAL }}..."
    @start "{{ sample_solution_file }}"

# Displays the current version of the project (requires bun)
[group('Versioning and Release')]
version:
    @bun -e "console.log('Current Version: {{ GREEN }}' + require('./package.json').version + '{{ NORMAL }}')"

# Updates related samples and docs to new version (requires bun)
[group('Versioning and Release')]
update-version:
    @bun -e "console.log('Update related samples and docs to new version: {{ GREEN }}' + require('./package.json').version + '{{ NORMAL }}')"
    @git submodule update --init --recursive
    @bun .build/update-version.ts

# Packs the source generator into a NuGet package and outputs it to the specified folder, including version, branch, commit, and copyright year information in the package metadata
[group('Build and Test')]
build-pack:
    @bun -e "const version = require('./package.json').version; console.log('Packing {{ BLUE }}Source Generator{{ NORMAL }} with {{ YELLOW }}' + version + '{{ NORMAL }}...');"
    @Write-Host "  Configuration:   {{ GREEN }}{{ configuration }}{{ NORMAL }}"
    @bun -e "const exec = require('child_process').execSync; console.log('  Branch:          {{ GREEN }}' + exec('git rev-parse --abbrev-ref HEAD').toString().trim() + '{{ NORMAL }}');"
    @bun -e "const exec = require('child_process').execSync; console.log('  Commit:          {{ GREEN }}' + exec('git rev-parse HEAD').toString().trim() + '{{ NORMAL }}');"
    @bun -e "console.log('  Copyright Year:  {{ GREEN }}' + new Date().getFullYear() + '{{ NORMAL }}');"
    @Write-Host "  Output Folder:   {{ GREEN }}{{ artifact_folder }}{{ NORMAL }}"
    @bun -e "const version = require('./package.json').version; const exec = require('child_process').execSync; const branch = exec('git rev-parse --abbrev-ref HEAD').toString().trim(); const commit = exec('git rev-parse HEAD').toString().trim(); const year = new Date().getFullYear(); const cmd = 'dotnet pack \"{{ root_folder }}Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.csproj\" --configuration \"{{ configuration }}\" --output \"{{ artifact_folder }}\" --include-symbols --property:Version=\"' + version + '\" --property:RepositoryBranch=\"' + branch + '\" --property:RepositoryCommit=\"' + commit + '\" --property:COPYRIGHT_YEAR=\"' + year + '\"'; exec(cmd, {stdio: 'inherit'});"

[group('Benchmarking')]
benchmark:
    @Write-Host "Running benchmarks for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet run --project "{{ benchmark_solution }}" --configuration "{{ configuration }}" --framework net10.0

# Runs a quick single-runtime (.NET 10.0) benchmark with reduced iterations — for development validation.
# Uses BenchmarkDotNet's short job (1 launch, 3 warmup, 3 iterations). Typical runtime: 5–15 min.

# Use `just benchmark` for the full multi-runtime overnight suite.
[group('Benchmarking')]
benchmark-quick:
    @Write-Host "Running quick benchmarks (net10.0, short job) for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }}..."
    @dotnet run --project "{{ benchmark_solution }}" --configuration "{{ configuration }}" --framework net10.0 -- --job short --runtimes net10.0

# Runs benchmarks and reminds you to update performance documentation
[group('Benchmarking')]
benchmark-docs: benchmark
    @Write-Host ""
    @Write-Host "{{ GREEN }}Benchmarks complete.{{ NORMAL }} Results are in {{ BLUE }}BenchmarkDotNet.Artifacts/results/{{ NORMAL }}."
    @Write-Host ""
    @Write-Host "{{ YELLOW }}Next steps — update performance documentation:{{ NORMAL }}"
    @Write-Host "  1. Open {{ BLUE }}README.md{{ NORMAL }} and update the {{ YELLOW }}## Performance{{ NORMAL }} section:"
    @Write-Host "     - Activities:   use {{ BLUE }}*ActivityBenchmarks*-report-github.md{{ NORMAL }}"
    @Write-Host "     - Logging:      use {{ BLUE }}*LoggerBenchmarks*-report-github.md{{ NORMAL }}"
    @Write-Host "     - Multi-target: use {{ BLUE }}*MultiTarget*-report-github.md{{ NORMAL }}"
    @Write-Host "     - Metrics:      use {{ BLUE }}*MetricsBenchmarks*-report-github.md{{ NORMAL }}"
    @Write-Host "  2. Regenerate {{ BLUE }}PERFORMANCE.md{{ NORMAL }} from all six *-report-github.md files."
    @Write-Host "  3. Update the environment header (machine / SDK / runtime versions)."
    @Write-Host ""
    @Write-Host "  See {{ BLUE }}.github/copilot-instructions.md{{ NORMAL }} § Benchmarking for full instructions."
