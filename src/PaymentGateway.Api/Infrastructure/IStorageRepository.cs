using PaymentGateway.Api.Models.DTOs;

namespace PaymentGateway.Api.Infrastructure
{
    public interface IStorageRepository
    {
        public PaymentDetails? GetPaymentInfoById(Guid paymentId);
        public bool SavePaymentDetails(Guid paymentId, PaymentDetails? paymentDetails);
    }
}
