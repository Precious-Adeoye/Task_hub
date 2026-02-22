# Maintenance Scenario: Security Hardening

## Scenario

Before deploying TaskHub to a shared staging environment, a security review identifies several hardening opportunities.

## 1. Current Security Posture

| Control | Status | Notes |
|---------|--------|-------|
| Password hashing (BCrypt) | Implemented | Default work factor |
| Cookie security (HttpOnly, Secure, SameSite) | Implemented | SameSite=Strict |
| Account lockout | Implemented | 5 attempts / 15 min |
| Rate limiting (auth) | Implemented | IP-based via AspNetCoreRateLimit |
| Input validation | Implemented | FluentValidation on all DTOs |
| RBAC authorization | Implemented | Member / OrgAdmin |
| Audit logging | Implemented | All mutations + auth events |
| HTTPS redirection | Implemented | UseHttpsRedirection() |
| ProblemDetails errors | Implemented | No stack traces leaked |
| User enumeration prevention | Implemented | Generic registration errors |
| CORS restriction | Implemented | localhost:3000 only |

## 2. Hardening Tasks

### Task 1: Add Security Headers Middleware

**Risk:** Browser-based attacks (clickjacking, MIME sniffing, XSS)

**Implementation:**

Create `src/TaskHub.Api/Middleware/SecurityHeadersMiddleware.cs`:

```csharp
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-XSS-Protection"] = "0"; // Disabled; use CSP instead
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
```

Register in `MiddlewareExtensions.cs`:
```csharp
app.UseMiddleware<SecurityHeadersMiddleware>();
```

**Verification:**
```bash
curl -I http://localhost:5000/health/live | grep -E "X-Content|X-Frame|Content-Security"
```

---

### Task 2: Enable HSTS

**Risk:** HTTP downgrade attacks

**Implementation in `Program.cs`:**

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
```

**Note:** Only enable in production. HSTS tells browsers to always use HTTPS for this domain.

---

### Task 3: Restrict Swagger to Development

**Risk:** API documentation exposed in production

**Implementation:**

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}
```

---

### Task 4: Add Request Size Limits

**Risk:** Large payload DoS attacks

**Implementation:**

```csharp
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});
```

Already in place for file uploads (10 MB limit in ImportExportController), but this adds a global safety net.

---

### Task 5: Upgrade Cookie Security

**Risk:** Session fixation, long-lived sessions

**Changes:**

```csharp
options.Cookie.Name = "__Host-TaskHub"; // __Host- prefix enforces Secure+Path=/+no Domain
options.ExpireTimeSpan = TimeSpan.FromHours(4);     // Reduce from 7 days
options.SlidingExpiration = true;
options.Cookie.MaxAge = TimeSpan.FromHours(8);       // Absolute maximum
```

**Note:** `__Host-` prefix is a browser security feature that prevents cookie scope manipulation.

---

### Task 6: Rate Limit All Endpoints

**Risk:** API abuse on non-auth endpoints

**Add to `appsettings.json`:**

```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      { "Endpoint": "POST:/api/v1/auth/login", "Period": "1m", "Limit": 5 },
      { "Endpoint": "POST:/api/v1/auth/register", "Period": "1h", "Limit": 10 },
      { "Endpoint": "*:/api/v1/*", "Period": "1m", "Limit": 100 },
      { "Endpoint": "POST:/api/v1/importexport/import*", "Period": "1h", "Limit": 20 }
    ]
  }
}
```

---

### Task 7: Add Anti-Forgery Token (CSRF Layer 2)

**Risk:** CSRF attacks if SameSite support is incomplete in older browsers

**Implementation:**

While `SameSite=Strict` prevents most CSRF, a double-submit cookie pattern adds defence-in-depth:

```csharp
// On login response, set a CSRF token cookie (readable by JS)
context.Response.Cookies.Append("XSRF-TOKEN", token, new CookieOptions
{
    HttpOnly = false, // JS must read this
    Secure = true,
    SameSite = SameSiteMode.Strict
});

// On state-changing requests, validate X-XSRF-TOKEN header matches cookie
```

**Note:** This is documented as a known enhancement in `AuthExtensions.cs`.

---

### Task 8: File Storage Encryption at Rest

**Risk:** Data exposure if server is compromised

**Implementation approach:**

```csharp
// In FileStorage.SaveAsync:
var json = JsonSerializer.Serialize(schema);
var encrypted = Encrypt(json, encryptionKey);
await File.WriteAllBytesAsync(filePath, encrypted);

// In FileStorage.LoadAsync:
var encrypted = await File.ReadAllBytesAsync(filePath);
var json = Decrypt(encrypted, encryptionKey);
var schema = JsonSerializer.Deserialize<FileStorageSchema>(json);
```

**Key management:** Use ASP.NET Data Protection API or environment variable.

---

### Task 9: Dependency Vulnerability Audit

**Action:**

```bash
# .NET packages
dotnet list package --vulnerable

# Frontend packages
cd frontend && npm audit

# Fix vulnerabilities
npm audit fix
```

**Schedule:** Run monthly or on CI pipeline.

---

### Task 10: Log Sanitisation

**Risk:** Sensitive data in log files

**Verify these are NOT logged:**
- Passwords (plain or hashed)
- Session cookies
- Full request bodies on auth endpoints
- Email addresses (only log user IDs)

**Implementation:** Serilog destructuring policy:

```csharp
.Destructure.ByTransforming<LoginRequest>(r => new { r.Username, Password = "***" })
```

## 3. Verification Checklist

```bash
# Run full test suite
dotnet test TaskHub.sln

# Check security headers
curl -I https://localhost:5001/health/live

# Verify rate limiting
for i in {1..6}; do curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test","password":"test"}'; done
# 6th request should return 429

# Verify Swagger is disabled in production
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/TaskHub.Api &
curl -s https://localhost:5001/swagger/index.html  # Should return 404
```

## 4. Priority Order

| Priority | Task | Effort | Impact |
|----------|------|--------|--------|
| 1 | Security headers middleware | Low | High |
| 2 | HSTS | Low | High |
| 3 | Restrict Swagger to dev | Low | Medium |
| 4 | Request size limits | Low | Medium |
| 5 | Rate limit all endpoints | Low | Medium |
| 6 | Upgrade cookie security | Low | Medium |
| 7 | Dependency audit | Low | Medium |
| 8 | Log sanitisation | Medium | Medium |
| 9 | Anti-forgery token | Medium | Low (SameSite covers most cases) |
| 10 | File storage encryption | High | High (if file storage used in prod) |

## 5. Post-Hardening Security Test

After implementing all hardening tasks, verify:

1. All 31 existing tests still pass
2. Security headers present on all responses
3. Swagger not accessible in Production environment
4. Rate limits enforced across all endpoints
5. No sensitive data in log files
6. HTTPS enforced with HSTS
7. `dotnet list package --vulnerable` returns no findings
