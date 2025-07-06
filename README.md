# Normaize - Data Toolbox

[![CI/CD Pipeline](https://github.com/Greg89/normaize-server/actions/workflows/ci.yml/badge.svg)](https://github.com/Greg89/normaize-server/actions/workflows/ci.yml)
[![Pull Request Check](https://github.com/Greg89/normaize-server/actions/workflows/pr-check.yml/badge.svg)](https://github.com/Greg89/normaize-server/actions/workflows/pr-check.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive web application for normalizing, comparing, analyzing, and visualizing data from various sources.

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
- **Entity Framework Core** with MySQL
- **AutoMapper** for object mapping
- **Swagger/OpenAPI** for API documentation
- **CORS** enabled for frontend communication
- **Docker** for containerization
- **Serilog** with Seq for structured logging
- **Auth0** for authentication

### Database
- **MySQL** (via Railway)
- **Entity Framework Core** migrations

### Deployment
- **Docker** containers
- **Railway** hosting platform
- **Environment-based** configuration

## Project Structure

```
normaize-server/
├── Normaize.API/           # Main API project
│   ├── Controllers/        # API endpoints
│   ├── Services/          # Business logic services
│   ├── Middleware/        # Custom middleware
│   └── Program.cs         # Application entry point
├── Normaize.Core/         # Business logic & models
│   ├── DTOs/             # Data transfer objects
│   ├── Interfaces/       # Service interfaces
│   └── Models/           # Domain models
├── Normaize.Data/         # Data access layer
│   ├── Repositories/     # Data repositories
│   └── NormaizeContext.cs # EF Core context
├── Normaize.Tests/        # Unit tests
├── Dockerfile            # Docker configuration
└── README.md
```

## Getting Started

### Prerequisites
- .NET 9 SDK
- MySQL database (or use Railway's MySQL plugin)
- Docker (optional, for containerized development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd normaize-server
   ```

2. **Set up environment variables**
   Create a `.env` file in the root directory:
   ```env
   # Database Configuration
   MYSQLHOST=localhost
   MYSQLDATABASE=normaize
   MYSQLUSER=your_username
   MYSQLPASSWORD=your_password
   MYSQLPORT=3306
   
   # Auth0 Configuration
   AUTH0_ISSUER=https://your-domain.auth0.com/
   AUTH0_AUDIENCE=https://your-api.com
   
   # Seq Logging (optional for local development)
   SEQ_URL=https://your-seq-instance.railway.app
   SEQ_API_KEY=your-seq-api-key
   ```

3. **Run the application**
   ```bash
   cd Normaize.API
   dotnet restore
   dotnet run
   ```

4. **Access the application**
   - API: http://localhost:5000
   - Swagger Documentation: http://localhost:5000/swagger
   - Health Check: http://localhost:5000/health

### Docker Development

```bash
docker build -t normaize-api .
docker run -p 5000:8080 normaize-api
```

## API Endpoints

### Health Check
- `GET /health` - Service health status

### DataSets
- `GET /api/datasets` - Get all datasets
- `GET /api/datasets/{id}` - Get specific dataset
- `POST /api/datasets/upload` - Upload new dataset
- `GET /api/datasets/{id}/preview` - Preview dataset data
- `GET /api/datasets/{id}/schema` - Get dataset schema
- `DELETE /api/datasets/{id}` - Delete dataset

For detailed API documentation, see [API.md](docs/API.md).

📚 **Full Documentation**: Check the [docs/](docs/) folder for complete project documentation.

## Deployment to Railway

### Prerequisites
1. Railway account
2. GitHub repository connected to Railway

### Steps
1. **Connect your repository** to Railway
2. **Add MySQL plugin** in Railway dashboard
3. **Set environment variables** in Railway:
   - `MYSQLHOST` (from Railway MySQL plugin)
   - `MYSQLDATABASE` (from Railway MySQL plugin)
   - `MYSQLUSER` (from Railway MySQL plugin)
   - `MYSQLPASSWORD` (from Railway MySQL plugin)
   - `MYSQLPORT` (from Railway MySQL plugin)
4. **Deploy** - Railway will automatically build and deploy using the Dockerfile

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `MYSQLHOST` | MySQL host address | Yes |
| `MYSQLDATABASE` | Database name | Yes |
| `MYSQLUSER` | Database username | Yes |
| `MYSQLPASSWORD` | Database password | Yes |
| `MYSQLPORT` | Database port | Yes |
| `AUTH0_ISSUER` | Auth0 issuer URL | Yes |
| `AUTH0_AUDIENCE` | Auth0 audience | Yes |
| `SEQ_URL` | Seq logging server URL | No* |
| `SEQ_API_KEY` | Seq API key | No* |
| `PORT` | Application port (set by Railway) | No |

*Seq logging is only enabled in non-Development environments when `SEQ_URL` is provided.

## Development

### Running Tests
```bash
dotnet test
```

### Database Migrations
```bash
cd Normaize.API
dotnet ef migrations add MigrationName
dotnet ef database update
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

- **Issues**: Report bugs and feature requests on GitHub
- **Documentation**: Check [API.md](docs/API.md) for detailed API documentation
- **Health Check**: Use `/health` endpoint to verify service status 