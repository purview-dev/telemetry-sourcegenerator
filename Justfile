#set shell := ["bash", "-uc"]

root_folder := "./src/"
solution_file := root_folder + "Purview.Telemetry.SourceGenerator.slnx"
test_solution := solution_file
configuration := "Release"
sample_solution_file := "./samples/SampleApp/SampleApp.slnx"
artifact_folder := "p:/sync-projects/.local-nuget/"

# Colour codes (ANSI)

esc := "\u{001b}"
colour_reset := esc + "[0m"
colour_green := esc + "[32m"
colour_orange := esc + "[33m"
colour_blue := esc + "[34m"

default:
    @just --list

build:
    @echo -e "Building {{ colour_blue }}{{ solution_file }}{{ colour_reset }} with {{ colour_orange }}{{ configuration }}{{ colour_reset }}..."
    @dotnet build "{{ solution_file }}" --configuration "{{ configuration }}"

test:
    @echo -e "Running tests for {{ colour_blue }}{{ test_solution }}{{ colour_reset }} with {{ colour_orange }}{{ configuration }}{{ colour_reset }}..."
    @dotnet test --solution "{{ test_solution }}" --configuration "{{ configuration }}"

release-final:
    @echo -e "Committing the changes and creating a new release..."
    @bun release

release-pre:
    @echo -e "Committing the changes and creating a new release..."
    @bun release -- --prerelease prerelease

pack: update-version build-pack

format:
    @echo -e "Formatting {{ colour_blue }}{{ root_folder }}{{ colour_reset }}..."
    @dotnet format "{{ root_folder }}"

vs:
    @echo -e "Opening {{ colour_blue }}{{ solution_file }}{{ colour_reset }} in {{ colour_orange }}Visual Studio{{ colour_reset }}..."
    @start "" "{{ solution_file }}"

code:
    @echo -e "Opening {{ colour_blue }}Visual Studio Code{{ colour_reset }}..."
    @code .

vs-s:
    @echo -e "Opening {{ colour_blue }}{{ sample_solution_file }}{{ colour_reset }} in {{ colour_orange }}Visual Studio{{ colour_reset }}..."
    @start "" "{{ sample_solution_file }}"

# Displays the current version of the project (requires bun)
version:
    @bun -e "console.log('Current Version: {{ colour_green }}' + require('./package.json').version + '{{ colour_reset }}')"

update-version:
    @bun -e "console.log('Update related samples and docs to new version: {{ colour_green }}' + require('./package.json').version + '{{ colour_reset }}')"
    @git submodule update --init --recursive
    @bun .build/update-version.js

build-pack:
    @pack_version="$$(bun -e 'console.log(require("./package.json").version)')"; \
     git_branch="$$(git rev-parse --abbrev-ref HEAD)"; \
     git_commit="$$(git rev-parse HEAD)"; \
     copyright_year="$$(date +%Y)"; \
     echo -e "Packing {{ colour_blue }}Source Generator{{ colour_reset }} with {{ colour_orange }}$${pack_version}{{ colour_reset }}..."; \
     echo -e "  Configuration:   {{ colour_green }}{{ configuration }}{{ colour_reset }}"; \
     echo -e "  Branch:          {{ colour_green }}$${git_branch}{{ colour_reset }}"; \
     echo -e "  Commit:          {{ colour_green }}$${git_commit}{{ colour_reset }}"; \
     echo -e "  Copyright Year:  {{ colour_green }}$${copyright_year}{{ colour_reset }}"; \
     echo -e "  Output Folder:   {{ colour_green }}{{ artifact_folder }}{{ colour_reset }}"; \
     dotnet pack "{{ root_folder }}Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.csproj" \
       --configuration "{{ configuration }}" \
       --output "{{ artifact_folder }}" \
       --include-symbols \
       --property:Version="$${pack_version}" \
       --property:RepositoryBranch="$${git_branch}" \
       --property:RepositoryCommit="$${git_commit}" \
       --property:COPYRIGHT_YEAR="$${copyright_year}"

act:
    @echo -e "Running {{ colour_blue }}act{{ colour_reset }}..."
    @act -P ubuntu-latest=-self-hosted
