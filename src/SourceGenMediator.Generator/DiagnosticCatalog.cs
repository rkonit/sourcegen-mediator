using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal sealed class DiagnosticDescriptors
{
    internal DiagnosticDescriptors(
        DiagnosticDescriptor missingRequestHandler,
        DiagnosticDescriptor duplicateRequestHandler,
        DiagnosticDescriptor handlerMustBePublic,
        DiagnosticDescriptor strictCqrsViolation,
        DiagnosticDescriptor requestMustNotBeAbstract,
        DiagnosticDescriptor requestShouldBeSealed,
        DiagnosticDescriptor handlerShouldBeSealed,
        DiagnosticDescriptor cqrsSuggestion)
    {
        MissingRequestHandler = missingRequestHandler;
        DuplicateRequestHandler = duplicateRequestHandler;
        HandlerMustBePublic = handlerMustBePublic;
        StrictCqrsViolation = strictCqrsViolation;
        RequestMustNotBeAbstract = requestMustNotBeAbstract;
        RequestShouldBeSealed = requestShouldBeSealed;
        HandlerShouldBeSealed = handlerShouldBeSealed;
        CqrsSuggestion = cqrsSuggestion;
    }

    internal DiagnosticDescriptor MissingRequestHandler { get; }
    internal DiagnosticDescriptor DuplicateRequestHandler { get; }
    internal DiagnosticDescriptor HandlerMustBePublic { get; }
    internal DiagnosticDescriptor StrictCqrsViolation { get; }
    internal DiagnosticDescriptor RequestMustNotBeAbstract { get; }
    internal DiagnosticDescriptor RequestShouldBeSealed { get; }
    internal DiagnosticDescriptor HandlerShouldBeSealed { get; }
    internal DiagnosticDescriptor CqrsSuggestion { get; }
}

internal static class DiagnosticCatalog
{
    internal static DiagnosticDescriptors Create()
        => new(
            new DiagnosticDescriptor("SM1001", "Missing request handler", "Request '{0}' has no handler implementation.", "SourceGenMediator", DiagnosticSeverity.Error, true),
            new DiagnosticDescriptor("SM1002", "Duplicate request handler", "Request '{0}' has multiple handlers; exactly one is required.", "SourceGenMediator", DiagnosticSeverity.Error, true),
            new DiagnosticDescriptor("SM1003", "Handler visibility invalid", "Handler type '{0}' must be public so generated dispatchers can resolve it.", "SourceGenMediator", DiagnosticSeverity.Error, true),
            new DiagnosticDescriptor("SM1004", "Strict CQRS violation", "Request '{0}' must implement ICommand<TResponse>, ICommand, or IQuery<TResponse> when SourceGenMediator_StrictCqrs is enabled.", "SourceGenMediator", DiagnosticSeverity.Error, true),
            new DiagnosticDescriptor("SM1005", "Abstract request type", "Request '{0}' cannot be abstract.", "SourceGenMediator", DiagnosticSeverity.Error, true),
            new DiagnosticDescriptor("SM2001", "Request should be sealed", "Request '{0}' should be sealed for optimal generated dispatch performance.", "SourceGenMediator", DiagnosticSeverity.Warning, true),
            new DiagnosticDescriptor("SM2002", "Handler should be sealed", "Handler '{0}' should be sealed for optimal generated dispatch performance.", "SourceGenMediator", DiagnosticSeverity.Warning, true),
            new DiagnosticDescriptor("SM3001", "CQRS suggestion", "Request '{0}' should implement ICommand<TResponse>, ICommand, or IQuery<TResponse> for clearer CQRS intent.", "SourceGenMediator", DiagnosticSeverity.Info, true));
}