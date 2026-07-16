# SDK Upgrade Plan: .NET 8 to .NET 9

## Goal
Upgrade the project from .NET 8 to .NET 9 (SDK 9.0.312).

## Priority
**Lowest priority.** Working application first. SDK upgrade only when all feature milestones are complete and the application is stable for end users.

## Steps
1. [ ] Update `global.json` to 9.0.312.
2. [ ] Update `Directory.Build.props` to `net9.0`.
3. [ ] Run `dotnet restore`, `dotnet build`, `dotnet test` to verify.
