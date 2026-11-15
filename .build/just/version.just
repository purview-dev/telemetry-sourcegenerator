# The package version, from package.json: the single source of truth Purview.BuildSdk versions every
# project and package from.

version_file := "package.json"

# Read by just itself rather than by spawning bun, so `just --list` needs no Node tooling.
current_version := if path_exists(version_file) == "true" { replace_regex(read(version_file), '(?s).*?"version"\s*:\s*"([^"]*)".*', '$1') } else { "0.0.0" }

# Display the current version, from package.json
[group('Version')]
version:
    echo "==> Current version: {{ GREEN }}{{ current_version }}{{ NORMAL }} (from {{ version_file }}, stamped into every build by Purview.BuildSdk)"

# Bump one part of the version in package.json - major, minor or patch - resetting the parts after it
#
#   just bump minor              # 2.0.1 -> 2.1.0
#   just bump patch --dry-run    # print the new version, write nothing
#
# A pre-release suffix is dropped. Requires Bun. Afterwards it runs `_after-bump`, which does nothing
# unless the root Justfile overrides it to refresh whatever else pins the version.
[doc('Bump one part of the version in package.json - major, minor or patch - resetting the parts after it')]
[arg("part", pattern="major|minor|patch")]
[group('Version')]
bump part *args: && _after-bump
    bun "{{ source_directory() / 'bump-version.ts' }}" "{{ part }}" "{{ version_file }}" {{ args }}

# Runs after a successful `bump`. Override it in the root Justfile, e.g. `_after-bump: version-sync`.
_after-bump:
