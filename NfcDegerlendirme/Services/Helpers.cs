using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace NfcDegerlendirme.Services;

public static class Helpers {
    static readonly Regex Tags = new("<[^>]*>", RegexOptions.Compiled);
    static readonly Regex Ctrl = new(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]", RegexOptions.Compiled);
    static readonly Regex GoogleHost = new(@"(^|\.)google\.[a-z]{2,3}(\.[a-z]{2})?$", RegexOptions.Compiled);
    static readonly Regex Color = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    public const string DefaultColor = "#1a73e8";

    public static string? CleanText(string? v) {
        if (v is null) return null;
        var s = Ctrl.Replace(Tags.Replace(v, ""), "").Trim();
        return s.Length == 0 ? null : s;
    }
    public static string SafeColor(string? c) => c != null && Color.IsMatch(c) ? c : DefaultColor;

    // Açık yönlendirme koruması: yalnızca https ve Google'a ait alan adları.
    public static string? SafeGoogleUrl(string? raw) {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var u)) return null;
        if (u.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(u.UserInfo)) return null;
        var h = u.Host.ToLowerInvariant();
        var ok = GoogleHost.IsMatch(h) || h is "g.page" or "g.co" or "goo.gl" or "maps.app.goo.gl";
        return ok ? u.AbsoluteUri : null;
    }
    // 4-5 yıldız: Google sayfası (varsa). 1-3 yıldız: asla.
    public static string? GoogleRedirect(int rating, string? url) => rating >= 4 ? SafeGoogleUrl(url) : null;

    public static string NewCardCode() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(12)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Slugify(string s) {
        const string from = "çğıİöşüÇĞÖŞÜ", to = "cgiiosucgosu";
        var sb = new StringBuilder();
        foreach (var ch in s) { var i = from.IndexOf(ch); sb.Append(i >= 0 ? to[i] : ch); }
        var r = Regex.Replace(sb.ToString().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        if (r.Length > 50) r = r[..50].Trim('-');
        return r.Length == 0 ? "firma" : r;
    }
    // CSV formül enjeksiyonu koruması
    public static string CsvCell(object? v) {
        var s = v?.ToString() ?? "";
        if (s.Length > 0 && "=+-@\t\r".Contains(s[0])) s = "'" + s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
    // Dosya imzasından tür tespiti; SVG kabul edilmez.
    public static string? DetectImage(byte[] b) {
        if (b.Length > 12 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "png";
        if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";
        if (b.Length > 12 && Encoding.ASCII.GetString(b, 0, 4) == "RIFF" && Encoding.ASCII.GetString(b, 8, 4) == "WEBP") return "webp";
        return null;
    }
}
