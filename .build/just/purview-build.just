# Local runs of the Purview.Build pipeline - the same `dotnet purview-build` tool, and the same
# .config/purview-build.json, that CI runs. Every Build stage - tests, lint, pack and pack validation -
# runs unless that file or an argument turns it off. Anything after the recipe name is passed to the
# tool, so a setting can be overridden for one run: `just pipeline-pr --Build:TestFilter="/*/*/*/*[Category=Unit]"`.

pipeline_tool := "dotnet purview-build"

# Run the PR pipeline (restore, build, lint, tests, pack, validate), as configured
[group('Pipeline')]
pipeline-pr *args: ensure-tools
    echo "==> Running the PR pipeline"
    {{ pipeline_tool }} {{ args }}

# Run the build pipeline (restore, build, lint, pack, validate), without tests
[group('Pipeline')]
pipeline-build *args: ensure-tools
    echo "==> Running the build pipeline"
    {{ pipeline_tool }} --Build:RunTests=false --Release:Mode=None {{ args }}

# Run the pipeline with tests forced on and release off (restore, build, lint, tests, pack, validate)
[group('Pipeline')]
pipeline-tests *args: ensure-tools
    echo "==> Running the tests pipeline"
    {{ pipeline_tool }} --Build:RunTests=true --Release:Mode=None {{ args }}
