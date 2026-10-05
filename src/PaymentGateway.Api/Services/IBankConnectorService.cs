using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public interface IBankConnectorService
    {
        Task<PaymentResponse> InitiatePaymentAsync(Guid paymentId, PaymentRequest paymentRequest, CancellationToken cancellationToken);
        PaymentResponse GetPaymentDetails(Guid paymentId);
    }
}
