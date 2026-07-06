# ChatApp API — Frontend Developer Delivery

**Backend is live and ready.** Here's everything you need to connect your React app.

---

## Quick Links

| Item | URL |
|------|-----|
| **Base API** | `https://chatapp-production-d621.up.railway.app` |
| **Interactive Docs** | `https://chatapp-production-d621.up.railway.app/scalar/v1` |
| **OpenAPI Spec** | `https://chatapp-production-d621.up.railway.app/openapi/v1.json` |
| **Health Check** | `https://chatapp-production-d621.up.railway.app/health` |
| **GitHub Repo** | `https://github.com/Mohamed-ehab-mohy/ChatApp` |

---

## 1. Auth Flow (REST)

### Register
```http
POST /api/v1/auth/register
Content-Type: application/json

{ "email": "user@example.com", "password": "Test123!" }
```

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "user@example.com", "password": "Test123!" }
```

### Response (both)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "user@example.com",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Store the token** — you'll send it as `Authorization: Bearer <token>` on every request.

> ⚠️ Rate limited: **10 requests per minute** on auth endpoints. Returns `429 Too Many Requests` if exceeded.

---

## 2. Chat History (REST)

### Get Messages
```http
GET /api/v1/messages?limit=50
Authorization: Bearer <token>
```

| Param | Default | Range | Description |
|-------|---------|-------|-------------|
| `limit` | `50` | 1–200 | Number of messages (oldest first) |

```json
[
  {
    "id": "guid",
    "content": "Hello! <b>bold</b> and <a href=\"https://...\">link</a>",
    "senderEmail": "user@example.com",
    "sentAt": "2026-07-06T20:30:00Z"
  }
]
```

---

## 3. Real-time Chat (SignalR)

### Connection
```typescript
import * as signalR from "@microsoft/signalr";

const BASE = "https://chatapp-production-d621.up.railway.app";
const token = localStorage.getItem("token"); // from Register/Login

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${BASE}/hub/chat`, { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

> The token is passed via `?access_token=<token>` query param (WebSocket limitation).

### Send a message
```typescript
await connection.invoke("SendMessage", "Hello everyone!");
```

### Receive messages
```typescript
connection.on("ReceiveMessage", (msg) => {
  console.log(msg.id, msg.content, msg.senderEmail, msg.sentAt);
});
```

### Message rules
- **Max 1000 characters**
- Empty/whitespace-only messages are **silently ignored**
- HTML allowed: `<b>`, `<i>`, `<u>`, `<a href="https://...">` — everything else is stripped

---

## 4. Validation & Errors

| Code | Meaning | When |
|------|---------|------|
| `200` | Success | Everything worked |
| `400` | Bad Input | Invalid email, short password, etc. |
| `401` | Unauthorized | Missing/invalid JWT token |
| `429` | Too Many Requests | Auth rate limit hit (try again in 1 min) |
| `500` | Server Error | Something broke on our side |

**400 shapes:**
- Validation errors: `{ "errors": { "FieldName": ["message"] } }`
- Duplicate email: `["Username '...' is already taken."]`

---

## 5. Quick Start Checklist

- [ ] Pick your frontend domain (Vercel / Cloudflare Pages)
- [ ] Deploy React app to that domain
- [ ] Tell me the domain so I add it to CORS (`AllowedOrigins__0`)
- [ ] In your React app, set `const BASE = "https://chatapp-production-d621.up.railway.app"`
- [ ] Implement Register page → store token
- [ ] Implement Login page → store token
- [ ] Implement Chat page → connect SignalR, fetch history, send/receive messages

---

## 6. Postman Collection

Import `docs/frontend/postman-collection.json` from the repo. It has all endpoints pre-configured with auto Bearer token variable.

---

## 7. Local Development (optional)

If you want to run the backend locally:

```bash
docker compose up -d    # starts PostgreSQL + API on port 5111
```

Then use `http://localhost:5111` as your base URL.

---

**Questions?** Ping me anytime.

**Repo**: `https://github.com/Mohamed-ehab-mohy/ChatApp`
**Commit**: `d22e33b` (7 Jul 2026)
