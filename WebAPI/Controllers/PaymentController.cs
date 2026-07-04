using Application.DTO;
using Application.Features.Payments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PaymentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Nhận thông tin đơn hàng từ request, gửi lệnh qua MediatR để tạo link thanh toán PayOS.
        /// Trả về link thanh toán (URL) để frontend redirect người dùng.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreatePaymentLink([FromBody] CreatePaymentLinkRequest request)
        {
            try
            {
                var command = new CreatePayOSLinkCommand(
                    request.OrderId,
                    request.Amount,
                    request.Description,
                    request.ReturnUrl,
                    request.CancelUrl
                );

                var paymentUrl = await _mediator.Send(command);
                return Ok(new { paymentUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Endpoint hứng dữ liệu Webhook từ máy chủ PayOS.
        /// Nhận payload, gửi lệnh qua MediatR để xử lý xác thực và cập nhật trạng thái đơn hàng.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> PayOSWebhook([FromBody] WebhookType webhookBody)
        {
            try
            {
                Console.WriteLine($"[WEBHOOK INCOMING] Received webhook from PayOS. Code: {webhookBody.code}, OrderCode: {webhookBody.data.orderCode}");
                var command = new ProcessPayOSWebhookCommand(webhookBody);
                var result = await _mediator.Send(command);

                if (result)
                {
                    return Ok(new { success = true, message = "Webhook processed successfully" });
                }
                
                return BadRequest(new { success = false, message = "Webhook verification failed or error code received" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class CreatePaymentLinkRequest
    {
        public Guid OrderId { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
