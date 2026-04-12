# ModernTalk — Web Portal for ModernUO

A web portal overlay for [ModernUO](https://github.com/modernuo/ModernUO) that adds account management, player registration, and a server info page — all with a retro Ultima Online aesthetic.

## What This Repo Contains

This is **not** a fork of ModernUO. It's an overlay that contains only the WebPortal project. The Dockerfile clones both upstream ModernUO and this repo at build time, injects the WebPortal into it, and builds everything inside the container.

```
moderntalk/
├── Dockerfile              # Clones both repos, injects WebPortal, builds
├── compose.yml             # Docker Compose with ports 2593 + 8080
├── Projects/WebPortal/     # The WebPortal project (our code)
└── configuration/          # Sample configuration files
```

## Quick Start

```bash
git clone https://github.com/Ferohers/moderntalk.git
cd moderntalk
docker compose build
docker compose up -d
```

- **Game server**: `localhost:2593` (UO client)
- **Web portal**: `http://localhost:8080` (browser)

> 💡 The Dockerfile clones both [ModernUO](https://github.com/modernuo/ModernUO) and [moderntalk](https://github.com/Ferohers/moderntalk) from GitHub inside the container. Your local directory stays clean — no build artifacts on the host.

## Features

- **Account registration** — Players create accounts via the web
- **Login & dashboard** — View account info, change password
- **Server info page** — Shows connection details, player count, server status
- **JWT authentication** — Secure token-based auth with HttpOnly cookies
- **Rate limiting** — Per-IP protection against brute force
- **Account lockout** — Progressive backoff after failed logins
- **Security headers** — CSP, X-Frame-Options, etc.
- **Retro UO theme** — Dark fantasy aesthetic matching the game

## Configuration

Settings are stored in `modernuo.json` and auto-created with defaults on first run. See [HOW_TO_RUN.md](HOW_TO_RUN.md) for full details.

Key settings:

| Setting | Default | Description |
|---------|---------|-------------|
| `webPortal.enabled` | `true` | Enable/disable the web portal |
| `webPortal.port` | `8080` | HTTP port for the web portal |
| `webPortal.connectionHost` | `localhost` | Host shown in "How to Connect" |
| `webPortal.connectionPort` | `2593` | Port shown in "How to Connect" |
| `server.name` | `ModernUO` | Server name displayed on pages |

## Ports

| Port | Service | Protocol |
|------|---------|----------|
| `2593` | Game Server | TCP (UO client) |
| `8080` | Web Portal | HTTP (browser) |

## How It Works

The Dockerfile clones both repos inside the container:
1. `git clone https://github.com/modernuo/ModernUO.git` — upstream server
2. `git clone https://github.com/Ferohers/moderntalk.git` — our WebPortal overlay
3. Copies `Projects/WebPortal/` from moderntalk into the ModernUO source tree
4. Patches `Application.csproj` to add ASP.NET Core + WebPortal references
5. Builds everything together with `dotnet publish`

At runtime, ModernUO's `AssemblyHandler` auto-loads `WebPortal.dll` from `Assemblies/` and calls its `Configure()` and `Initialize()` methods, starting Kestrel on port 8080.

## License

This project is licensed under the GNU General Public License v3.0, consistent with ModernUO's license.
