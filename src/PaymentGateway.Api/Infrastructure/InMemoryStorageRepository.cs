using System.Collections.Concurrent;

using PaymentGateway.Api.Models.DTOs;

namespace PaymentGateway.Api.Infrastructure
{
    public class InMemoryStorageRepository : IStorageRepository
    {
        private readonly ConcurrentDictionary<Guid, PaymentDetails> payments = new ConcurrentDictionary<Guid, PaymentDetails>();

        public InMemoryStorageRepository()
        {

        }

        public bool SavePaymentDetails(Guid paymentId, PaymentDetails? paymentDetails)
        {
            if (paymentDetails == null)
                return false;

            // this is an attempt to check the behavior to restore idempotency when multiple payments, but idempotency problem is still a problem for recurring payment attempts.
            if (payments.TryAdd(paymentId, paymentDetails))
                return true;

            return false;

        }

        public PaymentDetails? GetPaymentInfoById(Guid paymentId)
        {
            if (!payments.TryGetValue(paymentId, out var paymentDetails))
                return null;

            return paymentDetails;
        }
    }
}
