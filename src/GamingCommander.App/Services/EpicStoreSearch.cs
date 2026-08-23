using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GamingCommander.App.Services;

/// <summary>
/// Documented regen step 2: <c>searchStore(keywords)</c> for public catalog ids + title.
/// See <c>docs/research/epic_item_format.md</c>.
/// </summary>
internal static class EpicStoreSearch
{
    public const string Endpoint = "https://store.epicgames.com/graphql";

    public sealed record Hit(string Title, string Namespace, string CatalogId);

    public static Hit? Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        string q = query.Replace("\\", " ").Replace("\"", " ");
        string gql = "{ Catalog { searchStore(start: 0, count: 8, keywords: \"" + q
            + "\") { elements { title id namespace } } } }";
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent", "GamingCommander/0.1 (epic item regen)");
            using var resp = http.PostAsync(
                    Endpoint,
                    new StringContent(
                        JsonSerializer.Serialize(new { query = gql }),
                        Encoding.UTF8,
                        "application/json"))
                .GetAwaiter()
                .GetResult();
            if (!resp.IsSuccessStatusCode)
                return null;
            string body = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("data", out var data)
                || !data.TryGetProperty("Catalog", out var cat)
                || !cat.TryGetProperty("searchStore", out var store)
                || !store.TryGetProperty("elements", out var els)
                || els.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            Hit? fallback = null;
            foreach (JsonElement el in els.EnumerateArray())
            {
                string title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                string ns = el.TryGetProperty("namespace", out var n) ? n.GetString() ?? "" : "";
                string id = el.TryGetProperty("id", out var i) ? i.GetString() ?? "" : "";
                if (ns.Length == 0 || id.Length == 0)
                    continue;
                var hit = new Hit(title, ns, id);
                if (title.Equals(query, StringComparison.OrdinalIgnoreCase))
                    return hit;
                fallback ??= hit;
                if (title.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                    && !title.Contains("Director", StringComparison.OrdinalIgnoreCase))
                    return hit;
            }

            return fallback;
        }
        catch
        {
            return null;
        }
    }
}
