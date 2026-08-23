using GamingCommander.Core.Models;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Same game vs a different/garbled page. Used so a stale refresh cannot replace
/// a good sidecar with System Shock when the name search went wrong.
/// </summary>
internal static class MetadataIdentity
{
    public static bool Compatible(GameMetadataRecord existing, GameMetadataRecord incoming)
    {
        if (!HasIdentity(existing))
            return true;

        string? oldUrl = NormalizeUrl(existing.PcGamingWikiUrl);
        string? newUrl = NormalizeUrl(incoming.PcGamingWikiUrl);
        bool sameUrl = oldUrl is not null && newUrl is not null
            && oldUrl.Equals(newUrl, StringComparison.OrdinalIgnoreCase);

        string? oldId = NormalizeId(existing.SteamAppId);
        string? newId = NormalizeId(incoming.SteamAppId);
        bool sameSteam = oldId is not null && newId is not null && oldId == newId;

        if (oldUrl is not null && newUrl is not null && !sameUrl)
            return false;
        if (oldId is not null && newId is not null && !sameSteam)
            return false;

        // Same URL is not enough: vandalism can keep /wiki/ELEX and say "HELLO PONY".
        if (!IncomingHasText(incoming))
            return sameUrl || sameSteam || !HasIdentity(existing);

        return Overlaps(existing, incoming)
            || OverlapsSlug(existing.PcGamingWikiUrl, incoming)
            || OverlapsSlug(incoming.PcGamingWikiUrl, existing);
    }

    private static bool IncomingHasText(GameMetadataRecord r) =>
        !string.IsNullOrWhiteSpace(r.Developer)
        || !string.IsNullOrWhiteSpace(r.Publisher)
        || !string.IsNullOrWhiteSpace(r.Engine)
        || !string.IsNullOrWhiteSpace(r.Genre);

    private static bool Overlaps(GameMetadataRecord a, GameMetadataRecord b) =>
        SharesToken(a.Developer, b.Developer)
        || SharesToken(a.Publisher, b.Publisher)
        || SharesToken(a.Engine, b.Engine)
        || SharesToken(a.Genre, b.Genre)
        || SharesToken(a.Developer, b.Publisher)
        || SharesToken(a.Publisher, b.Developer);

    private static bool OverlapsSlug(string? wikiUrl, GameMetadataRecord other)
    {
        string? slug = SlugFromWiki(wikiUrl);
        if (slug is null)
            return false;
        return SharesToken(slug, other.Developer)
            || SharesToken(slug, other.Publisher)
            || SharesToken(slug, other.Engine)
            || SharesToken(slug, other.Genre)
            || SharesToken(slug, other.Description);
    }

    private static string? SlugFromWiki(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        int slash = url.LastIndexOf('/');
        string slug = slash >= 0 ? url[(slash + 1)..] : url;
        return Uri.UnescapeDataString(slug).Replace('_', ' ');
    }

    public static bool HasIdentity(GameMetadataRecord r) =>
        !string.IsNullOrWhiteSpace(r.PcGamingWikiUrl)
        || !string.IsNullOrWhiteSpace(r.SteamAppId)
        || !string.IsNullOrWhiteSpace(r.Developer)
        || !string.IsNullOrWhiteSpace(r.Publisher)
        || r.Details is { HasAny: true };

    private static string? NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        return url.Trim().TrimEnd('/').ToLowerInvariant();
    }

    private static string? NormalizeId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;
        return id.Trim();
    }

    private static bool SharesToken(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;
        foreach (string ta in Tokens(a))
        {
            foreach (string tb in Tokens(b))
            {
                if (ta == tb)
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Tokens(string text) =>
        text.Split(" ,/&;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length >= 4);
}
