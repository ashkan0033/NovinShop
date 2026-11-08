using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public enum OrderStatus { PendingPayment = 0, Paid = 1, Shipped = 2, Cancelled = 3, PendingApproval = 4 }

public static class OrderStatusInfo
{
    public static string Label(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "در انتظار پرداخت",
        OrderStatus.PendingApproval => "در انتظار بررسی فیش",
        OrderStatus.Paid => "پرداخت‌شده",
        OrderStatus.Shipped => "ارسال‌شده",
        _ => "لغوشده"
    };

    public static string Css(this OrderStatus s) => s switch
    {
        OrderStatus.PendingPayment => "wait",
        OrderStatus.PendingApproval => "wait",
        OrderStatus.Paid => "ok",
        OrderStatus.Shipped => "ship",
        _ => "off"
    };

    public static string MethodLabel(string? method) => method switch
    {
        "zarinpal" => "درگاه آنلاین زرین‌پال",
        "card_to_card" => "کارت به کارت (ثبت فیش)",
        "simulator" => "شبیه‌ساز درگاه آزمایشی",
        _ => "نامشخص"
    };
}

public class Order
{
    public int Id { get; set; }
    public string Code { get; set; } = "";            // کد پیگیری
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public long Subtotal { get; set; }
    public long DiscountAmount { get; set; }
    public string? CouponCode { get; set; }
    public long Shipping { get; set; }
    public long Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public string? PaymentRef { get; set; }

    // اطلاعات روش پرداخت، درگاه و فیش کارت به کارت
    public string? PaymentMethod { get; set; }        // zarinpal | card_to_card | simulator
    public string? Authority { get; set; }            // کد Authority زرین‌پال
    public string? ReceiptImagePath { get; set; }     // تصویر فیش واریزی کارت به کارت
    public string? ReceiptNote { get; set; }          // شماره پیگیری / توضیحات واریز مشتری
    public string? AdminNote { get; set; }            // یادداشت مدیر (مثلاً علت رد فیش)

    public List<OrderItem> Items { get; set; } = new();
}

// نام و قیمت در لحظه‌ی خرید ذخیره می‌شود (با تغییر یا حذف محصول، سفارش قدیمی دست‌نخورده می‌ماند)
public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int UnitPrice { get; set; }
    public int UnitCost { get; set; }               // بهای خرید واحد در لحظه ثبت سفارش (جهت محاسبه سود دقیق)
    public int Quantity { get; set; }
    public long Total => (long)UnitPrice * Quantity;
    public long CostTotal => (long)UnitCost * Quantity;
    public long Profit => Total - CostTotal;
}

public class CheckoutForm
{
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "نام باید حداقل ۳ کاراکتر باشد")]
    public string Name { get; set; } = "";

    public string Phone { get; set; } = "";          // اعتبارسنجی در کنترلر (پشتیبانی از ارقام فارسی)

    [Required(ErrorMessage = "آدرس را وارد کنید")]
    [StringLength(400, MinimumLength = 10, ErrorMessage = "آدرس باید حداقل ۱۰ کاراکتر باشد")]
    public string Address { get; set; } = "";

    public string PaymentMethod { get; set; } = "";
}

public class TrackForm
{
    public string? Code { get; set; }
    public string? Phone { get; set; }
}

public record CheckoutPage(CartViewModel Cart, CheckoutForm Form, List<PaymentGateway> Gateways);
public record OrderListViewModel(List<Order> Items, OrderStatus? Status, string? Q, int Page, int TotalPages, int Total);
