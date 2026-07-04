using Application.Interface;
using Domain.Entities.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Payments;

public record CreatePayOSLinkCommand(Guid OrderId, int Amount, string Description, string ReturnUrl, string CancelUrl) : IRequest<string>;

public class CreatePayOSLinkHandler : IRequestHandler<CreatePayOSLinkCommand, string>
{
    private readonly IAppDbContext _context;
    private readonly IPayOSService _payOSService;

    public CreatePayOSLinkHandler(IAppDbContext context, IPayOSService payOSService)
    {
        _context = context;
        _payOSService = payOSService;
    }

    public async Task<string> Handle(CreatePayOSLinkCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new Exception("Order not found.");

        // Generate a random orderCode as required by PayOS (must be long/int)
        long orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        order.PaymentTxnRef = orderCode.ToString();
        order.PaymentProvider = "PayOS";

        await _context.SaveChangesAsync(cancellationToken);

        var paymentUrl = await _payOSService.CreatePaymentLinkAsync(
            orderCode, 
            request.Amount, 
            request.Description, 
            request.ReturnUrl, 
            request.CancelUrl);

        return paymentUrl;
    }
}
