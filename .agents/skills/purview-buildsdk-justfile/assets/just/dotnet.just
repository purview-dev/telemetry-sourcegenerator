# .NET recipes: tools, restore, build, clean, test and pack, all against one solution.
#
# Override these in the root Justfile. `solution_file` is the one nearly every repository sets; "."
# lets dotnet find the single solution or project in the repository root.

solution_file := "."
build_configuration := "Debug"
artifacts_folder := "./artifacts"

# Every test. A TUnit/MTP tree-node filter: /assembly/namespace/class/test.
test_filter := "/*/*/*/*/"

# Restore the repository's .NET local tools (.config/dotnet-tools.json)
[group('Build and Test')]
ensure-tools:
    dotnet tool restore

# Restore NuGet packages for the solution
[group('Build and Test')]
restore *args:
    echo "==> Restoring {{ BLUE }}{{ solution_file }}{{ NORMAL }}"
    dotnet restore "{{ solution_file }}" {{ args }}

# Build the solution with build_configuration (default: Debug)
[group('Build and Test')]
build *args:
    echo "==> Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build "{{ solution_file }}" --configuration "{{ build_configuration }}" {{ args }}

# Clean the solution with build_configuration (default: Debug)
[group('Build and Test')]
clean *args:
    echo "==> Cleaning {{ BLUE }}{{ solution_file }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet clean "{{ solution_file }}" --configuration "{{ build_configuration }}" {{ args }}

# Run the tests matching a tree-node filter (default: every test)
#
#   just test "/*/*/*/*[Category=Unit]"
#   just test "/*/*/ProjectHarnessTests/*"
#
# Exit code 8 - "no test ran" - is ignored, so a filter that matches nothing in one test project does
# not fail the run for the others.
[doc('Run the tests matching a tree-node filter (default: every test)')]
[group('Build and Test')]
test filter=test_filter *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ YELLOW }}{{ build_configuration }}{{ NORMAL }}) with filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test --project "{{ solution_file }}" --configuration "{{ build_configuration }}" --treenode-filter "{{ filter }}" --ignore-exit-code 8 {{ args }}

# Pack every packable project into artifacts_folder (default: ./artifacts)
[group('Build and Test')]
pack artifact_folder=artifacts_folder *args:
    echo "==> Packing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) to {{ YELLOW }}{{ artifact_folder }}{{ NORMAL }}"
    dotnet pack "{{ solution_file }}" --configuration "{{ build_configuration }}" --output "{{ artifact_folder }}" {{ args }}
