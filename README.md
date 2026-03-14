# Auth Service

_A portfolio-ready authentication slice that demonstrates clean architecture, EF Core data access, FluentValidation, and JWT issuance on .NET 10._

## TL;DR for Busy Engineers

- **Separation of concerns**: API, Application, Domain, Infrastructure, and Tests are isolated projects with explicit references, making the codebase easy to reason about.
- **End-to-end happy path**: `/auth/register` ➜ validate ➜ hash ➜ persist ➜ `/auth/login` ➜ issue JWT signed with configurable issuer/audience/secret.
- **Guard rails included**: domain exceptions, validation errors, and unexpected failures flow through a middleware that converts them to HTTP-friendly payloads.
- **Confidence via tests**: unit, controller, and TestServer-driven integration cases run in <5s and cover the real pipeline (DI, EF Core, JWT).

## Architecture Cheatsheet
```
Client
  │
  ▼
Auth.Api (Controllers, DI, middleware, JWT auth)
  │          ▲ (DTOs, exception middleware)
  ▼          │
Auth.Application (Register/Login use cases, validation, abstractions)
  │          ▲
  ▼          │
Auth.Domain (Entities, FluentValidation rules, custom exceptions)
  │          ▲
  ▼          │
Auth.Infrastructure (EF Core DbContext + repositories, JwtTokenGenerator)
```

Key decisions:
- **Repository abstraction (`IUserRepository`)** keeps the use cases persistence-agnostic.
- **`UserValidation` + `ApplicationValidationException`** centralize input and entity-level guarantees.
- **`EnsureCreated()` bootstrap** is used during development; swap to migrations (`dotnet ef ...`) when the toolchain is available.
- **JWT signing** relies on `Microsoft.IdentityModel.Tokens` with secrets sourced from configuration, so rotating keys or moving to KeyVault is trivial.

## Project Map

| Project             | Responsibility | Interesting bits |
|---------------------|----------------|------------------|
| `Auth.Api`          | HTTP surface, DI modules, exception middleware, JWT setup | `Extensions/` hides the hosting plumbing so `Program.cs` stays minimal. |
| `Auth.Application`  | Register/Login use cases, validation orchestration        | Uses FluentValidation + SHA256 hashing (ready to swap for PBKDF2). |
| `Auth.Domain`       | Entities, value constraints, domain exceptions            | Lightweight and persistence-agnostic. |
| `Auth.Infrastructure` | EF Core DbContext, user repository, JWT token generator  | Targets SQLite for portability. |
| `Auth.Tests`        | xUnit suite: unit, controller, and TestServer integration  | In-memory EF + SQLite makes the suite deterministic and quick. |

## Local Dev Workflow

```bash
# 1. Restore & build
cd /home/xlauro/RiderProjects/Auth
DOTNET_ENVIRONMENT=Development dotnet build

# 2. Update settings if needed
code Auth.Api/appsettings.Development.json

# 3. Run the API (Swagger UI available at /scalar/v1)
DOTNET_ENVIRONMENT=Development dotnet run --project Auth.Api/Auth.Api.csproj
```

Configuration knobs live under `ConnectionStrings:DefaultConnection` and `Jwt:*`. Secrets are plain text for dev convenience—swap to user secrets, Azure App Config, or AWS Parameter Store when hosting for real.

## API Contract

| Endpoint | Description | Sample Request | Sample Response |
|----------|-------------|----------------|-----------------|
| `POST /auth/register` | Creates a new user if the email is unique. | `{ "email": "alice@example.com", "password": "P@ssw0rd!" }` | `{ "id": "GUID", "email": "alice@example.com" }` |
| `POST /auth/login` | Exchanges valid credentials for a JWT. | `{ "email": "alice@example.com", "password": "P@ssw0rd!" }` | `{ "token": "eyJhbGc..." }` |

Failure modes delivered by the exception middleware:
- `400 Bad Request` with a validation error array.
- `404 Not Found` when login targets a non-existent user.
- `409 Conflict` when attempting to register an existing email.
- `500 Internal Server Error` with a sanitized payload for everything else.

## Quality Gates

| Test Layer | Tech | Scope |
|------------|------|-------|
| Unit (`UseCasesTests`) | xUnit + EF InMemory | Register/Login behavior, validation, hashing |
| Controller (`AuthControllerTests`) | xUnit + fake repo/token | HTTP semantics and exception translation |
| Integration (`AuthIntegrationTests`) | WebApplicationFactory + SQLite in-memory | Real DI container, middleware, JWT, EF Core |

Run everything:
```bash
cd /home/xlauro/RiderProjects/Auth
DOTNET_ENVIRONMENT=Development dotnet test
```

## Future Enhancements (Intentionally Left TODO)

1. **Upgrade hashing** to PBKDF2/BCrypt + salt + pepper service.
2. **Introduce migrations** once `dotnet-ef` is installed; hook into CI/CD pipelines.
3. **Authorization policies** for role/claims-based protection of future endpoints.
4. **Observability hooks** (Serilog + OpenTelemetry) for traceability in distributed systems.
5. **Secrets management** via environment variables or dedicated secret stores.

## Conversation Starters

- Clean architecture discipline enforced via project references and abstractions.
- Middleware-first error handling that keeps controllers lean.
- Test strategy that mirrors how production requests travel through the stack.
- Easy on-ramp for cloud deployment (single SQLite file today, switch to Postgres/SQL Server tomorrow by editing `AppDbContext`).

> _Want to see it live or discuss trade-offs? Ping me and we can pair-program through a feature or migration strategy._
