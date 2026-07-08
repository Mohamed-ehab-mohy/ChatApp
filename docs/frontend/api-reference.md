# ChatApp API — Complete Reference

## Base URL

```
Development: http://localhost:5111
Production:  https://chatapp-production-d621.up.railway.app
Hosting:     Railway.app (Docker) + PostgreSQL (Railway managed)
```

---

## 1. Data Models (DTOs)

### AuthResponse
```typescript
interface AuthResponse {
  token: string;   // JWT Bearer token, expires in 60 minutes
  email: string;   // User's email address
  userId: string;  // GUID
}
// Refresh token is returned via Set-Cookie HttpOnly cookie (not in body)
```

### RegisterRequest
```typescript
interface RegisterRequest {
  email: string;    // Required, valid email format
  password: string; // Required, minimum 6 characters
}
```

### LoginRequest
```typescript
interface LoginRequest {
  email: string;    // Required, valid email format
  password: string; // Required
}
```

### MessageDto
```typescript
interface MessageDto {
  id: string;        // GUID
  content: string;   // Sanitized HTML (max 1000 chars)
  senderEmail: string;
  sentAt: string;    // ISO 8601 UTC
}
```

### SendMessageRequest
```typescript
interface SendMessageRequest {
  content: string;   // Required, max 1000 chars
}
```

### SubscribeRequest
```typescript
interface SubscribeRequest {
  endpoint: string;  // Push endpoint URL from browser
  p256dh: string;    // Client public key (base64, flat format)
  auth: string;      // Auth secret (base64, flat format)
  keys?: {           // Alternative nested format (from PushSubscription.toJSON())
    p256dh: string;
    auth: string;
  };
}
```

### UnsubscribeRequest
```typescript
interface UnsubscribeRequest {
  endpoint: string;  // The endpoint to remove
}
```

### PushNotificationPayload (Server → Service Worker)
```typescript
interface PushNotificationPayload {
  title: string;     // Sender email
  body: string;      // Message content (sanitized)
  url: string;       // URL to open on click (default: "/")
}
```

---

## 2. Endpoints

### 2.1 Register
```
POST /api/v1/auth/register
Content-Type: application/json
Body: RegisterRequest

200 → AuthResponse
400 → { errors: { Email: [...], Password: [...] } }
400 → ["Username '...' is already taken."]
```

### 2.2 Login
```
POST /api/v1/auth/login
Content-Type: application/json
Body: LoginRequest

200 → AuthResponse
400 → { errors: { Email: [...], Password: [...] } }
401 → (empty body)
```

### 2.3 Refresh Token
```
POST /api/v1/auth/refresh
(No body — refresh token read from HttpOnly cookie)

200 → AuthResponse (new access token, new refresh_token cookie sent)
401 → (empty body, cookie cleared)
```

> Frontend must send `credentials: "include"` on cross-origin requests.

### 2.4 Logout
```
POST /api/v1/auth/logout

200 → { message: "Logged out" }
```

### 2.5 Get VAPID Public Key
```
GET /api/v1/notifications/public-key
Authorization: Bearer {token}

200 → { publicKey: string }
401 → (empty body)
```

### 2.6 Subscribe to Push Notifications
```
POST /api/v1/notifications/subscribe
Content-Type: application/json
Authorization: Bearer {token}
Body: { endpoint: string, keys: { p256dh: string, auth: string } } or flat { endpoint, p256dh, auth }

200 → { message: "Subscribed" }
400 → { errors: { Endpoint: [...], P256DH: [...], Auth: [...] } }
401 → (empty body)
```

### 2.7 Unsubscribe from Push Notifications
```
POST /api/v1/notifications/unsubscribe
Content-Type: application/json
Authorization: Bearer {token}
Body: { endpoint: string }

200 → { message: "Unsubscribed" }
401 → (empty body)
```

### 2.8 Get Messages
```
GET /api/v1/messages?limit={number}
Authorization: Bearer {token}

Query:
  limit: int, default=50, clamped [1, 200]

200 → MessageDto[]
401 → (empty body)
```

---

## 3. SignalR Hub

### Connection
```
ws://{host}/hub/chat?access_token={token}
lng-http://{host}/hub/chat?access_token={token}
```

### Client Methods (invoke)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `SendMessage` | `content: string` | Send a chat message (max 1000 chars). Empties/whitespace-only messages are ignored. Content is HTML-sanitized — allowed tags: `b`, `i`, `u`, `a`. |

### Server Events (on)

| Event | Payload | Description |
|-------|---------|-------------|
| `ReceiveMessage` | `{ id: string, content: string, senderEmail: string, sentAt: string }` | Broadcast to all connected clients |

### TypeScript client
```typescript
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

const connection = new HubConnectionBuilder()
  .withUrl("http://localhost:5111/hub/chat", {
    accessTokenFactory: () => localStorage.getItem("token") ?? ""
  })
  .configureLogging(LogLevel.Information)
  .withAutomaticReconnect()
  .build();

// Start
await connection.start();

// Listen for incoming messages
connection.on("ReceiveMessage", (msg) => {
  console.log(msg.id, msg.content, msg.senderEmail, msg.sentAt);
});

// Send a message
await connection.invoke("SendMessage", "Hello world!");

// Handle reconnection
connection.onreconnected(() => console.log("Reconnected"));
connection.onclose(() => console.log("Disconnected"));
```

---

## 4. Validation Summary

| Endpoint | Field | Rule | Error Status |
|----------|-------|------|-------------|
| Register | email | Required, valid email | 400 |
| Register | password | Required, min 6 chars | 400 |
| Login | email | Required, valid email | 400 |
| Login | password | Required | 400 |
| SendMessage (SignalR) | content | Max 1000 chars, not empty | Silently ignored |
| Subscribe | endpoint | Required, valid URL | 400 |
| Subscribe | p256dh | Required | 400 |
| Subscribe | auth | Required | 400 |

---

## 5. HTTP Status Codes Summary

| Code | Meaning | Endpoints |
|------|---------|-----------|
| 200 | Success | All |
| 400 | Bad Request / Validation | Register, Login |
| 401 | Unauthorized | Messages, SignalR, Notifications |
| 404 | Not Found | Any invalid route |
| 500 | Internal Error | Any |

---

## 6. Headers

### Request Headers
| Header | Value | When |
|--------|-------|------|
| `Authorization` | `Bearer {token}` | Messages endpoint, SignalR |
| `Content-Type` | `application/json` | Register, Login |
| `X-Api-Version` | `1.0` | Optional, for versioning |

### Response Headers
| Header | Value | Notes |
|--------|-------|-------|
| `api-supported-versions` | `1.0` | API versioning info |
| `Content-Security-Policy` | `default-src 'self'; ...` | Present on API routes only |
| `X-Content-Type-Options` | `nosniff` | Security |
| `X-Frame-Options` | `DENY` | Security |
| `Referrer-Policy` | `no-referrer` | Security |
