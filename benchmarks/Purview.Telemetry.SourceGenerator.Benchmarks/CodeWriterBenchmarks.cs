using System.Collections.Generic;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Purview.Telemetry.SourceGenerator.Helpers;

namespace Purview.Telemetry.SourceGenerator.Benchmarks;

[MemoryDiagnoser]
[SimpleJob]
public class CodeWriterBenchmarks
{
    [Params(10, 50, 200, 1000)]
    public int Methods { get; set; }

    [Benchmark(Baseline = true)]
    public string Legacy_StringBuilder()
    {
        var sb = new StringBuilder(8192);
        for (var i = 0; i < Methods; i++)
        {
            EmitLegacy(sb, i);
        }
        return sb.ToString();
    }

    [Benchmark]
    public string New_CodeWriter()
    {
        using var cw = new CodeWriter(8192);
        for (var i = 0; i < Methods; i++)
        {
            EmitNew(cw, i);
        }
        return cw.ToString();
    }

    [Benchmark]
    public string New_CodeWriter_HighLevel()
    {
        using var cw = new CodeWriter(8192);
        for (var i = 0; i < Methods; i++)
        {
            EmitNewHighLevel(cw, i);
        }
        return cw.ToString();
    }

    [Benchmark]
    public string StringWriter_TextWriter()
    {
        using var sw = new StringWriter();
        for (var i = 0; i < Methods; i++)
        {
            EmitStringWriter(sw, i);
        }
        return sw.ToString();
    }

    [Benchmark]
    public string PlainString_Concat()
    {
        var result = string.Empty;
        for (var i = 0; i < Methods; i++)
        {
            result += $"\tpublic void M{i}(){{ }}\n";
        }
        return result;
    }

    [Benchmark]
    public string PlainString_Interpolation()
    {
        var sb = new StringBuilder(8192);
        for (var i = 0; i < Methods; i++)
        {
            sb.AppendLine($"\tpublic void M{i}(){{ }}");
        }
        return sb.ToString();
    }

    [Benchmark]
    public string PlainString_Join()
    {
        var parts = new string[Methods];
        for (var i = 0; i < Methods; i++)
        {
            parts[i] = $"\tpublic void M{i}(){{ }}";
        }
        return string.Join('\n', parts);
    }

    [Benchmark]
    public string PlainString_Accumulator()
    {
        var accumulator = new List<string>(Methods);
        for (var i = 0; i < Methods; i++)
        {
            accumulator.Add($"\tpublic void M{i}(){{ }}");
        }
        return string.Join('\n', accumulator);
    }

    static void EmitLegacy(StringBuilder b, int i)
    {
        b.Append('\t').Append("public void M").Append(i).AppendLine("() { }");
    }

    static void EmitStringWriter(StringWriter sw, int i)
    {
        sw.Write('\t');
        sw.Write("public void M");
        sw.Write(i);
        sw.WriteLine("() { }");
    }

    static void EmitNew(CodeWriter w, int i)
    {
        w.Indent(1).Write("public void M").Write(i).WriteLine("() { }");
    }

    static void EmitNewHighLevel(CodeWriter w, int i)
    {
        w.BeginMethod(1, "public void", $"M{i}").WriteLine(" { }");
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class CodeWriterComplexBenchmarks
{
    [Params(25, 100)]
    public int ClassCount { get; set; }

    [Benchmark(Baseline = true)]
    public string Legacy_ComplexClass()
    {
        var sb = new StringBuilder(16384);

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("namespace Generated");
        sb.AppendLine("{");

        for (var i = 0; i < ClassCount; i++)
        {
            EmitComplexClassLegacy(sb, i);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    [Benchmark]
    public string New_ComplexClass()
    {
        using var cw = new CodeWriter(16384);

        cw.WriteUsing("System")
            .WriteUsing("System.Threading.Tasks")
            .WriteLine()
            .WriteNamespace("Generated");

        for (var i = 0; i < ClassCount; i++)
        {
            EmitComplexClassNew(cw, i);
        }

        cw.CloseBrace();
        return cw.ToString();
    }

    static void EmitComplexClassLegacy(StringBuilder sb, int i)
    {
        sb.AppendLine($"\tpublic class Service{i}");
        sb.AppendLine("\t{");
        sb.AppendLine($"\t\tprivate readonly string _name = \"Service{i}\";");
        sb.AppendLine();
        sb.AppendLine($"\t\tpublic async Task<string> GetNameAsync()");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tawait Task.Delay(1);");
        sb.AppendLine("\t\t\treturn _name;");
        sb.AppendLine("\t\t}");
        sb.AppendLine();
        sb.AppendLine($"\t\tpublic void Process(int id, string data)");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tif (id <= 0) throw new ArgumentException(nameof(id));");
        sb.AppendLine("\t\t\tConsole.WriteLine($\"Processing {{id}}: {{data}}\");");
        sb.AppendLine("\t\t}");
        sb.AppendLine("\t}");
        sb.AppendLine();
    }

    static void EmitComplexClassNew(CodeWriter cw, int i)
    {
        cw.WriteClass(1, $"Service{i}")
            .Indent(2)
            .Write($"private readonly string _name = \"Service{i}\";")
            .WriteLine()
            .WriteLine()
            .BeginMethod(2, "public async Task<string>", "GetNameAsync")
            .BeginMethodBody(2)
            .WriteMethodCall(3, "await Task", "Delay", "1")
            .WriteReturn(3, "_name")
            .EndMethodBody(2)
            .WriteLine()
            .BeginMethod(2, "public void", "Process", "int id, string data")
            .BeginMethodBody(2)
            .WriteIf(3, "id <= 0")
            .Indent(4)
            .WriteLine("throw new ArgumentException(nameof(id));")
            .WriteMethodCall(3, "Console", "WriteLine", "$\"Processing {id}: {data}\"")
            .EndMethodBody(2)
            .EndMethodBody(1)
            .WriteLine();
    }
}

[MemoryDiagnoser]
[SimpleJob]
public class CodeWriterMicrobenchmarks
{
    private readonly CodeWriter _cw = new(1024);
    private readonly StringBuilder _sb = new(1024);

    [IterationSetup]
    public void Setup()
    {
        _cw.ToString(); // Clear internal state
        _sb.Clear();
    }

    [Benchmark(Baseline = true)]
    public void StringBuilder_SingleAppend()
    {
        _sb.Append("test");
    }

    [Benchmark]
    public void CodeWriter_SingleWrite()
    {
        _cw.Write("test");
    }

    [Benchmark]
    public void StringBuilder_ChainedAppends()
    {
        _sb.Append("public ").Append("void ").Append("Method").Append("()");
    }

    [Benchmark]
    public void CodeWriter_ChainedWrites()
    {
        _cw.Write("public ").Write("void ").Write("Method").Write("()");
    }

    [Benchmark]
    public void StringBuilder_WithIndent()
    {
        _sb.Append('\t').Append("public void Method();").AppendLine();
    }

    [Benchmark]
    public void CodeWriter_WithIndent()
    {
        _cw.Indent(1).Write("public void Method();").WriteLine();
    }

    [Benchmark]
    public void CodeWriter_HighLevelAPI()
    {
        _cw.BeginMethod(1, "public void", "Method").Semicolon().WriteLine();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cw.Dispose();
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
        switcher.Run(args);
    }
}
