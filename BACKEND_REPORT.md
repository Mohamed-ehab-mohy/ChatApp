# ChatApp — Backend Report

## Overview

Real-time chat application backend built with **.NET 10 Minimal API**, **SignalR**, **PostgreSQL**, **ASP.NET Core Identity**, and **JWT authentication**. Deployed on **Railway.app** (free tier).

**Live API**: `https://chatapp-production-d621.up.railway.app`
**Scalar Docs**: `https://chatapp-production-d621.up.railway.app/scalar/v1`
**GitHub**: `https://github.com/Mohamed-ehab-mohy/ChatApp`

---

## Architecture

```
Client (React)  ─── REST/HTTPS ───> .NET Minimal API ───> PostgreSQL
                  ─── WebSocket ───> SignalR Hub
```

- **3 pages**: Register, Login, Chat
- **REST endpoints**: Auth (Register/Login), Messages (GET)
- **Real-time**: SignalR hub at `/hub/chat` for broadcasting messages
- **Monorepo**: `backend/` + `docs/frontend/` + `docker-compose.yml`

---

## Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 10 — Minimal API |
| ORM | Entity Framework Core 10 + Npgsql |
| Database | PostgreSQL 16 (Railway managed) |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Real-time | SignalR |
| Validation | FluentValidation 12 |
| HTML Sanitization | HtmlSanitizer (Ganss.Xss) |
| API Docs | Microsoft.AspNetCore.OpenApi + Scalar.AspNetCore |
| API Versioning | Asp.Versioning.Http |
| Container | Docker / Docker Compose |
| Hosting | Railway.app (Docker + PostgreSQL) |

---

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | JWT token validation |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.9 | User management |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | PostgreSQL EF provider |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Input validation |
| `HtmlSanitizer` | 9.0.892 | XSS prevention in chat |
| `Asp.Versioning.Http` | 10.0.0 | API versioning |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 | OpenAPI generation |
| `Scalar.AspNetCore` | 2.16.10 | Interactive API docs |
| `Microsoft.OpenApi` | 2.7.5 | OpenAPI spec |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | EF migrations |

---

## Features Implemented

### 1. Authentication (ASP.NET Core Identity + JWT)
- Register with email/password
- Login returns JWT (60 min expiry)
- Identity handles password hashing, duplicate email detection
- All auth endpoints have rate limiting (10 req/min)

### 2. Real-time Chat (SignalR)
- Hub at `/hub/chat` with JWT auth via query string
- `SendMessage` — validates content (non-empty, max 1000 chars)
- `ReceiveMessage` — broadcasts sanitized message to all clients
- HtmlSanitizer allows safe tags: `b`, `i`, `u`, `a` (with `href`)

### 3. Message History (REST)
- `GET /api/v1/messages?limit=N` — returns messages oldest-first
- Limit auto-clamped to [1, 200]
- Requires JWT Bearer token

### 4. API Versioning
- Path: `/api/v1/...` + optional `?api-version=1.0` or `X-Api-Version: 1.0`
- Response header: `api-supported-versions: 1.0`

### 5. Security
- **CSP**: Applied on API routes (excluded for SignalR hub, Scalar, OpenAPI)
- **Rate Limiting**: 10 requests/minute on auth endpoints (returns 429)
- **Security Headers**: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` on all routes
- **CORS**: Configurable allowed origins, with `AllowCredentials()` for SignalR
- **JWT**: Bearer token validation, configurable issuer/audience/secret
- **Global Exception Handler**: Catches all unhandled exceptions, returns JSON error response

### 6. OpenAPI / Scalar
- Auto-generated OpenAPI 3.1 spec at `/openapi/v1.json`
- Interactive UI at `/scalar/v1` with JWT Bearer security scheme
- Document transformer adds Bearer auth to all requests

### 7. Health Check
- `GET /health` → `{ status: "healthy", timestamp: "..." }`

### 8. Database
- Auto-migration on startup (`db.Database.Migrate()`)
- PostgreSQL via Railway managed database
- Fallback: reads `DATABASE_URL` (Railway env var) first, then `ConnectionStrings:DefaultConnection`
- URI normalization: converts `postgresql://user:pass@host/db` to Npgsql format

