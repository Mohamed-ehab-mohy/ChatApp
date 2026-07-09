# ChatApp — Real-Time Chat Application

> A production-grade real-time chat platform powered by **ASP.NET Core 10**, **SignalR**, and **PostgreSQL**, featuring a modern **React 19** frontend with **PWA push notifications**.

[![Live API](https://img.shields.io/badge/Live_API-Railway.app-0B0D0E?style=for-the-badge&logo=railway&logoColor=white)](https://chatapp-production-d621.up.railway.app)
[![Live Demo](https://img.shields.io/badge/Live_Demo-Cloudflare_Pages-F38020?style=for-the-badge&logo=cloudflare&logoColor=white)](https://signalr-chat-room.pages.dev)
[![API Docs](https://img.shields.io/badge/API_Docs-Scalar-7B3FE4?style=for-the-badge&logo=scalar&logoColor=white)](https://chatapp-production-d621.up.railway.app/scalar/v1)
[![GitHub Backend](https://img.shields.io/badge/Backend_Repo-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Mohamed-ehab-mohy/ChatApp)
[![GitHub Frontend](https://img.shields.io/badge/Frontend_Repo-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/ahmedzaki-me/signalr-chat-room)

---

## Overview

ChatApp is a full-stack real-time messaging application built with a **.NET 10 Minimal API** backend and a **React 19 + TypeScript** frontend. It demonstrates secure authentication, bidirectional WebSocket communication via SignalR, persistent message history, and browser push notifications — all running on a cloud-native stack deployed via Docker on Railway.

---

## Architecture

```
                                ┌─────────────────────┐
                                │   React 19 + Vite   │
                                │  shadcn/ui + Radix  │
                                │   Tailwind CSS 4    │
                                └──────┬──────┬───────┘
                              REST │          │ WebSocket
                           (HTTPS) │          │ (WSS)
                                   ▼          ▼
                          ┌────────────────────────┐
                          │   .NET 10 Minimal API  │
                          │   ASP.NET Core 10      │
                          │   SignalR Hub          │
                          │   Identity + JWT       │
                          └───────────┬────────────┘
                                      │
                                      ▼
                          ┌────────────────────────┐
                          │      PostgreSQL 16     │
                          │   (Railway Managed)    │
                          └────────────────────────┘
```

| Layer | Technology |
|-------|-----------|
| **Frontend** | React 19, TypeScript, Vite 8, Tailwind CSS 4 |
| **UI Library** | shadcn/ui, Radix UI, Lucide React |
| **Backend** | .NET 10 — Minimal API |
| **Real-Time** | SignalR |
| **ORM** | Entity Framework Core 10 + Npgsql |
| **Database** | PostgreSQL 16 |
| **Authentication** | ASP.NET Core Identity + JWT Bearer |
| **Validation** | FluentValidation 12, Zod |
| **Push Notifications** | Web Push API (VAPID) |
| **API Documentation** | Scalar + OpenAPI 3.1 |
| **Containerization** | Docker / Docker Compose |
| **Hosting** | Railway.app (Docker + PostgreSQL) |

---

## Features

### Authentication
- User registration and login with **ASP.NET Core Identity**
- **JWT** access tokens (60-minute expiry)
- **HttpOnly refresh token cookie** — XSS-safe, rotated on each use (7-day expiry)
- Axios interceptor with automatic token injection and silent refresh

### Real-Time Chat
- **SignalR** WebSocket hub at `/hub/chat`
- Instant bidirectional messaging with automatic reconnection
- Messages broadcast to all connected clients in a shared group
- Message history via `GET /api/v1/messages` (paginated)

### Security
- **CSP headers** on all API routes (excluded for SignalR and docs)
- **HtmlSanitizer** — strips all unsafe HTML, allows safe tags only (`b`, `i`, `u`, `a`)
- **Rate limiting** — 10 requests/minute on authentication endpoints
- **Global exception handler** — prevents stack trace leaks
- **CORS** — whitelist-based origin policy with credential support
- **Security headers** — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`

### PWA Push Notifications
- **Web Push API** with **VAPID** authentication
- Service worker registered via **VitePWA**
- Notifications sent to all users except the message sender
- Expired subscriptions automatically cleaned up

### Developer Experience
- Interactive **Scalar** API documentation at `/scalar/v1`
- **OpenAPI 3.1** spec at `/openapi/v1.json`
- **Postman collection** included in `docs/frontend/`
- **Docker Compose** for zero-friction local development
- **API versioning** via path, query string, and header

---

## Project Structure

```
ChatApp/
│
├── backend/
│   ├── Program.cs                  # App entry point, DI, middleware, route mapping
│   ├── ChatApp.csproj              # NuGet package references
│   ├── Dockerfile                  # Multi-stage Docker build
│   ├── appsettings.json            # Configuration
│   │
│   ├── Data/
│   │   └── AppDbContext.cs         # EF Core database context
│   │
│   ├── Endpoints/
│   │   ├── AuthEndpoints.cs        # Register / Login / Refresh / Logout
│   │   ├── MessageEndpoints.cs     # GET /api/v1/messages
│   │   └── NotificationEndpoints.cs # Push subscription endpoints
│   │
│   ├── Hubs/
│   │   └── ChatHub.cs              # SignalR hub — SendMessage, ReceiveMessage
│   │
│   ├── Models/
│   │   ├── AppUser.cs              # Identity user entity
│   │   ├── Message.cs              # Chat message entity
│   │   ├── RefreshToken.cs         # Refresh token entity
│   │   └── PushSubscription.cs     # Push subscription entity
│   │
│   ├── Services/
│   │   ├── TokenService.cs         # JWT generation + refresh token lifecycle
│   │   └── PushNotificationService.cs # Web Push dispatch
│   │
│   ├── Settings/
│   │   ├── JwtSettings.cs          # JWT configuration POCO
│   │   └── VapidSettings.cs        # VAPID keys for push notifications
│   │
│   └── DTOs/
│       ├── Auth/                   # RegisterRequest, LoginRequest, AuthResponse
│       ├── Messages/               # MessageDto, SendMessageRequest
│       └── Notifications/          # SubscribeRequest, PushSubDto
│
├── docs/frontend/
│   ├── README.md                   # Frontend integration guide
│   ├── api-reference.md            # Complete API reference
│   ├── postman-collection.json     # Postman test collection
│   └── openapi-spec.json           # OpenAPI 3.1.1 specification
│
├── docker-compose.yml              # PostgreSQL + API for local development
├── railway.json                    # Railway deployment configuration
└── .env.example                    # Environment variable template
```

---

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/v1/auth/register` | — | Create a new account |
| `POST` | `/api/v1/auth/login` | — | Authenticate and receive JWT |
| `POST` | `/api/v1/auth/refresh` | Cookie | Refresh access token |
| `POST` | `/api/v1/auth/logout` | — | Revoke refresh token |
| `GET` | `/api/v1/messages?limit=N` | JWT | Fetch message history |
| `GET` | `/api/v1/notifications/public-key` | — | Get VAPID public key |
| `POST` | `/api/v1/notifications/subscribe` | JWT | Subscribe to push notifications |
| `POST` | `/api/v1/notifications/unsubscribe` | JWT | Unsubscribe from push notifications |
| `WS` | `/hub/chat?access_token=JWT` | Query | SignalR WebSocket hub |
| `GET` | `/health` | — | Server health check |

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Node.js](https://nodejs.org/) (for frontend)

### Backend (Docker Compose)

```bash
# Start PostgreSQL + API
docker compose up -d

# API is now at http://localhost:5111
```

### Backend (Manual)

```bash
# Ensure PostgreSQL is running on localhost:5432
cd backend
dotnet run -c Release

# API is now at http://localhost:5111
```

### Frontend

The frontend is maintained in a separate repository:

```bash
git clone https://github.com/ahmedzaki-me/signalr-chat-room.git
cd signalr-chat-room
npm install
```

Create a `.env` file:

```env
VITE_API_BASE_URL=http://localhost:5111
```

Start the development server:

```bash
npm run dev
```

---

## Tech Stack — Backend

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | JWT token validation |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.9 | User management |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | PostgreSQL EF Core provider |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Input validation |
| `HtmlSanitizer` | 9.0.892 | XSS prevention |
| `Asp.Versioning.Http` | 10.0.0 | API versioning |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 | OpenAPI generation |
| `Scalar.AspNetCore` | 2.16.10 | Interactive API docs |
| `WebPush` | 1.0.11 | Web Push notification delivery |

## Tech Stack — Frontend

| Package | Purpose |
|---------|---------|
| React 19 + TypeScript | UI framework |
| Vite 8 | Build tool |
| @microsoft/signalr | SignalR WebSocket client |
| Tailwind CSS 4 + shadcn/ui | Styling and components |
| Radix UI | Accessible primitives |
| React Hook Form + Zod | Form handling and validation |
| Axios | HTTP client with interceptors |
| React Router 7 | Client-side routing |
| VitePWA + Workbox | PWA and service worker |

---

## Deployment

### Railway

The backend is deployed on Railway using Docker:

```bash
railway login
railway up
```

Required environment variables in Railway Dashboard:

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | Auto-injected by Railway PostgreSQL |
| `JwtSettings__SecretKey` | 64-character random secret |
| `AllowedOrigins__0` | Frontend domain |

### Cloudflare Pages

The frontend is deployed on Cloudflare Pages connected to the [frontend repository](https://github.com/ahmedzaki-me/signalr-chat-room).

---

## Development Roadmap

```
Phase 1  — Project scaffolding (Identity, JWT, SignalR, API versioning)
Phase 2  — Message entity + database migration
Phase 3  — API versioning with route groups
Phase 4  — Auth endpoints (Register / Login with JWT)
Phase 5  — Chat hub + messages API
Phase 6  — Security audit (CORS, CSP, input sanitization)
Phase 7  — OpenAPI + Scalar UI documentation
Phase 8  — Docker + Railway deployment
Phase 9  — Refresh token rotation (HttpOnly cookie)
Phase 10 — PWA push notifications (Web Push + VAPID)
Phase 11 — Production polish, fixes, and documentation
```

---

## Contributors

### Mohamed Ehab — Back-End Developer

[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Mohamed-ehab-mohy "GitHub: Mohamed-ehab-mohy")
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/mohamed-ehab-mohy "LinkedIn: mohamed-ehab-mohy")

Responsible for the **backend API**: authentication system, real-time SignalR hub, database design, security implementation, Docker containerization, and Railway deployment.

### Ahmed Zaki — Front-End Developer

[![Portfolio](https://img.shields.io/badge/Portfolio-4285F4?style=for-the-badge&logo=google-chrome&logoColor=white)](https://ahmedzaki.me "Portfolio: ahmedzaki.me")
[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/ahmedzaki-me "GitHub: ahmedzaki-me")
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://linkedin.com/in/ahmedzaki-me "LinkedIn: ahmedzaki-me")

Responsible for the **React frontend**: modern UI with shadcn/ui components, SignalR client integration, PWA service worker, authentication flow, and Cloudflare Pages deployment.

---

## License

Distributed under the MIT License.
