namespace SourceGenMediator.Abstractions;

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    ValueTask<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    ValueTask<Unit> Handle(TCommand command, CancellationToken cancellationToken);
}