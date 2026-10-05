using FluentValidation;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Models.Validations
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequest>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty().WithMessage("Card number is required.")
                .MinimumLength(14).WithMessage("Card number must be at least 14 characters long.")
                .MaximumLength(19).WithMessage("Card number must not exceed 19 characters.")
                .Matches(@"^\d+$").WithMessage("Card number must contain only digits.");
            RuleFor(x => x.ExpiryMonth)
                .NotEmpty().WithMessage("Expiry month is required.")
                .InclusiveBetween(1, 12).WithMessage("Expiry month must be between 1 and 12.");
            RuleFor(x => x.ExpiryYear)
                .NotEmpty().WithMessage("Expiry year is required.")
                .GreaterThanOrEqualTo(DateTime.UtcNow.Year).WithMessage("Expiry year must be the current year or later.");
            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Length(3).WithMessage("Currency must be a 3-letter ISO code."); //must validate for USD, GBP and EUR
            RuleFor(x => x.Amount)
                .NotEmpty().WithMessage("Amount is required.")
                .GreaterThan(0).WithMessage("Amount must be greater than zero.");
            RuleFor(x => x.CVV)
                .NotEmpty().WithMessage("CVV is required.")
                .Matches(@"^\d{3,4}$").WithMessage("CVV must be 3 or 4 digits.");
            RuleFor(x => x)
                .Must(payment =>
                {
                    var now = DateTime.UtcNow;

                    return payment.ExpiryYear > now.Year || (payment.ExpiryYear == now.Year && payment.ExpiryMonth >= now.Month);
                })
                .WithMessage("Card is expired.");
        }
    }
}
