using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public enum InventoryTxType
{
    StockIn = 0,      // ورود کالا به انبار (خرید / تأمین)
    SaleOut = 1,      // خروج بابت فروش سفارش
    ReturnIn = 2,     // برگشت به انبار (لغو سفارش / مرجوعی)
    Adjustment = 3,   // تعدیل انبارگردانی
    WasteOut = 4      // ضایعات / کسری انبار
}

public static class InventoryTxTypeInfo
{
    public static string Label(this InventoryTxType t) => t switch
    {
        InventoryTxType.StockIn => "ورود به انبار (خرید کالا)",
        InventoryTxType.SaleOut => "خروج بابت فروش سفارش",
        InventoryTxType.ReturnIn => "برگشت به انبار (لغو سفارش)",
        InventoryTxType.Adjustment => "تعدیل انبارگردانی",
        InventoryTxType.WasteOut => "ضایعات / کسری انبار",
        _ => "نامشخص"
    };

    public static string Css(this InventoryTxType t) => t switch
    {
        InventoryTxType.StockIn => "ok",
        InventoryTxType.ReturnIn => "ship",
        InventoryTxType.SaleOut => "wait",
        InventoryTxType.Adjustment => "off",
        InventoryTxType.WasteOut => "er",
        _ => "off"
    };
}

public class InventoryTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public InventoryTxType Type { get; set; }

    /// <summary>
    /// مقدار تغییر موجودی (مثبت برای ورود، منفی برای خروج)
    /// </summary>
    public int QuantityChange { get; set; }

    /// <summary>
    /// موجودی نهایی محصول در انبار بلافاصله پس از این تراکنش
    /// </summary>
    public int StockAfter { get; set; }

    /// <summary>
    /// قیمت خرید / بهای تمام‌شده هر واحد در این تراکنش (تومان)
    /// </summary>
    public int UnitCost { get; set; }

    public long TotalCost => Math.Abs((long)QuantityChange) * UnitCost;

    [StringLength(120)]
    public string? Reference { get; set; } // نام تأمین‌کننده، شماره فاکتور خرید یا کد سفارش

    [StringLength(300)]
    public string? Note { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
