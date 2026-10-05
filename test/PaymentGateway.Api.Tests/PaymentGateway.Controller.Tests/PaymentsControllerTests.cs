using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.PaymentGateway.Controller.Tests
{
    public class PaymentsControllerTests
    {
        [Fact]
        public async Task GetPaymentAsync_ReturnsOkWithPayment()
        {
            // arrange
            var paymentId = Guid.NewGuid();
            var expected = new PaymentResponse { PaymentId = paymentId, Amount = 123, Currency = "GBP" };

            var bankServiceMock = new Mock<IBankConnectorService>();
            bankServiceMock.Setup(s => s.GetPaymentDetails(paymentId)).Returns(expected);

            var validatorMock = new Mock<IValidator<PaymentRequest>>();
            var controller = new PaymentsController(bankServiceMock.Object, validatorMock.Object);

            // act
            var actionResult = controller.GetPayment(paymentId);

            // assert
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(expected, ok.Value);
        }

        [Fact]
        public async Task ProcessPaymentAsync_InvalidRequest_ReturnsBadRequest()
        {
            // arrange
            var request = new PaymentRequest();

            var bankServiceMock = new Mock<IBankConnectorService>();

            var validationResult = new ValidationResult(new[] { new ValidationFailure("CardNumber", "required") });
            var validatorMock = new Mock<IValidator<PaymentRequest>>();
            validatorMock.Setup(v => v.Validate(request)).Returns(validationResult);

            var controller = new PaymentsController(bankServiceMock.Object, validatorMock.Object);

            // act
            var actionResult = await controller.ProcessPaymentAsync(request, CancellationToken.None);

            // assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            bankServiceMock.Verify(s => s.InitiatePaymentAsync(It.IsAny<Guid>(), It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ValidRequest_CallsServiceAndReturnsOk()
        {
            // arrange
            var request = new PaymentRequest { Amount = 100, CardNumber = "12345678901234", Currency = "GBP", CVV = "123", ExpiryMonth = 1, ExpiryYear = 2025 };
            var expected = new PaymentResponse { PaymentId = Guid.NewGuid(), Amount = 100, Currency = "GBP" };

            var bankServiceMock = new Mock<IBankConnectorService>();
            bankServiceMock.Setup(s => s.InitiatePaymentAsync(It.IsAny<Guid>(), request, CancellationToken.None)).ReturnsAsync(expected);

            var validatorMock = new Mock<IValidator<PaymentRequest>>();
            validatorMock.Setup(v => v.Validate(request)).Returns(new ValidationResult());

            var controller = new PaymentsController(bankServiceMock.Object, validatorMock.Object);

            // act
            var actionResult = await controller.ProcessPaymentAsync(request, CancellationToken.None);

            // assert
            var created = Assert.IsType<CreatedResult>(actionResult.Result);
            Assert.Equal(expected, created.Value);
            bankServiceMock.Verify(s => s.InitiatePaymentAsync(It.IsAny<Guid>(), request, CancellationToken.None), Times.Once);
        }
    }
}
