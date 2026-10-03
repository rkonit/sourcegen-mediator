# SourceGenMediator

A MediatR-compatible, compile-time mediator for .NET. Handlers are discovered by a Roslyn source generator that emits DI registrations and a `Type`-to-delegate dispatch map, so there is no runtime assembly scanning or reflection. The runtime is `ValueTask`-based and NativeAOT-safe.

- Targets `net8.0;net9.0;net10.0`, `IsAotCompatible=true`
- Requests, CQRS commands/queries, notifications, and `IAsyncEnumerable` streams
- Pipeline behaviors, pre/post processors, and exception handlers
- Tested with unit + integration tests, benchmarked against MediatR, checked with NativeAOT publish

## How it works

1. Your project installs the `SourceGenMediator` meta-package (or references the generator as an analyzer plus the Abstractions/Runtime/DI projects — see `samples/SourceGenMediator.SampleApp/SourceGenMediator.SampleApp.csproj`).
2. `src/SourceGenMediator.Generator/MediatorIncrementalGenerator.cs` finds handler types, validates them, and emits `SourceGenMediator.Generated.g.cs` containing `AddSourceGenMediatorGenerated()`.
3. Your app calls `services.AddSourceGenMediatorGenerated()`, which registers handler interfaces, a singleton `MediatorDispatchMap`, `IMediator`/`ISender`/`IPublisher` backed by `GeneratedMediator`, and a default `INotificationPublisher`.
4. `src/SourceGenMediator.Runtime/GeneratedMediator.cs` looks up the request/notification/stream type in the dispatch map and invokes the generated per-request pipeline. Pipeline behaviors and processors are resolved from DI at runtime (`sp.GetServices<>`), not generated.

## Projects

| Project | Purpose |
|---|---|
| `src/SourceGenMediator` | Meta-package: install this to get Abstractions, Runtime, DI, and the Generator with aligned versions |
| `src/SourceGenMediator.Abstractions` | Contracts only: `IRequest`, `ICommand`, `IQuery`, `ISender`, `IPublisher`, `IMediator`, handlers, pipeline, notifications, streams, `Unit` |
| `src/SourceGenMediator.Generator` | `netstandard2.0` Roslyn `IIncrementalGenerator` (`MediatorIncrementalGenerator`, `ModelBuilder`, `Emitter`, `Validator`, `WellKnownTypes`, `DiagnosticCatalog`) |
| `src/SourceGenMediator.Runtime` | `GeneratedMediator` + `MediatorDispatchMap`, `ForeachAwaitPublisher` (default), `TaskWhenAllPublisher` (opt-in) |
| `src/SourceGenMediator.DependencyInjection` | Transitive DI dependency; the `AddSourceGenMediatorGenerated()` method itself is emitted by the generator into the consuming assembly |
| `samples/SourceGenMediator.SampleApp` | Minimal console sample |
| `tests/SourceGenMediator.Tests` | Roslyn generator tests + hand-built dispatch-map unit tests |
| `tests/SourceGenMediator.IntegrationTests` | End-to-end tests using the real generator |
| `benchmarks/SourceGenMediator.Benchmarks` | BenchmarkDotNet comparison with MediatR 12.5.0 |

## Quickstart

### From NuGet (recommended, once published)

Install the meta-package — it pulls Abstractions, Runtime, DI integration, and the source generator (as an analyzer) with aligned versions:

```powershell
dotnet add package SourceGenMediator --prerelease
```

```xml
<PackageReference Include="SourceGenMediator" Version="1.0.0-preview.1" />
```

### From source

Reference the projects directly (the generator must be an analyzer):

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\SourceGenMediator.Abstractions\SourceGenMediator.Abstractions.csproj" />
  <ProjectReference Include="..\..\src\SourceGenMediator.Runtime\SourceGenMediator.Runtime.csproj" />
  <ProjectReference Include="..\..\src\SourceGenMediator.DependencyInjection\SourceGenMediator.DependencyInjection.csproj" />
  <ProjectReference Include="..\..\src\SourceGenMediator.Generator\SourceGenMediator.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

Then register and send (from `samples/SourceGenMediator.SampleApp/Program.cs`):

```csharp
using Microsoft.Extensions.DependencyInjection;
using SourceGenMediator.Abstractions;
using SourceGenMediator.DependencyInjection;

var services = new ServiceCollection();
services.AddSourceGenMediatorGenerated();

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
var id = await mediator.Send(new CreateOrderCommand("SKU-123"));

public sealed record CreateOrderCommand(string Sku) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
        => ValueTask.FromResult(Guid.NewGuid());
}
```

## Usage

### Requests and CQRS

