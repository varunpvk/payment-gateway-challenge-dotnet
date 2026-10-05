using FluentValidation;

using PaymentGateway.Api.Infrastructure;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Validations;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var bankBaseUrl = builder.Configuration["BankService:BaseUrl"];
if (string.IsNullOrWhiteSpace(bankBaseUrl))
{
    // fall back to mock bank client settings (useful for local development)
    var host = builder.Configuration["MockBankClient:Host"] ?? "localhost";
    var port = builder.Configuration.GetValue<int?>("MockBankClient:Port") ?? 8080;
    bankBaseUrl = $"http://{host}:{port}";
}

builder.Services.AddHttpClient<IMounteBankClient, MounteBankClient>(client =>
{
    client.BaseAddress = new Uri(bankBaseUrl ?? throw new InvalidOperationException("Bank service base URL not configured"));
});

builder.Services.AddSingleton<IStorageRepository, InMemoryStorageRepository>();
builder.Services.AddScoped<IBankConnectorService, BankConnectorService>();
builder.Services.AddTransient<IValidator<PaymentRequest>, PaymentRequestValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
