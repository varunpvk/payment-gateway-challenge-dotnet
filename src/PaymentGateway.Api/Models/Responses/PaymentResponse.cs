namespace PaymentGateway.Api.Models.Responses
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }
        public string Status { get; set; }
        public string CardNumberLastFourDigits { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Currency { get; set; }
        public int Amount { get; set; }
    }
}
