using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Models.DTOs;
using PaymentGateway.Api.Models.Enums;

namespace PaymentGateway.Api.Tests.PaymentGateway.Infrastructure.Tests
{
    public class InMemoryStorageRepositoryTests
    {
        [Fact]
        public void Given_Valid_PaymentDetails_When_SavePaymentDetails_Attempted_PaymentDetails_Are_SavedSuccessully_And_Returns_True()
        {
            // arrange
            IStorageRepository repository = new InMemoryStorageRepository();
            var paymentId = Guid.NewGuid();
            PaymentDetails paymentDetails = new PaymentDetails(PaymentStatus.Authorized, "1234", 12, 2028, "GBP", 1200);

            // act
            var result = repository.SavePaymentDetails(paymentId, paymentDetails);
            var savedDetails = repository.GetPaymentInfoById(paymentId);

            // assert
            Assert.True(result);
            Assert.Equal(paymentDetails, repository.GetPaymentInfoById(paymentId));
        }

        [Fact]
        public void Given_Null_PaymentDetails_When_SavePaymentDetails_Attempted_PaymentDetails_NotSaved_And_Returns_False()
        {
            // arrange
            IStorageRepository repository = new InMemoryStorageRepository();
            var paymentId = Guid.NewGuid();

            // act
            var result = repository.SavePaymentDetails(paymentId, null);

            // assert
            Assert.False(result);
            Assert.Null(repository.GetPaymentInfoById(paymentId));
        }

        [Fact]
        public void Given_PaymentId_That_Exists_When_SavePaymentDetails_Attempted_PaymentDetails_NotOverridden_And_Returns_False()
        {
            // arrange
            IStorageRepository repository = new InMemoryStorageRepository();
            var paymentId = Guid.NewGuid();
            PaymentDetails paymentDetails = new PaymentDetails(PaymentStatus.Authorized, "1234", 12, 2028, "GBP", 1200);
            var result = repository.SavePaymentDetails(paymentId, paymentDetails);
            PaymentDetails paymentDetails_new = new PaymentDetails(PaymentStatus.Authorized, "1234", 12, 2028, "GBP", 1200);

            // act
            var paymentResult = repository.SavePaymentDetails(paymentId, paymentDetails_new);

            // assert
            Assert.False(paymentResult);
            Assert.Equal(paymentDetails, repository.GetPaymentInfoById(paymentId));
        }
    }
}
