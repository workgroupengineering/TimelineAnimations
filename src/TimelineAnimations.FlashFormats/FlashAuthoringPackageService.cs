using System.IO.Compression;
using System.Text;

namespace TimelineAnimations.FlashFormats;

public static class FlashAuthoringPackageService
{
    private static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;
    public const string DefaultMimeType = "application/vnd.adobe.xfl";

    public static FlashAuthoringPackageDetectionResult Detect(ReadOnlySpan<byte> data, string? sourceLabel)
    {
        var extension = Path.GetExtension(sourceLabel ?? string.Empty);

        if (LooksLikeZipArchive(data))
        {
            return new FlashAuthoringPackageDetectionResult
            {
                Kind = FlashAuthoringContainerKind.CompressedFlaArchive,
                IsSupported = true,
                Message = "Detected a compressed Flash authoring archive."
            };
        }

        var textSample = TryDecodeUtf8Text(data);
        if (!string.IsNullOrWhiteSpace(textSample) &&
            (textSample.Contains("<DOMDocument", StringComparison.OrdinalIgnoreCase) ||
             textSample.Contains("ns.adobe.com/xfl", StringComparison.OrdinalIgnoreCase)))
        {
            return new FlashAuthoringPackageDetectionResult
            {
                Kind = FlashAuthoringContainerKind.DomDocumentXml,
                IsSupported = true,
                Message = "Detected a standalone Flash XFL DOM document."
            };
        }

        if (string.Equals(extension, ".fla", StringComparison.OrdinalIgnoreCase))
        {
            return new FlashAuthoringPackageDetectionResult
            {
                Kind = FlashAuthoringContainerKind.LegacyBinaryFla,
                IsSupported = false,
                Message = "This appears to be a legacy binary FLA. TimelineAnimations Studio supports modern Adobe Animate FLA archives based on XFL, but not the older pre-XFL binary FLA format."
            };
        }

        return new FlashAuthoringPackageDetectionResult
        {
            Kind = FlashAuthoringContainerKind.Unknown,
            IsSupported = false,
            Message = "The Flash authoring container could not be identified."
        };
    }

    public static FlashAuthoringPackageDetectionResult DetectDirectory(string folderPath)
    {
        if (Directory.Exists(folderPath) &&
            TryDetectDirectoryDocumentPath(folderPath, out var documentPath))
        {
            return new FlashAuthoringPackageDetectionResult
            {
                Kind = FlashAuthoringContainerKind.UncompressedXflDirectory,
                IsSupported = true,
                Message = documentPath.EndsWith(".xfl", StringComparison.OrdinalIgnoreCase)
                    ? "Detected an uncompressed Flash XFL directory rooted by an authoring .xfl document."
                    : "Detected an uncompressed Flash XFL directory."
            };
        }

        return new FlashAuthoringPackageDetectionResult
        {
            Kind = FlashAuthoringContainerKind.Unknown,
            IsSupported = false,
            Message = "The selected folder does not contain DOMDocument.xml or an authoring .xfl document."
        };
    }

    public static FlashAuthoringPackage Read(byte[] data, string sourceLabel)
    {
        var detection = Detect(data, sourceLabel);
        return detection.Kind switch
        {
            FlashAuthoringContainerKind.CompressedFlaArchive => ReadArchive(data, sourceLabel),
            FlashAuthoringContainerKind.DomDocumentXml => CreateLooseDocumentPackage(data, sourceLabel),
            FlashAuthoringContainerKind.LegacyBinaryFla => throw new InvalidOperationException(detection.Message),
            _ => throw new InvalidOperationException(detection.Message)
        };
    }

    public static FlashAuthoringPackage ReadDirectory(string folderPath)
    {
        var detection = DetectDirectory(folderPath);
        if (!detection.IsSupported)
        {
            throw new InvalidOperationException(detection.Message);
        }

        var entries = new List<FlashAuthoringPackageEntry>();
        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
                     .OrderBy(static path => path, PathComparer))
        {
            var relativePath = Path.GetRelativePath(folderPath, filePath)
                .Replace(Path.DirectorySeparatorChar, '/');
            entries.Add(new FlashAuthoringPackageEntry
            {
                RelativePath = relativePath,
                Data = File.ReadAllBytes(filePath),
                Kind = ClassifyEntry(relativePath)
            });
        }

