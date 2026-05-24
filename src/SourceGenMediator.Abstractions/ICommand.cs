namespace SourceGenMediator.Abstractions;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface ICommand : IRequest;