namespace Application.DTO;

public record VnPayPaymentUrlDto(
    string PaymentUrl,
    string TxnRef,
    DateTime ExpireAt
);

public record VnPayVerificationResultDto(
    bool IsValidSignature,
    bool IsSuccess,
    string TxnRef,
    decimal Amount,
    string ResponseCode,
    string TransactionStatus,
    string? TransactionNo,
    string? BankCode,
    DateTime? PayDate,
    IReadOnlyDictionary<string, string> RawData
);

public record VnPayReturnResponseDto(
    Guid? OrderId,
    bool Success,
    string Message,
    string PaymentStatus,
    string? ResponseCode,
    string? TransactionNo
);
