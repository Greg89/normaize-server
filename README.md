# Normaize - Data Toolbox

A comprehensive web application for normalizing, comparing, analyzing, and visualizing data from various sources.

## Features

- **Data Loading**: Support for multiple data sources (CSV, JSON, Excel, APIs)
- **Data Normalization**: Tools for standardizing and cleaning data
- **Data Comparison**: Compare datasets and identify differences
- **Data Analysis**: Statistical analysis and insights
- **Data Visualization**: Interactive charts and graphs
- **Modern UI**: Clean, responsive interface built with React

## Tech Stack

### Backend
- .NET 8 Web API
- Entity Framework Core
- SQL Server (for Railway deployment)
- CORS enabled for frontend communication

### Frontend
- React 18 with TypeScript
- Vite for fast development
- Tailwind CSS for styling
- Chart.js for data visualization
- Axios for API communication

### Deployment
- Docker containers
- Railway hosting platform
- Environment-based configuration

## Project Structure

```
normaize/
├── backend/                 # .NET Web API
│   ├── Normaize.API/       # Main API project
│   ├── Normaize.Core/      # Business logic
│   ├── Normaize.Data/      # Data access layer
│   └── Normaize.Tests/     # Unit tests
├── frontend/               # React application
│   ├── src/
│   ├── public/
│   └── package.json
├── docker-compose.yml      # Local development
├── .github/               # GitHub Actions
└── README.md
```

## Getting Started

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Docker (optional, for containerized development)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd normaize
   ```

2. **Backend Setup**
   ```bash
   cd backend
   dotnet restore
   dotnet run --project Normaize.API
   ```

3. **Frontend Setup**
   ```bash
   cd frontend
   npm install
   npm run dev
   ```

4. **Access the application**
   - Frontend: http://localhost:5173
   - Backend API: http://localhost:5000

### Docker Development

```bash
docker-compose up --build
```

## Deployment to Railway

1. **Connect your repository to Railway**
2. **Set environment variables** in Railway dashboard
3. **Deploy** - Railway will automatically build and deploy both frontend and backend

### Environment Variables

#### Backend
- `ConnectionStrings__DefaultConnection`: Database connection string
- `JWT__Secret`: JWT secret key
- `CORS__AllowedOrigins`: Allowed frontend origins

#### Frontend
- `VITE_API_URL`: Backend API URL

## API Documentation

The API documentation is available at `/swagger` when running the backend in development mode.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## License

MIT License - see LICENSE file for details 