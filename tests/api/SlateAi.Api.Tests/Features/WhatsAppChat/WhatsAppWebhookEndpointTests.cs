using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlateAi.Api.Features.Delivery;
using SlateAi.Api.Features.EveningMessages;
using SlateAi.Api.Features.TodaysMessage;
using SlateAi.Api.Features.WhatsAppChat;
using SlateAi.Api.Speech;
using SlateAi.Api.Tests.Infrastructure;

namespace SlateAi.Api.Tests.Features.WhatsAppChat;

/// <summary>T-106: Meta's webhook for today's message inside the chat, over HTTP with fakes (no database, no Meta).</summary>
public sealed class WhatsAppWebhookEndpointTests
{
    private const string Url = "/api/whatsapp/webhook";
    private const string Secret = "test-app-secret";
    private const string Token = "tok_abcdefghijklmnopqr";
    private const string Phone = "+919812345678";
    private const string From = "919812345678";
    private const string ChildName = "Ravikumar";
    private static readonly CancellationToken Ct = TestContext.Current.CancellationToken;
    private static readonly DateTimeOffset Now = new(2026, 11, 20, 13, 0, 0, TimeSpan.Zero);
    private static readonly Guid Child = Guid.NewGuid();
    private static readonly Guid Greeting = Guid.NewGuid();
    private static readonly Guid Disclosure = Guid.NewGuid();
    private static readonly Guid Concept = Guid.NewGuid();

    private readonly StubHttpHandler _http = new(
        () => StubHttpHandler.Json("""{"id":"media-1","messages":[{"id":"wamid.OUT"}]}"""));

    private readonly FakeWhatsAppChatStore _store = new();
    private readonly LogCapture _logs = new();

    public WhatsAppWebhookEndpointTests()
    {
        _store.Lessons[Token] = Lesson();
        var ogg = OpusEncoding.FromWav(FakeTextToSpeech.SilentWav(2)).Ogg;
        foreach (var clip in new[] { Greeting, Disclosure, Concept })
        {
            _store.Clips[clip] = (ogg, OpusEncoding.ContentType);
        }

        _store.ByPhone[Phone] = [new ChatChild(Child, "te")];
    }

