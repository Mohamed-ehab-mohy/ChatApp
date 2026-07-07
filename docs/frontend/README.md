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

1. **Register** or **Login** with `credentials: "include"` → receive `{ token, email, userId }` in **body** + `Set-Cookie: refresh_token=...` (**HttpOnly**)
2. The **refresh token** is in an **HttpOnly cookie** — JS cannot read it (XSS-safe)
3. Store the **access token** in memory (not localStorage!) — re-fetch from `/refresh` on page reload
4. Send access token as `Authorization: Bearer <token>` on all REST requests
5. For SignalR, pass token as `?access_token=<token>` query param
6. When the access token expires (60 min), call **Refresh** with `credentials: "include"` — the browser auto-sends the cookie

> **Access token expires in 60 minutes.** The HttpOnly cookie refresh token expires in **7 days** and is rotated on each use. All auth fetch calls MUST use `credentials: "include"` for the cookie to be set/sent. This prevents XSS from stealing the refresh token.

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

> A `Set-Cookie: refresh_token=...; HttpOnly; Secure; SameSite=None; Path=/api/v1/auth; Max-Age=604800` header is also sent.

**TypeScript example (must use `credentials: "include"`):**
```typescript
const res = await fetch(`${BASE}/api/v1/auth/register`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ email: "user@example.com", password: "Test123!" }),
  credentials: "include" // ← REQUIRED for the Set-Cookie to work cross-origin
});
const { token, email, userId } = await res.json();
// Store `token` in a JS variable (NOT localStorage)
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

**Must use `credentials: "include"`** — same as Register.

**Success `200`:** (same shape as Register, also sets `refresh_token` cookie)

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Unauthorized `401`:** empty body, no content.

---

### Refresh Token

Extends the session without re-entering credentials. Reads the `refresh_token` **HttpOnly cookie** automatically — no body needed. **Rotates** — old refresh token is revoked, a new one is issued via `Set-Cookie`.

```
POST /api/v1/auth/refresh
```

> No body required. The browser auto-sends the `refresh_token` cookie. Send `credentials: 'include'` in fetch.

**Success `200`:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

> A new `Set-Cookie: refresh_token=...` is also sent (rotation).

**Invalid/expired `401`:** empty body (cookie cleared on server side too).

**TypeScript example:**
```typescript
async function refreshAccessToken(): Promise<string | null> {
  const res = await fetch(`${BASE}/api/v1/auth/refresh`, {
    method: "POST",
    credentials: "include" // ← required for cross-origin cookies
  });
  if (!res.ok) return null;
  const data = await res.json();
  return data.token; // store in memory, not localStorage
}
```

> Refresh token expires in **7 days**. After that, user must re-login.

---

### Logout

Revokes the refresh token and clears the cookie.

```
POST /api/v1/auth/logout
```

**Success `200`:**
```json
{
  "message": "Logged out"
}
```

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

// Store access token in memory (not localStorage — XSS-safe)
let accessToken: string | null = null;

const BASE = process.env.NODE_ENV === "production"
  ? "https://chatapp-production-d621.up.railway.app"
  : "http://localhost:5111";

// Get access token via refresh (uses HttpOnly cookie automatically)
async function getToken(): Promise<string | null> {
  if (accessToken) return accessToken;
  const res = await fetch(`${BASE}/api/v1/auth/refresh`, {
    method: "POST",
    credentials: "include"
  });
  if (!res.ok) return null;
  const data = await res.json();
  accessToken = data.token;
  return accessToken;
}

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${BASE}/hub/chat`, {
    accessTokenFactory: () => accessToken ?? ""
  })
  .withAutomaticReconnect()
  .build();

// Get new token on reconnect
connection.onreconnecting(async () => {
  accessToken = await getToken();
});

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

## Push Notifications (PWA)

The backend supports **Web Push notifications** for PWA integration. When a user sends a message, all other subscribed users receive a push notification (even if the app is closed).

### Setup

