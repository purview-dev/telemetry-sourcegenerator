# Integration Tests

This directory contains integration tests for the Purview Telemetry Source Generator.

## Running Tests

### Standard Test Run (with snapshot verification)

```bash
# Using Makefile
make test

# Or directly with dotnet (note: .NET 10 SDK has issues with VSTest)
dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release
```

### Test Run Without Snapshot Verification (Compilation Only)

When you need to verify that code generation and compilation succeed without comparing against snapshots:

```bash
# Using Makefile
make test-no-verify

# Or using environment variable directly
PURVIEW_IGNORE_VERIFY=true dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release

# On Windows with PowerShell
$env:PURVIEW_IGNORE_VERIFY="true"; dotnet test ./src/Purview.Telemetry.SourceGenerator.slnx --configuration Release

# Or run test executable directly (works around .NET 10 SDK VSTest issues)
$env:PURVIEW_IGNORE_VERIFY="true"; .\src\Purview.Telemetry.SourceGenerator.IntegrationTests\bin\Release\net10.0\Purview.Telemetry.SourceGenerator.IntegrationTests.exe
```

### When to Use `--IgnoreVerify` (PURVIEW_IGNORE_VERIFY)

Use this option when:

- **After major refactoring** - Verify compilation succeeds before accepting hundreds of snapshot changes
- **Template changes** - Test that generated code compiles after modifying template files
- **Quick validation** - Get fast feedback (10s vs 40s) that code generation works
- **CI debugging** - Isolate compilation issues from snapshot comparison issues

The option allows tests to:
✅ Generate code from templates
✅ Compile generated code
✅ Validate diagnostics and errors
❌ Skip snapshot verification (Verify library)

This is useful after namespace changes or other refactorings that affect all snapshots predictably.

## Test Structure

- **Test Files**: `*Tests.cs` files containing test cases
- **Snapshots**: `Snapshots/*.verified.cs` files containing expected generated output
- **Received Files**: `Snapshots/*.received.cs` files show actual output when tests fail
- **Base Classes**: `SourceGeneratorTestBase.cs` provides common test infrastructure

## Snapshot Testing

Tests use the [Verify](https://github.com/VerifyTests/Verify) library for snapshot testing:

- Generated code is compared against `.verified.cs` snapshot files
- Differences create `.received.cs` files for review
- Template files (attributes) are auto-verified and update automatically
- Use `PURVIEW_IGNORE_VERIFY=true` to skip snapshot comparison entirely
