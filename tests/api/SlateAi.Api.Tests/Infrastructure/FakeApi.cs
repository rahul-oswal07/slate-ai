using Microsoft.Extensions.DependencyInjection;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>The API with no database: migrations off, unreachable connection string, services replaced by fakes.</summary>
public static class FakeApi
{
    private const string NoDatabase = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1";

    public static ApiFactory Create(Action<IServiceCollection> services) =>
        new(NoDatabase, initialiseDatabase: false, services: services);
}

/// <summary>A clock stuck at one instant.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
