# Normaize - Data Toolbox

[![CI/CD Pipeline](https://github.com/Greg89/normaize-server/actions/workflows/ci.yml/badge.svg)](https://github.com/Greg89/normaize-server/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=Greg89_normaize-server&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Greg89_normaize-server)

A comprehensive web application for normalizing, comparing, analyzing, and visualizing data from various sources.

This repository is the backend API (ASP.NET Core) for the Normaize client. The API uses PostgreSQL via Entity Framework Core and exposes dataset upload/preview, normalization jobs, and analysis endpoints.

## Features

- **Data Loading**: Support for multiple data sources (CSV, JSON, Excel)
- **Data Normalization**: Tools for standardizing and cleaning data
- **Data Comparison**: Compare datasets and identify differences
- **Data Analysis**: Statistical analysis and insights
- **Data Visualization**: Interactive charts and graphs
- **Modern API**: Clean, RESTful API built with .NET 9
- **Structured Logging**: Comprehensive logging with Seq integration for production monitoring
- **Authentication**: Auth0 integration for secure access

## Tech Stack

### Backend
- **.NET 9** Web API
- **Entity Framework Core** with PostgreSQL
- **CQRS (MediatR)** for request/handler routing
- **Swagger/OpenAPI** for API documentation
- **CORS** enabled for frontend communication
- **Docker** for containerization
- **Serilog** with Seq for structured logging
- **Auth0** for authentication

### Database
- **PostgreSQL** (via Railway or local)
- **Entity Framework Core** migrations

### Deployment
- **Docker** containers
- **Railway** hosting platform
- **Environment-based** configuration

## Project Structure

```
normaize-server/
├── src/
│   ├── Normaize.DataNormalization.API/            # ASP.NET Core Web API (controllers, Program.cs)
│   ├── Normaize.DataNormalization.Application/    # CQRS handlers, DTOs, interfaces
│   ├── Normaize.DataNormalization.Domain/         # Domain entities and value objects
│   └── Normaize.DataNormalization.Infrastructure/ # EF Core, repositories, services
├── tests/                                          # Unit/integration tests
├── docker-compose.yml                               # Local Docker orchestration
├── Dockerfile                                       # API container image
└── README.md
```

## Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL database (or use Railway's PostgreSQL plugin)
- Docker (optional, for containerized development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd normaize-server
   ```

2. **Set up configuration / environment variables**

   The API reads configuration from standard .NET configuration sources (appsettings + environment variables). For the database, you can use either:

   - `ConnectionStrings:DefaultConnection` (recommended locally), or
   - `DATABASE_URL` (Railway-style URL, used as a fallback)

   Example:
   ```env
   # Database
   ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=normaize;Username=postgres;Password=postgres
   # or
   DATABASE_URL=postgresql://user:pass@host:5432/db

   # Auth0 (optional in local dev; API will run unprotected if missing)
   Auth0__Domain=your-tenant.us.auth0.com
   Auth0__Audience=your-api-identifier

   # CORS
   AllowedOrigins__0=http://localhost:5173

   # Storage (local default)
   Storage__Provider=local
   Storage__BasePath=uploads
   ```

3. **Run the application**
   ```bash
   dotnet run --project src/Normaize.DataNormalization.API
   ```

4. **Access the application**
   - API: http://localhost:5001
   - Swagger UI (development / enabled): http://localhost:5001/
   - Health Check: http://localhost:5001/health

   Health endpoints:
   - `GET /health`
   - `GET /health/ready`
   - `GET /health/live`

### Docker Development

Preferred (uses `docker-compose.yml`):

```bash
docker compose up -d --build
```

Or build/run the API container directly:

```bash
docker build -t normaize-api .
docker run -p 5001:8080 normaize-api
```

## API Endpoints

### Health Check
- `GET /health` - Service health status

### DataSets
- `GET /api/datasets` - Get all datasets
- `GET /api/datasets?includeDeleted=true` - Include soft-deleted datasets
- `GET /api/datasets/{id}` - Get specific dataset
- `POST /api/datasets/upload` - Upload new dataset
- `GET /api/datasets/{id}/preview?rows=10` - Preview dataset data (default 10 rows, max 100)
- `GET /api/datasets/{id}/columns` - Get dataset columns
- `DELETE /api/datasets/{id}` - Delete dataset

For detailed API documentation, see [API.md](docs/API.md).

📚 **Full Documentation**: Check the [docs/](docs/) folder for complete project documentation.

## Deployment to Railway

### Prerequisites
1. Railway account
2. GitHub repository connected to Railway

### Steps
1. **Connect your repository** to Railway
2. **Add PostgreSQL plugin** in Railway dashboard
3. **Set environment variables** in Railway (typical):
   - `DATABASE_URL` (from Railway PostgreSQL)
   - `Auth0__Domain` / `Auth0__Audience` (if enabling auth)
   - `AllowedOrigins__0` (your client URL)
4. **Deploy** - Railway will automatically build and deploy using the Dockerfile

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `DATABASE_URL` | Railway Postgres URL (fallback if no connection string) | Yes (Railway) |
| `ConnectionStrings__DefaultConnection` | Standard Npgsql connection string | Yes (local) |
| `Auth0__Domain` | Auth0 domain (without `https://`) | No (recommended) |
| `Auth0__Audience` | Auth0 audience / API identifier | No (recommended) |
| `AllowedOrigins__0` | Allowed CORS origin for the client | Yes (production) |
| `Storage__Provider` | `local` or `s3` | No |
| `Storage__BasePath` | Local upload base directory | No (local) |
| `PORT` | Application port (set by Railway) | No |

*Seq logging is only enabled in non-Development environments when `SEQ_URL` is provided.

## Development

### Running Tests
```bash
dotnet test
```

### Database Migrations
```bash
dotnet ef migrations add MigrationName --project src/Normaize.DataNormalization.Infrastructure --startup-project src/Normaize.DataNormalization.API
dotnet ef database update --project src/Normaize.DataNormalization.Infrastructure --startup-project src/Normaize.DataNormalization.API
```

### Code Style
- Follow Microsoft C# coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and under 50 lines when possible

## Contributing

Please read [CONTRIBUTING.md](docs/CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: Report bugs and feature requests on GitHub please
- **Documentation**: Check [API.md](docs/API.md) for detailed API documentation
- **Health Check**: Use `/health` endpoint to verify service status 