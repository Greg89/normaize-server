# Normaize API Documentation

This document provides detailed information about the Normaize API endpoints, request/response formats, and usage examples.

## Base URL

- **Development**: `http://localhost:5000`
- **Production**: `https://your-railway-app.railway.app`

## Authentication

The API uses Auth0 JWT Bearer token authentication. All `/api/*` endpoints require a valid JWT token.

### Getting a Token

1. Authenticate with your Auth0 application
2. Include the JWT token in the Authorization header:
   ```
   Authorization: Bearer <your-jwt-token>
   ```

### Public Endpoints

- `GET /health` - Health check (no authentication required)
- `GET /health/basic` - Basic health check (no authentication required)
- `GET /health/startup` - Startup health check (no authentication required)

### Protected Endpoints

All `/api/*` endpoints require authentication:
- `GET /api/datasets` - Get all datasets
- `POST /api/datasets/upload` - Upload new dataset
- etc.

## Endpoints

### Health Check

#### GET `/health`

Returns the health status of the API.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "Normaize API"
}
```

#### GET `/health/basic`

Returns basic health status without database connectivity (suitable for CI environments).

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z",
  "service": "Normaize API",
  "version": "1.0.0",
  "environment": "Development"
}
```

#### GET `/health/startup`

Returns startup health check status using ASP.NET Core health checks.

**Response:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0012345",
  "entries": {
    "startup": {
      "data": {},
      "duration": "00:00:00.0012345",
      "status": "Healthy",
      "tags": []
    }
  }
}
```

### DataSets

#### GET `/api/datasets`

Retrieves all datasets.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Sample Dataset",
    "description": "A sample dataset for testing",
    "fileName": "sample.csv",
    "fileSize": 1024,
    "uploadDate": "2024-01-15T10:30:00Z",
    "rowCount": 100,
    "columnCount": 5
  }
]
```

#### GET `/api/datasets/{id}`

Retrieves a specific dataset by ID.

**Parameters:**
- `id` (integer): The dataset ID

**Response:**
```json
{
  "id": 1,
  "name": "Sample Dataset",
  "description": "A sample dataset for testing",
  "fileName": "sample.csv",
  "fileSize": 1024,
  "uploadDate": "2024-01-15T10:30:00Z",
  "rowCount": 100,
  "columnCount": 5
}
```

#### POST `/api/datasets/upload`

Uploads a new dataset file.

**Request:**
- Content-Type: `multipart/form-data`
- Body:
  - `file`: The dataset file (CSV, JSON, Excel)
  - `name`: Dataset name (string)
  - `description`: Dataset description (string, optional)

**Response:**
```json
{
  "success": true,
  "message": "Dataset uploaded successfully",
  "dataSetId": 1
}
```

#### GET `/api/datasets/{id}/preview`

Gets a preview of the dataset data.

**Parameters:**
- `id` (integer): The dataset ID
- `rows` (integer, optional): Number of rows to preview (default: 10)

**Response:**
```json
{
  "data": [
    ["Column1", "Column2", "Column3"],
    ["Value1", "Value2", "Value3"],
    ["Value4", "Value5", "Value6"]
  ],
  "totalRows": 100,
  "previewRows": 10
}
```

#### GET `/api/datasets/{id}/schema`

Gets the schema information for a dataset.

**Parameters:**
- `id` (integer): The dataset ID

**Response:**
```json
{
  "columns": [
    {
      "name": "Column1",
      "type": "string",
      "index": 0
    },
    {
      "name": "Column2",
      "type": "number",
      "index": 1
    }
  ],
  "totalColumns": 2
}
```

#### DELETE `/api/datasets/{id}`

Deletes a dataset.

**Parameters:**
- `id` (integer): The dataset ID

**Response:**
- Status: 204 No Content

## Error Responses

### 400 Bad Request
```json
{
  "message": "No file provided"
}
```

### 404 Not Found
```json
{
  "message": "Dataset not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "Error retrieving datasets"
}
```

## Supported File Formats

- **CSV**: Comma-separated values
- **JSON**: JavaScript Object Notation
- **Excel**: .xlsx and .xls files

## Rate Limiting

Currently, there are no rate limits implemented. Please be respectful of the service.

## CORS

The API supports CORS and allows requests from any origin in development. For production, configure allowed origins appropriately.

## Examples

### Upload a CSV file using curl

```bash
curl -X POST \
  -F "file=@data.csv" \
  -F "name=My Dataset" \
  -F "description=Sample dataset" \
  http://localhost:5000/api/datasets/upload
```

### Get dataset preview using JavaScript

```javascript
fetch('/api/datasets/1/preview?rows=5')
  .then(response => response.json())
  .then(data => console.log(data));
```

## Swagger Documentation

When running in development mode, interactive API documentation is available at `/swagger`. 