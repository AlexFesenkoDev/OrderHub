namespace OrderHub.Api.Models
{
    public record OrderItemDto(int ProductId, string Name, int Quantity, decimal UnitPrice);

    public class CreateOrderDto
    {
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string PaymentMethod { get; set; } = "card"; // "card" | "paypal" | "mock"
        public string[] NotifyVia { get; set; } = Array.Empty<string>(); // "email" | "sms" | "telegram"
        public List<OrderItemDto> Items { get; set; } = new();
        public string? PromoCode { get; set; }
        public string Currency { get; set; } = "USD";
        public string Country { get; set; } = "US";
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string? Error { get; set; }
    }
}
