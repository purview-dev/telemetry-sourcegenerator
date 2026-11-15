# Optional: Purview.Build pipeline runs that pack, validate and publish NuGet packages. Copy this file
# into the module folder only when the repository packs packages; the barrel imports it if present.
#
# Pack validation reads PackValidation in .config/purview-build.json. Its RequiredContent lists every
# file each package must contain, and a packed file it does not list fails the run - so a new file
# under a package's Sdk/, build/ or buildTransitive/ needs a RequiredContent entry too.

# Run the pipeline through pack and pack validation (restore, build, lint, tests, pack, validate), publishing nothing
[group('Pipeline')]
pipeline-pack-validate *args: ensure-tools
    echo "==> Running the pack and validate pipeline"
    {{ pipeline_tool }} --Build:RunPack=true --Build:ValidatePack=true --Release:Mode=None {{ args }}

# Run the release pipeline, publishing to a local NuGet feed (restore, build, lint, tests, pack, validate, publish)
#
# `just` runs recipes through sh, which strips backslashes from unquoted arguments. Set the
# LOCAL_NUGET_FEED_PATH environment variable, or use forward slashes:
#
#   just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
[doc('Run the release pipeline, publishing to a local NuGet feed (restore, build, lint, tests, pack, validate, publish)')]
[group('Pipeline')]
pipeline-local-release *args: ensure-tools
    echo "==> Running the local release pipeline"
    {{ pipeline_tool }} --Release:Mode=LocalNuGet {{ args }}

# Run the release pipeline, publishing to NuGet (restore, build, lint, tests, pack, validate, publish, GitHub release)
[group('Pipeline')]
pipeline-release *args: ensure-tools
    echo "==> Running the release pipeline"
    {{ pipeline_tool }} --Release:Mode=NuGet {{ args }}
