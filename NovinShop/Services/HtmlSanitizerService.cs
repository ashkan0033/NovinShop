using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NovinShop.Services;

// پاکیزه‌ساز HTML (Sanitizer) با لیست سفید تگ‌ها، صفت‌ها و استایل‌های مجاز برای جلوگیری کامل از XSS
public static class HtmlSanitizerService
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "hr", "div", "span", "blockquote", "pre", "code",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "strong", "b", "em", "i", "u", "s", "strike", "del", "sub", "sup", "mark", "small",
        "ul", "ol", "li",
        "a", "img", "figure", "figcaption",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "colgroup", "col", "caption"
    };

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br", "hr", "img", "col"
    };

    private static readonly HashSet<string> AllowedAttrs = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "target", "rel", "title", "src", "alt", "width", "height", "loading",
        "style", "dir", "align", "colspan", "rowspan", "border", "cellpadding", "cellspacing", "scope"
    };

    private static readonly HashSet<string> AllowedCssProps = new(StringComparer.OrdinalIgnoreCase)
    {
        "color", "background-color", "font-size", "font-weight", "font-style", "text-decoration",
        "text-align", "line-height", "width", "max-width", "min-width", "height",
        "border", "border-collapse", "border-color", "border-width", "border-style", "border-radius",
        "padding", "padding-top", "padding-right", "padding-bottom", "padding-left",
        "margin", "margin-top", "margin-right", "margin-bottom", "margin-left",
        "list-style-type", "vertical-align", "float", "display"
    };

    // حذف کامل بلوک‌های خطرناک به همراه محتوایشان
    private static readonly Regex StripBlock = new(
        @"<\s*(script|style|iframe|object|embed|applet|form|input|button|textarea|select|meta|link|base|svg|math)\b[^>]*>[\s\S]*?<\s*/\s*\1\s*>|<\s*(script|style|iframe|object|embed|applet|form|input|button|textarea|select|meta|link|base|svg|math)\b[^>]*/?\s*>|<!--[\s\S]*?-->",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(
        @"<\s*(/?)\s*([a-zA-Z0-9]+)((?:\s+[^\s""'=<>`/]+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s""'=<>`]+))?)*)\s*(/?)\s*>",
        RegexOptions.Compiled);

    private static readonly Regex AttrRegex = new(
        @"([a-zA-Z0-9_-]+)(?:\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'=<>`]+)))?",
        RegexOptions.Compiled);

    public static bool ContainsHtml(string? input) =>
        !string.IsNullOrWhiteSpace(input) && Regex.IsMatch(input, @"<\s*(p|div|span|h[1-6]|ul|ol|li|table|strong|b|em|i|u|img|a|br|blockquote|hr)\b", RegexOptions.IgnoreCase);

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        var cleaned = StripBlock.Replace(html, "");
        var sb = new StringBuilder(cleaned.Length);
        var lastIdx = 0;

        foreach (Match m in TagRegex.Matches(cleaned))
        {
            // متن بین تگ‌ها (< و > سرگردان در متن encode می‌شوند)
            if (m.Index > lastIdx)
                sb.Append(EncodeStrayAngles(cleaned[lastIdx..m.Index]));
            lastIdx = m.Index + m.Length;

            var isClose = m.Groups[1].Value == "/";
            var tag = m.Groups[2].Value.ToLowerInvariant();
            var rawAttrs = m.Groups[3].Value;

            if (!AllowedTags.Contains(tag))
                continue; // تگ غیرمجاز حذف می‌شود

            if (isClose)
            {
                if (!VoidTags.Contains(tag))
                    sb.Append("</").Append(tag).Append('>');
                continue;
            }

            sb.Append('<').Append(tag);
            var hasRel = false;
            var hasTargetBlank = false;

            foreach (Match am in AttrRegex.Matches(rawAttrs))
            {
                var name = am.Groups[1].Value.ToLowerInvariant();
                if (name.StartsWith("on") || !AllowedAttrs.Contains(name))
                    continue;

                var val = am.Groups[2].Success ? am.Groups[2].Value :
                          am.Groups[3].Success ? am.Groups[3].Value :
                          am.Groups[4].Success ? am.Groups[4].Value : "";
                val = WebUtility.HtmlDecode(val).Trim();

                if (name is "href" or "src")
                {
                    if (!IsSafeUrl(val, isImage: name == "src"))
                        continue;
                }
                else if (name == "style")
                {
                    val = SanitizeStyle(val);
                    if (string.IsNullOrEmpty(val)) continue;
                }
                else if (name == "target")
                {
                    if (val is not ("_blank" or "_self")) continue;
                    if (val == "_blank") hasTargetBlank = true;
                }
                else if (name == "rel")
                {
                    hasRel = true;
                    val = "noopener noreferrer";
                }
                else if (name == "dir")
                {
                    if (val is not ("rtl" or "ltr" or "auto")) continue;
                }

                sb.Append(' ').Append(name).Append("=\"").Append(WebUtility.HtmlEncode(val)).Append('"');
            }

            if (tag == "a" && hasTargetBlank && !hasRel)
                sb.Append(" rel=\"noopener noreferrer\"");

            sb.Append(VoidTags.Contains(tag) ? " />" : ">");
        }

        if (lastIdx < cleaned.Length)
            sb.Append(EncodeStrayAngles(cleaned[lastIdx..]));

        return sb.ToString().Trim();
    }

    private static string EncodeStrayAngles(string s) =>
        s.Replace("<", "&lt;").Replace(">", "&gt;");

    private static bool IsSafeUrl(string url, bool isImage)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        // حذف کاراکترهای کنترلی و فاصله‌ها برای جلوگیری از جاوااسکریپت مخفی
        var normalized = Regex.Replace(url, @"[\s\x00-\x1f]+", "").ToLowerInvariant();
        if (normalized.StartsWith("javascript:") || normalized.StartsWith("vbscript:") || normalized.StartsWith("data:"))
            return false;

        if (url.StartsWith('/') && !url.StartsWith("//"))
            return true;
        if (!isImage && (url.StartsWith('#') || normalized.StartsWith("mailto:") || normalized.StartsWith("tel:")))
            return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeStyle(string style)
    {
        var parts = new List<string>();
        foreach (var decl in style.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = decl.IndexOf(':');
            if (idx <= 0) continue;
            var prop = decl[..idx].Trim().ToLowerInvariant();
            var val = decl[(idx + 1)..].Trim();
            if (!AllowedCssProps.Contains(prop)) continue;

            // جلوگیری از expression()، url()، javascript: و import در CSS
            var check = val.ToLowerInvariant();
            if (check.Contains("expression") || check.Contains("url(") || check.Contains("javascript") ||
                check.Contains("vbscript") || check.Contains("@import") || check.Contains("behavior") ||
                check.Contains('<') || check.Contains('>') || check.Contains('\\'))
                continue;

            parts.Add($"{prop}: {val}");
        }
        return string.Join("; ", parts);
    }
}
