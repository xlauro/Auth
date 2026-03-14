# Auth Service

Modern .NET 10 authentication API built with clean architecture boundaries (API ➜ Application ➜ Domain ➜ Infrastructure). It exposes user registration and login backed by EF Core + SQLite, FluentValidation rules, JWT issuance, and a growing automated test suite.

## Why It Stands Out

- **Layered architecture:** Each project owns a single responsibility, making business rules portable and testable.
- **Robust validation & error handling:** FluentValidation in the application layer plus domain-specific exceptions and a global middleware guarantee consistent HTTP responses.
- **Secure defaults:** Passwords are hashed (SHA-256 placeholder) before persistence and JWT access tokens are issued with configurable issuer/audience/secret.
- **EF Core ready:** Infrastructure project ships with DbContext + repository abstraction; API bootstraps migrations automatically via `EnsureCreated()` and is prepared for future `dotnet ef` migrations.
- **Test-driven confidence:** Unit tests cover register/login use-cases and controller behavior, while WebApplicationFactory-based integration tests exercise the full pipeline (DI, middleware, JWT, EF Core).

## Solution Topology

```
Auth.sln
├── Auth.Api            # ASP.NET Core API (controllers, middleware, DI)
├── Auth.Application    # Use cases, abstractions, validation orchestration
├── Auth.Domain         # Entities, validation rules, domain exceptions
├── Auth.Infrastructure # EF Core, repositories, JWT services, data access
└── Auth.Tests          # xUnit suite (unit + integration via TestServer)
```

## Getting Started

### Prerequisites
- .NET SDK 10.0.201 or later
- SQLite (bundled with .NET provider, no separate install required)

### Configuration
Set the connection string and JWT secrets in `Auth.Api/appsettings.Development.json` (dev) or `appsettings.json` (prod-style). Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=auth.dev.db"
  },
  "Jwt": {
    "Issuer": "AuthApi.Dev",
    "Audience": "AuthApiClients.Dev",
    "Secret": "REPLACE_ME_WITH_A_SECURE_DEV_SECRET",
    "ExpiresMinutes": 120
  }
}
```

### Run the API

```bash
cd /home/xlauro/RiderProjects/Auth
DOTNET_ENVIRONMENT=Development dotnet run --project Auth.Api/Auth.Api.csproj
```

The app hosts Swagger (Scalar) UI at `/scalar/v1` in Development. The pipeline automatically ensures the SQLite database exists on startup.

## API Surface

| Endpoint            | Method | Body                               | Response                   |
|---------------------|--------|------------------------------------|----------------------------|
| `/auth/register`    | POST   | `{ "email": "user@x.com", "password": "P@ss" }` | `200 OK` + user id/email   |
| `/auth/login`       | POST   | `{ "email": "user@x.com", "password": "P@ss" }` | `200 OK` + JWT access token |

Errors bubble through the exception middleware:
- `409 Conflict` when a user already exists
- `404 Not Found` for missing users on login
- `400 Bad Request` for validation problems

## Testing & Quality Gates

Run the whole suite (unit + integration) with:

```bash
cd /home/xlauro/RiderProjects/Auth
DOTNET_ENVIRONMENT=Development dotnet test
```

Highlights:
- `UseCasesTests` validate registration/login flows with EF InMemory.
- `AuthControllerTests` simulate controller behavior via fake repos/token generator.
- `AuthIntegrationTests` spin up the real API via `WebApplicationFactory<Program>` against in-memory SQLite to ensure middleware, DI, and JWT issuance work end-to-end.

## Extending the Solution

1. **Production-grade password hashing:** swap the SHA-256 placeholder with PBKDF2, BCrypt, or Argon2.
2. **Migrations:** once `dotnet-ef` tooling is available, run `dotnet ef migrations add InitialCreate -p Auth.Infrastructure -s Auth.Api` and `dotnet ef database update` to move off `EnsureCreated()`.
3. **Authorization policies:** add role/claim-based policies and decorate controllers with `[Authorize]` as endpoints grow.
4. **Observability:** plug in Serilog + OpenTelemetry exporters for structured logs and traces.

## Talking Points for Recruiters

- Demonstrates ability to design clean boundaries and enforce them via project references and abstractions.
- Shows comfort with modern ASP.NET Core patterns (minimal `Program.cs`, DI modules, middleware).
- Proves testing mindset through both unit and integration coverage, including realistic in-memory SQLite hosting.
- Ready for cloud/containers due to zero external dependencies beyond SQLite; secrets isolated to configuration.

Feel free to reach out if you'd like a guided tour or want to see roadmap items in action!

