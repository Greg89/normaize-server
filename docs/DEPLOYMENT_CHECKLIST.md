# Railway Deployment Checklist

## Pre-Deployment Changes Made ✅

### API Server Updates
- ✅ **Dockerfile** - Updated to build new DDD API (not legacy API)
- ✅ **Program.cs** - Handles Railway's PORT environment variable
- ✅ **Health checks** - Configured for `/health/ready` endpoint
- ✅ **railway.json** - Health check path updated

### Configuration Files
- ✅ **appsettings.Production.json** - Production-optimized settings
- ✅ **CORS** - Environment-aware configuration
- ✅ **Auth0** - JWT authentication configured
- ✅ **Swagger** - Disabled in production by default

## Railway Deployment Steps

### 1. Database Migration (PostgreSQL)
**In Railway Dashboard:**
1. ❌ **Delete MySQL service**
2. ✅ **Add PostgreSQL service**: New → Database → Add PostgreSQL
3. ✅ **Link to API service**: Connect PostgreSQL to your API

### 2. API Environment Variables
**In Railway API Service → Variables:**

**For DEV/Staging Environment:**
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Development
# Or for staging:
# ASPNETCORE_ENVIRONMENT=Staging

# Database (use Railway's PostgreSQL references)
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};Port=${{Postgres.PGPORT}};SSL Mode=Require;Trust Server Certificate=true

# Auth0 (your existing configuration)
Auth0__Domain=<your-auth0-domain>.auth0.com
Auth0__Audience=normaize-api

# CORS (update with your actual client domain)
AllowedOrigins__0=https://<your-client-domain>.up.railway.app

# Enable Swagger in dev (optional)
Features__EnableSwagger=true
```

**For PRODUCTION Environment:**
```bash
# Environment
ASPNETCORE_ENVIRONMENT=Production

# Database (use Railway's PostgreSQL references)
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};Port=${{Postgres.PGPORT}};SSL Mode=Require;Trust Server Certificate=true

# Auth0 (your existing configuration)
Auth0__Domain=<your-auth0-domain>.auth0.com
Auth0__Audience=normaize-api

# CORS (update with your actual client domain)
AllowedOrigins__0=https://<your-production-domain>.com

# Disable Swagger in production
Features__EnableSwagger=false
```

### 3. Client Environment Variables
**In Railway Client Service → Variables:**

```bash
# Production
VITE_NODE_ENV=production

# Auth0 (your existing configuration)
VITE_AUTH0_DOMAIN=<your-auth0-domain>.auth0.com
VITE_AUTH0_CLIENT_ID=<your-spa-client-id>
VITE_AUTH0_AUDIENCE=normaize-api

# API URL (update after API deploys)
VITE_API_URL=https://<your-api-domain>.up.railway.app
```

### 4. Deploy
1. **Commit and push** code changes to GitHub
2. Railway will **auto-deploy** both services
3. **Check logs** for:
   - ✅ "Database migrations applied successfully"
   - ✅ "Auth0 JWT authentication configured"
   - ✅ "Using CORS policy: AllowSpecificOrigins"
   - ✅ "Application started"

### 5. Verify Deployment
```bash
# Health check (should return 200 OK)
curl https://your-api-domain.up.railway.app/health/ready

# Protected endpoint (should return 401 without token)
curl https://your-api-domain.up.railway.app/api/datasets
```

## Important Notes

### Database Connection String Format
Railway provides PostgreSQL variables as `${{Postgres.PGHOST}}`, etc.
The connection string must be in .NET format (not PostgreSQL URI format).

### Auth0 Configuration
- Your existing Auth0 tenant will work
- Update callback URLs in Auth0 if using custom domains
- Ensure API audience matches exactly: `normaize-api`

### CORS Configuration
- Must match client domain **exactly** (including protocol)
- No trailing slashes
- Update `AllowedOrigins__0` with your deployed client URL

### Automatic Migrations
- Migrations run automatically on startup
- Check logs for "✅ Database migrations applied successfully"
- If fails, app will continue in development (logs warning)

## Troubleshooting

### Build Fails
- Check Dockerfile builds correct project: `Normaize.DataNormalization.API`
- Verify all .csproj files are copied correctly
- Check logs for missing dependencies

### Database Connection Fails
- Verify connection string format (Host=xxx;Database=xxx;...)
- Check PostgreSQL service is running and linked
- Ensure SSL Mode=Require and Trust Server Certificate=true

### Health Check Fails
- Wait 30 seconds for startup period
- Check `/health/ready` endpoint manually
- Review application logs for errors

### CORS Errors in Client
- Verify AllowedOrigins__0 matches client domain exactly
- Check browser console for exact origin being blocked
- Update CORS configuration and redeploy

## Post-Deployment

### Update Auth0 (if needed)
If using custom domains, update in Auth0 dashboard:
- **Allowed Callback URLs**: Add new domain
- **Allowed Logout URLs**: Add new domain
- **Allowed Web Origins**: Add new domain

### Monitor Logs
Watch Railway logs for:
- Database query performance
- Authentication failures
- CORS rejections
- Application errors

### Test Features
- [ ] Login/logout with Auth0
- [ ] Upload dataset
- [ ] View datasets
- [ ] Run analysis
- [ ] Job processing

## Files Changed
- ✅ `Dockerfile` - Updated for new DDD API
- ✅ `src/Normaize.DataNormalization.API/Program.cs` - Railway PORT handling
- ✅ `railway.json` - Health check path
- ✅ `railway.toml` (client) - Build command

## Ready to Deploy!
All code changes are complete. Just need to:
1. Delete MySQL, add PostgreSQL
2. Set environment variables
3. Push to GitHub
4. Verify deployment
