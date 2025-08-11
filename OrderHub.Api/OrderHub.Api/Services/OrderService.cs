using OrderHub.Api.Models;
using System.Text;

namespace OrderHub.Api.Services
{
    public class OrderService
    {
        private static readonly List<dynamic> _orders = new();
        private static readonly string _logPath = Path.Combine(AppContext.BaseDirectory, "orderhub.log");
        private static readonly Random _rnd = new();

        public async Task<object> PlaceOrderAsync(CreateOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Order must contain at least one item.");

            if (string.IsNullOrWhiteSpace(dto.CustomerEmail) && string.IsNullOrWhiteSpace(dto.CustomerPhone))
                throw new InvalidOperationException("Customer contact is required (email or phone).");

            decimal subtotal = dto.Items.Sum(i => i.UnitPrice * i.Quantity);
            decimal discount = ApplyDiscounts(subtotal, dto.PromoCode);
            decimal taxed = ApplyTax(subtotal - discount, dto.Country);
            string currency = dto.Currency;

            PaymentResult payResult = dto.PaymentMethod.ToLower() switch
            {
                "card" => await PayByCardAsync(taxed, currency),
                "paypal" => await PayByPaypalAsync(taxed, currency),
                "mock" => await PayByMockAsync(taxed, currency),
                _ => new PaymentResult { Success = false, Error = "Unknown payment method" }
            };

            var orderId = Guid.NewGuid().ToString("N");
            var order = new
            {
                OrderId = orderId,
                CreatedAt = DateTimeOffset.UtcNow,
                Items = dto.Items,
                Subtotal = subtotal,
                Discount = discount,
                Total = taxed,
                Currency = currency,
                Payment = payResult,
                Customer = new { dto.CustomerEmail, dto.CustomerPhone },
                Country = dto.Country,
                PromoCode = dto.PromoCode
            };

            _orders.Add(order);

            foreach (var channel in dto.NotifyVia.Select(x => x.ToLower()))
            {
                try
                {
                    if (channel == "email" && !string.IsNullOrWhiteSpace(dto.CustomerEmail))
                        await SendEmailAsync(dto.CustomerEmail!, $"Order {orderId}", $"Total: {taxed} {currency}");
                    else if (channel == "sms" && !string.IsNullOrWhiteSpace(dto.CustomerPhone))
                        await SendSmsAsync(dto.CustomerPhone!, $"Order {orderId} paid: {payResult.Success}");
                    else if (channel == "telegram")
                        await SendTelegramAsync($"Order {orderId}: {taxed} {currency}");
                }
                catch (Exception ex)
                {
                    await LogAsync($"[WARN][{orderId}] Notification '{channel}' failed: {ex.Message}");
                }
            }

            await LogAsync($"[INFO][{orderId}] total={taxed} {currency}, paySuccess={payResult.Success}, method={dto.PaymentMethod}");

            return new
            {
                orderId,
                status = payResult.Success ? "paid" : "failed",
                total = taxed,
                currency,
                payment = payResult,
                items = dto.Items
            };
        }

        private decimal ApplyDiscounts(decimal subtotal, string? promo)
        {
            decimal discount = 0m;
            if (!string.IsNullOrWhiteSpace(promo))
            {
                if (promo.Equals("BLACK", StringComparison.OrdinalIgnoreCase)) discount += subtotal * 0.10m;
                if (promo.Equals("LOYAL", StringComparison.OrdinalIgnoreCase)) discount += 5m;
            }
            if (subtotal > 500m) discount += 15m;
            return Math.Clamp(discount, 0m, subtotal);
        }

        private decimal ApplyTax(decimal amount, string country)
        {
            decimal rate = country.ToUpper() switch
            {
                "US" => 0.085m,
                "PL" => 0.23m,
                "UA" => 0.20m,
                _ => 0.18m
            };
            return Math.Round(amount * (1 + rate), 2);
        }

        private Task<PaymentResult> PayByCardAsync(decimal amount, string currency)
        {
            bool ok = _rnd.Next(0, 100) > 5;
            return Task.FromResult(new PaymentResult
            {
                Success = ok,
                TransactionId = ok ? $"CARD-{Guid.NewGuid():N}" : null,
                Error = ok ? null : "Card declined"
            });
        }

        private Task<PaymentResult> PayByPaypalAsync(decimal amount, string currency)
        {
            bool ok = _rnd.Next(0, 100) > 10;
            return Task.FromResult(new PaymentResult
            {
                Success = ok,
                TransactionId = ok ? $"PP-{Guid.NewGuid():N}" : null,
                Error = ok ? null : "PayPal error"
            });
        }

        private Task<PaymentResult> PayByMockAsync(decimal amount, string currency)
        {
            return Task.FromResult(new PaymentResult
            {
                Success = true,
                TransactionId = $"MOCK-{Guid.NewGuid():N}"
            });
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            await LogAsync($"[EMAIL] to={to}; subject={subject}; body={body}");
        }

        private async Task SendSmsAsync(string phone, string text)
        {
            await LogAsync($"[SMS] to={phone}; text={text}");
        }

        private async Task SendTelegramAsync(string text)
        {
            await LogAsync($"[TG] {text}");
        }

        private async Task LogAsync(string line)
        {
            var log = $"{DateTimeOffset.UtcNow:O} {line}{Environment.NewLine}";
            await File.AppendAllTextAsync(_logPath, log, Encoding.UTF8);
        }

        public IEnumerable<object> GetAllOrders() => _orders;
    }
}
