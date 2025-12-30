# Production Deployment Guide

## Overview
This guide covers deploying both the Normaize API (server) and Client (frontend) to production environments, with specific instructions for Railway hosting.

## Pre-Deployment Checklist

### ✅ **API Server Ready**
- [x] Authentication configured with Auth0
- [x] CORS configured for production domains
- [x] Environment-specific configuration files
- [x] Health check endpoints configured
- [x] Production logging configuration
- [x] Database connection string prepared
- [x] Dockerfile optimized for production

### ✅ **Client App Ready**
- [x] Auth0 configuration for production
- [x] API URL configured for production endpoint
- [x] Build optimization enabled
- [x] Error tracking configured (optional)
- [x] Analytics configured (optional)

## Deployment Configuration

### 1. API Server Environment Variables

**Required Variables:**
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:$PORT

# Database (PostgreSQL)
ConnectionStrings__DefaultConnection=Host=your-postgres-host;Database=normaize_prod;Username=your-db-user;Password=your-db-password;Port=5432;SSL Mode=Require;Trust Server Certificate=true

# Authentication (Auth0)
Auth0__Domain=your-production-tenant.auth0.com
Auth0__Audience=normaize-api

# CORS
AllowedOrigins__0=https://your-client-domain.com
```

**Optional Variables:**
```bash
# Features
Features__EnableSwagger=false
Features__EnableDetailedLogging=false

# Logging
Serilog__MinimumLevel__Default=Information

# Performance
Performance__MaxConcurrentJobs=5
```

### 2. Client Environment Variables

**Required Variables:**
```bash
# App
VITE_NODE_ENV=production

# Auth0
VITE_AUTH0_DOMAIN=your-production-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=your-spa-client-id
VITE_AUTH0_AUDIENCE=normaize-api

# API
VITE_API_URL=https://your-api-domain.com
```

**Optional Variables:**
```bash
# Features
VITE_ENABLE_ANALYTICS=true
VITE_ENABLE_ERROR_TRACKING=true
VITE_LOG_LEVEL=warn

# Build
VITE_SOURCE_MAP=false
```

## Railway Deployment Steps

### Step 1: Prepare Auth0 Configuration

1. **Create Production Auth0 Tenant** (or use existing)
2. **Create API Application**:
   - Identifier: `normaize-api`
   - Algorithm: RS256
3. **Create SPA Application**:
   - Type: Single Page Application
   - Allowed Callback URLs: `https://your-client-domain.com/callback`
   - Allowed Logout URLs: `https://your-client-domain.com`
   - Allowed Web Origins: `https://your-client-domain.com`

### Step 2: Deploy API Server

1. **Create Railway Project** for API
2. **Connect GitHub Repository** (`normaize-server`)
3. **Set Environment Variables**:
   ```bash
   ASPNETCORE_ENVIRONMENT=Production
   ConnectionStrings__DefaultConnection=[Your PostgreSQL connection string]
   Auth0__Domain=[Your Auth0 domain]
   Auth0__Audience=normaize-api
   AllowedOrigins__0=[Your client domain]
   ```
4. **Add PostgreSQL Database** (Railway add-on)
5. **Deploy** - Railway will:
   - Build using Dockerfile
   - Apply database migrations automatically
   - Start health checks on `/health/ready`

### Step 3: Deploy Client App

1. **Create Railway Project** for Client
2. **Connect GitHub Repository** (`normaize-client`)
3. **Set Environment Variables**:
   ```bash
   VITE_NODE_ENV=production
   VITE_AUTH0_DOMAIN=[Your Auth0 domain]
   VITE_AUTH0_CLIENT_ID=[Your SPA client ID]
   VITE_AUTH0_AUDIENCE=normaize-api
   VITE_API_URL=[Your deployed API URL]
   ```
4. **Deploy** - Railway will:
   - Build using Vite
   - Serve static files
   - Health check on `/`

### Step 4: Configure Custom Domains (Optional)

