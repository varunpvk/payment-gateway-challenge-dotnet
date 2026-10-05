using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Infrastructure
{
    public class MounteBankClient : IMounteBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MounteBankClient>? _logger;

        public MounteBankClient(HttpClient httpClient, ILogger<MounteBankClient>? logger = null)
        {
            this._httpClient = httpClient;
            this._logger = logger;
        }

        public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken)
        {
            var masked = request.CardNumber != null && request.CardNumber.Length >= 4
                ? $"****{request.CardNumber.Substring(request.CardNumber.Length - 4)}"
                : "****";

            _logger?.LogInformation("Posting to mock bank. PaymentId={PaymentId}, CardEnding={CardEnding}", request.PaymentId, masked);

            var response = await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning("Bank returned non-success status. PaymentId={PaymentId}, StatusCode={StatusCode}", request.PaymentId, response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BankPaymentResponse>(cancellationToken);

            if (result == null)
            {
                _logger?.LogError("Bank returned empty response. PaymentId={PaymentId}", request.PaymentId);
                throw new InvalidOperationException("Bank returned an empty response");
            }

            _logger?.LogInformation("Received bank response. PaymentId={PaymentId}, Authorized={Authorized}", request.PaymentId, result.Authorized);

            return result;
        }
    }
}
