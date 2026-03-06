using System.Text;
using TimelineAnimations.Core.Models;

namespace TimelineAnimations.Core.Services;

public static class LibraryManagementService
{
    public const string RootFolderDisplayName = "Root";

    public static void EnsureLibraryMetadata(TimelineDocument document)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedLinkageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in document.LibraryItems)
        {
            item.Name = EnsureUniqueValue(
                item.Name,
                usedNames,
                "New Symbol",
                " ");
            item.FolderPath = NormalizeFolderPath(string.IsNullOrWhiteSpace(item.FolderPath)
                ? GetDefaultFolderPath(item)
                : item.FolderPath);
            item.LinkageId = EnsureUniqueValue(
                NormalizeLinkageId(item.LinkageId),
                usedLinkageIds,
                NormalizeLinkageId(item.Name),
                "_",
                "symbol");
            item.SourceAssetPath = NormalizeSourceAssetPath(item.SourceAssetPath);
        }
    }

    public static string EnsureUniqueLibraryName(TimelineDocument document, string? proposedName, Guid? excludeItemId = null)
    {
        var normalizedBaseName = string.IsNullOrWhiteSpace(proposedName)
            ? "New Symbol"
            : proposedName.Trim();
        var candidate = normalizedBaseName;
        var suffix = 2;

        while (document.LibraryItems.Any(item =>
                   item.Id != excludeItemId &&
                   string.Equals(item.Name, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{normalizedBaseName} {suffix}";
            suffix++;
        }

        return candidate;
    }

    public static string EnsureUniqueLinkageId(
        TimelineDocument document,
        string? proposedLinkageId,
        Guid? excludeItemId = null,
        string? fallbackName = null)
    {
        var baseId = NormalizeLinkageId(proposedLinkageId);
        if (string.IsNullOrWhiteSpace(baseId))
        {
            baseId = NormalizeLinkageId(fallbackName);
        }

        if (string.IsNullOrWhiteSpace(baseId))
        {
            baseId = "symbol";
        }

        var candidate = baseId;
        var suffix = 2;

        while (document.LibraryItems.Any(item =>
                   item.Id != excludeItemId &&
                   string.Equals(item.LinkageId, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{baseId}_{suffix}";
            suffix++;
        }

        return candidate;
    }

    public static string NormalizeFolderPath(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return string.Empty;
        }

        var segments = folderPath
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .Select(segment => segment.Trim())
            .ToArray();

        return segments.Length == 0 ? string.Empty : string.Join("/", segments);
    }

    public static string GetDisplayFolderPath(string? folderPath)
    {
        var normalized = NormalizeFolderPath(folderPath);
        return string.IsNullOrWhiteSpace(normalized) ? RootFolderDisplayName : normalized;
    }

    public static IEnumerable<LibraryItem> FilterItems(
        IEnumerable<LibraryItem> items,
        string? searchText,
        string? folderFilter)
    {
        var normalizedSearch = string.IsNullOrWhiteSpace(searchText)
            ? string.Empty
            : searchText.Trim();
        var normalizedFolderFilter = string.IsNullOrWhiteSpace(folderFilter)
            ? string.Empty
            : folderFilter.Trim();

        foreach (var item in items)
        {
            if (!MatchesFolder(item, normalizedFolderFilter))
            {
                continue;
            }

            if (normalizedSearch.Length > 0 &&
                !MatchesSearch(item, normalizedSearch))
            {
                continue;
            }

            yield return item;
        }
    }

    public static TimelineLayer RelinkLayerToLibraryItem(TimelineLayer sourceLayer, LibraryItem targetItem)
    {
        var relinked = TimelineEditingService.CreateLayerFromLibraryItem(
            targetItem,
            sourceLayer.Defaults.X,
            sourceLayer.Defaults.Y,
            sourceLayer.ZIndex);

        relinked.Id = sourceLayer.Id;
        relinked.Name = sourceLayer.Name;
        relinked.IsVisible = sourceLayer.IsVisible;
        relinked.IsLocked = sourceLayer.IsLocked;
        relinked.Defaults.X = sourceLayer.Defaults.X;
        relinked.Defaults.Y = sourceLayer.Defaults.Y;
        relinked.Defaults.Width = sourceLayer.Defaults.Width;
        relinked.Defaults.Height = sourceLayer.Defaults.Height;
        relinked.Defaults.Rotation = sourceLayer.Defaults.Rotation;
        relinked.Defaults.Opacity = sourceLayer.Defaults.Opacity;
        relinked.SymbolPlaybackMode = sourceLayer.SymbolPlaybackMode;
        relinked.SymbolPlaybackOffset = sourceLayer.SymbolPlaybackOffset;
        relinked.SymbolLockedFrame = sourceLayer.SymbolLockedFrame;
        relinked.SymbolButtonState = sourceLayer.SymbolButtonState;
        relinked.Behaviors = InteractionBehaviorService.CloneBehaviors(sourceLayer.Behaviors);
        return relinked;
    }

    public static string GetDefaultFolderPath(LibraryItem item)
    {
        if (item.IsComponent)
        {
            var category = string.IsNullOrWhiteSpace(item.ComponentCategory)
                ? "General"
                : item.ComponentCategory.Trim();
            return NormalizeFolderPath($"Components/{category}");
        }

        return item.SymbolKind switch
        {
            SymbolKind.Graphic => "Symbols/Graphics",
            SymbolKind.MovieClip => "Symbols/Movie Clips",
            SymbolKind.Button => "Symbols/Buttons",
            _ => "Symbols"
        };
    }

    public static string NormalizeSourceAssetPath(string? sourceAssetPath)
    {
        return string.IsNullOrWhiteSpace(sourceAssetPath)
            ? string.Empty
            : sourceAssetPath.Trim();
    }

    private static bool MatchesFolder(LibraryItem item, string folderFilter)
    {
        if (string.IsNullOrWhiteSpace(folderFilter) ||
            string.Equals(folderFilter, "All folders", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            GetDisplayFolderPath(item.FolderPath),
            folderFilter,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(LibraryItem item, string searchText)
    {
        return item.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               GetDisplayFolderPath(item.FolderPath).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.LinkageId.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.SourceAssetPath.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
               item.ComponentCategory.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLinkageId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSeparator = false;
        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasSeparator = false;
                continue;
            }

            if (character is '.' or '_' or '-')
            {
                builder.Append(character);
                lastWasSeparator = false;
                continue;
            }

            if (lastWasSeparator)
            {
                continue;
            }

            builder.Append('_');
            lastWasSeparator = true;
        }

        return builder.ToString().Trim('_');
    }

    private static string EnsureUniqueValue(
        string? value,
        HashSet<string> usedValues,
        string fallbackValue,
        string separator,
        string? emptyFallbackValue = null)
    {
        var normalizedBaseValue = string.IsNullOrWhiteSpace(value)
            ? fallbackValue
            : value.Trim();
        if (string.IsNullOrWhiteSpace(normalizedBaseValue))
        {
            normalizedBaseValue = emptyFallbackValue ?? fallbackValue;
        }

        var candidate = normalizedBaseValue;
        var suffix = 2;
        while (!usedValues.Add(candidate))
        {
            candidate = $"{normalizedBaseValue}{separator}{suffix}";
            suffix++;
        }

        return candidate;
    }
}
