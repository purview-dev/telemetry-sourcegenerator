# Recipes for the Purview Telemetry Source Generator. Run `just` for the list.
#
# The shared recipes - restore, build, test, clean, pack, lint, scrub, the pipelines, the skill
# targets and the version - come from .build/just/, copied verbatim from the purview-buildsdk-justfile
# skill. Change them there, not here; this file holds what belongs to this repository.

set quiet

# Let a definition here override a module's default variable or no-op hook.
set allow-duplicate-variables
set allow-duplicate-recipes

import ".build/just/index.just"

# -----------------------------------------------------------------------------
# This repository
# -----------------------------------------------------------------------------

root_folder := "./src/"
solution_file := root_folder + "Telemetry.SourceGenerator.slnx"
build_configuration := "Debug"
artifacts_folder := "./artifacts/"

sample_solution_file := "./samples/SampleApp/SampleApp.slnx"
multiproject_sample_solution_file := "./samples/SampleApp.MultiProject/SampleApp.MultiProject.slnx"
multiproject_sample_project := "./samples/SampleApp.MultiProject/SampleApp.MultiProject.ConsoleApp"
benchmark_solution := "./benchmarks/Purview.Telemetry.Benchmarks/Purview.Telemetry.Benchmarks.csproj"

# Displays the list of available commands
[private]
default:
    just --list

# -----------------------------------------------------------------------------
# Build and Test
# -----------------------------------------------------------------------------

# Run the tests matching a tree-node filter (default: every test), across all four legs
#
# Overrides the module's recipe, which targets a single test project: this solution holds four test
# projects (net48, net10.0, net11.0 and Repository.UnitTests), so it is run as a solution. The
# Microsoft.Testing.Platform run-level switches then apply to the whole run:
#   --timeout                             caps total execution time, so a hung test application cannot stall CI
#   --maximum-failed-tests                stops at the first N failures instead of burning the whole run
#   --results-directory-layout per-module gives every project/TFM its own <project>/<tfm>_<rid> folder, so the
#                                         four legs can no longer overwrite one another's report files
# These are not VSTest switches: VSTest mode rejects them.
[doc('Run the tests matching a tree-node filter (default: every test), across all four legs')]
[group('Build and Test')]
test filter=test_filter *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ YELLOW }}{{ build_configuration }}{{ NORMAL }}) with filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test --solution "{{ solution_file }}" --configuration "{{ build_configuration }}" --treenode-filter "{{ filter }}" --timeout 30m --maximum-failed-tests 25 --results-directory-layout per-module {{ args }}

# Run the test solution against every declared dependency floor (`DependencyFloor=true`), proving the
# lowest common .NET 10 versions this package declares still compile and pass. The ordinary `test`
# recipe resolves the latest servicing patch of the same line, which is what a consumer actually gets.
#
# `Directory.Packages.props` holds one property per package family, so the floor is a switch rather than
# a second file, and this is the leg that proves it. `ReleaseGateTests` keeps the recipe and the PR job
# wired up, so a declared floor cannot drift away from the version that was tested.
[doc('Run the tests against every declared dependency floor (`DependencyFloor=true`)')]
[group('Build and Test')]
test-floor filter=test_filter *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} against the dependency floor with filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test --solution "{{ solution_file }}" --configuration "{{ build_configuration }}" --treenode-filter "{{ filter }}" --timeout 30m --maximum-failed-tests 25 --results-directory-layout per-module -p:DependencyFloor=true {{ args }}

# Pack the source generator, stamping the version, branch, commit and copyright year into the package
#
# Overrides the module's `dotnet pack` recipe: `.build/build-pack.ts` packs the one packable project
# with that metadata, and the version it packs is the one `update-version` just synced into the
# docs and samples. `just pipeline-pack-validate` is the gate that proves what it wrote.
[doc('Pack the source generator, stamping the version, branch, commit and copyright year into the package')]
[group('Build and Test')]
pack: update-version build-pack

# Packs the source generator into a NuGet package and outputs it to the
# specified folder, including version, branch, commit, and copyright year
# information in the package metadata.
[doc('Pack the source generator into a NuGet package with version, branch, commit and copyright metadata')]
[group('Build and Test')]
build-pack:
    bun -e "const version = require('./package.json').version; console.log('Packing {{ BLUE }}Source Generator{{ NORMAL }} with {{ YELLOW }}' + version + '{{ NORMAL }}...');"
    echo "  Configuration:   {{ GREEN }}{{ build_configuration }}{{ NORMAL }}"
    bun -e "const { execFileSync } = require('child_process'); console.log('  Branch:          {{ GREEN }}' + execFileSync('git', ['rev-parse', '--abbrev-ref', 'HEAD'], { encoding: 'utf8' }).trim() + '{{ NORMAL }}');"
    bun -e "const { execFileSync } = require('child_process'); console.log('  Commit:          {{ GREEN }}' + execFileSync('git', ['rev-parse', 'HEAD'], { encoding: 'utf8' }).trim() + '{{ NORMAL }}');"
    bun -e "console.log('  Copyright Year:  {{ GREEN }}' + new Date().getFullYear() + '{{ NORMAL }}');"
    echo "  Output Folder:   {{ GREEN }}{{ artifacts_folder }}{{ NORMAL }}"
    bun .build/build-pack.ts "{{ root_folder }}" "{{ build_configuration }}" "{{ artifacts_folder }}"

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

