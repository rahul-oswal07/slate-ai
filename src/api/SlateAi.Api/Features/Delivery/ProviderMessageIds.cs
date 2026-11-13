using System.Security.Cryptography;
using System.Text;

namespace SlateAi.Api.Features.Delivery;

/// <summary>
/// What we keep of WhatsApp's message ID. A <c>wamid</c> is base64 that contains the recipient's phone number, so it is
/// never stored, logged or returned as is: only its SHA-256. Step 2's status webhook hashes the incoming wamid the same
/// way to find the row.
/// </summary>
public static class ProviderMessageIds
{
    public const string Prefix = "sha256:";

    public static string Hash(string providerMessageId) =>
        Prefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(providerMessageId)));
}
