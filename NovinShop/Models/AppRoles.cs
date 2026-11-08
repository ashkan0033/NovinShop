namespace NovinShop.Models;

public static class AppRoles
{
    public const string Admin = "Admin";           // مدیر کل (دسترسی کامل به تمام بخش‌ها و مدیریت کاربران)
    public const string Editor = "Editor";         // کارمند محصولات، محتوا و انبار (مشابه ویرایشگر وردپرس)
    public const string Accountant = "Accountant"; // حسابدار و مسئول سفارش‌ها و انبار

    public const string AllStaff = "Admin,Editor,Accountant";
    public const string CatalogStaff = "Admin,Editor";
    public const string OrderAndFinanceStaff = "Admin,Accountant";
    public const string WarehouseStaff = "Admin,Editor,Accountant";

    public static string Label(string role) => role switch
    {
        Admin => "مدیر کل (دسترسی کامل)",
        Editor => "کارمند محصولات و محتوا (ویرایشگر)",
        Accountant => "حسابدار و مسئول سفارش‌ها",
        _ => role
    };

    public static string BadgeCss(string role) => role switch
    {
        Admin => "ok",
        Editor => "ship",
        Accountant => "wait",
        _ => "off"
    };
}
