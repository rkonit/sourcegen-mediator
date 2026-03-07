using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal static class ModelBuilder
{
    internal static MediatorModel Build(WellKnownTypes types, ImmutableArray<INamedTypeSymbol> symbols)
    {
        var requestHandlers = ImmutableArray.CreateBuilder<RequestHandlerModel>();
        var notificationHandlers = ImmutableArray.CreateBuilder<NotificationHandlerModel>();
        var requestTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        var streamHandlers = ImmutableArray.CreateBuilder<StreamHandlerModel>();
        var streamRequestTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        var requestOfT = types.RequestOfT;
        var request = types.Request;
        var commandOfT = types.CommandOfT;
        var command = types.Command;
        var queryOfT = types.QueryOfT;
        var requestHandlerOfT = types.RequestHandlerOfT;
        var requestHandler = types.RequestHandler;
        var commandHandlerOfT = types.CommandHandlerOfT;
        var commandHandler = types.CommandHandler;
        var queryHandlerOfT = types.QueryHandlerOfT;
        var notificationHandlerOfT = types.NotificationHandlerOfT;
        var streamRequestOfT = types.StreamRequestOfT;
        var streamRequestHandlerOfT = types.StreamRequestHandlerOfT;
        var unit = types.Unit;

        static void AddUnique(ImmutableArray<INamedTypeSymbol>.Builder builder, INamedTypeSymbol symbol)
        {
            foreach (var existing in builder)
            {
                if (SymbolEqualityComparer.Default.Equals(existing, symbol))
                {
                    return;
                }
            }

            builder.Add(symbol);
        }

        foreach (var symbol in symbols.OrderBy(static s => s.ToDisplayString(), StringComparer.Ordinal))
        {
            foreach (var iface in symbol.AllInterfaces)
            {
                var orig = iface.OriginalDefinition;

                if (requestOfT is not null && SymbolEqualityComparer.Default.Equals(orig, requestOfT))
                {
                    AddUnique(requestTypes, symbol);
                    continue;
                }

                if (request is not null && SymbolEqualityComparer.Default.Equals(orig, request))
                {
                    AddUnique(requestTypes, symbol);
                    continue;
                }

                if (streamRequestOfT is not null && SymbolEqualityComparer.Default.Equals(orig, streamRequestOfT))
                {
                    AddUnique(streamRequestTypes, symbol);
                    continue;
                }

                if (requestHandlerOfT is not null && SymbolEqualityComparer.Default.Equals(orig, requestHandlerOfT))
                {
                    requestHandlers.Add(new RequestHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], iface.TypeArguments[1], RequestKind.General));
                    continue;
                }

                if (requestHandler is not null && unit is not null && SymbolEqualityComparer.Default.Equals(orig, requestHandler))
                {
                    requestHandlers.Add(new RequestHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], unit, RequestKind.General));
                    continue;
                }

                if (commandHandlerOfT is not null && SymbolEqualityComparer.Default.Equals(orig, commandHandlerOfT))
                {
                    requestHandlers.Add(new RequestHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], iface.TypeArguments[1], RequestKind.Command));
                    continue;
                }

                if (commandHandler is not null && unit is not null && SymbolEqualityComparer.Default.Equals(orig, commandHandler))
                {
                    requestHandlers.Add(new RequestHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], unit, RequestKind.Command));
                    continue;
                }

                if (queryHandlerOfT is not null && SymbolEqualityComparer.Default.Equals(orig, queryHandlerOfT))
                {
                    requestHandlers.Add(new RequestHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], iface.TypeArguments[1], RequestKind.Query));
                    continue;
                }

                if (notificationHandlerOfT is not null && SymbolEqualityComparer.Default.Equals(orig, notificationHandlerOfT))
                {
                    notificationHandlers.Add(new NotificationHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0]));
                    continue;
                }

                if (streamRequestHandlerOfT is not null && SymbolEqualityComparer.Default.Equals(orig, streamRequestHandlerOfT))
                {
                    streamHandlers.Add(new StreamHandlerModel(symbol, iface, (INamedTypeSymbol)iface.TypeArguments[0], iface.TypeArguments[1]));
                }
            }
        }

        return new MediatorModel(
            ImmutableArray.CreateRange(
                requestHandlers.ToImmutable()
                    .OrderBy(static h => h.RequestType.ToDisplayString(), StringComparer.Ordinal)
                    .ThenBy(static h => h.Implementation.ToDisplayString(), StringComparer.Ordinal)),
            ImmutableArray.CreateRange(
                notificationHandlers.ToImmutable()
                    .OrderBy(static h => h.NotificationType.ToDisplayString(), StringComparer.Ordinal)
                    .ThenBy(static h => h.Implementation.ToDisplayString(), StringComparer.Ordinal)),
            ImmutableArray.CreateRange(
                requestTypes.ToImmutable()
                    .OrderBy(static s => s.ToDisplayString(), StringComparer.Ordinal)),
            ImmutableArray.CreateRange(
                streamHandlers.ToImmutable()
                    .OrderBy(static h => h.RequestType.ToDisplayString(), StringComparer.Ordinal)
                    .ThenBy(static h => h.Implementation.ToDisplayString(), StringComparer.Ordinal)),
            ImmutableArray.CreateRange(
                streamRequestTypes.ToImmutable()
                    .OrderBy(static s => s.ToDisplayString(), StringComparer.Ordinal)),
            commandOfT,
            command,
            queryOfT);
    }
}