# Email Management Service

A production-ready, generic email management service built with ASP.NET Core 8, Clean Architecture, Mailjet integration, and a database-backed retry queue.

## Features

- **Mailjet integration** — direct REST API calls (no SDK dependency)
- **Dynamic templates** — DB-driven with variable substitution, versioning, and preview
- **DB-backed queue** — persistent email queue with exponential backoff retry (30s → 1m → 2m → failed)
- **Webhook processing** — receives Mailjet delivery/open/click/bounce events with idempotency
- **API-key authentication** — `ems_live_` prefixed keys with hash-based validation
- **Bootstrap mode** — auto-generates a key on first run when `API_KEY_BOOTSTRAP=true`
- **Structured logging** — per-email event and log tables for full audit trail
- **Swagger UI** — available in Development environment

## Architecture

```
src/
├── EmailManagement.Domain          Entities, enums, domain services (RetryPolicy, StatusTransitionValidator)
├── EmailManagement.Application     Ports (interfaces), DTOs, service layer, exceptions
├── EmailManagement.Infrastructure  EF Core (MySQL), repositories, Mailjet provider, webhook mapper, storage
├── EmailManagement.Api             ASP.NET Core controllers, auth handler, middleware, Swagger
└── EmailManagement.Worker          Background service that processes the email queue
```

- **Clean Architecture** with outward dependency direction: Domain → Application → Infrastructure/API/Worker
- **No MediatR** — direct service calls
- **EF Core 8** with `IEntityTypeConfiguration` per entity, `IUnitOfWork` for cross-repository commits

## Prerequisites

- .NET 8 SDK (`dotnet --version` should show 8.x)
- MySQL 8.0 (running on `localhost:3306`)
- A Mailjet account (API key + secret for sending)

## Configuration

All configuration lives in `.env` at the repository root (git-ignored). Copy `.env.example` to get started:

```bash
cp .env.example .env
```

### Required

| Variable | Description |
|---|---|
| `DATABASE_CONNECTION_STRING` | MySQL connection string |
| `MAILJET_API_KEY` | Mailjet API key |
| `MAILJET_SECRET_KEY` | Mailjet secret key |

### Optional

| Variable | Default | Description |
|---|---|---|
| `MAILJET_FROM_EMAIL` | — | Default sender email address |
| `MAILJET_FROM_NAME` | — | Default sender display name |
| `MAILJET_SKIP_STARTUP_VALIDATION` | `false` | Skip Mailjet credential validation on startup |
| `API_KEY_BOOTSTRAP` | `false` | Generate a bootstrap API key on first run (printed once to console) |
| `STORAGE_PATH` | `<app>/storage` | Local file storage path for attachments |
| `RateLimiting__SendPermitLimit` | `100` | Send endpoint rate limit (requests per window) |
| `RateLimiting__SendWindowSeconds` | `60` | Send endpoint rate limit window |
| `Worker__BatchSize` | `10` | Max emails processed per worker loop |
| `Worker__PollingIntervalSeconds` | `5` | Seconds between worker poll cycles |

## Database Setup

The schema is managed by EF Core migrations. To create the database and apply all migrations:

```bash
dotnet ef database update -s src/EmailManagement.Api
```

This creates 10 tables: `apikeys`, `emailtemplates`, `templateversions`, `templatevariables`, `emailproviders`, `emails`, `emailrecipients`, `emailattachments`, `emailevents`, `emaillogs`.

## Running

### API

```bash
dotnet run --project src/EmailManagement.Api
```

- Swagger UI: `http://localhost:5107/swagger` (Development only)
- Bootstrap key printed to console on first run (if `API_KEY_BOOTSTRAP=true`)

### Worker (separate process)

```bash
dotnet run --project src/EmailManagement.Worker
```

The worker polls the database every 5 seconds for queued emails, sends them via the configured provider, and handles retries.

## Authentication

All endpoints except `/api/v1/webhooks/mailjet` require an API key:

```
Authorization: Bearer ems_live_xxxxxxxxxxxxxxxxxxxxxxxx
```

### Key management

```bash
# Create a key
curl -X POST http://localhost:5107/api/v1/api-keys \
  -H "Authorization: Bearer <existing-key>" \
  -H "Content-Type: application/json" \
  -d '{"name":"my-app"}'

# List keys
curl http://localhost:5107/api/v1/api-keys \
  -H "Authorization: Bearer <existing-key>"

# Revoke a key
curl -X DELETE http://localhost:5107/api/v1/api-keys/{id} \
  -H "Authorization: Bearer <existing-key>"
```

## API Endpoints

### Templates

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/templates` | Create a template with variables |
| `GET` | `/api/v1/templates` | List all templates |
| `GET` | `/api/v1/templates/{id}` | Get template details |
| `PUT` | `/api/v1/templates/{id}` | Update template name/status |
| `DELETE` | `/api/v1/templates/{id}` | Delete (only if no emails sent) |
| `POST` | `/api/v1/templates/{id}/versions` | Create a new version |
| `GET` | `/api/v1/templates/{id}/versions` | List versions |
| `POST` | `/api/v1/templates/{id}/preview` | Render preview with variables |

### Providers

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/providers` | Create a provider (set `isDefault: true` for enqueue) |
| `GET` | `/api/v1/providers` | List providers |
| `PUT` | `/api/v1/providers/{id}` | Update provider |
| `DELETE` | `/api/v1/providers/{id}` | Delete provider |
| `POST` | `/api/v1/providers/{id}/test` | Test provider connectivity |

