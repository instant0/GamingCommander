using GamingCommander.Core.Models;

namespace GamingCommander.UI.ViewModels;

/// <summary>
/// Provides tag colors for rendering tag badges.
/// Decouples ShellViewModel from the App-layer TagColorService.
/// </summary>
public interface ITagColorProvider
{
    /// <summary>
    /// Get color pair (background, foreground) for a tag by name.
    /// </summary>
    (string Background, string Foreground) GetColor(string tag, TagType tagType);

    /// <summary>Determine tag type from tag name.</summary>
    TagType GetTagType(string tag);
}
