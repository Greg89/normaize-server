# Railway Environment Variables Configuration

## Overview
This document lists all environment variables that need to be configured in your Railway deployment for the Normaize API server.

## Critical Variables (Required)

### 1. Database Connection
Since you're switching from MySQL to PostgreSQL:

```bash
# Railway will provide the PostgreSQL connection string automatically
# But you need to map it to your app's expected variable name
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

**Alternative manual format:**
```bash
ConnectionStrings__DefaultConnection=Host=containers-us-west-xxx.railway.app;Database=railway;Username=postgres;Password=xxx;Port=xxxx;SSL Mode=Require;Trust Server Certificate=true
```

### 2. Auth0 Configuration
Your existing Auth0 credentials (already configured):

```bash
Auth0__Domain=your-existing-tenant.auth0.com
Auth0__Audience=normaize-api
```

### 3. CORS Origins
Update with your actual Railway client domain:

```bash
AllowedOrigins__0=https://your-client-app.up.railway.app
# Or if using custom domain:
AllowedOrigins__0=https://app.yourdomain.com
```

### 4. Environment Setting
**For Development/Staging:**
```bash
ASPNETCORE_ENVIRONMENT=Development
# Or
ASPNETCORE_ENVIRONMENT=Staging
```

**For Production:**
```bash
ASPNETCORE_ENVIRONMENT=Production
```

## Optional Variables

### Logging Level (Recommended)
```bash
Serilog__MinimumLevel__Default=Information
Serilog__MinimumLevel__Override__Microsoft=Warning
```

### Features
```bash
Features__EnableSwagger=false
Features__EnableDetailedLogging=false
```

### File Storage (if using local storage)
```bash
Storage__Provider=Local
Storage__BasePath=/app/data
```

## Railway-Specific Notes

### Automatic Variables
Railway automatically provides:
- `PORT` - Your app listens on this port (handled automatically)
- `RAILWAY_ENVIRONMENT` - The environment name
- `DATABASE_URL` (from PostgreSQL service) - Map this to ConnectionStrings__DefaultConnection

### PostgreSQL Service Setup
1. **Add PostgreSQL service** in your Railway project
2. **Link it to your API service**
3. **Use reference variable**: `${{Postgres.DATABASE_URL}}`
4. **The format will be**: `postgresql://user:password@host:port/database`
5. **Convert to .NET format**: Railway will need the connection string in the format:
   ```
   Host=xxx;Database=xxx;Username=xxx;Password=xxx;Port=xxx;SSL Mode=Require;Trust Server Certificate=true
   ```

### Important: Connection String Format
Railway's PostgreSQL provides `DATABASE_URL` in this format:
```
postgresql://username:password@host:port/database
```

But .NET expects this format:
```
Host=host;Database=database;Username=username;Password=password;Port=port;SSL Mode=Require;Trust Server Certificate=true
```

**Solution**: Use Railway's connection string template or set it manually.

## Step-by-Step Railway Configuration

### 1. Delete MySQL Service
- Go to your Railway project
- Delete the MySQL database service
- Remove any MySQL-related environment variables

### 2. Add PostgreSQL Service
- Click "New" → "Database" → "Add PostgreSQL"
- Railway will automatically provision it
- Note the connection details

### 3. Configure API Service Variables
Set these in your Railway API service settings → Variables:

**Required:**
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};Port=${{Postgres.PGPORT}};SSL Mode=Require;Trust Server Certificate=true
Auth0__Domain=<your-auth0-domain>
Auth0__Audience=normaize-api
AllowedOrigins__0=https://<your-client-domain>
```

**Optional:**
```
Features__EnableSwagger=false
Serilog__MinimumLevel__Default=Information
```

### 4. Deploy
- Push your code changes (updated Dockerfile, Program.cs)
- Railway will automatically rebuild and deploy
- Check deployment logs for migration success
- Test health endpoint: `https://your-api-domain/health/ready`

## Verification Checklist

After deployment, verify:

- [ ] Health check returns 200 OK: `https://your-api.railway.app/health/ready`
- [ ] Database migrations applied successfully (check logs)
- [ ] Auth0 authentication working (test with Bearer token)
- [ ] CORS allows your client domain
- [ ] API endpoints respond correctly

## Troubleshooting

### Database Connection Fails
- Check connection string format is correct
- Verify PostgreSQL service is running
- Ensure SSL Mode is set to "Require"
- Check Trust Server Certificate is true

### Migrations Fail
- Check database user has CREATE permissions
- Verify connection string is correct
- Look at startup logs for specific error

### CORS Errors
- Verify AllowedOrigins__0 matches your client domain exactly
- Check protocol (http vs https)
- Ensure no trailing slashes

### Auth0 Fails
- Verify Auth0__Domain and Auth0__Audience are correct
- Check Auth0 API configuration in dashboard
- Test token manually with curl

## Migration Command (If Needed)
If automatic migrations fail, you can run manually:

```bash
# Railway CLI
railway run dotnet ef database update --project src/Normaize.DataNormalization.API
```

But the app is configured to run migrations automatically on startup.
