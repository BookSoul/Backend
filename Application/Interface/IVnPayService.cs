using Application.DTO;

namespace Application.Interface;

public interface IVnPayService
{
    VnPayPaymentUrlDto CreatePaymentUrl(
        Guid orderId,
        decimal amount,
        string orderInfo,
        string ipAddress,
        CancellationToken cancellationToken = default);

    VnPayVerificationResultDto VerifyReturnData(
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken = default);
}
