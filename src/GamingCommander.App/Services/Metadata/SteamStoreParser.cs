using System.Text.Json;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// Parses Steam Store <c>appdetails</c> JSON into source facts.
/// HTTP 200 is not success — the envelope <c>success</c> flag is required.
/// </summary>
public static class SteamStoreParser
{
    /// <summary>
    /// Returns facts for <paramref name="appId"/>, or null if the payload is missing,
    /// not JSON, or <c>success</c> is false.
    /// </summary>
    public static SteamStoreFacts? Parse(string json, string appId)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(appId))
            return null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty(appId, out JsonElement block))
                return null;

            if (!block.TryGetProperty("success", out JsonElement successEl) || !successEl.GetBoolean())
                return new SteamStoreFacts(false, null, [], [], null, [], null, null, appId, null, null);

            if (!block.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Object)
                return new SteamStoreFacts(false, null, [], [], null, [], null, null, appId, null, null);

            string? name = GetString(data, "name");
            string? steamAppId = data.TryGetProperty("steam_appid", out JsonElement idEl) && idEl.ValueKind == JsonValueKind.Number
                ? idEl.GetRawText()
                : appId;

            int? score = null;
            if (data.TryGetProperty("metacritic", out JsonElement meta)
                && meta.ValueKind == JsonValueKind.Object
                && meta.TryGetProperty("score", out JsonElement scoreEl)
                && scoreEl.TryGetInt32(out int s))
            {
                score = s;
            }

            string? release = null;
            if (data.TryGetProperty("release_date", out JsonElement rel)
                && rel.ValueKind == JsonValueKind.Object)
            {
                release = GetString(rel, "date");
            }

            return new SteamStoreFacts(
                Success: true,
                Name: name,
                Developers: GetStringArray(data, "developers"),
                Publishers: GetStringArray(data, "publishers"),
                ReleaseDate: release,
                Genres: GetGenreDescriptions(data),
                ShortDescription: GetString(data, "short_description"),
                MetacriticScore: score,
                SteamAppId: steamAppId,
                HeaderImage: GetString(data, "header_image"),
                Website: GetString(data, "website"));
        }
    }

    private static string? GetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.String)
            return null;
        string? v = el.GetString();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<string>();
        foreach (JsonElement item in el.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                list.Add(item.GetString()!);
        }

        return list;
    }

    private static IReadOnlyList<string> GetGenreDescriptions(JsonElement data)
    {
        if (!data.TryGetProperty("genres", out JsonElement el) || el.ValueKind != JsonValueKind.Array)
            return [];

        var list = new List<string>();
        foreach (JsonElement item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            string? desc = GetString(item, "description");
            if (desc is not null)
                list.Add(desc);
        }

        return list;
    }
}
