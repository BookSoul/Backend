using Application.DTO;
using Application.Interface;
using Microsoft.Extensions.Configuration;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSClient _payOS;

        public PayOSService(IConfiguration configuration)
        {
            var clientId = configuration["PayOS:ClientId"] ?? throw new ArgumentNullException("PayOS:ClientId");
            var apiKey = configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException("PayOS:ApiKey");
            var checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("PayOS:ChecksumKey");

            var options = new PayOSOptions
            {
                ClientId = clientId,
                ApiKey = apiKey,
                ChecksumKey = checksumKey
            };
            _payOS = new PayOSClient(options);
        }

        public async Task<string> CreatePaymentLinkAsync(long orderCode, int amount, string description, string returnUrl, string cancelUrl)
        {
            try
            {
                var paymentData = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = amount,
                    Description = description,
                    CancelUrl = cancelUrl,
                    ReturnUrl = returnUrl
                };

                var createPaymentResult = await _payOS.PaymentRequests.CreateAsync(paymentData);
                return createPaymentResult.CheckoutUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create PayOS payment link: {ex.Message}", ex);
            }
        }

        public async Task<Application.DTO.WebhookDataType> VerifyWebhookDataAsync(Application.DTO.WebhookType webhookBody)
        {
            try
            {
                // Map to new SDK Webhook type
                var payOSWebhook = new Webhook
                {
                    Code = webhookBody.code ?? string.Empty,
                    Description = webhookBody.desc ?? string.Empty,
                    Success = webhookBody.success,
                    Data = new WebhookData
                    {
                        OrderCode = webhookBody.data.orderCode,
                        Amount = webhookBody.data.amount,
                        Description = webhookBody.data.description ?? string.Empty,
                        AccountNumber = webhookBody.data.accountNumber ?? string.Empty,
                        Reference = webhookBody.data.reference ?? string.Empty,
                        TransactionDateTime = webhookBody.data.transactionDateTime ?? string.Empty,
                        Currency = webhookBody.data.currency ?? string.Empty,
                        PaymentLinkId = webhookBody.data.paymentLinkId ?? string.Empty,
                        Code = webhookBody.data.code ?? string.Empty,
                        Description2 = webhookBody.data.desc ?? string.Empty,
                        CounterAccountBankId = webhookBody.data.counterAccountBankId ?? string.Empty,
                        CounterAccountBankName = webhookBody.data.counterAccountBankName ?? string.Empty,
                        CounterAccountName = webhookBody.data.counterAccountName ?? string.Empty,
                        CounterAccountNumber = webhookBody.data.counterAccountNumber ?? string.Empty,
                        VirtualAccountName = webhookBody.data.virtualAccountName ?? string.Empty,
                        VirtualAccountNumber = webhookBody.data.virtualAccountNumber ?? string.Empty
                    },
                    Signature = webhookBody.signature ?? string.Empty
                };

                // This will verify the webhook using the checksumKey provided to PayOSOptions
                var verifiedData = await _payOS.Webhooks.VerifyAsync(payOSWebhook);
                
                return new Application.DTO.WebhookDataType
                {
                    orderCode = verifiedData.OrderCode,
                    amount = (int)verifiedData.Amount,
                    description = verifiedData.Description,
                    accountNumber = verifiedData.AccountNumber,
                    reference = verifiedData.Reference,
                    transactionDateTime = verifiedData.TransactionDateTime,
                    currency = verifiedData.Currency,
                    paymentLinkId = verifiedData.PaymentLinkId,
                    code = verifiedData.Code,
                    desc = verifiedData.Description, // SDK WebhookData doesn't have Desc
                    counterAccountBankId = verifiedData.CounterAccountBankId,
                    counterAccountBankName = verifiedData.CounterAccountBankName,
                    counterAccountName = verifiedData.CounterAccountName,
                    counterAccountNumber = verifiedData.CounterAccountNumber,
                    virtualAccountName = verifiedData.VirtualAccountName,
                    virtualAccountNumber = verifiedData.VirtualAccountNumber
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Webhook verification failed: {ex.Message}", ex);
            }
        }
    }
}
