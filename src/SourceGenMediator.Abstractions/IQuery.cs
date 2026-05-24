namespace SourceGenMediator.Abstractions;

public interface IQuery<out TResponse> : IRequest<TResponse>;