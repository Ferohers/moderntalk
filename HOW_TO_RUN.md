# How to Run ModernUO with Web Portal

## Quick Start (Pre-built Image)

The easiest way — pull the pre-built image from GitHub Container Registry.

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed
- [Docker Compose](https://docs.docker.com/compose/install/) installed (usually included with Docker Desktop)

### Step 1: Clone the Repository

```bash
git clone https://github.com/Ferohers/moderntalk.git
cd moderntalk
```

### Step 2: Start the Server

```bash
docker compose up -d
```

This pulls the pre-built image from `ghcr.io/ferohers/moderntalk:latest` — no local build needed.

### Step 3: Verify It's Running

```bash
docker compose ps
docker compose logs -f
```

You should see:
```
Web Portal starting on port 8080
```

### Step 4: Access the Web Portal

Open your browser: `http://localhost:8080`

### Step 5: Create an Account

1. Click **"Sign Up"** on the welcome page
2. Fill in a username (3-30 characters) and password (8+ characters)
3. Click **"Create Account"**
4. You'll be redirected to the account dashboard

### Step 6: Connect with a UO Client

1. Install a compatible Ultima Online client (version 7.0.96.0 or later)
2. Configure the client to connect to:
   - **Host:** `localhost` (or your server's IP)
   - **Port:** `2593`
3. Log in with the account you created on the web portal

### Step 7: Stop the Server

```bash
docker compose down
```

> 💡 Your data is preserved in Docker volumes. Use `docker compose down -v` to delete all data.

---

## Building Locally

If you want to build the image yourself (e.g., to test changes before pushing):

1. Edit `compose.yml` — comment out the `image` line and uncomment the `build` section:
   ```yaml
   # image: ghcr.io/ferohers/moderntalk:latest
   build:
     context: .
     dockerfile: Dockerfile
   ```

2. Build and run:
   ```bash
   docker compose build
   docker compose up -d
   ```

> ⏱️ First build takes 5-10 minutes. The Dockerfile clones both ModernUO and moderntalk from GitHub inside the container.

---

## Configuration

The Web Portal reads its settings from ModernUO's configuration system. Settings are stored in `modernuo.json` and are auto-created with defaults on first run.

### Default Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `webPortal.enabled` | `true` | Enable/disable the web portal |
| `webPortal.port` | `8080` | HTTP port for the web portal |
| `webPortal.jwtSecret` | *(auto-generated)* | JWT signing key. Auto-generates a secure random key on first run. |
| `webPortal.maxLoginAttemptsPerMinute` | `5` | Max login attempts per minute per IP |
| `webPortal.accountLockoutMinutes` | `15` | Base lockout duration (progressive: 5 fails→15min, 10→1hr, 15→24hr) |
| `webPortal.accessTokenExpiryMinutes` | `15` | How long the login session lasts (minutes) |
| `webPortal.refreshTokenExpiryDays` | `7` | How long "remember me" lasts (days) |
| `webPortal.connectionHost` | `localhost` | Host shown in "How to Connect" guide |
| `webPortal.connectionPort` | `2593` | Port shown in "How to Connect" guide |
| `server.name` | `ModernUO` | Server name displayed on the welcome page |

### SMTP Settings (for Password Reset Emails)

| Setting | Default | Description |
|---------|---------|-------------|
| `webPortal.smtp.enabled` | `false` | Enable/disable email sending |
| `webPortal.smtp.host` | `""` | SMTP server hostname |
| `webPortal.smtp.port` | `587` | SMTP server port |
| `webPortal.smtp.username` | `""` | SMTP authentication username |
| `webPortal.smtp.password` | `""` | SMTP authentication password |
| `webPortal.smtp.useSsl` | `true` | Use SSL/TLS for SMTP connection |
| `webPortal.smtp.fromAddress` | `""` | Sender email address |
| `webPortal.smtp.fromName` | `ModernUO` | Sender display name |
| `webPortal.passwordResetBaseUrl` | `""` | Base URL for password reset links (e.g. `https://uo.example.com`) |

### Changing Settings

Edit `./configuration/modernuo.json` on the host machine, then restart:

```bash
docker compose restart
```

### Example Configuration

```json
{
  "webPortal.enabled": true,
  "webPortal.port": 8080,
  "webPortal.connectionHost": "uo.myserver.com",
  "webPortal.connectionPort": 2593,
  "server.name": "My UO Server",
  "webPortal.smtp.enabled": true,
  "webPortal.smtp.host": "smtp.gmail.com",
  "webPortal.smtp.port": 587,
  "webPortal.smtp.username": "your-email@gmail.com",
  "webPortal.smtp.password": "your-app-password",
  "webPortal.smtp.fromAddress": "your-email@gmail.com",
  "webPortal.passwordResetBaseUrl": "https://uo.myserver.com"
}
```

---

## Ports

| Port | Service | Protocol |
|------|---------|----------|
| `2593` | Game Server | TCP (UO client protocol) |
| `8080` | Web Portal | HTTP (web browser) |

### Changing the Web Portal Port

1. Edit `modernuo.json`: set `"webPortal.port"` to your desired port
2. Update `compose.yml`:
   ```yaml
   ports:
     - "2593:2593"
     - "9090:9090"    # Changed from 8080
   ```
3. If building locally, also update the `Dockerfile`:
   ```dockerfile
   EXPOSE 9090
   ```
4. Restart: `docker compose up -d`

---

## Troubleshooting

### Web Portal doesn't start

1. Check the server logs: `docker compose logs -f`
2. Verify `webPortal.enabled` is `true` in `modernuo.json`
3. Verify the port isn't already in use: `lsof -i :8080`

### Can't access the web portal from another machine

1. Make sure the port is open in your firewall
2. Ensure the port mapping includes `0.0.0.0`:
   ```yaml
   ports:
     - "0.0.0.0:8080:8080"
   ```

### "Invalid credentials" on login

- The Web Portal uses the same account system as the game server
- Usernames must be 3-30 characters, cannot contain `< > : " / \ | ? *`
- Passwords must be at least 8 characters
- After 5 failed login attempts, the account is locked for 15 minutes

### JWT secret changes on every restart

- If `webPortal.jwtSecret` is not set in `modernuo.json`, a new random key is generated each time
- This invalidates all existing login sessions on restart
- To persist the key, set it in `modernuo.json`:
  ```json
  {
    "webPortal.jwtSecret": "your-base64-encoded-secret-here"
  }
  ```
- Generate a secure key: `openssl rand -base64 32`

---

## Security Notes

- **JWT tokens** are stored in HttpOnly, Secure, SameSite=Strict cookies
- **Passwords** are hashed with Argon2 (same as the game server)
- **Rate limiting**: 5 login attempts/minute per IP, 3 registrations/hour per IP
- **Account lockout**: Progressive backoff after failed login attempts
- **Security headers**: CSP, X-Frame-Options, X-Content-Type-Options, etc.
- **Anti-enumeration**: Same error message whether username exists or not
- **HTTPS**: For production, put behind a reverse proxy (nginx/Caddy) with TLS
