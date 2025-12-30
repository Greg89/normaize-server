# Authentication Setup Guide

## Overview
The Normaize API now uses Auth0 JWT Bearer token authentication to secure all endpoints except health checks.

## What's Been Implemented

### 1. API Security Changes
- ✅ **All controllers protected** with `[Authorize]` attributes:
  - `DataSetsController` - Dataset operations
  - `AnalysisController` - Data analysis operations  
  - `DataNormalizationController` - Job processing operations
  - `StatisticsController` - Statistical calculations
- ✅ **Health endpoints remain open** for monitoring
- ✅ **JWT Bearer authentication** configured in Program.cs
- ✅ **Swagger UI** updated with Bearer token authentication

### 2. Auth0 Configuration
- ✅ **JWT validation** with Auth0 domain and audience
- ✅ **Token validation parameters** (issuer, audience, lifetime)
- ✅ **Authentication events** with logging
- ✅ **Claims extraction** support for user identification

### 3. Configuration Files Updated
- ✅ **appsettings.json** - Base Auth0 configuration structure
- ✅ **appsettings.Development.json** - Development Auth0 settings

## Configuration Required

### 1. Auth0 Setup
You need to configure these settings in your Auth0 dashboard and environment:

**appsettings.Development.json:**
```json
{
  "Auth0": {
    "Domain": "your-dev-tenant.us.auth0.com",
    "Audience": "normaize-api"
  }
}
```

**Production Environment Variables:**
```bash
Auth0__Domain=your-production-tenant.auth0.com
Auth0__Audience=normaize-api
```

### 2. Client Configuration
Update your client's `.env` file to match:

```bash
VITE_AUTH0_DOMAIN=your-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=your-spa-client-id
VITE_AUTH0_AUDIENCE=normaize-api
VITE_API_URL=http://localhost:5001
```

### 3. Auth0 Dashboard Setup

1. **Create an API** in Auth0:
   - Name: `Normaize API`
   - Identifier: `normaize-api` (this is your audience)
   - Signing Algorithm: `RS256`

2. **Create a Single Page Application**:
   - Name: `Normaize Client`
   - Application Type: `Single Page Application`
   - Allowed Callback URLs: `http://localhost:5173/callback, https://your-client-domain.com/callback`
   - Allowed Logout URLs: `http://localhost:5173, https://your-client-domain.com`
   - Allowed Web Origins: `http://localhost:5173, https://your-client-domain.com`

3. **Configure Scopes** (if needed):
   - `read:datasets`
   - `write:datasets`
   - `delete:datasets`
   - `read:analysis`
   - `write:analysis`

## Testing Authentication

### 1. Without Token (Should Fail)
```bash
curl -X GET "http://localhost:5001/api/datasets" \
  -H "accept: application/json"
# Expected: 401 Unauthorized
```

### 2. With Valid Token (Should Succeed)
```bash
curl -X GET "http://localhost:5001/api/datasets" \
  -H "accept: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
# Expected: 200 OK with datasets list
```

### 3. Health Check (Always Available)
```bash
curl -X GET "http://localhost:5001/health"
# Expected: 200 OK with health status
```

## Current Status

### ✅ Completed
- API endpoints secured with `[Authorize]` attributes
- Auth0 JWT Bearer authentication configured
- Configuration structure in place
- Swagger UI supports Bearer token input
- Warning messages for missing configuration

### ⏳ Pending
- Auth0 tenant setup and configuration
- Client-server integration testing
- Production environment variable configuration

## Security Features

### 1. JWT Validation
- **Issuer validation** against Auth0 domain
- **Audience validation** for API authorization
- **Lifetime validation** with 5-minute clock skew tolerance
- **Signature validation** using Auth0's public keys

### 2. Logging & Monitoring
- **Authentication failures** logged as warnings
- **Successful token validation** logged with user ID
- **Missing configuration** warnings during startup

### 3. Claims Support
The API supports these JWT claims for user identification:
- `sub` (Auth0 standard)
- `NameIdentifier` (ASP.NET Core standard)
- `nameid` (alternative)
- `user_id` (custom)

## Next Steps

1. **Set up Auth0 tenant** with API and SPA application
2. **Configure development environment** with Auth0 settings  
3. **Test client authentication flow** against secured API
4. **Set up production environment variables**
5. **Test end-to-end authentication** in deployed environment

## Troubleshooting

### Common Issues

1. **"Auth0 configuration missing" warning**
   - Solution: Set `Auth0:Domain` and `Auth0:Audience` in configuration

2. **401 Unauthorized responses**
   - Check JWT token is included in Authorization header
   - Verify token hasn't expired
   - Confirm audience matches API configuration

3. **Token validation fails**
   - Verify Auth0 domain is correct
   - Check clock skew if timing issues
   - Ensure Auth0 API uses RS256 algorithm

4. **CORS issues with authentication**
   - Update CORS policy for production domains
   - Ensure preflight requests handle auth headers