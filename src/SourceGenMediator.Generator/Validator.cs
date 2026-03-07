using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal static class Validator
{
    internal static bool Run(MediatorModel model, DiagnosticDescriptors diagnostics, bool strictCqrs, SourceProductionContext context)
    {
        var hasErrors = false;

        foreach (var handler in model.RequestHandlers)
        {
            if (handler.Implementation.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerMustBePublic, handler.Implementation.Locations.FirstOrDefault(), handler.Implementation.ToDisplayString()));
                hasErrors = true;
            }

            if (handler.Implementation.TypeKind == TypeKind.Class && !handler.Implementation.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerShouldBeSealed, handler.Implementation.Locations.FirstOrDefault(), handler.Implementation.ToDisplayString()));
            }
        }

        foreach (var notification in model.NotificationHandlers)
        {
            if (notification.Implementation.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerMustBePublic, notification.Implementation.Locations.FirstOrDefault(), notification.Implementation.ToDisplayString()));
                hasErrors = true;
            }

            if (notification.Implementation.TypeKind == TypeKind.Class && !notification.Implementation.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerShouldBeSealed, notification.Implementation.Locations.FirstOrDefault(), notification.Implementation.ToDisplayString()));
            }
        }

        foreach (var duplicate in model.RequestHandlers.GroupBy(static h => h.RequestType, SymbolEqualityComparer.Default).Where(static g => g.Count() > 1))
        {
            foreach (var offender in duplicate)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.DuplicateRequestHandler, offender.Implementation.Locations.FirstOrDefault(), offender.RequestType.ToDisplayString()));
            }

            hasErrors = true;
        }

        foreach (var request in model.RequestTypes)
        {
            if (request.TypeKind == TypeKind.Class && request.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.RequestMustNotBeAbstract, request.Locations.FirstOrDefault(), request.ToDisplayString()));
                hasErrors = true;
                continue;
            }

            if (request.TypeKind == TypeKind.Class && !request.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.RequestShouldBeSealed, request.Locations.FirstOrDefault(), request.ToDisplayString()));
            }

            var handlers = model.RequestHandlers.Where(h => SymbolEqualityComparer.Default.Equals(h.RequestType, request)).ToImmutableArray();
            if (handlers.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.MissingRequestHandler, request.Locations.FirstOrDefault(), request.ToDisplayString()));
                hasErrors = true;
                continue;
            }

            var isCommand = model.CommandMarker is not null && request.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, model.CommandMarker));
            var isVoidCommand = model.CommandVoidMarker is not null && request.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, model.CommandVoidMarker));
            var isQuery = model.QueryMarker is not null && request.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, model.QueryMarker));
            if (!isCommand && !isVoidCommand && !isQuery)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    strictCqrs ? diagnostics.StrictCqrsViolation : diagnostics.CqrsSuggestion,
                    request.Locations.FirstOrDefault(),
                    request.ToDisplayString()));

                if (strictCqrs)
                {
                    hasErrors = true;
                }
            }
        }

        foreach (var handler in model.StreamHandlers)
        {
            if (handler.Implementation.DeclaredAccessibility != Accessibility.Public)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerMustBePublic, handler.Implementation.Locations.FirstOrDefault(), handler.Implementation.ToDisplayString()));
                hasErrors = true;
            }

            if (handler.Implementation.TypeKind == TypeKind.Class && !handler.Implementation.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.HandlerShouldBeSealed, handler.Implementation.Locations.FirstOrDefault(), handler.Implementation.ToDisplayString()));
            }
        }

        foreach (var duplicate in model.StreamHandlers.GroupBy(static h => h.RequestType, SymbolEqualityComparer.Default).Where(static g => g.Count() > 1))
        {
            foreach (var offender in duplicate)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.DuplicateRequestHandler, offender.Implementation.Locations.FirstOrDefault(), offender.RequestType.ToDisplayString()));
            }

            hasErrors = true;
        }

        foreach (var request in model.StreamRequestTypes)
        {
            if (request.TypeKind == TypeKind.Class && request.IsAbstract)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.RequestMustNotBeAbstract, request.Locations.FirstOrDefault(), request.ToDisplayString()));
                hasErrors = true;
                continue;
            }

            if (request.TypeKind == TypeKind.Class && !request.IsSealed)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.RequestShouldBeSealed, request.Locations.FirstOrDefault(), request.ToDisplayString()));
            }

            var handlers = model.StreamHandlers.Where(h => SymbolEqualityComparer.Default.Equals(h.RequestType, request)).ToImmutableArray();
            if (handlers.Length == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostics.MissingRequestHandler, request.Locations.FirstOrDefault(), request.ToDisplayString()));
                hasErrors = true;
            }
        }

        return hasErrors;
    }
}