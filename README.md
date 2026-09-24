# dmp.job.invoiceworker

Background worker of the DMP marketplace that tracks crypto invoices created in [Bitcart](https://bitcart.ai).
It polls the `InvoiceWorkerTask` table in PostgreSQL, opens a Bitcart invoice WebSocket for every pending invoice
and applies each status update to the order: it updates `OrderHeader.Status`, records a `Payment` row, notifies
`dmp.api.web` (which pushes the update to the client) and, when the payment completes, queues the
"purchase complete" email in the `Mail` table. When an invoice reaches a final state (complete, paid over,
expired, invalid), the invoice task is replaced by a `TransactionWorkerTask` that `dmp.job.trxworker` picks up.

## Tech stack

- .NET 10 (`net10.0`), C# with nullable reference types, Generic Host (`Microsoft.Extensions.Hosting` 10.0)
- Entity Framework Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0 (PostgreSQL)
- `System.Net.WebSockets.ClientWebSocket` for the Bitcart invoice WebSocket
- `IHttpClientFactory` (`Microsoft.Extensions.Http` 10.0) for calls to `dmp.api.web`
- Central Package Management (`Directory.Packages.props`)

## Project structure

```
dmp.job.invoiceworker.slnx
├── dmp.job.invoiceworker/        Job.InvoiceWorker – host executable
│   ├── Program.cs                DI/configuration setup
│   ├── InvoiceWorkerService.cs   polling loop + one WebSocket listener per invoice
│   └── appsettings*.json         configuration per environment (Local, Development, Production)
├── DMP.BL/                       business logic (copy of the shared DMP library, trimmed to what this worker uses)
│   ├── Services/PaymentService.cs  Bitcart status -> order/payment status mapping, API notification, email
│   └── Models/                   Bitcart message/enum models, options classes, API request model
├── DMP.DataAccess/               EF Core DbContext and entities (copy of the shared DMP data model)
├── Directory.Build.props         common MSBuild settings (net10.0, nullable, analyzers)
├── Directory.Packages.props      NuGet package versions
└── Dockerfile
```

## Configuration

Settings come from `appsettings.json`, `appsettings.{DOTNET_ENVIRONMENT}.json` and environment variables
(`Section__Key`, which take precedence). The committed files contain only placeholders; see `.env.example`.

| Setting (env var)                                  | Description                                                                | Example |
| -------------------------------------------------- | -------------------------------------------------------------------------- | ------- |
| `DOTNET_ENVIRONMENT`                               | Environment name (`Local`, `Development`, `Production`)                    | `Production` |
| `ConnectionStrings__DmpConnection`                 | PostgreSQL connection string of the DMP database                           | `Host=postgres-dmp;Port=5432;Database=dmarketplace;Username=admin;Password=change-me` |
| `DMP.API.WEB__Url`                                 | Base URL of `dmp.api.web`                                                  | `http://dmp-api-web:80` |
| `DMP.API.WEB__User` / `DMP.API.WEB__Password`      | Basic-auth credentials of the worker account in `dmp.api.web`              | `invoiceworker` / `change-me` |
| `BitcartOptions__WebSocketUri`                     | Bitcart invoice WebSocket base URI (the invoice id is appended)            | `ws://bitcart-backend:8000/ws/invoices` |
| `Minio__s3PublicEndpoint`                          | Public S3/MinIO endpoint, used to build the logo URL in emails             | `http://localhost:9000` |
| `DmpHosts__Client`                                 | Public URL of the client web app, used for the order link in emails        | `http://localhost:3000` |
| `DmpHosts__Seller`                                 | Public URL of the seller web app                                           | `http://localhost:8080` |

The environment variable names containing dots (`DMP.API.WEB__*`) cannot be exported from a POSIX shell, but work
from Docker/Compose `environment:` sections and `.env` files.

## Getting started

Prerequisites: .NET 10 SDK, a PostgreSQL database with the DMP schema, a running Bitcart
instance and `dmp.api.web`.

Run locally (uses `appsettings.Local.json` via `Properties/launchSettings.json`):

```bash
dotnet run --project dmp.job.invoiceworker
```

Run with Docker:

```bash
docker build -t dmp-job-invoiceworker .
docker run --env-file .env dmp-job-invoiceworker
```

The container runs as the non-root `app` user and exposes no ports.

## Commands

| Command                                      | Purpose |
| -------------------------------------------- | ------- |
| `dotnet build dmp.job.invoiceworker.slnx -c Release` | Build all projects |
| `dotnet format dmp.job.invoiceworker.slnx`   | Apply code style from `.editorconfig` |
| `dotnet format dmp.job.invoiceworker.slnx --verify-no-changes` | Check formatting |

## Related repositories

- [dmp](https://github.com/denis-susha/dmp) – umbrella repository of the DMP system
- [dmp.api.web](https://github.com/denis-susha/dmp.api.web) – web API; creates invoice tasks and receives payment updates from this worker
- [dmp.job.trxworker](https://github.com/denis-susha/dmp.job.trxworker) – processes the `TransactionWorkerTask` rows this worker creates
- [dmp.job.server](https://github.com/denis-susha/dmp.job.server) – sends the queued emails
- [dmp.docker](https://github.com/denis-susha/dmp.docker) – Docker Compose setup including Bitcart
