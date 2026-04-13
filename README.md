# ModernUO Web Portal

A standalone web portal for [ModernUO](https://github.com/modernuo/ModernUO) — the high-performance Ultima Online server emulator.

This project provides a web-based account management portal that integrates with ModernUO at build time. It is maintained separately from the ModernUO source code and injected via Docker.

## Features

- **Account Registration & Login** — Create and manage game accounts via web browser
- **Account Dashboard** — View account status, character count, change password/email
- **Server Info API** — Public endpoint showing server name, player count, connection info
- **Security** — JWT auth with HttpOnly cookies, rate limiting, progressive account lockout, Argon2 passwords
- **Retro UO Theme** — Dark stone textures, gold accents, Cinzel/Crimson Text fonts

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
| Game Server | localhost:2593 |

## How It Works

The Dockerfile performs a multi-stage build:

1. **Clone** — Pulls upstream ModernUO from GitHub (`main` branch)
2. **Inject** — Copies `Projects/WebPortal/` into the source tree
3. **Patch** — Modifies `Application.csproj`, `assemblies.json`, and `ModernUO.slnx` to include the WebPortal
4. **Build** — Compiles everything together with `dotnet publish`
5. **Package** — Creates a lean runtime image with ASP.NET Core

This means you **only maintain the WebPortal code** — ModernUO updates are picked up automatically on rebuild.

## Configuration

Settings are stored in `modernuo.json` (auto-created on first run). Mount the configuration directory:

```yaml
volumes:
  - ./configuration:/app/Configuration
```

| Setting | Default | Description |
|---------|---------|-------------|
| `webPortal.enabled` | `true` | Enable/disable the web portal |
| `webPortal.port` | `8080` | HTTP port |
| `webPortal.jwtSecret` | *(auto-generated)* | JWT signing key |
| `webPortal.connectionHost` | `localhost` | Host shown in "How to Connect" |
| `webPortal.connectionPort` | `2593` | Port shown in "How to Connect" |
| `server.name` | `ModernUO` | Server name on welcome page |

### Persistent JWT Secret

By default, a new JWT secret is generated on each restart (invalidating sessions). To persist it:

```bash
openssl rand -base64 32
```

Add to `configuration/modernuo.json`:
```json
{
  "webPortal.jwtSecret": "your-base64-key-here"
}
```

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
├── Projects/WebPortal/        # WebPortal source code
│   ├── Configuration/         # Settings reader
│   ├── Endpoints/             # API routes (auth, account, server)
│   ├── Middleware/             # Rate limiting, lockout, security headers
│   ├── Models/                # Request/response DTOs
│   ├── Services/              # Auth, account, token, game thread dispatch
│   ├── wwwroot/               # Frontend (HTML, CSS, JS)
│   ├── WebPortal.csproj       # Project file
│   └── WebPortalHost.cs       # Entry point (Configure/Initialize)
├── Dockerfile                 # Multi-stage build
├── compose.yml                # Deploy with GHCR image
├── compose2.yml               # Deploy with local build
└── .github/workflows/         # CI: auto-publish to GHCR
```

## Security Notes

- JWT tokens stored in HttpOnly, Secure, SameSite=Strict cookies
- Passwords hashed with Argon2 (same as game server)
- Rate limiting: 5 login/min, 3 registrations/hour per IP
- Progressive lockout: 5 fails → 15min, 10 → 1hr, 15 → 24hr
- Security headers: CSP, X-Frame-Options, HSTS
- **For production**: Put behind a reverse proxy (nginx/Caddy) with TLS

## License

The WebPortal project follows the same license as [ModernUO](https://github.com/modernuo/ModernUO).
