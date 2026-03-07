using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal enum RequestKind
{
    General,
    Command,
    Query
}

internal sealed class RequestHandlerModel
{
    internal RequestHandlerModel(INamedTypeSymbol implementation, INamedTypeSymbol handlerInterface, INamedTypeSymbol requestType, ITypeSymbol responseType, RequestKind kind)
    {
        Implementation = implementation;
        HandlerInterface = handlerInterface;
        RequestType = requestType;
        ResponseType = responseType;
        Kind = kind;
    }

    internal INamedTypeSymbol Implementation { get; }
    internal INamedTypeSymbol HandlerInterface { get; }
    internal INamedTypeSymbol RequestType { get; }
    internal ITypeSymbol ResponseType { get; }
    internal RequestKind Kind { get; }
}

internal sealed class NotificationHandlerModel
{
    internal NotificationHandlerModel(INamedTypeSymbol implementation, INamedTypeSymbol handlerInterface, INamedTypeSymbol notificationType)
    {
        Implementation = implementation;
        HandlerInterface = handlerInterface;
        NotificationType = notificationType;
    }

    internal INamedTypeSymbol Implementation { get; }
    internal INamedTypeSymbol HandlerInterface { get; }
    internal INamedTypeSymbol NotificationType { get; }
}

internal sealed class StreamHandlerModel
{
    internal StreamHandlerModel(INamedTypeSymbol implementation, INamedTypeSymbol handlerInterface, INamedTypeSymbol requestType, ITypeSymbol responseType)
    {
        Implementation = implementation;
        HandlerInterface = handlerInterface;
        RequestType = requestType;
        ResponseType = responseType;
    }

    internal INamedTypeSymbol Implementation { get; }
    internal INamedTypeSymbol HandlerInterface { get; }
    internal INamedTypeSymbol RequestType { get; }
    internal ITypeSymbol ResponseType { get; }
}

internal sealed class MediatorModel
{
    internal MediatorModel(
        ImmutableArray<RequestHandlerModel> requestHandlers,
        ImmutableArray<NotificationHandlerModel> notificationHandlers,
        ImmutableArray<INamedTypeSymbol> requestTypes,
        ImmutableArray<StreamHandlerModel> streamHandlers,
        ImmutableArray<INamedTypeSymbol> streamRequestTypes,
        INamedTypeSymbol? commandMarker,
        INamedTypeSymbol? commandVoidMarker,
        INamedTypeSymbol? queryMarker)
    {
        RequestHandlers = requestHandlers;
        NotificationHandlers = notificationHandlers;
        RequestTypes = requestTypes;
        StreamHandlers = streamHandlers;
        StreamRequestTypes = streamRequestTypes;
        CommandMarker = commandMarker;
        CommandVoidMarker = commandVoidMarker;
        QueryMarker = queryMarker;
    }

    internal ImmutableArray<RequestHandlerModel> RequestHandlers { get; }
    internal ImmutableArray<NotificationHandlerModel> NotificationHandlers { get; }
    internal ImmutableArray<INamedTypeSymbol> RequestTypes { get; }
    internal ImmutableArray<StreamHandlerModel> StreamHandlers { get; }
    internal ImmutableArray<INamedTypeSymbol> StreamRequestTypes { get; }
    internal INamedTypeSymbol? CommandMarker { get; }
    internal INamedTypeSymbol? CommandVoidMarker { get; }
    internal INamedTypeSymbol? QueryMarker { get; }
}