1. **API Domain**: `api.yourdomain.com`
2. **Client Domain**: `app.yourdomain.com` or `yourdomain.com`
3. **Update Environment Variables** with actual domains
4. **Update Auth0 Configuration** with production domains

## Database Setup

### PostgreSQL Configuration

**Connection String Format:**
```
Host=hostname;Database=dbname;Username=user;Password=password;Port=5432;SSL Mode=Require;Trust Server Certificate=true
```

**Database Migrations:**
- Migrations are applied automatically on startup
- No manual intervention required
- Check logs for migration status

## Security Configuration

### 1. CORS Policy
- ✅ Specific origins only (no wildcards)
- ✅ Credentials allowed for Auth0
- ✅ Secure headers included

### 2. Authentication
- ✅ JWT Bearer tokens required
- ✅ Auth0 signature validation
- ✅ Audience and issuer validation
- ✅ Token lifetime validation

### 3. Logging Security
- ✅ Sensitive data logging disabled
- ✅ Minimum log levels for production
- ✅ Structured logging for monitoring

## Monitoring & Health Checks

### API Health Endpoints
- `/health` - Detailed health information (JSON)
- `/health/ready` - Readiness probe (for load balancers)
- `/health/live` - Liveness probe (for orchestrators)

### What's Monitored
- ✅ Database connectivity
- ✅ Configuration validation
- ✅ Storage availability
- ✅ Memory usage
- ✅ Application startup status

## Performance Optimization

### API Server
- ✅ Response compression enabled
- ✅ Static file caching
- ✅ Database connection pooling
- ✅ Async operations throughout
- ✅ Memory-efficient data processing

### Client App
- ✅ Code splitting
- ✅ Asset optimization
- ✅ Tree shaking enabled
- ✅ Minification in production
- ✅ Gzip compression

## Troubleshooting

### Common Issues

1. **"Auth0 configuration missing" warning**
   - Check `Auth0__Domain` and `Auth0__Audience` environment variables
   - Verify Railway environment variable format (double underscore)

2. **CORS errors**
   - Verify `AllowedOrigins__0` matches client domain exactly
   - Check protocol (http vs https)
   - Ensure trailing slashes match

3. **Database connection fails**
   - Verify PostgreSQL connection string format
   - Check SSL requirements for cloud databases
   - Ensure firewall allows Railway IPs

4. **Authentication fails**
   - Verify Auth0 tenant domain is correct
   - Check SPA application configuration
   - Ensure API audience matches exactly

5. **Health checks fail**
   - Check `/health/ready` endpoint responds
   - Verify database migrations completed
   - Check application logs for startup errors

### Deployment Logs

**Check Railway logs for:**
- Build process completion
- Migration application status
- Environment variable loading
- CORS policy selection
- Authentication configuration status

## Post-Deployment Verification

### 1. API Verification
```bash
# Health check
curl https://your-api-domain.com/health

# Protected endpoint (should return 401)
curl https://your-api-domain.com/api/datasets

# With auth token (should return 200)
curl -H "Authorization: Bearer YOUR_TOKEN" https://your-api-domain.com/api/datasets
```

### 2. Client Verification
- ✅ Login flow works with Auth0
- ✅ API calls succeed with authentication
- ✅ Dataset operations function correctly
- ✅ File uploads work
- ✅ Error handling displays properly

## Scaling Considerations

### Horizontal Scaling
- API server is stateless and horizontally scalable
- Client app serves static files (CDN-friendly)
- Database connection pooling supports multiple instances

### Vertical Scaling
- Monitor memory usage for large file processing
- CPU usage for statistical calculations
- Database performance for large datasets

## Security Best Practices

### Production Security
- ✅ HTTPS enforced (Railway provides automatically)
- ✅ Secrets stored as environment variables
- ✅ No sensitive data in logs
- ✅ CORS restricted to specific origins
- ✅ JWT tokens with expiration
- ✅ Database connections encrypted

### Ongoing Maintenance
- Regular Auth0 token rotation
- Database backup strategy
- Log monitoring and alerting
- Security update schedule