### Emails

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/emails/send` | Enqueue an email (returns `202 Accepted`) |
| `GET` | `/api/v1/emails` | Query emails (filter by status, template, recipient, date) |
| `GET` | `/api/v1/emails/{id}` | Get email details + recipients |
| `GET` | `/api/v1/emails/{id}/events` | Get delivery events |
| `GET` | `/api/v1/emails/{id}/logs` | Get processing logs |

### Webhooks (no auth required by default)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/webhooks/mailjet` | Receive Mailjet event callbacks |

## Email Processing Flow

```
POST /emails/send
  → Validates template (active, variables present)
  → Renders subject + body
  → Persists email, recipients, Queued event, log
  → Returns 202 Accepted

Worker picks up Queued/Retrying emails
  → Claims via atomic UPDATE (prevents double-processing)
  → Sends via Mailjet REST API
  → On success: status → Sent, logs MessageId
  → On transient failure: status → Retrying, schedules next attempt
  → On permanent failure (4xx) or max retries: status → Failed
```

## Webhook Events

Mailjet posts to `/api/v1/webhooks/mailjet` on delivery status changes. Events are mapped:

| Mailjet Event | Internal Event |
|---|---|
| `sent` | Sent |
| `delivered` | Delivered |
| `open` | Opened |
| `click` | Clicked |
| `bounce` | Bounced |
| `blocked` | Blocked |
| `spam` | Spam |
| `unsub` | Unsubscribed |

Events are **idempotent** — duplicate `event_guid` values are ignored. Status transitions are **monotonic** — a delivered email won't be downgraded to opened.

## Testing

```bash
# Run all tests
dotnet test

# Unit tests only
dotnet test tests/EmailManagement.Tests.Unit

# Integration tests only (uses SQLite in-memory, no MySQL required)
dotnet test tests/EmailManagement.Tests.Integration
```

**60 unit tests** covering:
- RetryPolicy exponential backoff
- Email status transition rules (worker + webhook precedence)
- Template variable rendering (HTML encoding, missing vars, whitespace)
- Mailjet webhook mapping (all event types, idempotency, timestamps)
- Webhook authentication (Bearer token, Basic auth, no-auth fallback)
- API key hashing (deterministic, salted, irreversible)
- Mailjet provider (success, permanent/transient failures, HTTP errors, payload structure)

**22 integration tests** covering:
- Email enqueue with full DB persistence (recipients, events, logs)
- Guard clauses (missing template, inactive template, missing variables, no provider)
- Worker processing (transient → retry scheduling, permanent → failed, success → sent)
- Webhook processing (status transitions, idempotency, out-of-order precedence)
- API key round-trip (generate → validate → revoke → rejected)
- Full HTTP flow (auth, template CRUD, provider setup, send, query, webhook)

## Project Structure

```
EmailManagement.sln
├── src/
│   ├── EmailManagement.Domain/
│   │   ├── Entities/           10 entities (ApiKey, EmailTemplate, Email, ...)
│   │   ├── Enums/              7 enums (EmailStatus, EmailEventType, ...)
│   │   └── DomainServices/     RetryPolicy, EmailStatusTransitionValidator
│   ├── EmailManagement.Application/
│   │   ├── Ports/              Repository interfaces, IEmailProvider, IUnitOfWork
│   │   ├── Services/           ApiKeyService, TemplateService, EmailEnqueueService, ...
│   │   ├── DTOs/               Request/Response records
│   │   └── Exceptions/         NotFoundException, ConflictException, ValidationException
│   ├── EmailManagement.Infrastructure/
│   │   ├── Persistence/        ApplicationDbContext, entity configs, migrations, repositories
│   │   ├── Providers/          MailjetEmailProvider, MailjetWebhookMapper, MailjetWebhookAuthenticator
│   │   ├── Security/           ApiKeyHasher (SHA-256 + salted prefix)
│   │   ├── Storage/            LocalFileStorage
│   │   ├── Options/            MailjetOptions, WorkerOptions, RateLimitingOptions
│   │   └── Configuration/      EnvFileLoader (.env ancestor lookup)
│   ├── EmailManagement.Api/
│   │   ├── Controllers/        EmailController, TemplateController, ProviderController, ApiKeyController, WebhookController
│   │   ├── Authentication/     ApiKeyAuthenticationHandler (custom Bearer scheme)
│   │   ├── Middleware/          GlobalExceptionHandler (consistent error envelope)
│   │   └── Bootstrap/          BootstrapApiKeyHostedService
│   └── EmailManagement.Worker/
│       └── EmailProcessingWorker.cs   BackgroundService polling for queued emails
├── tests/
│   ├── EmailManagement.Tests.Unit/         60 tests
│   └── EmailManagement.Tests.Integration/  22 tests (SQLite in-memory)
├── .env                Real configuration (git-ignored)
├── .env.example        Template with placeholders
└── .gitignore
```
