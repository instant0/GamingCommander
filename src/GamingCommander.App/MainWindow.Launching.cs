using System.Diagnostics;
using GamingCommander.Core;
using GamingCommander.Core.Models;
using GamingCommander.Core.Services;

namespace GamingCommander.App;

/// <summary>
/// Game launching member-part of <see cref="MainWindow"/> (Plan 125 Phase 1c split).
/// </summary>
public partial class MainWindow
{
    private Task LaunchSelectedGameAsync()
    {
        if (_viewModel is null) return Task.CompletedTask;

        // Must have a game selected (not at root level)
        if (_viewModel.IsAtRootLevel)
        {
            _viewModel.StatusText = "Navigate into a library root first, then select a game to launch.";
            return Task.CompletedTask;
        }

        var item = _viewModel.SelectedItem;
        if (item is null || string.IsNullOrEmpty(item.LaunchTarget))
        {
            _viewModel.StatusText = item is not null
                ? $"No launch target for {item.Title}"
                : "No game selected.";
            return Task.CompletedTask;
        }

        string target = item.LaunchTarget;

        // If it's a directory entry (not a file), don't launch
        if (item.Kind == FileSystemEntryKind.Directory)
        {
            _viewModel.NavigateInto();
            return Task.CompletedTask;
        }

        try
        {
            string args;
            string? libraryName = _viewModel.GetCurrentLibraryName();
            GameEntry? game = item.GameId is not null ? FindGameById(item.GameId) : null;
            if (game is not null)
            {
                (target, args) = GameLaunchResolver.Resolve(game);
            }
            else
            {
                args = LaunchArgumentComposer.IsSteamUri(item.CommandLineArguments)
                    ? string.Empty
                    : item.CommandLineArguments;
            }

            if (string.IsNullOrEmpty(target))
            {
                _viewModel.StatusText = $"No launch target for {item.Title}";
                return Task.CompletedTask;
            }

            _viewModel.StatusText = string.IsNullOrEmpty(args)
                ? $"Launching: {target}"
                : $"Launching: {target} {args}";

            if (LaunchArgumentComposer.IsSteamUri(target))
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true,
                });
            }
            else
            {
                using var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    Arguments = args,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(target) ?? "",
                });
            }

            _viewModel.StatusText = $"Launched: {item.Title}";
            if (game is not null)
                QueueSilentMetadataIfStale(game);
        }
        catch (Exception ex)
        {
            _viewModel.StatusText = $"Launch failed: {ex.Message}";
        }

        return Task.CompletedTask;
    }
}