1. **Install** `@microsoft/signalr` and your preferred PWA/service-worker framework
2. **Generate VAPID keys** on the backend (already pre-generated for dev):
   - **Public Key**: `BANXPzDBlJhFcXq8js9i-OxEj39v8Rddd74yhdkNMG6Msj-xq6I6NjMd9awLEhV3WOUjmI1qjICihcUIWv19wdM`
3. **Register service worker** and request notification permission

### Endpoints

#### Get VAPID Public Key

```
GET /api/v1/notifications/public-key
Authorization: Bearer <token>
```

**Success `200`:**
```json
{
  "publicKey": "BANXPzDBlJhFcXq8js9i-OxEj39v8Rddd74yhdkNMG6Msj-xq6I6NjMd9awLEhV3WOUjmI1qjICihcUIWv19wdM"
}
```

#### Subscribe to Push Notifications

Saves the browser's push subscription to the server.

```
POST /api/v1/notifications/subscribe
Content-Type: application/json
Authorization: Bearer <token>

{
  "endpoint": "https://fcm.googleapis.com/...",
  "p256dh": "BC...",
  "auth": "A..."
}
```

**Success `200`:** `{ "message": "Subscribed" }`

#### Unsubscribe from Push Notifications

```
POST /api/v1/notifications/unsubscribe
Content-Type: application/json
Authorization: Bearer <token>

{
  "endpoint": "https://fcm.googleapis.com/..."
}
```

**Success `200`:** `{ "message": "Unsubscribed" }`

### Frontend Integration Example

```typescript
const BASE = "https://chatapp-production-d621.up.railway.app";
const VAPID_PUBLIC_KEY = "BANXPzDBlJhFcXq8js9i-OxEj39v8Rddd74yhdkNMG6Msj-xq6I6NjMd9awLEhV3WOUjmI1qjICihcUIWv19wdM";

// 1. Get token from auth
async function getToken(): Promise<string | null> { /* ... */ }

// 2. Subscribe to push
async function subscribeToPush(token: string) {
  const registration = await navigator.serviceWorker.register("/sw.js");
  const sub = await registration.pushManager.subscribe({
    userVisibleOnly: true,
    applicationServerKey: VAPID_PUBLIC_KEY
  });

  await fetch(`${BASE}/api/v1/notifications/subscribe`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`
    },
    body: JSON.stringify(sub.toJSON())
  });
}

// 3. Service worker (sw.js) handles incoming notifications
self.addEventListener("push", (event) => {
  const data = event.data?.json() ?? {};
  self.registration.showNotification(data.title, {
    body: data.body,
    data: { url: data.url }
  });
});

self.addEventListener("notificationclick", (event) => {
  event.notification.close();
  clients.openWindow(event.notification.data?.url ?? "/");
});
```

> Push notifications are automatically sent to all users except the message sender. Expired/removed subscriptions are cleaned up automatically.

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

## Production (Railway)

| Item | Value |
|------|-------|
| **API Base URL** | `https://chatapp-production-d621.up.railway.app` |
| **Scalar Docs** | `https://chatapp-production-d621.up.railway.app/scalar/v1` |
| **Health Check** | `https://chatapp-production-d621.up.railway.app/health` |

**Railway env vars needed** (set in Railway Dashboard → Variables):
| Key | Value |
|-----|-------|
| `JwtSettings__SecretKey` | Your 64-char secure random key |
| `AllowedOrigins__0` | `https://<your-frontend-domain>` (after frontend deploy) |

> Rate limiting: 10 requests/minute on `/auth/*` endpoints. Returns `429` if exceeded.

---

## Global Error Handling

All unhandled exceptions return:
```json
{
  "errors": ["An error occurred"]
}
```
In development mode, the actual error message is shown.

---

## Related Files

| File | Description |
|------|-------------|
| `./postman-collection.json` | Import into Postman for testing |
| `./openapi-spec.json` | OpenAPI 3.1.1 specification |
| `https://github.com/Mohamed-ehab-mohy/ChatApp` | GitHub repository |
