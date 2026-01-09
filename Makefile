include .build/common.mk

# Makefile for Purview.Telemetry.SourceGenerator
# ---------------------------------------------
# This Makefile is used to automate the build, test, and release process for the Purview.Telemetry.SourceGenerator project.
# Type `make` to see a list of the available targets.
# Note, this makes use of:
# - bun: https://bun.sh/
# - dotnet: https://dot.net/
# - git: https://git-scm.com/
# ---------------------------------------------

# Variables
ROOT_FOLDER = ./src/
SOLUTION_FILE = $(ROOT_FOLDER)Purview.Telemetry.SourceGenerator.slnx
TEST_PROJECT = $(ROOT_FOLDER)Purview.Telemetry.SourceGenerator.slnx
CONFIGURATION = Release

SAMPLE_SOLUTION_FILE = ./samples/SampleApp/SampleApp.slnx

PACK_VERSION := $(shell bun -e 'console.log(require("./package.json").version)')
GIT_BRANCH := $(shell git rev-parse --abbrev-ref HEAD)
GIT_COMMIT := $(shell git rev-parse HEAD)
COPYRIGHT_YEAR := $(shell bun -e 'console.log(new Date().getFullYear().toString())')

ARTIFACT_FOLDER = p:/sync-projects/.local-nuget/

# Common variables
COLOUR_RESET  = \033[0m

COLOUR_BLACK  = \033[30m
COLOUR_RED    = \033[31m
COLOUR_GREEN  = \033[32m
COLOUR_ORANGE = \033[33m
COLOUR_BLUE   = \033[34m
COLOUR_PURPLE = \033[35m
COLOUR_CYAN   = \033[36m
COLOUR_WHITE  = \033[37m
COLOUR_GREY   = \033[90m

# Targets
build: ## Builds the project.
	@echo -e "Building $(COLOUR_BLUE)$(SOLUTION_FILE)$(COLOUR_RESET) with $(COLOUR_ORANGE)$(CONFIGURATION)$(COLOUR_RESET)..."
	@dotnet build $(SOLUTION_FILE) --configuration $(CONFIGURATION)

test: ## Runs the tests for the project.
	@echo -e "Running tests for $(COLOUR_BLUE)$(TEST_PROJECT)$(COLOUR_RESET) with $(COLOUR_ORANGE)$(CONFIGURATION)$(COLOUR_RESET)..."
	@dotnet test --solution $(TEST_PROJECT) --configuration $(CONFIGURATION)

release-final: ## Creates a new release, e.g. v3.0.1.
	@echo -e "Committing the changes and creating a new release..."
	@bun release

release-pre: ## Creates a new pre-release, e.g. v3.0.1-prerelease.1.
	@echo -e "Committing the changes and creating a new release..."
	@bun release -- --prerelease prerelease

pack: update-version build-pack ## Packs the project into a nuget package using PACK_VERSION argument.
	
format: ## Formats the code according to the rules of the src/.editorconfig file.
	@echo -e "Formatting $(COLOUR_BLUE)$(SOLUTION_FILE)$(COLOUR_RESET)..."
	@dotnet format $(ROOT_FOLDER)

vs: ## Opens the project in Visual Studio.
	@echo -e "Opening $(COLOUR_BLUE)$(SOLUTION_FILE)$(COLOUR_RESET) in $(COLOUR_ORANGE)Visual Studio$(COLOUR_RESET)..."
	@start "$(SOLUTION_FILE)"

code: ## Opens the project in Visual Studio Code.
	@echo -e "Opening $(COLOUR_BLUE)Visual Studio Code$(COLOUR_RESET)..."
	@code .

vs-s: ## Opens the sample project in Visual Studio.
	@echo -e "Opening $(COLOUR_BLUE)$(SAMPLE_SOLUTION_FILE)$(COLOUR_RESET) in $(COLOUR_ORANGE)Visual Studio$(COLOUR_RESET)..."
	@start "$(SAMPLE_SOLUTION_FILE)"

version: ## Displays the current version of the project.
	@echo -e "Current Version: $(COLOUR_GREEN)$(PACK_VERSION)$(COLOUR_RESET)"
	
update-version: ## Update related samples and docs to new version.
	@echo -e "Update related samples and docs to new version: $(COLOUR_GREEN)$(PACK_VERSION)$(COLOUR_RESET)"
	@git submodule update --init --recursive
	@bun .build/update-version.js

# Internal targets
build-pack:
	@echo -e "Packing $(COLOUR_BLUE)Source Generator$(COLOUR_RESET) with $(COLOUR_ORANGE)$(PACK_VERSION)$(COLOUR_RESET)..."
	@echo -e "  Configuration:   $(COLOUR_GREEN)$(CONFIGURATION)$(COLOUR_RESET)"
	@echo -e "  Branch:          $(COLOUR_GREEN)$(GIT_BRANCH)$(COLOUR_RESET)"
	@echo -e "  Commit:          $(COLOUR_GREEN)$(GIT_COMMIT)$(COLOUR_RESET)"
	@echo -e "  Copyright Year:  $(COLOUR_GREEN)$(COPYRIGHT_YEAR)$(COLOUR_RESET)"
	@echo -e "  Output Folder:   $(COLOUR_GREEN)$(ARTIFACT_FOLDER)$(COLOUR_RESET)"

	@dotnet pack "$(ROOT_FOLDER)Purview.Telemetry.SourceGenerator/Purview.Telemetry.SourceGenerator.csproj" \
		--configuration "$(CONFIGURATION)" \
		--output "$(ARTIFACT_FOLDER)" \
		--include-symbols \
		--property:Version="$(PACK_VERSION)" \
		--property:RepositoryBranch="$(GIT_BRANCH)" \
		--property:RepositoryCommit="$(GIT_COMMIT)" \
		--property:COPYRIGHT_YEAR="$(COPYRIGHT_YEAR)"

# Testing targets - not ready for use yes.
act:
	@echo -e "Running $(COLOUR_BLUE)act$(COLOUR_RESET)..."
	act -P ubuntu-latest=-self-hosted
