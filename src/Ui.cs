using System.Globalization;

namespace CodexCreditMonitor;

internal static class Ui
{
    private static string _language = "auto";
    internal static void SetLanguage(string? language) => _language = language is "nl" or "en" ? language : "auto";
    internal static bool IsEnglish => _language == "en" || (_language == "auto" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase));
    internal static string T(string dutch, string english) => IsEnglish ? english : dutch;

    internal static string SelectInfo(string source)
    {
        var marker = IsEnglish ? "<!-- en -->" : "<!-- nl -->";
        var start = source.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return source;
        start += marker.Length;
        var end = source.IndexOf("<!--", start, StringComparison.Ordinal);
        return source[start..(end < 0 ? source.Length : end)].Trim();
    }
}
