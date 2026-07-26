using SourceGenMediator.Abstractions;
using SourceGenMediator.DependencyInjection;
using SourceGenMediator.Runtime;

using Microsoft.Extensions.DependencyInjection;

namespace SourceGenMediator.IntegrationTests;

public class MediatorIntegrationTests
{
    [Fact]
    public async Task Void_Command_Dispatches_Through_Generated_Pipeline()
    {
        VoidDoThingHandler.Calls.Clear();
        var provider = BuildProvider(_ => { });
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new VoidDoThing("hello"));

        Assert.Contains("hello", VoidDoThingHandler.Calls);
    }

    [Fact]
    public async Task Sender_And_Publisher_Resolve_From_Generated_Registrations()
    {
        SenderDoneHandler.Called = false;
        var provider = BuildProvider(_ => { });

        var sender = provider.GetRequiredService<ISender>();
        var publisher = provider.GetRequiredService<IPublisher>();

        var response = await sender.Send(new SenderPing("hi"));
        await publisher.Publish(new SenderDone("hi"));

        Assert.Equal("hi", response);
        Assert.True(SenderDoneHandler.Called);
    }

    [Fact]
    public async Task Pre_And_Post_Processors_Run_Around_Handler()
    {
        PrePostPre.Calls.Clear();
        var provider = BuildProvider(services =>
        {
            services.AddTransient<IRequestPreProcessor<PrePostPing>, PrePostPre>();
            services.AddTransient<IRequestPostProcessor<PrePostPing, string>, PrePostPost>();
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PrePostPing("a"));

        Assert.Equal("a", result);
        Assert.Equal(["pre:a", "post:a"], PrePostPre.Calls);
    }

    [Fact]
    public async Task Exception_Handler_Returns_Fallback_Response()
    {
        var provider = BuildProvider(services =>
        {
            services.AddTransient<IRequestExceptionHandler<ExFallbackPing, string>, ExFallbackHandler>();
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ExFallbackPing("x"));

        Assert.Equal("fallback:x", result);
    }

    [Fact]
    public async Task Exception_Action_Runs_Then_Rethrows()
    {
        ExObserveAction.Called = false;
        var provider = BuildProvider(services =>
        {
            services.AddTransient<IRequestExceptionAction<ExObservePing>, ExObserveAction>();
        });
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(new ExObservePing("x")).AsTask());
        Assert.True(ExObserveAction.Called);
    }

    [Fact]
    public async Task Stream_Behavior_Wraps_Handler_Stream()
    {
        var provider = BuildProvider(services =>
        {
            services.AddTransient<IStreamPipelineBehavior<StreamNumbers, int>, StreamDoublingBehavior>();
        });
        var mediator = provider.GetRequiredService<IMediator>();

        var results = new List<int>();
        await foreach (var item in mediator.CreateStream(new StreamNumbers(3)))
        {
            results.Add(item);
        }

        Assert.Equal([2, 4, 6], results);
    }

    [Fact]
    public async Task Custom_Publisher_Is_Used_For_Notifications()
    {
        PubFirstHandler.Calls.Clear();
        var provider = BuildProvider(services =>
        {
            services.AddSingleton<INotificationPublisher, TaskWhenAllPublisher>();
        });

        Assert.IsType<TaskWhenAllPublisher>(provider.GetRequiredService<INotificationPublisher>());

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Publish(new PubDone("v"));

        Assert.Equal(2, PubFirstHandler.Calls.Count);
        Assert.Contains("first", PubFirstHandler.Calls);
        Assert.Contains("second", PubFirstHandler.Calls);
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddSourceGenMediatorGenerated();
        configure(services);
        return services.BuildServiceProvider();
    }
}
