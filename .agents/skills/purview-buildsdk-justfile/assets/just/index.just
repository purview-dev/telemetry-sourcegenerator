# The barrel for the shared just modules. The root Justfile imports this file, and nothing else:
#
#   set allow-duplicate-variables
#   set allow-duplicate-recipes
#
#   import ".build/just/index.just"
#
# Every module gives its variables a default. The root Justfile overrides one by defining the same
# name - `solution_file := "src/Acme.slnx"` - and overrides a no-op hook such as `_after-bump` the
# same way. Both settings are what let a root definition win; without them a duplicate is an error.
#
# The required modules are imported unconditionally. The optional ones use `import?`, so a module is
# switched on by copying its file into this folder and off by deleting it: this file never changes.
#
# These files are copied verbatim from the purview-buildsdk-justfile skill. Change a recipe there,
# not here, and put anything specific to one repository in its root Justfile.

# Telemetry opt-outs and the .NET CLI speed-ups, exported to every recipe.
import "env.just"

# version, bump
import "version.just"

# ensure-tools, restore, build, clean, test, pack
import "dotnet.just"

# lint-check, lint-fix, vs, scrub
import "utilities.just"

# pipeline-pr, pipeline-build, pipeline-tests - local runs of the Purview.Build pipeline
import "purview-build.just"

# skills-* - the Purview.BuildSdk agent-skill targets
import "skills.just"

# Optional. pipeline-pack-validate, pipeline-local-release, pipeline-release - for a repository that
# packs NuGet packages.
import? "purview-build-nuget.just"

# Optional. docs, docs-install, docs-check - for a pm-docs-site project site (purview-dev/pm-skills).
import? "docs.just"
