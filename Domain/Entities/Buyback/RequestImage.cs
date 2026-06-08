namespace Domain.Entities.Buyback;

public class RequestImage
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public BuybackRequest Request { get; set; } = null!;
}