# Builds the multi-project sample solution with the specified configuration
[group('Samples - Build and Test')]
build-mp:
    echo "Building {{ BLUE }}{{ multiproject_sample_solution_file }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet build "{{ multiproject_sample_solution_file }}" --configuration "{{ build_configuration }}"

# Runs the multi-project sample, printing the aggregated telemetry names
[group('Samples - Build and Test')]
run-mp:
    echo "Running {{ BLUE }}{{ multiproject_sample_project }}{{ NORMAL }} with {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}..."
    dotnet run --project "{{ multiproject_sample_project }}" --configuration "{{ build_configuration }}"

# -----------------------------------------------------------------------------
# Pipeline gates
# -----------------------------------------------------------------------------

# Pack the generator to an isolated feed and cache, then build, publish and run throwaway consumers that reference
# the package only: a Native AOT console, a Blazor WebAssembly app (trimmed, plus RunAOTCompilation when wasm-tools is
# installed) and a Blazor Server host, each leak-scanned for compiler-host assemblies.
#
# The tests and samples reference the generator project; this is the gate that proves what ships. The CI
# `packed-consumers` job runs it for linux-x64. Purview.SourceGeneratorFramework and Purview.BuildSdk are restored
# from nuget.org, plus TELEMETRY_PRERELEASE_FEED or LOCAL_NUGET_FEED_PATH when set (or pass -PrereleaseFeed).
[doc('Pack the generator and prove the package works for Native AOT, Blazor WebAssembly and Blazor Server consumers')]
[group('Pipeline')]
smoke-packed-consumers *args:
    echo "==> Running the packed consumer gate"
    pwsh -NoProfile -File scripts/test-packed-consumers.ps1 {{ args }}

# Prove every PackValidation ForbiddenContent rule in .config/purview-build.json rejects a package: injects a matching
# file into scratch copies of the packed package and runs the real purview-build validation on them. Reads artifacts/,
# so pack first (`just pipeline-pack-validate`, which runs this recipe itself). `just pipeline-local-release` moves its
# packages to the local feed, so pass -PackagesFolder p:/_sync-projects/.local-nuget after it.
[doc('Prove every PackValidation ForbiddenContent rule rejects a package')]
[group('Pipeline')]
verify-pack-rules *args:
    echo "==> Proving the pack-validation forbidden-content rules"
    pwsh -NoProfile -File scripts/test-pack-validation-rules.ps1 {{ args }}

# Run the pipeline through pack and pack validation, then prove every ForbiddenContent rule rejects a package
#
# Overrides the module's recipe so that packing locally proves the rules in .config/purview-build.json
# without a second command. It reads artifacts/ - pass -Build:ArtifactsFolder if args moved this run's
# packages somewhere else.
[doc('Run the pipeline through pack and pack validation, then prove every ForbiddenContent rule rejects a package')]
[group('Pipeline')]
pipeline-pack-validate *args: ensure-tools
    echo "==> Running the pack and validate pipeline"
    {{ pipeline_tool }} --Build:RunPack=true --Build:ValidatePack=true --Release:Mode=None {{ args }}
    just verify-pack-rules

# Scan build or publish output for compiler-host assemblies (the generator, the refactorings,
# Purview.SourceGeneratorFramework, Roslyn) that must never reach an application.
[doc('Scan build or publish output for compiler-host assemblies')]
[group('Pipeline')]
scan-compiler-host-leaks +paths:
    pwsh -NoProfile -File scripts/assert-no-compiler-host-assemblies.ps1 -Path {{ paths }}

# -----------------------------------------------------------------------------
# Versioning
# -----------------------------------------------------------------------------

# Sync the version in package.json into the docs and samples
#
# Runs after every `just bump`, through `_after-bump` below, so a release needs only the bump. A
# hand-edited package.json still needs it run by hand, before `just pack`.
[doc('Sync the version in package.json into the docs and samples')]
[group('Version')]
update-version:
    echo "==> Updating related samples and docs to version {{ GREEN }}{{ current_version }}{{ NORMAL }}"
    git submodule update --init --recursive
    bun .build/update-version.ts

