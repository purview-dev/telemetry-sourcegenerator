using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Purview.Telemetry.SourceGenerator;

// Simple validation program to test multi-target functionality without hanging
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Multi-Target Validation ===");
        
        const string testSource = """
            using System;
            using Microsoft.Extensions.Logging;
            using System.Diagnostics;

            [assembly: Purview.Telemetry.EnableMultiTargetGeneration]

            namespace Test;

            [Purview.Telemetry.TelemetryGeneration]
            public partial interface ITestService
            {
                [Purview.Telemetry.Telemetry(
                    GenerateActivity = true,
                    GenerateLogging = true,
                    ActivityName = "test_operation",
                    LogMessage = "Test operation executed"
                )]
                void TestOperation(string userId, int count);
            }
            """;

        try
        {
            var generator = new TelemetrySourceGenerator();
            var compilation = CreateCompilation(testSource);
            
            var driver = CSharpGeneratorDriver.Create(generator);
            var result = driver.RunGeneratorsAndUpdateCompilation(
                compilation, 
                out var outputCompilation, 
                out var diagnostics);

            Console.WriteLine($"Generated sources: {result.GetRunResult().Results.SelectMany(r => r.GeneratedSources).Count()}");
            Console.WriteLine($"Diagnostics: {diagnostics.Length}");
            
            if (diagnostics.Length > 0)
            {
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine($"  {diagnostic.Severity}: {diagnostic.GetMessage()}");
                }
            }

            var generatedSources = result.GetRunResult().Results.SelectMany(r => r.GeneratedSources).ToArray();
            foreach (var source in generatedSources)
            {
                Console.WriteLine($"Generated: {source.HintName}");
            }

            // Look specifically for multi-target generated files
            var multiTargetSources = generatedSources.Where(s => s.HintName.Contains("MultiTarget")).ToArray();
            if (multiTargetSources.Length > 0)
            {
                Console.WriteLine("Multi-target generation SUCCESS!");
                foreach (var source in multiTargetSources)
                {
                    Console.WriteLine($"Multi-target file: {source.HintName}");
                    Console.WriteLine("Content preview:");
                    var content = source.SourceText.ToString();
                    var lines = content.Split('\n').Take(10).ToArray();
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
            }
            else
            {
                Console.WriteLine("No multi-target files generated - check pipeline registration");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("=== Validation Complete ===");
    }

    static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
