using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Tests.PaymentGateway.Infrastructure.Tests;

public class MounteBankClientTests
{
    private class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

        public FakeHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request, cancellationToken));
        }
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsBankResponse_WhenSuccessful()
    {
        // arrange
        HttpRequestMessage? captured = null;
        var responseObj = new BankPaymentResponse { Authorized = true, Code = "auth-123" };
        var handler = new FakeHandler((req, ct) =>
        {
            captured = req;
            var json = JsonSerializer.Serialize(responseObj);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new MounteBankClient(client);

        var bankRequest = new BankPaymentRequest { CardNumber = "4111111111111111", Amount = "1.00", Currency = "GBP", CVV = "123", ExpiryDate = "012025" };

        // act
        var result = await sut.ProcessPaymentAsync(bankRequest, CancellationToken.None);

        // assert
        Assert.NotNull(result);
        Assert.True(result.Authorized);
        Assert.Equal("auth-123", result.Code);
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal(new Uri(client.BaseAddress, "/payments"), captured.RequestUri);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Throws_HttpRequestException_OnNonSuccessStatus()
    {
        // arrange
        var handler = new FakeHandler((req, ct) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new MounteBankClient(client);

        var bankRequest = new BankPaymentRequest { CardNumber = "4111", Amount = "1.00", Currency = "GBP", CVV = "123", ExpiryDate = "012025" };

        // act/assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await sut.ProcessPaymentAsync(bankRequest, CancellationToken.None));
    }

    [Fact]
    public async Task ProcessPaymentAsync_Throws_InvalidOperationException_WhenResponseContentIsNull()
    {
        // arrange - respond with JSON null
        var handler = new FakeHandler((req, ct) =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var sut = new MounteBankClient(client);

        var bankRequest = new BankPaymentRequest { CardNumber = "4111", Amount = "1.00", Currency = "GBP", CVV = "123", ExpiryDate = "012025" };

        // act/assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.ProcessPaymentAsync(bankRequest, CancellationToken.None));
    }
}
