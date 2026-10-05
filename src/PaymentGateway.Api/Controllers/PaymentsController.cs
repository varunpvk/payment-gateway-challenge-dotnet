using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Models.Requests;

using FluentValidation;

using System.Net;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IBankConnectorService _bankConnectorService;
    private readonly IValidator<PaymentRequest> _paymentRequestValidator;
    private readonly ILogger<PaymentsController>? _logger;

    public PaymentsController(
        IBankConnectorService bankConnectorService,
        IValidator<PaymentRequest> paymentRequestValidator,
        ILogger<PaymentsController>? logger = null)
    {
        this._bankConnectorService = bankConnectorService;
        this._paymentRequestValidator = paymentRequestValidator;
        this._logger = logger;
    }

    [HttpGet("/payments/{paymentId}")]
    public ActionResult<PaymentResponse?> GetPayment(Guid paymentId)
    {
        var paymentDetails = _bankConnectorService.GetPaymentDetails(paymentId);

        if(paymentDetails == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(paymentDetails);
    }

    [HttpPost("/payments")]
    public async Task<ActionResult<PaymentResponse>> ProcessPaymentAsync([FromBody] PaymentRequest paymentRequest, CancellationToken cancellationToken)
    {
        // create correlation id as soon as request is received
        var paymentId = Guid.NewGuid();
        

        _logger?.LogInformation("Received payment request. PaymentId={PaymentId}, Amount={Amount}, Currency={Currency}", paymentId, paymentRequest.Amount, paymentRequest.Currency);

        var validationResult = _paymentRequestValidator.Validate(paymentRequest);
        
        if (!validationResult.IsValid)
        {
            _logger?.LogWarning("Validation failed for PaymentId={PaymentId}: {Errors}", paymentId, string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage)));
            return new BadRequestObjectResult(validationResult.Errors);
        }

        _logger?.LogInformation("Initiating payment. PaymentId={PaymentId}", paymentId);

        var paymentResponse = await _bankConnectorService.InitiatePaymentAsync(paymentId, paymentRequest, cancellationToken);

        _logger?.LogInformation("Payment processed. PaymentId={PaymentId}, Status={Status}", paymentResponse.PaymentId, paymentResponse.Status);

        return Created($"/payments/{paymentResponse.PaymentId}", paymentResponse);
    }
}