using System.Text.RegularExpressions;

namespace NovinShop.Services;

public static class SlugHelper
{
    // «هدفون بی‌سیم نویزکنسل» → «هدفون-بی-سیم-نویزکنسل»  (فارسی در آدرس مجاز است)
    public static string Generate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "item";
        var s = text.Trim().ToLowerInvariant().Replace('ي', 'ی').Replace('ك', 'ک');
        s = Regex.Replace(s, @"[^\p{L}\p{Nd}]+", "-").Trim('-');
        if (s.Length > 100) s = s[..100].Trim('-');
        return s.Length == 0 ? "item" : s;
    }
}
