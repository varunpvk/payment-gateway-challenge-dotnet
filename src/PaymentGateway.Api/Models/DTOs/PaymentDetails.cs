using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Models.DTOs
{
    public record PaymentDetails
    {
        public PaymentStatus Status { get; init; }
        public string CardNumberLastFourDigits { get; init; }
        public int ExpiryMonth { get; init; }
        public int ExpiryYear { get; init; }
        public string Currency { get; init; }
        public int Amount { get; init; }

        public PaymentDetails(PaymentStatus status, string cardNumberLastFour, int expiryMonth, int expiryYear, string currency, int amount)
        {
            Status = status;
            CardNumberLastFourDigits = cardNumberLastFour;
            ExpiryMonth = expiryMonth;
            ExpiryYear = expiryYear;
            Currency = currency;
            Amount = amount;
        }
    }
    
}
