using System.Net.Http.Json;
using System.Text.Json;

namespace NovinShop.Services;

// سرویس اتصال به درگاه پرداخت زرین‌پال (نسخه ۴ REST API - پشتیبانی از حالت عملیاتی و Sandbox)
// مستندات رسمی: https://www.zarinpal.com/docs/paymentGateway/connectToGateway.html
public class ZarinpalService
{
    private readonly HttpClient _http;
    public ZarinpalService(HttpClient http) => _http = http;

    private static string BaseUrl(bool sandbox) =>
        sandbox ? "https://sandbox.zarinpal.com" : "https://payment.zarinpal.com";

    public record RequestResult(bool Ok, string? Authority, string? StartPayUrl, string? Error);
    public record VerifyResult(bool Ok, string? RefId, string? CardPan, int Code, string? Error);

    public async Task<RequestResult> RequestAsync(
        string merchantId, bool sandbox, long amountToman, string description, string callbackUrl, string? mobile = null)
    {
        if (string.IsNullOrWhiteSpace(merchantId) || merchantId.Trim().Length < 32)
            return new RequestResult(false, null, null, "کد درگاه (Merchant ID) زرین‌پال در پنل مدیریت تنظیم نشده یا نامعتبر است.");

        var url = $"{BaseUrl(sandbox)}/pg/v4/payment/request.json";
        var payload = new
        {
            merchant_id = merchantId.Trim(),
            amount = amountToman * 10L,      // تبدیل تومان به ریال (IRR)
            currency = "IRR",
            description,
            callback_url = callbackUrl,
            metadata = new { mobile = mobile ?? "" }
        };

        try
        {
            using var resp = await _http.PostAsJsonAsync(url, payload);
            var raw = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() == 100 &&
                data.TryGetProperty("authority", out var authEl))
            {
                var authority = authEl.GetString()!;
                var startPay = $"{BaseUrl(sandbox)}/pg/StartPay/{authority}";
                return new RequestResult(true, authority, startPay, null);
            }

            var errMsg = ExtractError(root) ?? $"خطای درگاه زرین‌پال (وضعیت HTTP {(int)resp.StatusCode})";
            return new RequestResult(false, null, null, errMsg);
        }
        catch (Exception ex)
        {
            return new RequestResult(false, null, null, "خطا در برقراری ارتباط با سرور زرین‌پال: " + ex.Message);
        }
    }

    public async Task<VerifyResult> VerifyAsync(string merchantId, bool sandbox, long amountToman, string authority)
    {
        var url = $"{BaseUrl(sandbox)}/pg/v4/payment/verify.json";
        var payload = new
        {
            merchant_id = merchantId.Trim(),
            amount = amountToman * 10L,      // مبلغ به ریال
            authority
        };

        try
        {
            using var resp = await _http.PostAsJsonAsync(url, payload);
            var raw = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("code", out var codeEl))
            {
                var code = codeEl.GetInt32();
                if (code is 100 or 101)
                {
                    var refId = data.TryGetProperty("ref_id", out var r) ? r.ToString() : "";
                    var cardPan = data.TryGetProperty("card_pan", out var cp) ? cp.GetString() : null;
                    return new VerifyResult(true, refId, cardPan, code, null);
                }
            }

            var errMsg = ExtractError(root) ?? "تراکنش توسط بانک تأیید نشد.";
            return new VerifyResult(false, null, null, -1, errMsg);
        }
        catch (Exception ex)
        {
            return new VerifyResult(false, null, null, -1, "خطا در اعتبارسنجی تراکنش با زرین‌پال: " + ex.Message);
        }
    }

    private static string? ExtractError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errs)) return null;
        if (errs.ValueKind == JsonValueKind.Object && errs.TryGetProperty("message", out var m))
            return m.GetString();
        if (errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0 &&
            errs[0].TryGetProperty("message", out var m0))
            return m0.GetString();
        return null;
    }
}
