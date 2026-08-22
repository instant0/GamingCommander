using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GamingCommander.App.Services;

/// <summary>
/// Builds the programmatic help dialog showing keyboard shortcuts and feature descriptions.
/// Extracted from MainWindow to keep window code-behind focused on event handling.
/// </summary>
internal static class HelpDialogBuilder
{
    /// <summary>
    /// Creates and shows the help dialog window with all keyboard shortcuts.
    /// Returns a Task that completes when the dialog is closed.
    /// </summary>
    /// <param name="owner">The parent window for dialog positioning.</param>
    internal static async Task ShowHelpAsync(Window owner)
    {
        string version = Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString(3) ?? "0.0.0";
        var bodyBrush = AppTheme.TextSecondary;
        var headerBrush = AppTheme.TextAccent;
        var keyBrush = AppTheme.TextHighlight;
        var backgroundBrush = AppTheme.PaneBg;

        var keys = new (string key, string desc)[]
        {
            ("F1", "Help — this window"),
            ("F2", "Library Setup — add/remove/rescan folders"),
            ("F3", "View game metadata (coming soon)"),
            ("F4", "Configure game — name, type, exe, PCGW launch options"),
            ("F5", "Rescan current folder or all roots"),
            ("F8", "Filter/category view (coming soon)"),
            ("F10", "Quit GamingCommander"),
            ("Enter", "Launch game / drill into folder"),
            ("Esc / Backspace", "Go up one level"),
            ("Up / Down", "Navigate list"),
        };

        var panel = new StackPanel { Spacing = 8, Background = backgroundBrush };

        panel.Children.Add(new TextBlock
        {
            Text = "GamingCommander",
            FontSize = AppTheme.FontSizeAppTitle,
            FontWeight = FontWeight.Bold,
            Foreground = headerBrush,
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Version {version}",
            FontSize = AppTheme.FontSizeBody,
            Foreground = bodyBrush,
            Margin = new Thickness(0, 0, 0, 8),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "A Norton Commander-style game launcher and library manager.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = AppTheme.FontSizeBody,
            Foreground = bodyBrush,
            Margin = new Thickness(0, 0, 0, 8),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "© 2026 ins",
            FontSize = AppTheme.FontSizeBody,
            Foreground = bodyBrush,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "https://github.com/instant0/GamingCommander",
            FontSize = AppTheme.FontSizeLabel,
            Foreground = headerBrush,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "License: Creative Commons Attribution-NonCommercial 4.0 (CC BY-NC 4.0)",
            TextWrapping = TextWrapping.Wrap,
            FontSize = AppTheme.FontSizeLabel,
            Foreground = bodyBrush,
        });
        panel.Children.Add(new TextBlock
        {
            Text = "https://creativecommons.org/licenses/by-nc/4.0/",
            FontSize = AppTheme.FontSizeLabel,
            Foreground = headerBrush,
            Margin = new Thickness(0, 0, 0, 12),
        });

        panel.Children.Add(new TextBlock
        {
            Text = "Keyboard Reference",
            FontSize = AppTheme.FontSizeSubHeader,
            FontWeight = FontWeight.Bold,
            Foreground = headerBrush,
            Margin = new Thickness(0, 0, 0, 4),
        });

        foreach (var (key, desc) in keys)
        {
            var row = new Grid
            {
                ColumnDefinitions =
                [
                    new ColumnDefinition(140, GridUnitType.Pixel),
                    new ColumnDefinition(1, GridUnitType.Star),
                ],
                Margin = new Thickness(0, 2),
            };
            row.Children.Add(new TextBlock { Text = key, Foreground = keyBrush, FontWeight = FontWeight.Bold, FontSize = AppTheme.FontSizeBody });
            row.Children.Add(new TextBlock { Text = desc, Foreground = bodyBrush, FontSize = AppTheme.FontSizeBody, Margin = new Thickness(8, 0, 0, 0) });
            Grid.SetColumn(row.Children[1], 1);
            panel.Children.Add(row);
        }

        panel.Children.Add(new TextBlock
        {
            Text = "\nData is stored in the app's data/ directory. No game files are modified.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = bodyBrush,
            FontSize = AppTheme.FontSizeLabel,
            FontStyle = FontStyle.Italic,
            Margin = new Thickness(0, 12, 0, 0),
        });

        var helpWindow = new Window
        {
            Title = "About / Help — GamingCommander",
            Width = 480,
            Height = 580,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SystemDecorations = SystemDecorations.Full,
            Content = new ScrollViewer
            {
                Background = backgroundBrush,
                Content = new Border
                {
                    Padding = new Thickness(20),
                    Background = backgroundBrush,
                    Child = panel,
                },
            },
        };

        await helpWindow.ShowDialog(owner);
    }
}
