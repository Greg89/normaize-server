# Seq Connection Troubleshooting Guide

## Quick Diagnostic Checklist

### 1. Check Seq Server URL Configuration

**Issue**: `appsettings.Beta.json` doesn't have `Seq:ServerUrl` configured.

**Fix**: Add Seq configuration to `appsettings.Beta.json`:

```json
{
  "Seq": {
    "ServerUrl": "https://your-seq-server.com"
  }
}
```

**Or use environment variable**:
```bash
SEQ_URL=https://your-seq-server.com
```

### 2. Verify Environment Variable is Set

Check if `SEQ_URL` environment variable is set in your beta environment:

```bash
# In your deployment environment
echo $SEQ_URL  # Linux/Mac
echo %SEQ_URL%  # Windows
```

The code checks `configuration["Seq:ServerUrl"]` which can come from:
- `appsettings.json` → `Seq:ServerUrl`
- `appsettings.Beta.json` → `Seq:ServerUrl`
- Environment variable → `SEQ_URL` (if mapped)

### 3. Check Application Startup Logs

Look for these log messages during startup:

**✅ Seq Connected:**
```
[Serilog] Writing to Seq at https://your-seq-server.com
```

**❌ Seq Not Configured:**
- No Seq-related messages (means `Seq:ServerUrl` is empty/null)
- Console and file logging will still work

### 4. Test Seq Connection

**From your beta application server**, test connectivity:

```bash
# Test HTTP connectivity
curl -v https://your-seq-server.com/api/events/raw

# Or test with PowerShell (Windows)
Invoke-WebRequest -Uri "https://your-seq-server.com/api/events/raw" -Method POST
```

**Expected Response:**
- `200 OK` or `201 Created` = Connection works
- `401 Unauthorized` = Connection works but needs API key
- `Connection refused` / `Timeout` = Network/firewall issue

### 5. Check Seq API Key (if required)

If your Seq instance requires authentication, add the API key:

**Environment Variable:**
```bash
SEQ_API_KEY=your-api-key-here
```

**Or in appsettings:**
```json
{
  "Seq": {
    "ServerUrl": "https://your-seq-server.com",
    "ApiKey": "your-api-key-here"
  }
}
```

**Note**: Current implementation doesn't support API key yet - we'll need to add it.

### 6. Verify Serilog Package is Installed

Check that `Serilog.Sinks.Seq` is installed:

```bash
dotnet list package | grep Seq
```

Should show:
```
Serilog.Sinks.Seq 8.0.0
```

### 7. Check Log Level Configuration

Seq sink is configured with `restrictedToMinimumLevel: LogEventLevel.Information`.

Make sure your logs are at Information level or higher:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"  // ✅ Good
      // "Default": "Debug"     // ⚠️ Debug logs won't go to Seq
    }
  }
}
```

### 8. Check Network/Firewall

**Common Issues:**
- Firewall blocking outbound HTTPS (port 443)
- VPN required to access Seq server
- Seq server behind internal network only
- DNS resolution failing

**Test:**
```bash
# Test DNS resolution
nslookup your-seq-server.com

# Test port connectivity
telnet your-seq-server.com 443
```

### 9. Check Application Logs for Errors

Look for Serilog/Seq connection errors in:
- Console output
- File logs (`logs/normaize-*.log`)
- Application event logs

**Common Error Messages:**
- `Unable to connect to Seq server` = Network issue
- `401 Unauthorized` = API key required
- `404 Not Found` = Wrong URL
- `Timeout` = Firewall/network blocking

### 10. Verify Configuration is Loaded

Add temporary logging to verify Seq URL is being read:

```csharp
// In SerilogConfiguration.cs, add:
var seqServerUrl = configuration["Seq:ServerUrl"];
Console.WriteLine($"🔍 Seq Server URL: {(string.IsNullOrWhiteSpace(seqServerUrl) ? "NOT CONFIGURED" : seqServerUrl)}");
```

## Quick Fix: Add Seq Configuration to Beta

Add this to `appsettings.Beta.json`:

```json
{
  "Seq": {
    "ServerUrl": "https://your-actual-seq-server-url.com"
  }
}
```

Or set environment variable in your deployment:
```bash
SEQ_URL=https://your-actual-seq-server-url.com
```

## Next Steps After Fixing

1. **Restart the application** to pick up new configuration
2. **Make a test API call** to generate logs
3. **Check Seq UI** - logs should appear within seconds
4. **Verify trace correlation** - logs should have `TraceId` and `SpanId` properties