# Runs after a successful `just bump`: the version is pinned in the docs and samples as well.
_after-bump: update-version

# -----------------------------------------------------------------------------
# Utilities
# -----------------------------------------------------------------------------

# Format the C# with `dotnet format`
#
# CSharpier is the check that gates a commit (`just lint-check`); this is the IDE-style pass over the
# whole of src/, kept because editors here still defer to it.
[doc('Format the C# with `dotnet format`')]
[group('Utilities')]
format:
    echo "Formatting {{ BLUE }}{{ root_folder }}{{ NORMAL }}..."
    dotnet format "{{ root_folder }}"

# Opens the root folder in Visual Studio Code
[group('Utilities')]
code:
    echo "Opening {{ BLUE }}Visual Studio Code{{ NORMAL }}..."
    code "{{ root_folder }}"

# Opens the sample solution in the default associated application
[group('Utilities')]
vs-s:
    echo "Opening {{ BLUE }}{{ sample_solution_file }}{{ NORMAL }}..."
    open "{{ sample_solution_file }}"

# Opens the multiproject sample solution in the default associated application
[group('Utilities')]
vs-mp:
    echo "Opening {{ BLUE }}{{ multiproject_sample_solution_file }}{{ NORMAL }}..."
    open "{{ multiproject_sample_solution_file }}"

# -----------------------------------------------------------------------------
# Benchmarking
# -----------------------------------------------------------------------------

# Runs the full benchmark suite: .NET 10, .NET 11 and .NET Framework 4.8 in one invocation.
#
# Release is fixed rather than taken from `build_configuration`: BenchmarkDotNet numbers from a Debug build
# are not meaningful. Every job names its runtime explicitly (`RuntimeMoniker.Net10_0`, `Net11_0`, `Net48`),
# so one report carries all three runtimes and the launch framework only selects the BenchmarkDotNet host.
# `Net11_0` needs BenchmarkDotNet 0.16.0-preview.2 or later; there is no stable release with it yet.
[doc('Run the full benchmark suite for .NET 10, .NET 11 and .NET Framework 4.8 in one invocation')]
[group('Benchmarking')]
benchmark:
    echo "Running benchmarks for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}Release{{ NORMAL }}..."
    dotnet run --project "{{ benchmark_solution }}" --configuration Release --framework net11.0 -- --artifacts BenchmarkDotNet.Artifacts

# Runs a quick single-runtime (.NET 11) benchmark with reduced iterations
# for development validation.
#
# Uses BenchmarkDotNet's short job:
#   - 1 launch
#   - 3 warmups
#   - 3 iterations
#
# Its own artifacts folder keeps it from overwriting the full suite's report.
# Use `just benchmark` for the full three-runtime suite.
[doc('Run a quick single-runtime (.NET 11) benchmark with reduced iterations')]
[group('Benchmarking')]
benchmark-quick:
    echo "Running quick benchmarks (net11.0, short job) for {{ BLUE }}{{ benchmark_solution }}{{ NORMAL }} with {{ YELLOW }}Release{{ NORMAL }}..."
    dotnet run --project "{{ benchmark_solution }}" --configuration Release --framework net11.0 -- --job short --runtimes net11.0 --artifacts BenchmarkDotNet.Artifacts/quick

# Runs benchmarks and reminds you to update performance documentation
[group('Benchmarking')]
benchmark-docs: benchmark
    echo ""
    echo "{{ GREEN }}Benchmarks complete.{{ NORMAL }} Results are in {{ BLUE }}BenchmarkDotNet.Artifacts/results/{{ NORMAL }}."
    echo ""
    echo "{{ YELLOW }}Next steps - update performance documentation:{{ NORMAL }}"
    echo "  1. Update the {{ YELLOW }}## Performance{{ NORMAL }} section of {{ BLUE }}README.md{{ NORMAL }}:"
    echo "     - Activities:   use {{ BLUE }}*ActivityBenchmarks*-report-github.md{{ NORMAL }}"
    echo "     - Logging:      use {{ BLUE }}*LoggerBenchmarks*-report-github.md{{ NORMAL }}"
    echo "     - Multi-target: use {{ BLUE }}*MultiTarget*-report-github.md{{ NORMAL }}"
    echo "     - Metrics:      use {{ BLUE }}*MetricsBenchmarks*-report-github.md{{ NORMAL }}"
    echo "  2. Regenerate {{ BLUE }}docs/wiki/reference/Performance.md{{ NORMAL }} from all six *-report-github.md files, one section per family with a row per runtime."
    echo "  3. Update the environment header (machine / SDK / runtime versions) in both files."
    echo ""
    echo "  See {{ BLUE }}AGENTS.md{{ NORMAL }} - Benchmarking and performance documentation for the full rules."
