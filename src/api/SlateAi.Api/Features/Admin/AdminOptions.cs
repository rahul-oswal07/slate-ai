namespace SlateAi.Api.Features.Admin;

public sealed class AdminOptions
{
    public const string Section = "Admin";

    /// <summary>Shorter keys leave the admin endpoints unmapped.</summary>
    public const int MinKeyLength = 16;

    /// <summary>
    /// The secret in the admin page's link (<c>/admin/greetings#key=…</c>), sent back as the <c>X-Admin-Key</c> header.
    /// From user-secrets or an App Service setting (<c>Admin__Key</c>), never the repo. Unset: no admin endpoints.
    /// </summary>
    public string? Key { get; set; }

    public bool Enabled => Key is { Length: >= MinKeyLength };
}
