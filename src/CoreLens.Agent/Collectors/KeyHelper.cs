using System.Text.RegularExpressions;

namespace CoreLens.Agent.Collectors;

internal static class KeyHelper
{
    public static string Sanitize(string value)
    {
        var lower = value.Trim().ToLowerInvariant();
        var sanitized = Regex.Replace(lower, @"[^a-z0-9]+", "-");
        return sanitized.Trim('-');
    }
}
