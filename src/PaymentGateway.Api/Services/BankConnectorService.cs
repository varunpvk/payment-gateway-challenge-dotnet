using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Services
{
    public class BankConnectorService : IBankConnectorService
    {
        private readonly IStorageRepository _storageRepository;
        private readonly IMounteBankClient _mounteBankCleint;
        private readonly ILogger<BankConnectorService>? _logger;

        public BankConnectorService(IStorageRepository storageRepository, IMounteBankClient mounteBankCleint, ILogger<BankConnectorService>? logger = null)
        {
            this._storageRepository = storageRepository;
            this._mounteBankCleint = mounteBankCleint;
            this._logger = logger;

        }

        public PaymentResponse? GetPaymentDetails(Guid paymentId)
        {
            var paymentDetails = _storageRepository.GetPaymentInfoById(paymentId);
            return paymentDetails != null ? new PaymentResponse
            {
                PaymentId = paymentId,
                Amount = paymentDetails.Amount,
                CardNumberLastFourDigits = paymentDetails.CardNumberLastFourDigits,
                ExpiryMonth = paymentDetails.ExpiryMonth,
                ExpiryYear = paymentDetails.ExpiryYear,
                Currency = paymentDetails.Currency,
                Status = paymentDetails.Status.ToString(),
            } : null;
        }

        public async Task<PaymentResponse> InitiatePaymentAsync(Guid paymentId, PaymentRequest paymentRequest, CancellationToken cancellationToken)
        {
            var expiryDate = String.Concat(paymentRequest.ExpiryMonth.ToString(), paymentRequest.ExpiryYear.ToString());
            decimal amount = (paymentRequest.Amount / 100);
            var bankPaymentRequest = new BankPaymentRequest
            {
                PaymentId = paymentId,
                CardNumber = paymentRequest.CardNumber,
                ExpiryDate = expiryDate,
                Amount = amount.ToString(),
                Currency = paymentRequest.Currency,
                CVV = paymentRequest.CVV,
            };

            var masked = bankPaymentRequest.CardNumber != null && bankPaymentRequest.CardNumber.Length >= 4
                ? $"****{bankPaymentRequest.CardNumber.Substring(bankPaymentRequest.CardNumber.Length - 4)}"
                : "****";

            _logger?.LogInformation("Sending payment to bank. PaymentId={PaymentId}, CardEnding={CardEnding}", paymentId, masked);

            var bankPaymentResponse = await _mounteBankCleint.ProcessPaymentAsync(bankPaymentRequest, cancellationToken);
            PaymentStatus paymentStatus = bankPaymentResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;
            var cardNumber = bankPaymentRequest.CardNumber;
            string cardNumberLastFourDigits = cardNumber.Substring(cardNumber.Length - 4, 4);
            var expiryMonth = paymentRequest.ExpiryMonth;
            var expiryYear = paymentRequest.ExpiryYear;
            string currency = paymentRequest.Currency;


            PaymentDetails paymentDetails = new PaymentDetails(
                paymentStatus, cardNumberLastFourDigits, expiryMonth, expiryYear, currency, paymentRequest.Amount);

            var saveResult = _storageRepository.SavePaymentDetails(paymentId, paymentDetails);

            if(!saveResult)
            {
                _logger?.LogError("Failed to save payment details. PaymentId={PaymentId}", paymentId);
                throw new InvalidOperationException("Failed to save payment details.");
            }

            _logger?.LogInformation("Payment details saved. PaymentId={PaymentId}", paymentId);

            return new PaymentResponse
            {
                PaymentId = paymentId,
                Amount = paymentRequest.Amount,
                CardNumberLastFourDigits = cardNumberLastFourDigits,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear,
                Currency = currency,
                Status = paymentStatus.ToString(),
            };
        }
    }
}

