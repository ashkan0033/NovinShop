using System.ComponentModel.DataAnnotations;

namespace NovinShop.Models;

public enum AccountingEntryType
{
    Income = 0,   // دخل / درآمد
    Expense = 1   // خرج / هزینه
}

public class AccountingEntry
{
    public int Id { get; set; }

    public AccountingEntryType Type { get; set; } = AccountingEntryType.Expense;

    [Required(ErrorMessage = "دسته‌بندی سند مالی را انتخاب کنید")]
    [StringLength(80)]
    public string Category { get; set; } = "خرید و تأمین کالا";

    [Required(ErrorMessage = "شرح سند مالی را وارد کنید")]
    [StringLength(200)]
    public string Title { get; set; } = "";

    [Range(1, 100_000_000_000, ErrorMessage = "مبلغ معتبر وارد کنید")]
    public long Amount { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(400)]
    public string? Note { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
