using System.Globalization;
using System.Resources;

namespace CodexCreditMonitor;

internal static class Ui
{
    private static readonly ResourceManager Resources = new("CodexCreditMonitor.Strings", typeof(Ui).Assembly);
    private static string _language = "auto";
    internal static event Action? LanguageChanged;
    internal static void SetLanguage(string? language)
    {
        var next = language is "nl" or "en" ? language : "auto";
        if (_language == next) return;
        _language = next;
        LanguageChanged?.Invoke();
    }
    internal static bool IsEnglish => _language == "en" || (_language == "auto" && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase));
    internal static string CurrentLanguage => _language;
    internal static string T(string dutch, string english) => IsEnglish ? english : dutch;
    internal static string S(string key) => Resources.GetString(key, IsEnglish ? CultureInfo.GetCultureInfo("en") : CultureInfo.GetCultureInfo("nl")) ?? key;

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
