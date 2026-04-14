# ModernUO Web Portal + Commander API

A standalone web portal and admin API for [ModernUO](https://github.com/modernuo/ModernUO) — the high-performance Ultima Online server emulator.

This project provides two HTTP services that integrate with ModernUO at build time. They are maintained separately from the ModernUO source code and injected via Docker.

## Services

| Service | Port | Purpose | Auth Level |
|---------|------|---------|------------|
| **Web Portal** | 8080 | Player-facing: account registration, login, dashboard | Any account |
| **Commander API** | 8090 | Admin-facing: server management, player control | GameMaster+ only |
| **Game Server** | 2593 | UO client protocol | Any account |

## Features

### Web Portal (port 8080)
- **Account Registration & Login** — Create and manage game accounts via web browser
- **Account Dashboard** — View account status, character count, change password/email
- **Server Info API** — Public endpoint showing server name, player count, connection info
- **Security** — JWT auth with HttpOnly cookies, rate limiting, progressive account lockout, Argon2 passwords
- **Retro UO Theme** — Dark stone textures, gold accents, Cinzel/Crimson Text fonts

### Commander API (port 8090)
- **Admin Authentication** — JWT-based login restricted to GameMaster+ accounts
- **Server Control** — Status, world save, shutdown, restart with countdown, broadcast messages
- **Player Management** — List/search online players, inspect details, kick, ban, view equipment/backpack/skills/properties
- **Account Management** — Search accounts, ban/unban, change access levels, view characters, IP lookup
- **World Inspection** — World stats, item inspection
- **Audit Logging** — All admin actions logged with actor, target, timestamp
- **Rate Limiting** — Per-IP rate limiting on all admin endpoints

## Quick Start

### Option A: Pre-built Image (Recommended)

```bash
git clone https://github.com/Ferohers/moderntalk.git
cd moderntalk
git checkout thin-portal
docker compose up -d
```

### Option B: Build Locally

If the GitHub image is unavailable:

```bash
docker compose -f compose2.yml build    # First build: ~5-10 min
docker compose -f compose2.yml up -d
```

### Access

| Service | URL |
|---------|-----|
| Web Portal | http://localhost:8080 |
| Commander API | http://localhost:8090 |
| Game Server | localhost:2593 |

## How It Works

The Dockerfile performs a multi-stage build:

1. **Clone** — Pulls upstream ModernUO from GitHub (`main` branch)
2. **Inject** — Copies `Projects/WebPortal/` and `Projects/CommanderApi/` into the source tree
3. **Patch** — Modifies `Application.csproj`, `assemblies.json`, and `ModernUO.slnx` to include both projects
4. **Build** — Compiles everything together with `dotnet publish`
5. **Package** — Creates a lean runtime image with ASP.NET Core

This means you **only maintain the portal and API code** — ModernUO updates are picked up automatically on rebuild.

## Configuration

Settings are stored in `modernuo.json` (auto-created on first run). Mount the configuration directory:

```yaml
volumes:
  - ./configuration:/app/Configuration
```

### Web Portal Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `webPortal.enabled` | `true` | Enable/disable the web portal |
| `webPortal.port` | `8080` | HTTP port |
| `webPortal.jwtSecret` | *(auto-generated)* | JWT signing key |
| `webPortal.connectionHost` | `localhost` | Host shown in "How to Connect" |
| `webPortal.connectionPort` | `2593` | Port shown in "How to Connect" |
| `server.name` | `ModernUO` | Server name on welcome page |

### Commander API Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `commanderApi.enabled` | `true` | Enable/disable the Commander API |
| `commanderApi.port` | `8090` | HTTP port |
| `commanderApi.jwtSecret` | *(auto-generated)* | JWT signing key |
| `commanderApi.jwtExpiryHours` | `24` | Token expiry time |
| `commanderApi.maxLoginAttemptsPerMinute` | `10` | Rate limit for login attempts |
| `commanderApi.accountLockoutMinutes` | `15` | Lockout duration after failed attempts |
| `commanderApi.corsOrigins` | *(empty — AllowAnyOrigin)* | Comma-separated CORS origins. Empty = any origin (safe for native apps) |

### Persistent JWT Secrets

By default, new JWT secrets are generated on each restart (invalidating sessions). To persist them:

```bash
openssl rand -base64 32
```

Add to `configuration/modernuo.json`:
```json
{
  "webPortal.jwtSecret": "your-base64-key-here",
  "commanderApi.jwtSecret": "your-other-base64-key-here"
}
```

## Commander API Endpoints

### Authentication — `/api/admin/auth`

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/admin/auth/login` | Login with GameMaster+ credentials |
| GET | `/api/admin/auth/verify` | Verify JWT token validity |
| POST | `/api/admin/auth/logout` | Logout (client-side token discard) |

### Server Control — `/api/admin/server`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/server/status` | Server uptime, player count, memory |
| POST | `/api/admin/server/save` | Trigger world save |
| POST | `/api/admin/server/shutdown` | Shutdown server (optional save) |
| POST | `/api/admin/server/restart` | Restart with countdown |
| POST | `/api/admin/server/broadcast` | Broadcast to all players |
| POST | `/api/admin/server/staff-message` | Message to staff only |

### Player Management — `/api/admin/players`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/players` | List online players |
| GET | `/api/admin/players/search?name=X` | Search players |
| GET | `/api/admin/players/{serial}` | Player details |
| POST | `/api/admin/players/{serial}/kick` | Kick player |
| POST | `/api/admin/players/{serial}/ban` | Ban player's account |
| POST | `/api/admin/players/{serial}/unban` | Unban player's account |
| GET | `/api/admin/players/{serial}/equipment` | Equipped items |
| GET | `/api/admin/players/{serial}/backpack` | Backpack contents |
| GET | `/api/admin/players/{serial}/skills` | Player skills |
| GET | `/api/admin/players/{serial}/properties` | Mobile properties |

### Account Management — `/api/admin/accounts`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/accounts` | List all accounts |
| GET | `/api/admin/accounts/search?username=X` | Search accounts |
| GET | `/api/admin/accounts/{username}` | Account details |
| POST | `/api/admin/accounts/{username}/ban` | Ban account |
| POST | `/api/admin/accounts/{username}/unban` | Unban account |
| POST | `/api/admin/accounts/{username}/access-level` | Change access level |
| GET | `/api/admin/accounts/{username}/characters` | List characters |
| GET | `/api/admin/accounts/by-ip/{ip}` | Accounts by IP |

### World — `/api/admin/world`

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/world/stats` | World statistics |
| GET | `/api/admin/world/items/{serial}` | Inspect item |
| GET | `/api/admin/world/audit-log` | Recent admin actions |

## Volumes

| Mount | Container Path | Purpose |
|-------|---------------|---------|
| Game data files | `/gamefiles` | UO client data (maps, art, etc.) |
| Configuration | `/app/Configuration` | `modernuo.json` settings |
| Save data | `/app/Saves` | World saves, account data |

## Pinning ModernUO Version

By default, the Dockerfile tracks the `main` branch. To pin to a specific version:

```bash
docker compose -f compose2.yml build --build-arg MODERNUO_BRANCH=v0.10.0
```

Or edit the `Dockerfile`:
```dockerfile
ARG MODERNUO_BRANCH=v0.10.0
```

## Project Structure

```
├── Projects/
│   ├── WebPortal/              # Web Portal source code
│   │   ├── Configuration/      # Settings reader
│   │   ├── Endpoints/          # API routes (auth, account, server)
│   │   ├── Middleware/          # Rate limiting, lockout, security headers
│   │   ├── Models/             # Request/response DTOs
│   │   ├── Services/           # Auth, account, token, game thread dispatch
│   │   ├── wwwroot/            # Frontend (HTML, CSS, JS)
│   │   ├── WebPortal.csproj    # Project file
│   │   └── WebPortalHost.cs    # Entry point (Configure/Initialize)
│   │
│   └── CommanderApi/            # Commander API source code
│       ├── Configuration/      # Settings reader (port 8090)
│       ├── Endpoints/          # API routes (auth, server, players, accounts, world)
│       ├── Middleware/          # Audit logging, rate limiting
│       ├── Models/             # Request/response DTOs
│       ├── Services/            # Auth, server, player, account, audit, game thread dispatch
│       ├── CommanderApi.csproj # Project file
│       └── CommanderApiHost.cs  # Entry point (Configure/Initialize)
│
├── old/                        # Previous uo-commander implementation (reference only)
├── plans/                      # Architecture and planning documents
├── Dockerfile                  # Multi-stage build (injects both projects)
├── compose.yml                 # Deploy with GHCR image
├── compose2.yml                # Deploy with local build
└── .github/workflows/          # CI: auto-publish to GHCR
```

## Security Notes

### Web Portal
- JWT tokens stored in HttpOnly, Secure, SameSite=Strict cookies
- Passwords hashed with Argon2 (same as game server)
- Rate limiting: 5 login/min, 3 registrations/hour per IP
- Progressive lockout: 5 fails → 15min, 10 → 1hr, 15 → 24hr
- Security headers: CSP, X-Frame-Options, HSTS

### Commander API
- JWT tokens via `Authorization: Bearer` header (native app, not browser cookies)
- GameMaster+ access required for all endpoints
- Higher-rank protection: cannot kick/ban equal or higher AccessLevel
- Per-IP rate limiting: 60 requests/minute per endpoint
- Login rate limiting: 10 attempts/minute per username
- All admin actions audit-logged with actor, target, timestamp
- Proper Microsoft.IdentityModel JWT validation (not hand-rolled)

### Production
- **For production**: Put behind a reverse proxy (nginx/Caddy) with TLS
- Use different JWT secrets for WebPortal and Commander API
- Restrict Commander API port (8090) to admin networks only

## License

This project follows the same license as [ModernUO](https://github.com/modernuo/ModernUO).
