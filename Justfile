root_folder := "./src/"
solution_file := root_folder + "Purview.Telemetry.SourceGenerator.slnx"
test_solution := solution_file
configuration := "Release"
sample_solution_file := "./samples/SampleApp/SampleApp.slnx"
artifact_folder := "p:/sync-projects/.local-nuget/"
benchmark_solution := justfile_directory() + "/benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj"

# Displays the list of available commands
default:
    @just --list

# Builds the solution with the specified configuration (default: Release)
[group('Build and Test')]
build:
    @echo -e "Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet build "{{ solution_file }}" --configuration "{{ configuration }}"

# Runs tests for the solution with the specified configuration (default: Release)
[group('Build and Test')]
test:
    @echo -e "Running tests for {{ BLUE }}{{ test_solution }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet test --solution "{{ test_solution }}" --configuration "{{ configuration }}"

# Builds the sample solution with the specified configuration (default: Release)
[group('Samples - Build and Test')]
build-s:
    @echo -e "Building {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet build "{{ sample_solution_file }}" --configuration "{{ configuration }}"

# Runs tests for the sample solution with the specified configuration (default: Release)
[group('Samples - Build and Test')]
test-s:
    @echo -e "Running tests for {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet test --solution "{{ sample_solution_file }}" --configuration "{{ configuration }}"

# Creates a new release (final or prerelease) using bun
[group('Versioning and Release')]
release-final:
    @echo -e "Committing the changes and creating a new release..."
    @bun release

# Creates a new prerelease using bun
[group('Versioning and Release')]
release-pre:
    @echo -e "Committing the changes and creating a new release..."
    @bun release -- --prerelease prerelease

# Packs the source generator into a NuGet package and outputs it to the specified folder
[group('Build and Test')]
pack: update-version build-pack

# Formats the code in the root folder
format:
    @echo -e "Formatting {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    @dotnet format "{{ root_folder }}"

# Opens the solution file in Visual Studio
[group('System/ Shell')]
vs:
    @echo -e "Opening {{ BLUE }}{{ solution_file }}{{ NORMAL }} in {{ YELLOW }}Visual Studio{{ NORMAL }}..."
    @start "{{ solution_file }}"

# Opens the root folder in Visual Studio Code
[group('System/ Shell')]
code:
    @echo -e "Opening {{ BLUE }}Visual Studio Code{{ NORMAL }}..."
    @code "{{ root_folder }}"

# Opens the sample solution file in Visual Studio
[group('System/ Shell')]
vs-s:
    @echo -e "Opening {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }} in {{ YELLOW }}Visual Studio{{ NORMAL }}..."
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
    @echo -e "  Configuration:   {{ GREEN }}{{ configuration }}{{ NORMAL }}"
    @bun -e "const exec = require('child_process').execSync; console.log('  Branch:          {{ GREEN }}' + exec('git rev-parse --abbrev-ref HEAD').toString().trim() + '{{ NORMAL }}');"
    @bun -e "const exec = require('child_process').execSync; console.log('  Commit:          {{ GREEN }}' + exec('git rev-parse HEAD').toString().trim() + '{{ NORMAL }}');"
    @bun -e "console.log('  Copyright Year:  {{ GREEN }}' + new Date().getFullYear() + '{{ NORMAL }}');"
    @echo -e "  Output Folder:   {{ GREEN }}{{ artifact_folder }}{{ NORMAL }}"
    @bun -e "const version = require('./package.json').version; const exec = require('child_process').execSync; const branch = exec('git rev-parse --abbrev-ref HEAD').toString().trim(); const commit = exec('git rev-parse HEAD').toString().trim(); const year = new Date().getFullYear(); const cmd = 'dotnet pack \"{{ root_folder }}Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.csproj\" --configuration \"{{ configuration }}\" --output \"{{ artifact_folder }}\" --include-symbols --property:Version=\"' + version + '\" --property:RepositoryBranch=\"' + branch + '\" --property:RepositoryCommit=\"' + commit + '\" --property:COPYRIGHT_YEAR=\"' + year + '\"'; exec(cmd, {stdio: 'inherit'});"

# Runs GitHub Actions locally using act (requires act to be installed and configured)
[group('CI/ CD')]
act:
    @echo -e "Running {{ BLUE }}act{{ NORMAL }}..."
    @act -P ubuntu-latest=-self-hosted

[group('Benchmarking')]
benchmark:
    @echo -e "Running benchmarks for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}{{ configuration }}{{ NORMAL }}..."
    @dotnet run --project "{{ benchmark_solution }}" --configuration "{{ configuration }}" --framework net10.0

# Runs a quick single-runtime (.NET 10.0) benchmark with reduced iterations — for development validation.
# Uses BenchmarkDotNet's short job (1 launch, 3 warmup, 3 iterations). Typical runtime: 5–15 min.
# Use `just benchmark` for the full multi-runtime overnight suite.
[group('Benchmarking')]
benchmark-quick:
    @echo -e "Running quick benchmarks (net10.0, short job) for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }}..."
    @dotnet run --project "{{ benchmark_solution }}" --configuration "{{ configuration }}" --framework net10.0 -- --job short --runtimes net10.0

# Runs benchmarks and reminds you to update performance documentation
[group('Benchmarking')]
benchmark-docs: benchmark
    @echo -e ""
    @echo -e "{{ GREEN }}Benchmarks complete.{{ NORMAL }} Results are in {{ BLUE }}BenchmarkDotNet.Artifacts/results/{{ NORMAL }}."
    @echo -e ""
    @echo -e "{{ YELLOW }}Next steps — update performance documentation:{{ NORMAL }}"
    @echo -e "  1. Open {{ BLUE }}README.md{{ NORMAL }} and update the {{ YELLOW }}## Performance{{ NORMAL }} section:"
    @echo -e "     - Activities:   use {{ BLUE }}*ActivityBenchmarks*-report-github.md{{ NORMAL }}"
    @echo -e "     - Logging:      use {{ BLUE }}*LoggerBenchmarks*-report-github.md{{ NORMAL }}"
    @echo -e "     - Multi-target: use {{ BLUE }}*MultiTarget*-report-github.md{{ NORMAL }}"
    @echo -e "     - Metrics:      use {{ BLUE }}*MetricsBenchmarks*-report-github.md{{ NORMAL }}"
    @echo -e "  2. Regenerate {{ BLUE }}PERFORMANCE.md{{ NORMAL }} from all six *-report-github.md files."
    @echo -e "  3. Update the environment header (machine / SDK / runtime versions)."
    @echo -e ""
    @echo -e "  See {{ BLUE }}.github/copilot-instructions.md{{ NORMAL }} § Benchmarking for full instructions."
