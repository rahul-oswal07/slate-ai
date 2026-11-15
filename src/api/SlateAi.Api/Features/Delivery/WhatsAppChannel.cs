using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// Sends over WhatsApp to the number saved on the admin page (T-118, D-110), else the registered test phone in
/// <see cref="WhatsAppOptions.Recipients"/> (on the Cloud API test number Meta delivers only to registered phones, at most
/// 5); a family with neither is skipped and keeps the demo chat. Step 1 of T-40: an approved template
/// whose URL button opens the parent page, where the greeting, lesson audio and catch-up already play. Each kind goes as
/// its approved template (T-104, D-98); a kind or language without one is skipped (<c>no-template</c>), as is the plain
/// evening message until its factual template is approved (D-107).
/// </summary>
public sealed class WhatsAppChannel(WhatsAppClient client, IOptions<WhatsAppOptions> options) : IParentChannel
{
    public string Name => DeliveryChannels.WhatsApp;

    public Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if ((message.Phone ?? settings.RecipientFor(message.ChildId)) is not { } number)
        {
            return Task.FromResult(ChannelResult.Skipped("no-recipient"));
        }

        var template = message.Kind switch
        {
            DeliveryKinds.Todays => settings.Todays,
            DeliveryKinds.CatchUp => settings.CatchUp,
            _ => settings.Evening,
        };
        if (string.IsNullOrWhiteSpace(template.Name) || !template.Languages.TryGetValue(message.Language, out var language))
        {
            return Task.FromResult(ChannelResult.Skipped("no-template"));
        }

        // Code couldn't fill the template (a lesson not in Nalikali, a name missing in this language): send nothing
        // rather than a half-filled or wrong message.
        if (message.Parameters.Count == 0)
        {
            return Task.FromResult(ChannelResult.Skipped("no-values"));
        }

        if (template.WordingVersion is { } version && message.WordingVersion != version)
        {
            return Task.FromResult(ChannelResult.Skipped("wording-version"));
        }

        return client.SendTemplateAsync(number, template.Name, language, message.Parameters, message.Token, cancellationToken);
    }
}
