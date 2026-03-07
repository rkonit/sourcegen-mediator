using Microsoft.CodeAnalysis;

namespace SourceGenMediator.Generator;

internal static class Emitter
{
    internal static string Emit(MediatorModel model)
    {
        var requestRegistrations = string.Join("\n",
            model.RequestHandlers.Select(static h =>
                $"        services.AddTransient<{h.HandlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {h.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();").Distinct(StringComparer.Ordinal));

        var notificationRegistrations = string.Join("\n",
            model.NotificationHandlers.SelectMany(static h => new[]
            {
                $"        services.AddTransient<{h.HandlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {h.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();",
                $"        services.AddTransient<{h.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();",
            }).Distinct(StringComparer.Ordinal));

        var streamRegistrations = string.Join("\n",
            model.StreamHandlers.Select(static h =>
                $"        services.AddTransient<{h.HandlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}, {h.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>();").Distinct(StringComparer.Ordinal));

        var requestDispatcherHelpers = string.Join("\n\n",
            model.RequestHandlers.Select(static (h, index) =>
            {
                var requestFq = h.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var responseFq = h.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var handlerFq = h.HandlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var behaviorFq = $"global::SourceGenMediator.Abstractions.IPipelineBehavior<{requestFq}, {responseFq}>";
                var preFq = $"global::SourceGenMediator.Abstractions.IRequestPreProcessor<{requestFq}>";
                var postFq = $"global::SourceGenMediator.Abstractions.IRequestPostProcessor<{requestFq}, {responseFq}>";
                var exceptionHandlerFq = $"global::SourceGenMediator.Abstractions.IRequestExceptionHandler<{requestFq}, {responseFq}>";
                var exceptionActionFq = $"global::SourceGenMediator.Abstractions.IRequestExceptionAction<{requestFq}>";
                return
$$"""
    private static class __SourceGenMediatorRequestPipeline{{index}}
    {
        private static int _pipelineMode;

        public static async System.Threading.Tasks.ValueTask<object?> Dispatch(System.IServiceProvider sp, object request, System.Threading.CancellationToken ct)
        {
            var typedRequest = ({{requestFq}})request;
            var handler = sp.GetRequiredService<{{handlerFq}}>();
            var mode = System.Threading.Volatile.Read(ref _pipelineMode);
            if (mode == 1)
            {
                return await handler.Handle(typedRequest, ct).ConfigureAwait(false);
            }

            var behaviorEnumerable = sp.GetServices<{{behaviorFq}}>();
            using var behaviorEnumerator = behaviorEnumerable.GetEnumerator();
            var hasBehaviors = behaviorEnumerator.MoveNext();

            if (!hasBehaviors
                && !HasServices<{{preFq}}>(sp)
                && !HasServices<{{postFq}}>(sp)
                && !HasServices<{{exceptionHandlerFq}}>(sp)
                && !HasServices<{{exceptionActionFq}}>(sp))
            {
                if (mode == 0)
                {
                    System.Threading.Interlocked.CompareExchange(ref _pipelineMode, 1, 0);
                }

                return await handler.Handle(typedRequest, ct).ConfigureAwait(false);
            }

            if (mode == 0)
            {
                System.Threading.Interlocked.CompareExchange(ref _pipelineMode, 2, 0);
            }

            return await ExecuteWithPipeline(sp, handler, typedRequest, behaviorEnumerator, hasBehaviors, ct).ConfigureAwait(false);
        }

        private static bool HasServices<TService>(System.IServiceProvider sp)
        {
            using var enumerator = sp.GetServices<TService>().GetEnumerator();
            return enumerator.MoveNext();
        }

        private static async System.Threading.Tasks.ValueTask<{{responseFq}}> ExecuteWithPipeline(
            System.IServiceProvider sp,
            {{handlerFq}} handler,
            {{requestFq}} typedRequest,
            System.Collections.Generic.IEnumerator<{{behaviorFq}}> behaviorEnumerator,
            bool hasBehaviors,
            System.Threading.CancellationToken ct)
        {
            foreach (var pre in sp.GetServices<{{preFq}}>())
            {
                await pre.Process(typedRequest, ct).ConfigureAwait(false);
            }

            {{responseFq}} response;
            try
            {
                if (!hasBehaviors)
                {
                    response = await handler.Handle(typedRequest, ct).ConfigureAwait(false);
                }
                else
                {
                    response = await ExecuteWithBehaviors(handler, typedRequest, behaviorEnumerator, ct).ConfigureAwait(false);
                }
            }
            catch (System.Exception ex)
            {
                var resolved = false;
                {{responseFq}} fallback = default!;
                foreach (var exceptionHandler in sp.GetServices<{{exceptionHandlerFq}}>())
                {
                    var result = await exceptionHandler.Handle(typedRequest, ex, ct).ConfigureAwait(false);
                    if (result.Handled)
                    {
                        fallback = result.Response!;
                        resolved = true;
                        break;
                    }
                }

                if (!resolved)
                {
                    foreach (var exceptionAction in sp.GetServices<{{exceptionActionFq}}>())
                    {
                        await exceptionAction.Execute(typedRequest, ex, ct).ConfigureAwait(false);
                    }

                    throw;
                }

                response = fallback;
            }

            foreach (var post in sp.GetServices<{{postFq}}>())
            {
                await post.Handle(typedRequest, response, ct).ConfigureAwait(false);
            }

            return response;
        }

        private static System.Threading.Tasks.ValueTask<{{responseFq}}> ExecuteWithBehaviors(
            {{handlerFq}} handler,
            {{requestFq}} typedRequest,
            System.Collections.Generic.IEnumerator<{{behaviorFq}}> behaviorEnumerator,
            System.Threading.CancellationToken ct)
        {
            return InvokeCurrent(behaviorEnumerator.Current);

            System.Threading.Tasks.ValueTask<{{responseFq}}> InvokeCurrent({{behaviorFq}} currentBehavior)
                => currentBehavior.Handle(typedRequest, Next, ct);

            System.Threading.Tasks.ValueTask<{{responseFq}}> Next()
            {
                if (!behaviorEnumerator.MoveNext())
                {
                    return handler.Handle(typedRequest, ct);
                }

                return InvokeCurrent(behaviorEnumerator.Current);
            }
        }
    }
""";
            }));

        var requestDispatchEntries = string.Join("\n",
            model.RequestHandlers.Select(static (h, index) =>
            {
                var requestFq = h.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"                [typeof({requestFq})] = __SourceGenMediatorRequestPipeline{index}.Dispatch,";
            }));

        var notificationDispatchEntries = string.Join("\n",
            model.NotificationHandlers
                .GroupBy(static h => h.NotificationType, SymbolEqualityComparer.Default)
                .OrderBy(static g => g.Key!.ToDisplayString(), StringComparer.Ordinal)
                .Select(static g =>
                {
                    var entries = string.Join("\n", g.OrderBy(static h => h.Implementation.ToDisplayString(), StringComparer.Ordinal)
                        .Select(h =>
                            $"                    static (sp, notification, ct) => sp.GetRequiredService<{h.Implementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>().Handle(({h.NotificationType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})notification, ct),"));

                    return $"                [typeof({g.Key!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})] = new System.Func<System.IServiceProvider, object, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>[]\n                {{\n{entries}\n                }},";
                }));

        var streamDispatcherHelpers = string.Join("\n\n",
            model.StreamHandlers.Select(static (h, index) =>
            {
                var streamRequestFq = h.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var streamResponseFq = h.ResponseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var streamHandlerFq = h.HandlerInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var streamBehaviorFq = $"global::SourceGenMediator.Abstractions.IStreamPipelineBehavior<{streamRequestFq}, {streamResponseFq}>";
                return
$$"""
    private static class __SourceGenMediatorStreamPipeline{{index}}
    {
        public static object Dispatch(System.IServiceProvider sp, object request, System.Threading.CancellationToken ct)
        {
            var typedRequest = ({{streamRequestFq}})request;
            var handler = sp.GetRequiredService<{{streamHandlerFq}}>();
            global::SourceGenMediator.Abstractions.StreamHandlerDelegate<{{streamResponseFq}}> pipeline = () => handler.Handle(typedRequest, ct);
            foreach (var behavior in System.Linq.Enumerable.Reverse(sp.GetServices<{{streamBehaviorFq}}>()))
            {
                var next = pipeline;
                var current = behavior;
                pipeline = () => current.Handle(typedRequest, next, ct);
            }

            return (object)pipeline();
        }
    }
""";
            }));

        var streamDispatchEntries = string.Join("\n",
            model.StreamHandlers.Select(static (h, index) =>
            {
                var streamRequestFq = h.RequestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return $"                [typeof({streamRequestFq})] = __SourceGenMediatorStreamPipeline{index}.Dispatch,";
            }));

        return $$"""
// <auto-generated/>
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SourceGenMediator.DependencyInjection;

public static class SourceGenMediatorGeneratedServiceCollectionExtensions
{
{{requestDispatcherHelpers}}

{{streamDispatcherHelpers}}

    public static IServiceCollection AddSourceGenMediatorGenerated(this IServiceCollection services)
    {
{{requestRegistrations}}
{{notificationRegistrations}}
{{streamRegistrations}}
        services.AddSingleton<SourceGenMediator.Runtime.MediatorDispatchMap>(static _ => new SourceGenMediator.Runtime.MediatorDispatchMap
        {
            RequestDispatchers = new System.Collections.Generic.Dictionary<System.Type, System.Func<System.IServiceProvider, object, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<object?>>>
            {
{{requestDispatchEntries}}
            },
            NotificationDispatchers = new System.Collections.Generic.Dictionary<System.Type, System.Func<System.IServiceProvider, object, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask>[]>
            {
{{notificationDispatchEntries}}
            },
            StreamDispatchers = new System.Collections.Generic.Dictionary<System.Type, System.Func<System.IServiceProvider, object, System.Threading.CancellationToken, object>>
            {
{{streamDispatchEntries}}
            }
        });
        services.AddSingleton<SourceGenMediator.Abstractions.IMediator, SourceGenMediator.Runtime.GeneratedMediator>();
        services.AddSingleton<SourceGenMediator.Abstractions.ISender>(static sp => (SourceGenMediator.Abstractions.ISender)sp.GetRequiredService<SourceGenMediator.Abstractions.IMediator>());
        services.AddSingleton<SourceGenMediator.Abstractions.IPublisher>(static sp => (SourceGenMediator.Abstractions.IPublisher)sp.GetRequiredService<SourceGenMediator.Abstractions.IMediator>());
        services.TryAddSingleton<SourceGenMediator.Abstractions.INotificationPublisher, SourceGenMediator.Runtime.ForeachAwaitPublisher>();
        return services;
    }
}
""";
    }
}