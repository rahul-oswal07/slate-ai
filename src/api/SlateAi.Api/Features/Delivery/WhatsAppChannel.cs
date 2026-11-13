using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// Sends over WhatsApp only to the registered test phones (<see cref="WhatsAppOptions.Recipients"/>, at most 5 on the
/// Cloud API test number); every other family is skipped and keeps the demo chat. Step 1 of T-40: an approved template
/// whose URL button opens the parent page, where the greeting, lesson audio and catch-up already play.
/// </summary>
public sealed class WhatsAppChannel(WhatsAppClient client, IOptions<WhatsAppOptions> options) : IParentChannel
{
    public string Name => DeliveryChannels.WhatsApp;

    public Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.RecipientFor(message.ChildId) is not { } number)
        {
            return Task.FromResult(ChannelResult.Skipped("no-recipient"));
        }

        var template = message.Kind == DeliveryKinds.Evening ? settings.Evening : settings.CatchUp;
        if (string.IsNullOrWhiteSpace(template.Name) || !template.Languages.TryGetValue(message.Language, out var language))
        {
            return Task.FromResult(ChannelResult.Skipped("no-template"));
        }

        if (template.WordingVersion is { } version && message.WordingVersion != version)
        {
            return Task.FromResult(ChannelResult.Skipped("wording-version"));
        }

        return client.SendTemplateAsync(number, template.Name, language, message.Parameters, message.Token, cancellationToken);
    }
}
