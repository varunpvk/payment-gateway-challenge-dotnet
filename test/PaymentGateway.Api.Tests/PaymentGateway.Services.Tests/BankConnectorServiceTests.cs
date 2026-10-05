using Moq;

using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.PaymentGateway.Services.Tests
{
    public class BankConnectorServiceTests
    {
        [Fact]
        public async Task Given_ValidPaymentRequest_And_Bank_AuthorizesPayment_When_PaymentInitiated_Then_PaymentResponse_Generated_And_Details_Saved_To_Storage()
        {
            // arrange
            Mock<IMounteBankClient> mounteBankClientMock = new Mock<IMounteBankClient>();
            Mock<IStorageRepository> storageRepositoryMock = new Mock<IStorageRepository>();
            IBankConnectorService bankConnectorService = new BankConnectorService(storageRepositoryMock.Object, mounteBankClientMock.Object);

            BankPaymentResponse bankPaymentResponse = new BankPaymentResponse
            {
                 Authorized = true,
                 Code = Guid.NewGuid().ToString(),
            };

            mounteBankClientMock.Setup(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), new CancellationToken()))
                .ReturnsAsync(bankPaymentResponse);
            storageRepositoryMock.Setup(o => o.SavePaymentDetails(It.IsAny<Guid>(), It.IsAny<PaymentDetails>())).Returns(true);


            // act
            var paymentResult = await bankConnectorService.InitiatePaymentAsync(Guid.NewGuid(), new PaymentRequest
            {
                Amount = 100,
                CardNumber = "1234567890123456",
                Currency = "GBP",
                CVV = "123",
                ExpiryMonth = 01,
                ExpiryYear = 2029
            }, CancellationToken.None);

            // assert
            Assert.NotNull(paymentResult);
            Assert.Equal("Authorized", paymentResult.Status);
            Assert.Equal(100, paymentResult.Amount);
            Assert.Equal("GBP", paymentResult.Currency);
            Assert.Equal("3456", paymentResult.CardNumberLastFourDigits);

            // verify storage save was called once with a PaymentDetails matching the request
            storageRepositoryMock.Verify(o => o.SavePaymentDetails(
                It.IsAny<Guid>(),
                It.Is<PaymentDetails>(pd => pd.Amount == 100 && pd.Currency == "GBP")
            ), Times.Once);

            // verify bank client was called once
            mounteBankClientMock.Verify(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), new CancellationToken()), Times.Once);
            
        }

        [Fact]
        public async Task Given_ValidPaymentRequest_And_Bank_DeclinedPayment_When_PaymentInitiated_Then_PaymentResponse_Generated_Details_Saved_To_Storage()
        {
            // arrange
            Mock<IMounteBankClient> mounteBankClientMock = new Mock<IMounteBankClient>();
            Mock<IStorageRepository> storageRepositoryMock = new Mock<IStorageRepository>();
            IBankConnectorService bankConnectorService = new BankConnectorService(storageRepositoryMock.Object, mounteBankClientMock.Object);

            BankPaymentResponse bankPaymentResponse = new BankPaymentResponse
            {
                Authorized = false,
                Code = Guid.NewGuid().ToString(),
            };

            mounteBankClientMock.Setup(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), new CancellationToken()))
                .ReturnsAsync(bankPaymentResponse);
            storageRepositoryMock.Setup(o => o.SavePaymentDetails(It.IsAny<Guid>(), It.IsAny<PaymentDetails>())).Returns(true);

            // act
            var paymentResult = await bankConnectorService.InitiatePaymentAsync(Guid.NewGuid(), new PaymentRequest
            {
                Amount = 100,
                CardNumber = "1234567890123456",
                Currency = "GBP",
                CVV = "123",
                ExpiryMonth = 01,
                ExpiryYear = 2029
            }, CancellationToken.None);

            // assert
            Assert.NotNull(paymentResult);
            Assert.Equal("Declined", paymentResult.Status);
            // verify storage save was called once with a PaymentDetails matching the request
            storageRepositoryMock.Verify(o => o.SavePaymentDetails(
                It.IsAny<Guid>(),
                It.Is<PaymentDetails>(pd => pd.Amount == 100 && pd.Currency == "GBP")
            ), Times.Once);

            // verify bank client was called once
            mounteBankClientMock.Verify(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), new CancellationToken()), Times.Once);

        }


        [Fact]
        public async Task Given_ValidPaymentRequest_And_Bank_DidntRespond_DueTo_Transient_NetworkIssue_And_TimedOut_Then_TaskCanceledException_Is_Thrown()
        {
            // arrange
            Mock<IMounteBankClient> mounteBankClientMock = new Mock<IMounteBankClient>();
            Mock<IStorageRepository> storageRepositoryMock = new Mock<IStorageRepository>();
            IBankConnectorService bankConnectorService = new BankConnectorService(storageRepositoryMock.Object, mounteBankClientMock.Object);

            // simulate timeout / transient network failure from bank client
            mounteBankClientMock.Setup(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), new CancellationToken()))
                .ThrowsAsync(new TaskCanceledException());

            // act + assert - service should propagate the timeout
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await bankConnectorService.InitiatePaymentAsync(Guid.NewGuid(), new PaymentRequest
                {
                    Amount = 100,
                    CardNumber = "1234567890123456",
                    Currency = "GBP",
                    CVV = "123",
                    ExpiryMonth = 01,
                    ExpiryYear = 2029
                }, CancellationToken.None)
            );

            // ensure storage was not called
            storageRepositoryMock.Verify(o => o.SavePaymentDetails(It.IsAny<Guid>(), It.IsAny<PaymentDetails>()), Times.Never);
            mounteBankClientMock.Verify(o => o.ProcessPaymentAsync(It.IsAny<BankPaymentRequest>(), CancellationToken.None), Times.Once);
        }
    }
}
