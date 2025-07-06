# Environment Variables

This document describes all environment variables required for the Normaize API.

## Database Configuration

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `MYSQLHOST` | MySQL server hostname | Yes | - |
| `MYSQLDATABASE` | MySQL database name | Yes | - |
| `MYSQLUSER` | MySQL username | Yes | - |
| `MYSQLPASSWORD` | MySQL password | Yes | - |
| `MYSQLPORT` | MySQL port | Yes | 3306 |

## Auth0 Configuration

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `AUTH0_ISSUER` | Auth0 issuer URL | Yes | - |
| `AUTH0_AUDIENCE` | Auth0 audience | Yes | - |

## Seq Logging Configuration

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `SEQ_URL` | Seq server URL (e.g., https://your-seq.railway.app) | No* | - |
| `SEQ_API_KEY` | Seq API key for authentication | No* | - |

*Seq logging is only enabled in non-Development environments when `SEQ_URL` is provided.

## Application Configuration

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Application environment (Development, Beta, Production) | No | Development |
| `PORT` | Port to listen on | No | 5000 |

**Environment Behavior:**
- **Development**: Console logging only
- **Beta/Production**: Console + Seq logging (if SEQ_URL is provided)

## Local Development

For local development, create a `.env` file in the root directory:

```env
# Database Configuration
MYSQLHOST=localhost
MYSQLDATABASE=normaize
MYSQLUSER=root
MYSQLPASSWORD=password
MYSQLPORT=3306

# Auth0 Configuration
AUTH0_ISSUER=https://your-domain.auth0.com/
AUTH0_AUDIENCE=https://your-api.com

# Seq Logging Configuration (optional for local development)
SEQ_URL=https://your-seq-instance.railway.app
SEQ_API_KEY=your-seq-api-key

# Application Configuration
ASPNETCORE_ENVIRONMENT=Development
PORT=5000
```

## Railway Deployment

When deploying to Railway, add these environment variables in your Railway project settings:

1. **Database variables** - Railway will automatically provide these for MySQL services
2. **Auth0 variables** - Configure these for your Auth0 application
3. **Seq variables** - Point to your Railway-hosted Seq instance
4. **Application variables** - Set environment and port as needed

### Environment-Specific Configuration

**Beta Environment:**
```env
ASPNETCORE_ENVIRONMENT=Beta
SEQ_URL=https://your-seq-instance.railway.app
SEQ_API_KEY=your-seq-api-key
```

**Production Environment:**
```env
ASPNETCORE_ENVIRONMENT=Production
SEQ_URL=https://your-seq-instance.railway.app
SEQ_API_KEY=your-seq-api-key
```

### Seq Setup on Railway

1. Create a new Seq service in your Railway project
2. Get the Seq URL from the service details
3. Set `SEQ_URL` to your Seq service URL
4. Optionally set `SEQ_API_KEY` if you configure API key authentication in Seq 