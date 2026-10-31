using Xunit;

namespace GamingCommander.App.Tests;

/// <summary>
/// The repair journal is one shared append-only file. Tests that write or read it run
/// serialized so a concurrent append cannot swallow another test's line.
/// </summary>
[CollectionDefinition("RepairJournal", DisableParallelization = true)]
public sealed class RepairJournalCollection
{
}
