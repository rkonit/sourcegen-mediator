using SourceGenMediator.Abstractions;
using SourceGenMediator.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSourceGenMediatorGenerated();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
var id = await mediator.Send(new CreateOrderCommand("SKU-123"));
Console.WriteLine($"Created order id: {id}");

public sealed record CreateOrderCommand(string Sku) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(Guid.NewGuid());
}