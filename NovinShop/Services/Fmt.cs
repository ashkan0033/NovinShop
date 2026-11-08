using System.Globalization;
using NovinShop.Models;

namespace NovinShop.Services;

public static class Fmt
{
    public static string Fa(string s) =>
        string.Concat(s.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c));

    // 2490000 → ۲٬۴۹۰٬۰۰۰
    public static string Num(long n) => Fa(n.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "٬"));

    public static string Toman(long n) => Num(n) + " تومان";

    public static string ProductIcon(Product p)
    {
        var n = p.Name ?? "";
        if (n.Contains("گوشی") || n.Contains("موبایل") || n.Contains("گلکسی") || n.Contains("آیفون")) return "📱";
        if (n.Contains("لپ‌تاپ") || n.Contains("لپ تاپ") || n.Contains("مک‌بوک")) return "💻";
        if (n.Contains("هدفون") || n.Contains("ایرپاد") || n.Contains("هندزفری")) return "🎧";
        if (n.Contains("ساعت") || n.Contains("واچ") || n.Contains("مچ‌بند")) return "⌚";
        if (n.Contains("پاوربانک") || n.Contains("شارژر") || n.Contains("باتری")) return "🔋";
        if (n.Contains("کیبورد") || n.Contains("صفحه‌کلید")) return "⌨️";
        if (n.Contains("ماوس") || n.Contains("موس")) return "🖱️";
        if (n.Contains("هودی") || n.Contains("کاپشن") || n.Contains("پالتو")) return "🧥";
        if (n.Contains("پیراهن") || n.Contains("تی‌شرت") || n.Contains("تیشرت")) return "👔";
        if (n.Contains("کتانی") || n.Contains("کفش")) return "👟";
        if (n.Contains("کوله‌پشتی") || n.Contains("کوله") || n.Contains("کیف")) return "🎒";
        if (n.Contains("عینک")) return "🕶️";
        if (n.Contains("اسپرسوساز") || n.Contains("قهوه‌ساز") || n.Contains("قهوه")) return "☕";
        if (n.Contains("سرخ‌کن") || n.Contains("پلوپز")) return "🍟";
        if (n.Contains("جارو")) return "🤖";
        if (n.Contains("چراغ") || n.Contains("لامپ") || n.Contains("آباژور")) return "💡";
        if (n.Contains("قابلمه") || n.Contains("تابه") || n.Contains("سرویس")) return "🍲";
        if (n.Contains("کتاب") || n.Contains("رمان") || n.Contains("دفتر")) return "📘";
        if (n.Contains("خودکار") || n.Contains("روان‌نویس") || n.Contains("قلم")) return "🖋️";
        if (n.Contains("تبلت") || n.Contains("آیپد")) return "📲";
        if (n.Contains("کنسول") || n.Contains("پلی‌استیشن") || n.Contains("دسته بازی")) return "🎮";
        if (n.Contains("سرم") || n.Contains("کرم") || n.Contains("لوسیون") || n.Contains("ضدآفتاب")) return "🧴";
        if (n.Contains("عطر") || n.Contains("ادکلن")) return "🌸";
        if (n.Contains("مسواک")) return "🪥";
        if (n.Contains("سشوار") || n.Contains("اتو مو") || n.Contains("ریش‌تراش")) return "💇‍♀️";
        if (n.Contains("زیرانداز") || n.Contains("یوگا")) return "🧘";
        if (n.Contains("دمبل") || n.Contains("کش ورزشی") || n.Contains("طناب")) return "🏋️";
        if (n.Contains("قمقمه") || n.Contains("فلاسک") || n.Contains("ماگ")) return "🥤";
        if (n.Contains("چادر") || n.Contains("کمپینگ") || n.Contains("کیسه خواب")) return "⛺";
        if (!string.IsNullOrWhiteSpace(p.Category?.Icon)) return p.Category.Icon;
        return "📦";
    }

    private static readonly string[] PersianMonths =
    {
        "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
        "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند"
    };

    public static (string Day, string Month) ShamsiDayMonth(DateTime dt)
    {
        try
        {
            var pc = new PersianCalendar();
            var d = dt.Year < 1900 ? DateTime.UtcNow : dt;
            return (Fa(pc.GetDayOfMonth(d).ToString("00")), PersianMonths[pc.GetMonth(d)]);
        }
        catch
        {
            return ("۲۰", "اردیبهشت");
        }
    }

    public static DateTime TehranNow()
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
        catch { try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); } catch { tz = TimeZoneInfo.Local; } }
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    // تاریخ شمسی به وقت تهران: ۱۴۰۵/۰۸/۰۵ ۱۴:۳۰
    public static string Date(DateTime utc)
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
        catch { try { tz = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); } catch { tz = TimeZoneInfo.Local; } }
        var d = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        var pc = new PersianCalendar();
        return Fa($"{pc.GetYear(d)}/{pc.GetMonth(d):00}/{pc.GetDayOfMonth(d):00} {d:HH:mm}");
    }
}
