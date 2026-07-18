namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Represents a single command button in the bottom command bar.
/// </summary>
public sealed class ShellCommandViewModel
{
    /// <summary>The F-key or keyboard shortcut that triggers this command (e.g., 'F1', 'F5').</summary>
    public required string Hotkey { get; init; }

    /// <summary>Display text shown on the command button.</summary>
    public required string Label { get; init; }
}
