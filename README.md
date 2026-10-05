# Payment Gateway

A .NET implementation of the Checkout.com Payment Gateway challenge.

The service exposes an API that allows merchants to process payments through a simulated acquiring bank and retrieve previously processed payments.

## Architecture

The solution follows a lightweight (thin) interpretation of Onion Architecture, keeping the core payment workflow separated from HTTP, persistence, and external acquiring-bank concerns.
For the scope of this exercise, the architecture is intentionally kept small rather than introducing additional projects or abstraction layers purely to enforce architectural boundaries.
The main responsibilities are separated into:
- API boundary – HTTP endpoints and request validation.
- Application/service layer – payment orchestration and business workflow.
- External integration – acquiring-bank communication through a typed HttpClient.
- Persistence abstraction – storage and retrieval of processed payments.
Dependencies are kept behind interfaces where they represent external/infrastructure concerns, allowing the payment workflow to be tested independently.

The implementation separates the HTTP API, payment orchestration, acquiring-bank integration, and persistence concerns.

```text
                     POST /payments
                           │
                           ▼
                 ┌──────────────────┐
                 │PaymentsController│
                 └────────┬─────────┘
                          │
                          ▼
                 ┌──────────────────┐
                 │ BankConnector    │
                 │ Service          │
                 └───────┬──────────┘
                         │
                 ┌───────┴────────┐
                 ▼                ▼
        ┌────────────────┐   ┌────────────────┐
        │ MounteBank     │   │ Storage        │
        │ Client         │   │ Repository     │
        └───────┬────────┘   └────────────────┘
                │
                ▼
        ┌────────────────┐
        │ Acquiring Bank │
        │ (Mountebank)   │
        └────────────────┘
```

### Responsibilities

- **PaymentsController** – HTTP boundary, request validation and HTTP responses.
- **BankConnectorService** – payment orchestration and mapping between API, domain and bank models.
- **MounteBankClient** – integration with the simulated acquiring bank using `HttpClient`.
- **StorageRepository** – in-memory persistence of processed payments.
- **PaymentRequestValidator** – validates incoming merchant payment requests.

## API

### Process a payment

```http
POST /payments
Content-Type: application/json
```

Example request:

```json
{
  "cardNumber": "1234567890123456",
  "expiryMonth": 12,
  "expiryYear": 2028,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}
```

A valid request is submitted to the acquiring bank.

The resulting payment is stored with either an `Authorized` or `Declined` status.

### Retrieve a payment

```http
GET /payments/{paymentId}
```

Returns the previously processed payment identified by `paymentId`.

Sensitive card information is not returned by the API. Only the last four digits of the card number are exposed.

## Running Locally

### Prerequisites

- .NET 8 SDK
- Docker / Docker Compose

### Start the acquiring-bank simulator

```bash
docker-compose up bank_simulator
```

Then start the Payment Gateway:

```bash
dotnet run --project src/PaymentGateway.Api/PaymentGateway.Api.csproj
```

The acquiring-bank endpoint is configurable and is not hard-coded into the application.

## Docker

A Dockerfile is provided for packaging the Payment Gateway.

The supplied Docker Compose configuration contains both:

- Payment Gateway
- Mountebank acquiring-bank simulator

Build and start the services with:

```bash
docker-compose up --build
```

Within the Docker network the Payment Gateway communicates with Mountebank using the Docker service name rather than `localhost`.

The bank endpoint is supplied through configuration:

```text
BankService__BaseUrl=http://bank_simulator:8080
```

This allows the same application image to be deployed to different environments without recompilation.

## Running Tests

Run the automated test suite with:

```bash
dotnet test
```

The tests covers the scenarios mentioned below:

- successful payment authorization
- declined payments
- invalid payment requests
- acquiring-bank failures/timeouts
- payment persistence
- duplicate storage behaviour
- controller and service behaviour

## Validation

Incoming payment requests are validated before being sent to the acquiring bank.

Validation includes:

- card number format
- card expiry date
- supported currency
- positive payment amount
- CVV format

Invalid requests are rejected before invoking the acquiring bank.

## Security

Payment data is deliberately separated between the external bank request and the internally persisted payment model.

The full card number and CVV are required when communicating with the acquiring bank, but they are **not persisted**.

Stored payment information contains only the last four digits of the card number together with the non-sensitive information required to retrieve the payment.

Sensitive card data is also excluded from application logging.

## Observability

The application uses structured `ILogger` logging around important payment-processing operations and acquiring-bank interactions.

Logs are designed to provide sufficient context to investigate failures while avoiding sensitive payment information such as full card numbers and CVVs.

In a production environment I would additionally expose metrics for:

- payment authorization/decline rates
- acquiring-bank latency
- acquiring-bank error and timeout rates
- API latency and error rates

Distributed tracing could also be introduced to correlate requests across the Payment Gateway and downstream services.

## Error Handling

The implementation distinguishes between different classes of failure.

### Invalid merchant request

Invalid input is rejected at the API boundary and is not submitted to the acquiring bank.

### Payment declined

A decline is a valid business outcome rather than an application failure. The payment is persisted with a `Declined` status.

### Acquiring-bank failure

Network errors and timeouts are treated as downstream failures rather than payment declines.

Retries are deliberately not performed automatically. A timeout does not guarantee that the acquiring bank failed to process the payment, so blindly retrying could result in duplicate payments unless an idempotency mechanism is available.

## Persistence

The challenge uses an in-memory repository.

This keeps persistence infrastructure outside the scope of the exercise while retaining a repository abstraction so that durable storage could be introduced without changing the payment-processing workflow.

`ConcurrentDictionary` is used by the in-memory implementation to provide thread-safe access.

Data is lost when the application is restarted.

## Design Decisions

### Separate API, domain and acquiring-bank models

The acquiring bank has its own request/response contract rather than exposing its representation throughout the application.

This also prevents sensitive bank-request data from becoming part of the persisted payment model.

### Typed HttpClient

The acquiring-bank integration uses a typed `HttpClient`, allowing endpoint configuration and HTTP concerns to remain within the infrastructure boundary.

### Validation at the API boundary

Merchant requests are validated before entering payment processing. Invalid requests therefore do not result in acquiring-bank calls or stored payments.

### No automatic payment retries

Automatic retries were deliberately avoided because payment requests are not inherently safe to repeat.

A production implementation should introduce an idempotency strategy before retrying ambiguous payment operations.

## Production Considerations

The implementation intentionally keeps infrastructure lightweight for the scope of the exercise.

For a production payment gateway I would consider:

- persistent database storage over in-memory repository
- merchant authentication and authorization
- idempotency keys
- reconciliation of ambiguous payment outcomes
- encryption and secrets management
- PCI requirements
- distributed tracing and operational metrics
- health/readiness endpoints
- rate limiting
- acquiring-bank resilience policies
- horizontal scaling
- CI/CD and automated deployment

In particular, an acquiring-bank timeout creates an **ambiguous payment state**: the gateway cannot know whether the bank processed the request before the connection failed.

A production system should persist an internal pending/unknown state and reconcile it with the acquiring bank rather than assuming the payment failed or blindly retrying it.

## Assumptions

- Payment persistence is intentionally in-memory for the scope of this exercise.
- `Authorized` and `Declined` represent acquiring-bank business outcomes.
- Invalid requests do not create payments.
- A declined payment is retained because it represents a valid payment attempt and bank decision.
- The supplied Mountebank service represents the external acquiring bank.