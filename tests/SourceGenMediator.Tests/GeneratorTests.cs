using System.Collections.Immutable;

using SourceGenMediator.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SourceGenMediator.Tests;

public class GeneratorTests
{
    [Fact]
    public void Emits_SM1001_When_Request_Has_No_Handler()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : IRequest<string>;
""";

        var diagnostics = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "SM1001");
    }

    [Fact]
    public void Emits_SM1004_When_StrictCqrs_Enabled_And_Request_Not_CommandOrQuery()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : IRequest<string>;
public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}
""";

        var diagnostics = RunGenerator(source, strictCqrs: true);
        Assert.Contains(diagnostics, d => d.Id == "SM1004");
    }

    [Fact]
    public void Emits_SM1002_When_Duplicate_Handlers_Exist()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : IRequest<string>;
public sealed class PingHandlerA : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}
public sealed class PingHandlerB : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}
""";

        var diagnostics = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "SM1002");
    }

    [Fact]
    public void Emits_SM1003_When_Handler_Is_Not_Public()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : IRequest<string>;
internal sealed class PingHandler : IRequestHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}
""";

        var diagnostics = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "SM1003");
    }

    [Fact]
    public void Generates_Code_When_All_Valid()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : ICommand<string>;
public sealed class PingHandler : ICommandHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping command, CancellationToken cancellationToken)
        => ValueTask.FromResult(command.Value);
}
""";

        var (diagnostics, generatedTrees) = RunGeneratorWithOutput(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.True(generatedTrees.Length > 0, "Expected generated source output");
    }

    [Fact]
    public void Emits_SM1001_When_StreamRequest_Has_No_Handler()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record NumbersQuery(int Count) : IStreamRequest<int>;
""";

        var diagnostics = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "SM1001");
    }

    [Fact]
    public void Generates_Code_For_Stream_Handler()
    {
        const string source = """
using SourceGenMediator.Abstractions;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

public sealed record NumbersQuery(int Count) : IStreamRequest<int>;
public sealed class NumbersHandler : IStreamRequestHandler<NumbersQuery, int>
{
    public async IAsyncEnumerable<int> Handle(NumbersQuery request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var i = 1; i <= request.Count; i++) yield return i;
    }
}
""";

        var (diagnostics, generatedTrees) = RunGeneratorWithOutput(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.True(generatedTrees.Length > 0, "Expected generated source output");
        var generatedText = generatedTrees[0].ToString();
        Assert.Contains("StreamDispatchers", generatedText);
        Assert.Contains("NumbersQuery", generatedText);
    }

    [Fact]
    public void Emits_SM1002_When_Duplicate_Stream_Handlers_Exist()
    {
        const string source = """
using SourceGenMediator.Abstractions;
using System.Collections.Generic;
using System.Threading;

public sealed record NumbersQuery(int Count) : IStreamRequest<int>;
public sealed class NumbersHandlerA : IStreamRequestHandler<NumbersQuery, int>
{
    public async IAsyncEnumerable<int> Handle(NumbersQuery request, CancellationToken cancellationToken)
    { yield return 1; }
}
public sealed class NumbersHandlerB : IStreamRequestHandler<NumbersQuery, int>
{
    public async IAsyncEnumerable<int> Handle(NumbersQuery request, CancellationToken cancellationToken)
    { yield return 2; }
}
""";

        var diagnostics = RunGenerator(source);
        Assert.Contains(diagnostics, d => d.Id == "SM1002");
    }

    [Fact]
    public void Generates_Pipeline_Wiring_For_Request_Handler()
    {
        const string source = """
using SourceGenMediator.Abstractions;

public sealed record Ping(string Value) : ICommand<string>;
public sealed class PingHandler : ICommandHandler<Ping, string>
{
    public ValueTask<string> Handle(Ping command, CancellationToken cancellationToken)
        => ValueTask.FromResult(command.Value);
}
""";

        var (diagnostics, generatedTrees) = RunGeneratorWithOutput(source);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        var generatedText = generatedTrees[0].ToString();
        Assert.Contains("IPipelineBehavior", generatedText);
        Assert.Contains("GetServices", generatedText);
        Assert.Contains("pipeline", generatedText);
    }

    private static ImmutableArray<Diagnostic> RunGenerator(string source, bool strictCqrs = false)
    {
        var (diagnostics, _) = RunGeneratorWithOutput(source, strictCqrs);
        return diagnostics;
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<SyntaxTree> GeneratedTrees) RunGeneratorWithOutput(string source, bool strictCqrs = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(static a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(static a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzerOptions = new TestAnalyzerConfigOptionsProvider(strictCqrs);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new MediatorIncrementalGenerator().AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options,
            optionsProvider: analyzerOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
        var result = driver.GetRunResult();

        var allDiagnostics = generatorDiagnostics
            .AddRange(result.Diagnostics)
            .AddRange(result.Results.SelectMany(static r => r.Diagnostics));

        var generatedTrees = outputCompilation.SyntaxTrees
            .Except(compilation.SyntaxTrees)
            .ToImmutableArray();

        return (allDiagnostics, generatedTrees);
    }

    private sealed class TestAnalyzerConfigOptionsProvider(bool strictCqrs) : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions _options = new TestAnalyzerConfigOptions(strictCqrs);

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestAnalyzerConfigOptions(bool strictCqrs) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (string.Equals(key, "build_property.SourceGenMediator_StrictCqrs", StringComparison.OrdinalIgnoreCase))
            {
                value = strictCqrs ? "true" : "false";
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}