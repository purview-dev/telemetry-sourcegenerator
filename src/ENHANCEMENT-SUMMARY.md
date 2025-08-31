# Test Output Enhancement

This enhancement adds optional output generation capabilities to the Purview Telemetry Source Generator testing framework.

## What's New

- **Optional output generation**: Tests can now write generated source files to disk for inspection
- **Environment variable controlled**: Enable/disable via `PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true`
- **Customizable output directory**: Set location via `PURVIEW_TELEMETRY_OUTPUT_DIRECTORY`
- **Comprehensive content**: Includes generated sources, input sources, diagnostics, and metadata
- **Git ignored by default**: Output folders are automatically excluded from source control

## Files Added/Modified

### New Files
- `Configuration/TestOutputConfiguration.cs` - Configuration management for output features
- `Helpers/TestOutputWriter.cs` - Core functionality for writing test output
- `Examples/OutputGenerationExampleTests.cs` - Example tests demonstrating the feature
- `TEST-OUTPUT-GUIDE.md` - Comprehensive usage documentation
- `.gitignore-additions` - Git ignore patterns for output directories

### Modified Files
- `SourceGeneratorTestBase.cs` - Enhanced to support automatic output generation with caller member names
- `TelemetrySourceGeneratorMultiTargetTests.cs` - Updated to demonstrate output capabilities
- `Purview.Telemetry.SourceGenerator.IntegrationTests.csproj` - Added required packages and documentation files
- `Directory.Packages.props` - Added System.Text.Json for metadata serialization

## Key Features

1. **Zero Impact When Disabled**: No performance overhead when feature is not enabled
2. **Test-Specific Directories**: Each test gets its own organized output folder
3. **Rich Metadata**: JSON metadata with timestamps, counts, and environment information
4. **Diagnostic Capture**: Compilation warnings and errors are saved to text files
5. **Input/Output Comparison**: Both source inputs and generated outputs are preserved
6. **Automatic Cleanup**: Old output is cleaned before new test runs

## Usage Examples

```bash
# Enable output generation
export PURVIEW_TELEMETRY_OUTPUT_GENERATED_FILES=true

# Run tests
make test

# Inspect generated content
ls generated-output/Generate_GivenBasicMultiTargetMethod_GeneratesCorrectly/
```

## Benefits for Development

- **Debugging**: Easily inspect what the source generator produces
- **Learning**: Understand how different inputs create different outputs  
- **Troubleshooting**: Diagnose generation issues with full context
- **Documentation**: Generated README files explain the test results
- **CI Integration**: Optional artifact collection for build systems

## Integration

The enhancement integrates seamlessly with the existing test infrastructure:
- Uses the same `GenerateAsync()` methods
- Automatic test name detection via `[CallerMemberName]`
- Works with all existing test patterns and verification
- Respects existing snapshot testing workflows

This enhancement maintains full backward compatibility while adding powerful debugging capabilities for source generator development.