- `IRequest<TResponse>` with `IRequestHandler<TReq, TResp>` for the general case
- `ICommand<T>` / `ICommand` with `ICommandHandler<,>` for void or value-returning commands
- `IQuery<T>` with `IQueryHandler<,>` for reads
- `ISender.Send(...)` has generic (`Send<TRequest, TResponse>`) and non-generic overloads; void requests use the `Unit` struct
- Prefer `sealed` requests and handlers (warnings `SM2001`/`SM2002` otherwise)

### Pipeline

Register open generics manually; they are picked up at runtime:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IRequestPreProcessor<>), typeof(ValidationPreProcessor<>));
services.AddTransient(typeof(IRequestPostProcessor<,>), typeof(MetricsPostProcessor<,>));
```

- Behaviors wrap the handler (`RequestHandlerDelegate<TResponse> next`); when none are registered the generated code takes a direct fast path
- Exception handling: implement `IRequestExceptionHandler<TReq, TResp>` to return a fallback, or `IRequestExceptionAction<TReq>` for side effects before rethrow

### Notifications

```csharp
public sealed record OrderCreated(Guid Id) : INotification;

public sealed class AuditTrail : INotificationHandler<OrderCreated>
{
    public ValueTask Handle(OrderCreated notification, CancellationToken ct)
        => ValueTask.CompletedTask;
}

await mediator.Publish(new OrderCreated(id));
```

Publishing with no handlers is a no-op. The default `ForeachAwaitPublisher` awaits sequentially; replace `INotificationPublisher` with `TaskWhenAllPublisher` (`src/SourceGenMediator.Runtime/TaskWhenAllPublisher.cs`) for parallel fan-out.

### Streaming

```csharp
public sealed record Search(string Term) : IStreamRequest<string>;

await foreach (var item in mediator.CreateStream(new Search("sku")))
{
}
```

Wrap with `IStreamPipelineBehavior<TReq, TResp>`; behaviors apply outermost-first via reverse enumeration.

## Generator diagnostics

| Code | Severity | Meaning |
|---|---|---|
| `SM1001` | Error | Request/stream has no handler |
| `SM1002` | Error | Request/stream has multiple handlers; exactly one is required |
| `SM1003` | Error | Handler must be `public` |
| `SM1004` | Error | Strict-CQRS violation (only with `SourceGenMediator_StrictCqrs=true`) |
| `SM1005` | Error | Request type cannot be abstract |
| `SM2001`/`SM2002` | Warning | Request/handler should be `sealed` |
| `SM3001` | Info | Suggest `ICommand`/`IQuery` over plain `IRequest` |

Enable strict CQRS with MSBuild:

```xml
<PropertyGroup>
  <SourceGenMediator_StrictCqrs>true</SourceGenMediator_StrictCqrs>
</PropertyGroup>
```

The generated file is `SourceGenMediator.Generated.g.cs` and uses fully-qualified type names.

## Benchmarks

Run with `dotnet run -c Release -f net10.0 --project benchmarks/SourceGenMediator.Benchmarks/SourceGenMediator.Benchmarks.csproj`. Sample results on .NET 10 (machine-specific, Intel i7-10875H):

| Method | Mean | Allocated |
|---|---|---:|
| SourceGenMediator: Send | 67.50 ns | 24 B |
| MediatR: Send (baseline) | 73.99 ns | 200 B |
| SourceGenMediator: Send + 1 behavior | 171.34 ns | 224 B |
| MediatR: Send + 1 behavior | 161.94 ns | 440 B |
| SourceGenMediator: Publish | 99.08 ns | 152 B |
| MediatR: Publish | 107.30 ns | 288 B |

SourceGenMediator is faster on plain `Send`/`Publish` with substantially fewer allocations; the behavior path trades a few nanoseconds for roughly half the allocations.

## Building, testing, AOT

```powershell
dotnet build SourceGenMediator.slnx -c Release
dotnet test SourceGenMediator.slnx -c Release
dotnet publish samples/SourceGenMediator.SampleApp/SourceGenMediator.SampleApp.csproj -f net10.0 -r linux-x64 -p:EnableAotCheck=true
```

CI (`.github/workflows/aot.yml`) publishes the sample as NativeAOT and runs the binary. The sample csproj scopes `PublishAot` behind `EnableAotCheck=true` so the `netstandard2.0` analyzer project is unaffected.

## Releasing

Versions come from git tags via MinVer (see `Directory.Build.props`; tags must be `v`-prefixed per `MinVerTagPrefix`):

```powershell
git tag v1.0.0-preview.1; git push origin v1.0.0-preview.1
```

Pushing a `v*` tag triggers `.github/workflows/nuget.yml`, which builds, tests, packs all five packages with the tag version, and pushes to nuget.org using Trusted Publishing (OIDC, no API key). Use MAJOR for breaking changes, MINOR for features, PATCH for fixes.

## License

MIT — see [LICENSE](LICENSE).
