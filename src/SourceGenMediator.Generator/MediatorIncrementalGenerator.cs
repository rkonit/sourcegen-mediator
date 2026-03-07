using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenMediator.Generator;

[Generator]
public sealed class MediatorIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Extract well-known SourceGenMediator interface symbols once per compilation.
        // These only change when the SourceGenMediator package reference changes,
        // not on every user edit, so Roslyn can cache this step effectively.
        var wellKnownTypes = context.CompilationProvider
            .Select(static (compilation, _) => WellKnownTypes.From(compilation));

        var candidateTypes = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { BaseList: not null },
                static (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)ctx.Node) as INamedTypeSymbol)
            .Where(static symbol => symbol is not null)
            .Select(static (symbol, _) => symbol!);

        var strictCqrs = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => ReadBoolean(options.GlobalOptions, "build_property.SourceGenMediator_StrictCqrs"));

        var pipeline = wellKnownTypes.Combine(candidateTypes.Collect()).Combine(strictCqrs);
        context.RegisterSourceOutput(pipeline, static (spc, source) =>
            Execute(source.Left.Left, source.Left.Right, source.Right, spc));
    }

    private static void Execute(WellKnownTypes types, ImmutableArray<INamedTypeSymbol> symbols, bool strictCqrs, SourceProductionContext context)
    {
        var model = ModelBuilder.Build(types, symbols);
        var diagnostics = DiagnosticCatalog.Create();
        var hasErrors = Validator.Run(model, diagnostics, strictCqrs, context);
        if (hasErrors)
        {
            return;
        }

        context.AddSource("SourceGenMediator.Generated.g.cs", SourceText.From(Emitter.Emit(model), System.Text.Encoding.UTF8));
    }

    private static bool ReadBoolean(AnalyzerConfigOptions options, string key)
        => options.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;
}