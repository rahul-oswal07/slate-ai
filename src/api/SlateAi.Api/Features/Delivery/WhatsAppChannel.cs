using Microsoft.Extensions.Options;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// Sends over WhatsApp only to the registered test phones (<see cref="WhatsAppOptions.Recipients"/>, at most 5 on the
/// Cloud API test number); every other family is skipped and keeps the demo chat. Step 1 of T-40: an approved template
/// whose URL button opens the parent page, where the greeting, lesson audio and catch-up already play.
/// </summary>
/// <param name="helloWorld">
/// Temporary, until <c>todays_lesson_link_v1</c> and <c>catchup_link_v1</c> are approved (T-104, #182): every delivery
/// sends Meta's pre-approved <c>hello_world</c> instead (no parameters, no button, none of our text), which only proves a
/// message reaches the registered phone. T-104 removes this once the plain evening message (T-104 c) is settled.
/// </param>
public sealed class WhatsAppChannel(WhatsAppClient client, IOptions<WhatsAppOptions> options, bool helloWorld = true)
    : IParentChannel
{
    public string Name => DeliveryChannels.WhatsApp;

    public Task<ChannelResult> SendAsync(DeliveryMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (settings.RecipientFor(message.ChildId) is not { } number)
        {
            return Task.FromResult(ChannelResult.Skipped("no-recipient"));
        }

        if (helloWorld)
        {
            return client.SendTemplateAsync(
                number, DeliveryEndpoints.HelloWorld, DeliveryEndpoints.HelloWorldLanguage, [], null, cancellationToken);
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