---

## Docker & Deployment

### Local Development
```bash
docker compose up -d    # PostgreSQL + API on port 5111
```

### Dockerfile
- Multi-stage build (build → publish → runtime)
- Shell entrypoint evaluates `$PORT` at runtime (fallback 8080)
- Alpine-based runtime image for minimal size

### Railway Deployment
- `railway.json` with `DOCKERFILE` builder, `dockerfilePath: backend/Dockerfile`
- Auto-deploys on git push to `main`
- Env vars set via Railway Dashboard: `JwtSettings__SecretKey`, `AllowedOrigins__0`

---

## Project Structure

```
ChatApp/
├── backend/
│   ├── Program.cs                  # App entry, DI, middleware, routes
│   ├── ChatApp.csproj              # NuGet references
│   ├── Dockerfile                  # Multi-stage Docker build
│   ├── appsettings.json            # Configuration
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core context
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs        # Register/Login handlers
│   │   └── MessageEndpoints.cs     # GET messages handler
│   ├── Hubs/
│   │   └── ChatHub.cs              # SignalR hub
│   ├── Models/
│   │   ├── AppUser.cs              # Identity user
│   │   └── Message.cs              # Chat message entity
│   ├── Services/
│   │   └── TokenService.cs         # JWT generation
│   ├── Settings/
│   │   └── JwtSettings.cs          # JWT config POCO
│   └── DTOs/
│       ├── Auth/
│       │   ├── RegisterRequest.cs  # + FluentValidation
│       │   ├── LoginRequest.cs     # + FluentValidation
│       │   └── AuthResponse.cs     # Response DTO
│       └── Messages/
│           ├── MessageDto.cs       # Response DTO
│           └── SendMessageRequest.cs
├── docker-compose.yml              # Local dev stack
├── railway.json                    # Railway deploy config
├── .env.example                    # Env var template
├── .gitignore
├── BACKEND_REPORT.md               # This file
└── docs/frontend/
    ├── README.md                   # Frontend integration guide
    ├── api-reference.md            # Complete API reference
    ├── postman-collection.json     # Postman test collection
    └── openapi-spec.json           # OpenAPI 3.1.1 spec
```

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Minimal API over Controllers | Lighter, fewer files, modern .NET approach |
| HtmlSanitizer over HtmlEncode | Allows rich text (bold, italic, links) safely |
| CSP excluded for /hub, /scalar, /openapi | SignalR needs inline scripts; Scalar needs CSS |
| URI normalization for DATABASE_URL | Npgsql doesn't accept postgresql:// URI format |
| Shell entrypoint with $PORT | Railway assigns dynamic port at runtime |
| Rate limiting on auth only | Prevents brute force; messages endpoint already needs auth |
| `EnsureCreated()` → `Migrate()` | `Migrate()` supports incremental schema changes |

---

## Security Measures

1. **XSS Prevention**: HtmlSanitizer strips all tags except `b`, `i`, `u`, `a` with `href` on `https` only
2. **Brute Force Protection**: Rate limiter (10 requests/minute) on auth endpoints
3. **JWT Validation**: Issuer, audience, lifetime, and signing key validation
4. **CSP**: Restricts script/style sources to `'self'` on API endpoints
5. **Security Headers**: nosniff, DENY framing, no-referrer on every response
6. **Exception Safety**: Global handler prevents stack trace leaks in production
7. **CORS**: Whitelist-based origin policy with credentials support for SignalR
8. **SQL Injection**: EF Core parameterized queries (no raw SQL)
9. **Password Security**: ASP.NET Core Identity PBKDF2 hashing

---

## Deliverables for Frontend

Located in `docs/frontend/`:
- `README.md` — Step-by-step integration guide (auth flow, endpoints, SignalR, errors, examples)
- `api-reference.md` — Complete DTOs, endpoints, validation rules, status codes
- `postman-collection.json` — Pre-built Postman collection with auto Bearer token
- `openapi-spec.json` — OpenAPI 3.1.1 specification (also live at `/openapi/v1.json`)
- Live Scalar UI: `/scalar/v1`
