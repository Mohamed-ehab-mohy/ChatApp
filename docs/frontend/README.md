# ChatApp API — Frontend Integration Guide

## Quick Start

| Item | Value |
|------|-------|
| **Base URL** | `https://chatapp-production-d621.up.railway.app` |
| **Local Base URL** | `http://localhost:5111` |
| **API Docs (Scalar)** | `https://chatapp-production-d621.up.railway.app/scalar/v1` |
| **OpenAPI Spec** | `https://chatapp-production-d621.up.railway.app/openapi/v1.json` |
| **Postman Collection** | `./postman-collection.json` |
| **SignalR Hub** | `/hub/chat` |
| **Hosting** | Railway.app (PostgreSQL via Railway) |

## Authentication Flow

1. **Register** or **Login** via REST → receive `{ token, email, userId }`
2. Store the JWT token (localStorage / sessionStorage)
3. Send token as `Authorization: Bearer <token>` on all REST requests
4. For SignalR, pass token as `?access_token=<token>` query param

> **Token expires in 60 minutes.** Re-login to get a new one.

---

## REST API

### Register

Creates a new user account.

```
POST /api/v1/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!"
}
```

**Validation rules:**
| Field | Rule |
|-------|------|
| `email` | Required, valid email format |
| `password` | Required, at least 6 characters |

**Success `200`:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Validation error `400`:**
```json
{
  "errors": {
    "Email": ["'Email' is not a valid email address."],
    "Password": ["'Password' must be at least 6 characters."]
  }
}
```

**Duplicate email `400`:**
```json
[
  "Username 'user@example.com' is already taken."
]
```

---

### Login

Authenticates an existing user.

```
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Test123!"
}
```

**Validation rules:**
| Field | Rule |
|-------|------|
| `email` | Required, valid email format |
| `password` | Required |

**Success `200`:** (same shape as Register)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Unauthorized `401`:** empty body, no content.

---

### Get Messages

Fetches chat messages (paginated by limit).

```
GET /api/v1/messages?limit=50
Authorization: Bearer <token>
```

| Query Param | Type | Default | Description |
|-------------|------|---------|-------------|
| `limit` | `int` | `50` | Number of messages (auto-clamped 1–200) |

**Success `200`:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "content": "Hello! <b>bold</b> and <a href=\"https://example.com\">link</a>",
    "senderEmail": "user@example.com",
    "sentAt": "2026-07-06T20:30:00Z"
  }
]
```

**Unauthorized `401`:** when no valid Bearer token.

> Messages are sorted by `sentAt` ascending (oldest first).

---

## SignalR Real-time Chat

### Connection

```
ws://localhost:5111/hub/chat?access_token=<token>
```

The `access_token` query parameter is **required** (WebSocket connections cannot set custom headers).

### Send a message (Client → Server)

```typescript
await connection.invoke("SendMessage", content);
```

| Parameter | Type | Max Length | Notes |
|-----------|------|-----------|-------|
| `content` | `string` | 1000 | Sanitized — HTML tags `b`, `i`, `u`, `a` are allowed; all other tags/stripts are removed. |

If content is empty, whitespace-only, or over 1000 characters — the message is silently ignored.

### Receive messages (Server → Client)

```typescript
connection.on("ReceiveMessage", (message) => {
  // message: { id: string, content: string, senderEmail: string, sentAt: string }
});
```

The server broadcasts every message to all connected clients in the **GlobalGroup**.

### Connection lifecycle

| Event | Behavior |
|-------|----------|
| `OnConnectedAsync` | Client joins `GlobalGroup` |
| `OnDisconnectedAsync` | Client leaves `GlobalGroup` |

### SignalR client setup example

```typescript
import * as signalR from "@microsoft/signalr";

const BASE = process.env.NODE_ENV === "production"
  ? "https://chatapp-production-d621.up.railway.app"
  : "http://localhost:5111";

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${BASE}/hub/chat`, {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

await connection.start();

connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
});

await connection.invoke("SendMessage", "Hello everyone!");
```

---

## Error Handling

| Status Code | Meaning | When |
|-------------|---------|------|
| `200` | Success | Request processed successfully |
| `400` | Validation Error | Invalid input (email format, empty fields, etc.) |
| `401` | Unauthorized | Missing or invalid JWT token |
| `404` | Not Found | Invalid route |
| `500` | Internal Server Error | Server-side failure |

**400 response shapes:**
- FluentValidation errors: `{ "errors": { "FieldName": ["error message"] } }`
- Duplicate/domain errors: `["error description"]`

---

## Security Headers

All non-hub, non-scalar, non-openapi responses include:

| Header | Value |
|--------|-------|
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'` |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `no-referrer` |

---

## API Versioning

- Version is passed via query string or header: `?api-version=1.0` or `X-Api-Version: 1.0`
- Responses include `api-supported-versions: 1.0` header
- Current version: **1.0**

---

## Local Development

```bash
# 1. Ensure PostgreSQL is running on localhost:5432

# 2. Start the backend
cd backend
dotnet run -c Release

# 3. Backend is now at http://localhost:5111
```

**Allowed CORS origins** (configured in `appsettings.json`):
- `http://localhost:5173` (Vite)
- `http://localhost:3000` (Create React App)

---

## Related Files

| File | Description |
|------|-------------|
| `./postman-collection.json` | Import into Postman for testing |
| `./openapi-spec.json` | OpenAPI 3.1.1 specification |
| `https://github.com/Mohamed-ehab-mohy/ChatApp` | GitHub repository |
