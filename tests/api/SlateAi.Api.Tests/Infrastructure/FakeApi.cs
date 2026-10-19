using Microsoft.Extensions.DependencyInjection;

namespace SlateAi.Api.Tests.Infrastructure;

/// <summary>The API with no database: migrations off, unreachable connection string, services replaced by fakes.</summary>
public static class FakeApi
{
    private const string NoDatabase = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1";

    public static ApiFactory Create(
        Action<IServiceCollection>? services = null,
        IReadOnlyDictionary<string, string?>? settings = null,
        string? webRoot = null) =>
        new(NoDatabase, initialiseDatabase: false, webRoot: webRoot, settings: settings, services: services);
}

/// <summary>A clock stuck at one instant.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
