# Software Bill of Materials

**Standard:** [CycloneDX](https://cyclonedx.org/) 1.6 JSON  
**File:** [`GamingCommander.cdx.json`](./GamingCommander.cdx.json)  
**Subject:** shipped app `GamingCommander.App` 0.4.0 and its NuGet dependencies (including transitives)  
**Generated:** CycloneDX module for .NET 5.4.0

This is the machine-readable SBOM. Do not hand-edit the JSON.

## First-party vs third-party

| Component | License |
|-----------|---------|
| GamingCommander (our code) | CC BY-NC 4.0 — see `/LICENSE` |
| NuGet packages in the JSON | Each package’s own license (typically MIT for Avalonia / .NET) |

## Regenerate

From the repo root (needs .NET 8 SDK):

```bash
dotnet tool restore
dotnet tool run dotnet-CycloneDX -- src/GamingCommander.App/GamingCommander.App.csproj \
  -rs -ed -rt win-x64 \
  -o docs/sbom -fn GamingCommander.cdx.json -F Json \
  -sn GamingCommander -sv 0.4.0 -st Application
```

`-ed` excludes development-only packages. Test projects are not in this BOM (App project only).
