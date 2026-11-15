# Formatting, opening the solution, and scrubbing the build output.

csharpier := "dotnet csharpier"

# Check formatting with CSharpier
[group('Utilities')]
lint-check:
    {{ csharpier }} check .

# Format the code with CSharpier
[group('Utilities')]
lint-fix:
    {{ csharpier }} format .

# Open the solution in Visual Studio, or whichever application is registered for it
[group('Utilities')]
[windows]
vs:
    start "" "{{ solution_file }}"

# Open the solution in whichever application is registered for it
[group('Utilities')]
[macos]
vs:
    open "{{ solution_file }}"

# Open the solution in whichever application is registered for it
[group('Utilities')]
[linux]
vs:
    xdg-open "{{ solution_file }}"

# Delete every bin/, obj/ and artifacts_folder, stop the build servers, and restore from scratch
#
# The build servers go first: the MSBuild server and the compiler server hold files open under obj/,
# which on Windows stops them being deleted. node_modules/ and .git/ are never searched.
[doc('Delete every bin/, obj/ and artifacts_folder, stop the build servers, and restore from scratch')]
[group('Utilities')]
scrub:
    echo "==> Scrubbing build output and stopping the build servers"
    dotnet build-server shutdown
    find . \( -name node_modules -o -name .git \) -prune -o -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
    rm -rf "{{ artifacts_folder }}"
    just restore --force-evaluate
