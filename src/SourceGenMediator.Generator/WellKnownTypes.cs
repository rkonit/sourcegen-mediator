using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal sealed class WellKnownTypes : IEquatable<WellKnownTypes>
{
    internal INamedTypeSymbol? RequestOfT { get; }
    internal INamedTypeSymbol? Request { get; }
    internal INamedTypeSymbol? CommandOfT { get; }
    internal INamedTypeSymbol? Command { get; }
    internal INamedTypeSymbol? QueryOfT { get; }
    internal INamedTypeSymbol? RequestHandlerOfT { get; }
    internal INamedTypeSymbol? RequestHandler { get; }
    internal INamedTypeSymbol? CommandHandlerOfT { get; }
    internal INamedTypeSymbol? CommandHandler { get; }
    internal INamedTypeSymbol? QueryHandlerOfT { get; }
    internal INamedTypeSymbol? NotificationHandlerOfT { get; }
    internal INamedTypeSymbol? StreamRequestOfT { get; }
    internal INamedTypeSymbol? StreamRequestHandlerOfT { get; }
    internal INamedTypeSymbol? Unit { get; }

    private WellKnownTypes(
        INamedTypeSymbol? requestOfT,
        INamedTypeSymbol? request,
        INamedTypeSymbol? commandOfT,
        INamedTypeSymbol? command,
        INamedTypeSymbol? queryOfT,
        INamedTypeSymbol? requestHandlerOfT,
        INamedTypeSymbol? requestHandler,
        INamedTypeSymbol? commandHandlerOfT,
        INamedTypeSymbol? commandHandler,
        INamedTypeSymbol? queryHandlerOfT,
        INamedTypeSymbol? notificationHandlerOfT,
        INamedTypeSymbol? streamRequestOfT,
        INamedTypeSymbol? streamRequestHandlerOfT,
        INamedTypeSymbol? unit)
    {
        RequestOfT = requestOfT;
        Request = request;
        CommandOfT = commandOfT;
        Command = command;
        QueryOfT = queryOfT;
        RequestHandlerOfT = requestHandlerOfT;
        RequestHandler = requestHandler;
        CommandHandlerOfT = commandHandlerOfT;
        CommandHandler = commandHandler;
        QueryHandlerOfT = queryHandlerOfT;
        NotificationHandlerOfT = notificationHandlerOfT;
        StreamRequestOfT = streamRequestOfT;
        StreamRequestHandlerOfT = streamRequestHandlerOfT;
        Unit = unit;
    }

    internal static WellKnownTypes From(Compilation compilation) => new(
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IRequest`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IRequest"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.ICommand`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.ICommand"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IQuery`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IRequestHandler`2"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IRequestHandler`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.ICommandHandler`2"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.ICommandHandler`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IQueryHandler`2"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.INotificationHandler`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IStreamRequest`1"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.IStreamRequestHandler`2"),
        compilation.GetTypeByMetadataName("SourceGenMediator.Abstractions.Unit"));

    public bool Equals(WellKnownTypes? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return SymbolEqualityComparer.Default.Equals(RequestOfT, other.RequestOfT)
            && SymbolEqualityComparer.Default.Equals(Request, other.Request)
            && SymbolEqualityComparer.Default.Equals(CommandOfT, other.CommandOfT)
            && SymbolEqualityComparer.Default.Equals(Command, other.Command)
            && SymbolEqualityComparer.Default.Equals(QueryOfT, other.QueryOfT)
            && SymbolEqualityComparer.Default.Equals(RequestHandlerOfT, other.RequestHandlerOfT)
            && SymbolEqualityComparer.Default.Equals(RequestHandler, other.RequestHandler)
            && SymbolEqualityComparer.Default.Equals(CommandHandlerOfT, other.CommandHandlerOfT)
            && SymbolEqualityComparer.Default.Equals(CommandHandler, other.CommandHandler)
            && SymbolEqualityComparer.Default.Equals(QueryHandlerOfT, other.QueryHandlerOfT)
            && SymbolEqualityComparer.Default.Equals(NotificationHandlerOfT, other.NotificationHandlerOfT)
            && SymbolEqualityComparer.Default.Equals(StreamRequestOfT, other.StreamRequestOfT)
            && SymbolEqualityComparer.Default.Equals(StreamRequestHandlerOfT, other.StreamRequestHandlerOfT)
            && SymbolEqualityComparer.Default.Equals(Unit, other.Unit);
    }

    public override bool Equals(object? obj) => obj is WellKnownTypes other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = SymbolEqualityComparer.Default.GetHashCode(RequestOfT!);
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(RequestHandlerOfT!);
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(NotificationHandlerOfT!);
            hash = (hash * 397) ^ SymbolEqualityComparer.Default.GetHashCode(StreamRequestOfT!);
            return hash;
        }
    }
}