    [Fact]
    public async Task Off_by_default_the_webhook_is_not_there()
    {
        await using var factory = Create(enabled: false);
        using var client = factory.CreateClient();

        var verify = await client.GetAsync($"{Url}?hub.mode=subscribe&hub.verify_token=verify-me&hub.challenge=42", Ct);
        var post = await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token))));

        Assert.Equal(HttpStatusCode.NotFound, verify.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task Verify_echoes_the_challenge_only_for_our_token()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var ok = await client.GetAsync($"{Url}?hub.mode=subscribe&hub.verify_token=verify-me&hub.challenge=1158201444", Ct);
        var wrong = await client.GetAsync($"{Url}?hub.mode=subscribe&hub.verify_token=guess&hub.challenge=1158201444", Ct);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("1158201444", await ok.Content.ReadAsStringAsync(Ct));
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
    }

    [Theory]
    [InlineData("sha256=0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("not-a-signature")]
    [InlineData(null)]
    public async Task A_bad_or_missing_signature_gets_401_and_nothing_is_handled(string? signature)
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var response = await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token))), signature ?? "");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task Send_it_here_sends_the_voice_notes_text_picture_question_and_reason_list_in_order()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var response = await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token))));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Greeting: upload + voice note; disclosure and concept joined (D-127): one upload + one voice note; then the rest.
        Assert.Equal(
            ["media", "audio", "media", "audio", "text", "button", "list"],
            _http.Requests.Select(r => r.Request.RequestUri!.AbsolutePath.EndsWith("/media", StringComparison.Ordinal)
                ? "media"
                : Kind(r.Body)));
        var messages = _http.Requests.Where(r => !r.Request.RequestUri!.AbsolutePath.EndsWith("/media", StringComparison.Ordinal))
            .Select(r => JsonDocument.Parse(r.Body).RootElement).ToList();
        Assert.All(messages, m => Assert.Equal(From, m.GetProperty("to").GetString()));
        var text = messages[2].GetProperty("text");
        Assert.Contains("ఈ రోజు పాఠం", text.GetProperty("body").GetString());
        Assert.Contains("ఇంటి పని: నలికలి గణితం భాగం 1, పేజీ 4", text.GetProperty("body").GetString());
        Assert.False(text.GetProperty("preview_url").GetBoolean());
        var buttons = messages[3].GetProperty("interactive").GetProperty("action").GetProperty("buttons").EnumerateArray()
            .Select(b => b.GetProperty("reply").GetProperty("id").GetString()).ToList();
        Assert.Equal([$"pq:{Token}:>", $"pq:{Token}:<", $"pq:{Token}:="], buttons);
        var rows = messages[4].GetProperty("interactive").GetProperty("action").GetProperty("sections")[0].GetProperty("rows");
        Assert.Equal(5, rows.GetArrayLength());
        // The answer never reaches the phone (guardrail 4); names and numbers never reach the logs (NFR-05).
        Assert.DoesNotContain(_http.Requests, r => r.Body.Contains("\"answer\"", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(_logs.Lines, l => l.Contains(ChildName) || l.Contains("9812345678"));
    }

    [Fact]
    public async Task A_repeated_event_is_handled_once()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();
        var body = Event(Tap("wamid.SAME", ChatReplyIds.ForLesson(Token)));

        await PostAsync(client, body);
        var sent = _http.Requests.Count;
        var again = await PostAsync(client, body);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(7, sent);
        Assert.Equal(sent, _http.Requests.Count);
    }

    [Fact]
    public async Task Each_clip_is_uploaded_once_and_its_media_id_reused()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token))));
        await PostAsync(client, Event(Tap("wamid.2", ChatReplyIds.ForLesson(Token))));

        Assert.Equal(2, _http.Requests.Count(r => r.Request.RequestUri!.AbsolutePath.EndsWith("/media", StringComparison.Ordinal)));
        Assert.Equal(4, _http.Requests.Count(r => r.Body.StartsWith('{') && Kind(r.Body) == "audio"));
    }

    [Fact]
    public async Task A_late_event_after_the_window_closed_sends_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token), at: Now.AddHours(-24).AddSeconds(-1))));

        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task An_unknown_token_or_a_number_not_saved_for_the_family_gets_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        var unknown = await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson("tok_unknownunknownunkn"))));
        var stranger = await PostAsync(client, Event(Tap("wamid.2", ChatReplyIds.ForLesson(Token), from: "919000000000")));

        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stranger.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Theory]
    [InlineData("not-approved")]
    [InlineData("stopped")]
    [InlineData("no-consent")]
    [InlineData("expired")]
    public async Task Nothing_is_sent_unless_approved_consented_not_stopped_and_not_expired(string rule)
    {
        // Guardrail 3 and the send rules, checked again at tap time.
        _store.Lessons[Token] = rule switch
        {
            "not-approved" => Lesson() with { TodaysApproved = false },
            "stopped" => Lesson() with { Stopped = true },
            "no-consent" => Lesson() with { Consent = "no" },
            _ => Lesson() with { ExpiresAt = Now.AddMinutes(-1) },
        };
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Tap("wamid.1", ChatReplyIds.ForLesson(Token))));

        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task A_picture_tap_is_saved_for_Today_and_answered_without_right_or_wrong()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Reply("wamid.1", ChatReplyIds.ForPicture(Token, ">"))));
        await PostAsync(client, Event(Reply("wamid.2", ChatReplyIds.ForPicture(Token, "<"))));
        await PostAsync(client, Event(Reply("wamid.3", ChatReplyIds.ForPicture(Token, "abc"))));

        Assert.Equal((">", "<"), _store.Pictures[Token]);
        var replies = _http.Requests.Select(r => JsonDocument.Parse(r.Body).RootElement.GetProperty("text").GetProperty("body").GetString()).ToList();
        Assert.Equal(2, replies.Count);
        Assert.Contains(">", replies[0]);
        Assert.Contains("టీచర్", replies[1]);
    }

    [Fact]
    public async Task A_reason_from_the_list_is_saved_and_thanked()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Reply("wamid.1", ChatReplyIds.ForReason(Token, "festival"), list: true)));
        await PostAsync(client, Event(Reply("wamid.2", ChatReplyIds.ForReason(Token, "diagnosis"), list: true)));

        var (childId, category, _) = Assert.Single(_store.Reasons);
        Assert.Equal((Child, "festival"), (childId, category));
        Assert.Single(_http.Requests);
    }

    [Theory]
    [InlineData("STOP")]
    [InlineData(" stop. ")]
    [InlineData("ఆపండి")]
    public async Task A_stop_reply_stops_the_family_and_confirms(string text)
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Text("wamid.1", text)));

        Assert.Equal([Child], _store.Stopped);
        var reply = JsonDocument.Parse(Assert.Single(_http.Requests).Body).RootElement;
        Assert.Equal("సందేశాలు ఆపేశాం", reply.GetProperty("text").GetProperty("body").GetString());
    }

    [Fact]
    public async Task A_late_stop_still_stops_but_sends_nothing()
    {
        await using var factory = Create();
        using var client = factory.CreateClient();

        await PostAsync(client, Event(Text("wamid.1", "STOP", at: Now.AddDays(-2))));

        Assert.Equal([Child], _store.Stopped);
        Assert.Empty(_http.Requests);
    }

    private static ChatLesson Lesson() => new(
        Child, Guid.NewGuid(), new DateOnly(2026, 11, 20), "te", ChildName, "yes", false, Now.AddDays(14), [Phone], true,
        new SlotValues("పది తీసుకుని తీసివేత", "నలికలి గణితం భాగం 1, పేజీ 4"),
        new ParentTodaysMessage(
            "ఈ రోజు పాఠం ఒక ఆలోచన.", ["పుల్లలు తీసుకోండి.", "కట్టలు కట్టండి."], new ParentPicture(47, 52), null, null),
        ">", null, Greeting, Disclosure, Concept);

    private ApiFactory Create(bool enabled = true) => FakeApi.Create(
        services =>
        {
            services.AddHttpClient<WhatsAppClient>().ConfigurePrimaryHttpMessageHandler(() => _http);
            services.AddSingleton<IWhatsAppChatStore>(_store);
            services.AddSingleton<TimeProvider>(new FixedClock(Now));
            services.AddSingleton<ILoggerProvider>(_logs);
        },
        settings: new Dictionary<string, string?>
        {
            ["WhatsApp:PhoneNumberId"] = "1234567890",
            ["WhatsApp:AccessToken"] = "test-token",
            ["WhatsApp:AppSecret"] = Secret,
            ["WhatsApp:VerifyToken"] = "verify-me",
            ["WhatsApp:ChatLesson:Enabled"] = enabled ? "true" : "false",
        });

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string json, string? signature = null)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new("application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = content };
        signature ??= WebhookPayload.Sign(bytes, Secret);
        if (signature.Length > 0)
        {
            request.Headers.TryAddWithoutValidation(WebhookPayload.SignatureHeader, signature);
        }

        return await client.SendAsync(request, Ct);
    }

    private static string Kind(string body)
    {
        var root = JsonDocument.Parse(body).RootElement;
        var type = root.GetProperty("type").GetString()!;
        return type == "interactive" ? root.GetProperty("interactive").GetProperty("type").GetString()! : type;
    }

    /// <summary>A whatsapp_business_account event with these messages, for our phone number ID.</summary>
    public static string Event(params object[] messages) => JsonSerializer.Serialize(new
    {
        @object = "whatsapp_business_account",
        entry = new[]
        {
            new
            {
                id = "WABA",
                changes = new[]
                {
                    new
                    {
                        field = "messages",
                        value = new
                        {
                            messaging_product = "whatsapp",
                            metadata = new { display_phone_number = "15550000000", phone_number_id = "1234567890" },
                            contacts = new[] { new { profile = new { name = ChildName + "'s dad" }, wa_id = From } },
                            messages,
                        },
                    },
                },
            },
        },
    });

    /// <summary>A tap on the template's quick reply.</summary>
    private static object Tap(string id, string payload, string from = From, DateTimeOffset? at = null) => new
    {
        from, id, timestamp = Seconds(at), type = "button", button = new { payload, text = "Send it here" },
    };

    /// <summary>A tap on a reply button or list row we sent.</summary>
    private static object Reply(string id, string replyId, bool list = false) => list
        ? new { from = From, id, timestamp = Seconds(null), type = "interactive", interactive = (object)new { type = "list_reply", list_reply = new { id = replyId, title = "x" } } }
        : new { from = From, id, timestamp = Seconds(null), type = "interactive", interactive = (object)new { type = "button_reply", button_reply = new { id = replyId, title = "x" } } };

    private static object Text(string id, string body, DateTimeOffset? at = null) => new
    {
        from = From, id, timestamp = Seconds(at), type = "text", text = new { body },
    };

    private static string Seconds(DateTimeOffset? at) => (at ?? Now.AddSeconds(-5)).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Every formatted log line, to check that no name or number is logged.</summary>
    private sealed class LogCapture : ILoggerProvider
    {
        public ConcurrentQueue<string> Lines { get; } = new();

        public ILogger CreateLogger(string categoryName) => new Logger(Lines);

        public void Dispose()
        {
        }

        private sealed class Logger(ConcurrentQueue<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                lines.Enqueue(formatter(state, exception));
        }
    }
}
