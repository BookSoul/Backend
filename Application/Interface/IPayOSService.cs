using Application.DTO;

namespace Application.Interface
{
    public interface IPayOSService
    {
        Task<string> CreatePaymentLinkAsync(long orderCode, int amount, string description, string returnUrl, string cancelUrl);
        
        /// <summary>
        /// Xác thực dữ liệu webhook từ PayOS. Trả về WebhookDataType nếu thành công.
        /// </summary>
        Task<Application.DTO.WebhookDataType> VerifyWebhookDataAsync(Application.DTO.WebhookType webhookBody);
    }
}