        var documentEntry = LocateDocumentEntry(entries);
        if (documentEntry is null)
        {
            throw new InvalidOperationException("The selected XFL folder does not contain DOMDocument.xml or an authoring .xfl document.");
        }

        return new FlashAuthoringPackage
        {
            Kind = FlashAuthoringContainerKind.UncompressedXflDirectory,
            SourceLabel = folderPath,
            DocumentRelativePath = documentEntry.RelativePath,
            DocumentData = documentEntry.Data,
            Entries = entries
        };
    }

    public static void WriteArchive(Stream stream, FlashAuthoringPackage package)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var entry in package.Entries.OrderBy(static item => item.RelativePath, PathComparer))
        {
            var archiveEntry = archive.CreateEntry(entry.RelativePath);
            using var archiveStream = archiveEntry.Open();
            archiveStream.Write(entry.Data, 0, entry.Data.Length);
        }
    }

    public static void WriteDirectory(string folderPath, FlashAuthoringPackage package)
    {
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, recursive: true);
        }

        Directory.CreateDirectory(folderPath);
        foreach (var entry in package.Entries)
        {
            var filePath = Path.Combine(folderPath, entry.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, entry.Data);
        }
    }

    public static FlashAuthoringPackage CreatePackage(
        string sourceLabel,
        string documentXml,
        IReadOnlyList<FlashAuthoringPackageEntry> supplementalEntries,
        FlashAuthoringContainerKind kind)
    {
        var documentBytes = Encoding.UTF8.GetBytes(documentXml);
        var entries = new List<FlashAuthoringPackageEntry>
        {
            new()
            {
                RelativePath = "DOMDocument.xml",
                Data = documentBytes,
                Kind = FlashAuthoringPackageEntryKind.Document
            }
        };

        entries.AddRange(supplementalEntries
            .Where(static entry => !string.Equals(entry.RelativePath, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase))
            .Select(static entry => new FlashAuthoringPackageEntry
            {
                RelativePath = NormalizeRelativePath(entry.RelativePath),
                Data = entry.Data,
                Kind = entry.Kind
            }));

        entries = entries
            .GroupBy(static entry => NormalizeRelativePath(entry.RelativePath), PathComparer)
            .Select(static group =>
            {
                var entry = group.Last();
                return new FlashAuthoringPackageEntry
                {
                    RelativePath = NormalizeRelativePath(group.Key),
                    Data = entry.Data,
                    Kind = entry.Kind
                };
            })
            .OrderBy(static entry => entry.RelativePath, PathComparer)
            .ToList();

        return new FlashAuthoringPackage
        {
            Kind = kind,
            SourceLabel = sourceLabel,
            DocumentRelativePath = "DOMDocument.xml",
            DocumentData = documentBytes,
            Entries = entries
        };
    }

    private static FlashAuthoringPackage ReadArchive(byte[] data, string sourceLabel)
    {
        using var stream = new MemoryStream(data, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        var entries = archive.Entries
            .Where(static entry => !string.IsNullOrWhiteSpace(entry.FullName))
            .OrderBy(static entry => entry.FullName, PathComparer)
            .Select(ReadArchiveEntry)
            .ToList();

        var documentEntry = LocateDocumentEntry(entries);
        if (documentEntry is null)
        {
            throw new InvalidOperationException("Flash package does not contain DOMDocument.xml or an authoring .xfl document.");
        }

        return new FlashAuthoringPackage
        {
            Kind = FlashAuthoringContainerKind.CompressedFlaArchive,
            SourceLabel = sourceLabel,
            DocumentRelativePath = documentEntry.RelativePath,
            DocumentData = documentEntry.Data,
            Entries = entries
        };
    }

    private static FlashAuthoringPackage CreateLooseDocumentPackage(byte[] data, string sourceLabel)
    {
        var entry = new FlashAuthoringPackageEntry
        {
            RelativePath = "DOMDocument.xml",
            Data = data,
            Kind = FlashAuthoringPackageEntryKind.Document
        };

        return new FlashAuthoringPackage
        {
            Kind = FlashAuthoringContainerKind.DomDocumentXml,
            SourceLabel = sourceLabel,
            DocumentRelativePath = entry.RelativePath,
            DocumentData = data,
            Entries = [entry]
        };
    }

    private static FlashAuthoringPackageEntry ReadArchiveEntry(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var buffer = new MemoryStream();
        entryStream.CopyTo(buffer);
        var relativePath = NormalizeRelativePath(entry.FullName);
        return new FlashAuthoringPackageEntry
        {
            RelativePath = relativePath,
            Data = buffer.ToArray(),
            Kind = ClassifyEntry(relativePath)
        };
    }

    private static FlashAuthoringPackageEntryKind ClassifyEntry(string relativePath)
    {
        if (string.Equals(relativePath, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.Document;
        }

        if (!relativePath.Contains('/') &&
            relativePath.EndsWith(".xfl", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.ProjectFile;
        }

        if (relativePath.StartsWith("LIBRARY/", StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(relativePath, "LIBRARY/manifest.xml", StringComparison.OrdinalIgnoreCase)
                ? FlashAuthoringPackageEntryKind.LibraryManifest
                : relativePath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                    ? FlashAuthoringPackageEntryKind.LibrarySymbol
                    : FlashAuthoringPackageEntryKind.Other;
        }

        if (string.Equals(relativePath, "MEDIA/manifest.xml", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.MediaManifest;
        }

        if (string.Equals(relativePath, "PublishSettings.xml", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.PublishSettings;
        }

        if (string.Equals(relativePath, "mimetype", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.MimeType;
        }

        if (string.Equals(relativePath, "META-INF/metadata.xml", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.Metadata;
        }

        if (string.Equals(relativePath, "MobileSettings.xml", StringComparison.OrdinalIgnoreCase))
        {
            return FlashAuthoringPackageEntryKind.MobileSettings;
        }

        return FlashAuthoringPackageEntryKind.Other;
    }

    private static bool TryDetectDirectoryDocumentPath(string folderPath, out string documentPath)
    {
        documentPath = string.Empty;
        if (!Directory.Exists(folderPath))
        {
            return false;
        }

        var domDocumentPath = Path.Combine(folderPath, "DOMDocument.xml");
        if (File.Exists(domDocumentPath))
        {
            documentPath = "DOMDocument.xml";
            return true;
        }

        var xflFilePath = Directory.EnumerateFiles(folderPath, "*.xfl", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, PathComparer)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xflFilePath))
        {
            documentPath = Path.GetFileName(xflFilePath);
            return true;
        }

        return false;
    }

    private static FlashAuthoringPackageEntry? LocateDocumentEntry(IReadOnlyList<FlashAuthoringPackageEntry> entries)
    {
        return entries.FirstOrDefault(static entry => string.Equals(entry.RelativePath, "DOMDocument.xml", StringComparison.OrdinalIgnoreCase)) ??
               entries.FirstOrDefault(static entry =>
                   entry.Kind == FlashAuthoringPackageEntryKind.ProjectFile &&
                   !entry.RelativePath.Contains('/')) ??
               entries.FirstOrDefault(static entry => entry.Kind == FlashAuthoringPackageEntryKind.Document) ??
               entries.FirstOrDefault(static entry => entry.Kind == FlashAuthoringPackageEntryKind.ProjectFile);
    }

    private static bool LooksLikeZipArchive(ReadOnlySpan<byte> data)
    {
        return data.Length >= 4 &&
               data[0] == (byte)'P' &&
               data[1] == (byte)'K' &&
               data[2] is 3 or 5 or 7 &&
               data[3] is 4 or 6 or 8;
    }

    private static string? TryDecodeUtf8Text(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }

        try
        {
            var text = Encoding.UTF8.GetString(data);
            return text.TrimStart('\uFEFF', '\0', ' ', '\t', '\r', '\n');
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
