using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NovinShop.Services;

// متن ساده‌ی قالب‌بندی‌شده → HTML امن. ابتدا همه‌چیز HTML-encode می‌شود، پس کد مخرب اجرا نمی‌شود.
//   # تیتر بزرگ   ## تیتر کوچک   - مورد لیست   **پررنگ**   [متن](https://...)   خط خالی = پاراگراف جدید
public static class ContentRenderer
{
    private static readonly Regex Bold = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex Link = new(@"\[([^\]]+)\]\((https?://[^\s)]+|/[^\s)]*)\)", RegexOptions.Compiled);

    private static string Inline(string line)
    {
        var s = WebApplicationEncode(line);
        s = Bold.Replace(s, "<strong>$1</strong>");
        return Link.Replace(s, "<a href=\"$2\" rel=\"noopener\">$1</a>");   // فقط http(s) و آدرس داخلی
    }

    private static string WebApplicationEncode(string s) => WebUtility.HtmlEncode(s);

    public static string ToHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        if (HtmlSanitizerService.ContainsHtml(text))
            return HtmlSanitizerService.Sanitize(text);
        var html = new StringBuilder();
        var para = new List<string>();
        var inList = false;

        void FlushPara()
        {
            if (para.Count == 0) return;
            html.Append("<p>").Append(string.Join("<br />", para.Select(Inline))).Append("</p>");
            para.Clear();
        }
        void CloseList() { if (inList) { html.Append("</ul>"); inList = false; } }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var l = raw.TrimEnd();
            if (l.Length == 0) { FlushPara(); CloseList(); }
            else if (l.StartsWith("## ")) { FlushPara(); CloseList(); html.Append("<h3>").Append(Inline(l[3..])).Append("</h3>"); }
            else if (l.StartsWith("# ")) { FlushPara(); CloseList(); html.Append("<h2>").Append(Inline(l[2..])).Append("</h2>"); }
            else if (l.StartsWith("- "))
            {
                FlushPara();
                if (!inList) { html.Append("<ul>"); inList = true; }
                html.Append("<li>").Append(Inline(l[2..])).Append("</li>");
            }
            else { CloseList(); para.Add(l); }
        }
        FlushPara(); CloseList();
        return html.ToString();
    }
}
