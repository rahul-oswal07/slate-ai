using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>Delivery to families' phones (T-40, FR-16): the outbox, its dispatcher and the configured channel.</summary>
public static class DeliverySetup
{
    public static IServiceCollection AddDelivery(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DeliveryOptions>()
            .Bind(configuration.GetSection(DeliveryOptions.Section))
            .Validate(
                o => o.Channel is DeliveryChannels.Demo or DeliveryChannels.WhatsApp && o.MaxAttempts > 0 && o.BatchSize > 0,
                $"{DeliveryOptions.Section}:Channel must be \"{DeliveryChannels.Demo}\" or \"{DeliveryChannels.WhatsApp}\".")
            .ValidateOnStart();
        services.Configure<WhatsAppOptions>(configuration.GetSection(WhatsAppOptions.Section));
        services.AddHttpClient<WhatsAppClient>(http => http.Timeout = TimeSpan.FromSeconds(20));
        services.AddScoped<WhatsAppChannel>();
        services.AddSingleton<DemoChannel>();
        services.AddScoped<IParentChannel>(sp =>
            sp.GetRequiredService<IOptions<DeliveryOptions>>().Value.Channel == DeliveryChannels.WhatsApp
                ? sp.GetRequiredService<WhatsAppChannel>()
                : sp.GetRequiredService<DemoChannel>());
        services.AddScoped<IDeliveryStore, DeliveryStore>();
        services.AddScoped<DeliveryDispatcher>();
        return services;
    }
}
