using GamingCommander.Core;
using GamingCommander.Core.Models;

namespace GamingCommander.App.Services.Metadata;

/// <summary>
/// PCGW first (AppID page, else name). Then Steam Store if an AppID is known
/// locally or was read from the PCGW page. Never writes <c>games.json</c> / DisplayName.
/// </summary>
public sealed class MetadataService : IMetadataService
{
    public static readonly TimeSpan Freshness = TimeSpan.FromDays(30);

    private readonly IMetadataStore _store;
    private readonly IConfigService _config;
    private readonly SteamStoreLookup? _steam;
    private readonly PcgwLookup? _pcgw;
    private readonly MetadataOnlineGate? _online;

    public MetadataService(
        IMetadataStore store,
        IConfigService config,
        SteamStoreLookup? steam = null,
        PcgwLookup? pcgw = null,
        MetadataOnlineGate? online = null)
    {
        _store = store;
        _config = config;
        _steam = steam;
        _pcgw = pcgw;
        _online = online;
    }

    /// <summary>True when the sidecar is missing or older than <see cref="Freshness"/>.</summary>
    public static bool IsStale(GameMetadataRecord? record) =>
        record?.LastUpdated is not DateTimeOffset updated
        || DateTimeOffset.UtcNow - updated >= Freshness;

    /// <inheritdoc />
    public async Task<GameMetadataRecord?> RefreshAsync(
        string gameEntryId,
        string? steamAppId,
        string? displayName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(gameEntryId))
            return null;

        GameMetadataRecord? existing = _store.Get(gameEntryId);
        if (!_config.Load().EnableOnlineMetadata)
            return existing;

        if (_online is { AllowsHttp: false })
            return existing;

        if (existing?.LastUpdated is DateTimeOffset updated
            && DateTimeOffset.UtcNow - updated < Freshness)
        {
            return existing;
        }

        GameMetadataRecord? merged = existing;

        if (_pcgw is not null)
        {
            PcgwLookupResult? page = await _pcgw.LookupAsync(steamAppId, displayName, cancellationToken)
                .ConfigureAwait(false);
            GameMetadataRecord? rec = page is null ? null : CommonMetadataParser.ToRecord(page, gameEntryId);
            merged = Merge(merged, rec, MetadataSource.Pcgw);
        }

        string? appId = FirstNonEmpty(steamAppId, merged?.SteamAppId);
        if (_steam is not null && appId is not null)
        {
            string? raw = await _steam.FetchRawAsync(appId, cancellationToken).ConfigureAwait(false);
            SteamStoreFacts? facts = raw is null ? null : SteamStoreParser.Parse(raw, appId);
            GameMetadataRecord? rec = facts is null ? null : CommonMetadataParser.ToRecord(facts, gameEntryId);
            merged = Merge(merged, rec, MetadataSource.Steam);
        }

        if (merged is not null)
        {
            merged = merged with
            {
                GameEntryId = gameEntryId,
                LastUpdated = DateTimeOffset.UtcNow,
            };
            _store.Upsert(gameEntryId, merged);
        }

        return merged;
    }

    /// <summary>
    /// Incoming fills holes. PCGW overwrites release date, engine, and wiki URL when it has them.
    /// </summary>
    internal static GameMetadataRecord? Merge(
        GameMetadataRecord? existing,
        GameMetadataRecord? incoming,
        MetadataSource incomingSource)
    {
        if (incoming is null)
            return existing;
        if (existing is null)
            return incoming;

        bool pcgw = incomingSource == MetadataSource.Pcgw;
        return existing with
        {
            Developer = existing.Developer ?? incoming.Developer,
            Publisher = existing.Publisher ?? incoming.Publisher,
            Genre = existing.Genre ?? incoming.Genre,
            Description = existing.Description ?? incoming.Description,
            CoverArtUrl = existing.CoverArtUrl ?? incoming.CoverArtUrl,
            OfficialWebsite = existing.OfficialWebsite ?? incoming.OfficialWebsite,
            MetacriticScore = existing.MetacriticScore ?? incoming.MetacriticScore,
            SteamAppId = existing.SteamAppId ?? incoming.SteamAppId,
            GogGameId = existing.GogGameId ?? incoming.GogGameId,
            ReleaseDate = pcgw && incoming.ReleaseDate is not null
                ? incoming.ReleaseDate
                : existing.ReleaseDate ?? incoming.ReleaseDate,
            Engine = pcgw && incoming.Engine is not null
                ? incoming.Engine
                : existing.Engine ?? incoming.Engine,
            PcGamingWikiUrl = incoming.PcGamingWikiUrl ?? existing.PcGamingWikiUrl,
            LastMetadataSource = incoming.LastMetadataSource ?? existing.LastMetadataSource,
            Details = incoming.Details is { HasAny: true } ? incoming.Details : existing.Details,
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (string? value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }
}
