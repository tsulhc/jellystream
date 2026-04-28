using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Jellystream.Models;

namespace Jellyfin.Plugin.Jellystream.Services;

/// <summary>
/// Minimal M3U parser tailored to AceStream entries.
/// </summary>
public sealed partial class M3UParser : IM3UParser
{
    private static readonly Regex AttributeRegex = CreateAttributeRegex();
    private static readonly Regex ContentIdRegex = CreateContentIdRegex();

    /// <inheritdoc />
    public IReadOnlyList<JellystreamChannel> Parse(string playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist))
        {
            return [];
        }

        var channels = new List<JellystreamChannel>();
        var seenContentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> pendingAttributes = new(StringComparer.OrdinalIgnoreCase);
        string? pendingName = null;

        foreach (var rawLine in playlist.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.Equals("#EXTM3U", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
            {
                pendingAttributes = ParseAttributes(line);
                pendingName = ParseName(line);
                continue;
            }

            if (line.StartsWith('#'))
            {
                continue;
            }

            var contentId = ExtractContentId(line);
            if (contentId is null || !seenContentIds.Add(contentId))
            {
                pendingAttributes.Clear();
                pendingName = null;
                continue;
            }

            var name = SanitizeName(pendingName ?? pendingAttributes.GetValueOrDefault("tvg-name") ?? contentId[..Math.Min(8, contentId.Length)]);
            var group = SanitizeOptional(pendingAttributes.GetValueOrDefault("group-title"));
            var logo = SanitizeOptional(pendingAttributes.GetValueOrDefault("tvg-logo"));
            var tvgId = SanitizeOptional(pendingAttributes.GetValueOrDefault("tvg-id"));
            var id = CreateStableId(contentId);

            channels.Add(new JellystreamChannel(id, name, contentId, group, logo, tvgId));
            pendingAttributes.Clear();
            pendingName = null;
        }

        return channels;
    }

    private static Dictionary<string, string> ParseAttributes(string line)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in AttributeRegex.Matches(line))
        {
            attributes[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return attributes;
    }

    private static string? ParseName(string line)
    {
        var commaIndex = line.LastIndexOf(',');
        return commaIndex >= 0 && commaIndex + 1 < line.Length ? line[(commaIndex + 1)..].Trim() : null;
    }

    private static string? ExtractContentId(string line)
    {
        if (line.StartsWith("acestream://", StringComparison.OrdinalIgnoreCase))
        {
            line = line[12..];
        }
        else if (Uri.TryCreate(line, UriKind.Absolute, out var uri))
        {
            var query = ParseQuery(uri.Query);
            line = query.GetValueOrDefault("id") ?? query.GetValueOrDefault("content_id") ?? query.GetValueOrDefault("contentId") ?? line;
        }

        var match = ContentIdRegex.Match(line);
        return match.Success ? match.Value.ToLower(CultureInfo.InvariantCulture) : null;
    }

    private static string SanitizeName(string value)
    {
        var sanitized = value.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
        return sanitized.Length == 0 ? "AceStream Channel" : sanitized[..Math.Min(120, sanitized.Length)];
    }

    private static string? SanitizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = value.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ').Trim();
        return sanitized.Length == 0 ? null : sanitized[..Math.Min(512, sanitized.Length)];
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(part[..separator]);
            var value = Uri.UnescapeDataString(part[(separator + 1)..]);
            values[key] = value;
        }

        return values;
    }

    private static string CreateStableId(string contentId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(contentId));
        return Convert.ToHexString(hash, 0, 8).ToLower(CultureInfo.InvariantCulture);
    }

    [GeneratedRegex("([A-Za-z0-9_-]+)=\"([^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex CreateAttributeRegex();

    [GeneratedRegex("[a-fA-F0-9]{40}", RegexOptions.Compiled)]
    private static partial Regex CreateContentIdRegex();
}
