using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Application.DTO;
using Application.Interface;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Service;

public class VnPayService : IVnPayService
{
    private readonly IConfiguration _configuration;

    public VnPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public VnPayPaymentUrlDto CreatePaymentUrl(
        Guid orderId,
        decimal amount,
        string orderInfo,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("VnPay");
        var paymentUrl = Require(section["PaymentUrl"], "VnPay:PaymentUrl");
        var tmnCode = Require(section["TmnCode"], "VnPay:TmnCode");
        var hashSecret = Require(section["HashSecret"], "VnPay:HashSecret");
        var returnUrl = Require(section["ReturnUrl"], "VnPay:ReturnUrl");
        var version = section["Version"] ?? "2.1.0";
        var command = section["Command"] ?? "pay";
        var locale = section["Locale"] ?? "vn";
        var orderType = section["OrderType"] ?? "other";
        var expireMinutes = int.TryParse(section["ExpireMinutes"], out var minutes) && minutes > 0 ? minutes : 15;

        var now = DateTime.UtcNow.AddHours(7);
        var expireAt = now.AddMinutes(expireMinutes);
        var txnRef = orderId.ToString("N");
        var vnpAmount = ((long)Math.Round(amount, 0, MidpointRounding.AwayFromZero) * 100).ToString(CultureInfo.InvariantCulture);

        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = version,
            ["vnp_Command"] = command,
            ["vnp_TmnCode"] = tmnCode,
            ["vnp_Amount"] = vnpAmount,
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress,
            ["vnp_Locale"] = locale,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = orderType,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_ExpireDate"] = expireAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)
        };

        var signData = BuildQueryString(values);
        var secureHash = HmacSha512(hashSecret, signData);
        var url = $"{paymentUrl}?{signData}&vnp_SecureHash={secureHash}";

        return new VnPayPaymentUrlDto(url, txnRef, expireAt);
    }

    public VnPayVerificationResultDto VerifyReturnData(
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken = default)
    {
        var hashSecret = Require(_configuration["VnPay:HashSecret"], "VnPay:HashSecret");
        var receivedHash = query.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var pair in query)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) ||
                pair.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                pair.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            values[pair.Key] = pair.Value ?? string.Empty;
        }

        var computedHash = HmacSha512(hashSecret, BuildQueryString(values));
        var validSignature = receivedHash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
        var responseCode = values.GetValueOrDefault("vnp_ResponseCode") ?? string.Empty;
        var transactionStatus = values.GetValueOrDefault("vnp_TransactionStatus") ?? string.Empty;
        var amountText = values.GetValueOrDefault("vnp_Amount") ?? "0";
        decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amountInSmallestUnit);
        var payDate = ParseVnPayDate(values.GetValueOrDefault("vnp_PayDate"));

        return new VnPayVerificationResultDto(
            validSignature,
            validSignature && responseCode == "00" && transactionStatus == "00",
            values.GetValueOrDefault("vnp_TxnRef") ?? string.Empty,
            amountInSmallestUnit / 100m,
            responseCode,
            transactionStatus,
            values.GetValueOrDefault("vnp_TransactionNo"),
            values.GetValueOrDefault("vnp_BankCode"),
            payDate,
            values);
    }

    private static string Require(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} is not configured.");
        }

        return value.Trim();
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> values)
    {
        return string.Join("&", values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => $"{WebUtility.UrlEncode(pair.Key)}={WebUtility.UrlEncode(pair.Value)}"));
    }

    private static string HmacSha512(string key, string input)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(inputBytes)).ToLowerInvariant();
    }

    private static DateTime? ParseVnPayDate(string? value)
    {
        if (DateTime.TryParseExact(
                value,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return DateTime.SpecifyKind(parsed.AddHours(-7), DateTimeKind.Utc);
        }

        return null;
    }
}
