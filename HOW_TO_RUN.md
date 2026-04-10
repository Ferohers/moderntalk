# How to Run ModernUO with Web Portal

Step-by-step guide to get the ModernUO server with the Web Portal up and running.

---

## Option A: Docker (Recommended)

This is the easiest way to run the project. Everything is containerized.

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) installed
- [Docker Compose](https://docs.docker.com/compose/install/) installed (usually included with Docker Desktop)

### Step 1: Clone the Repository

```bash
git clone https://github.com/Ferohers/moderntalk.git
cd moderntalk
```

### Step 2: Build the Docker Image

```bash
docker compose build
```

This will:
1. Pull the .NET 10 SDK image
2. Install native dependencies (libargon2, libicu, etc.)
3. Clone the source code from your GitHub repo
4. Build ModernUO with the Web Portal included
5. Package everything into a runtime image with ASP.NET Core

> ⏱️ First build takes 5-10 minutes. Subsequent builds are faster due to Docker caching.

### Step 3: Start the Server

```bash
docker compose up -d
```

### Step 4: Verify It's Running

```bash
# Check the container is running
docker compose ps

# Check the logs
docker compose logs -f
```

You should see output like:
```
Web Portal starting on port 8080
```

### Step 5: Access the Web Portal

Open your browser and go to:

```
http://localhost:8080
```

You should see the ModernUO welcome page with the "How to Connect" guide.

### Step 6: Create an Account

1. Click **"Sign Up"** on the welcome page
2. Fill in a username (3-30 characters) and password (8+ characters)
3. Click **"Create Account"**
4. You'll be redirected to the account dashboard

### Step 7: Connect with a UO Client

1. Install a compatible Ultima Online client (version 7.0.96.0 or later)
2. Configure the client to connect to:
   - **Host:** `localhost` (or your server's IP)
   - **Port:** `2593`
3. Log in with the account you created on the web portal

### Step 8: Stop the Server

```bash
docker compose down
```

> 💡 Your data is preserved in Docker volumes. Use `docker compose down -v` to delete all data.

---

## Option B: Inject into an Existing ModernUO Installation (Linux)

If you already have a working ModernUO installation on Linux and want to add the Web Portal to it.

### Prerequisites

- An existing ModernUO installation (built from source)
- .NET 10 SDK installed
- The `webportal-inject.tar.gz` archive

### Step 1: Create the Injection Archive

On your development machine (where the Web Portal source code is):

```bash
./pack-webportal.sh webportal-inject.tar.gz
```

### Step 2: Transfer the Archive to the Server

```bash
scp webportal-inject.tar.gz user@your-server:/tmp/
```

### Step 3: Extract and Run the Installer

On the target server:

```bash
cd /path/to/modernuo
tar -xzf /tmp/webportal-inject.tar.gz
chmod +x install-webportal.sh
./install-webportal.sh /path/to/modernuo
```

The installer will:
1. Copy the Web Portal project files
2. Patch `Application.csproj` (add ASP.NET Core + WebPortal reference)
3. Patch `assemblies.json` (add WebPortal.dll)
4. Patch `ModernUO.slnx` (add WebPortal project)
5. Optionally patch Dockerfile and compose.yml

### Step 4: Rebuild ModernUO

```bash
cd /path/to/modernuo
./publish.sh release linux x64
```

### Step 5: Run the Server

```bash
cd Distribution
dotnet ModernUO.dll
```

The Web Portal will start automatically on port 8080.

---

## Option C: Build from Source (Development)

For developers who want to work on the code.

### Prerequisites

- .NET 10 SDK installed (`dotnet --version` should show 10.x)
- Git
- Native dependencies:
  - **Ubuntu/Debian:** `sudo apt install libicu-dev libdeflate-dev zstd libargon2-dev liburing-dev`
  - **macOS:** `brew install argon2 libdeflate`

### Step 1: Clone the Repository

```bash
git clone https://github.com/Ferohers/moderntalk.git
cd moderntalk
```

### Step 2: Build

```bash
./publish.sh release linux x64
```

Or manually:

```bash
dotnet build Projects/Application/Application.csproj -c Release
```

### Step 3: Run

```bash
cd Distribution
dotnet ModernUO.dll
```

### Step 4: Access the Web Portal

```
http://localhost:8080
```

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

### Changing Settings

Edit `modernuo.json` in the Distribution folder (or the Configuration volume in Docker):

```json
{
  "webPortal.enabled": true,
  "webPortal.port": 8080,
  "webPortal.connectionHost": "uo.myserver.com",
  "webPortal.connectionPort": 2593,
  "server.name": "My UO Server"
}
```

Then restart the server for changes to take effect.

### Docker Configuration

With Docker, you can map the configuration directory as a volume:

```yaml
# Already configured in compose.yml
volumes:
  - ./configuration:/app/Configuration
```

Edit `./configuration/modernuo.json` on the host machine, then restart:

```bash
docker compose restart
```

---

## Ports

| Port | Service | Protocol |
|------|---------|----------|
| `2593` | Game Server | TCP (UO client protocol) |
| `8080` | Web Portal | HTTP (web browser) |

### Changing the Web Portal Port

1. Edit `modernuo.json`: set `"webPortal.port"` to your desired port
2. If using Docker, also update `compose.yml`:
   ```yaml
   ports:
     - "2593:2593"
     - "9090:9090"    # Changed from 8080
   ```
3. If using Docker, also update the `Dockerfile`:
   ```dockerfile
   EXPOSE 9090
   ```
4. Restart the server

---

## Troubleshooting

### Web Portal doesn't start

1. Check the server logs for errors:
   ```bash
   docker compose logs -f
   ```
2. Verify `webPortal.enabled` is `true` in `modernuo.json`
3. Verify the port isn't already in use:
   ```bash
   lsof -i :8080
   ```

### Can't access the web portal from another machine

1. Make sure the port is open in your firewall
2. If using Docker, ensure the port mapping includes `0.0.0.0`:
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
- Generate a secure key:
  ```bash
  openssl rand -base64 32
  ```

---

## Security Notes

- **JWT tokens** are stored in HttpOnly, Secure, SameSite=Strict cookies
- **Passwords** are hashed with Argon2 (same as the game server)
- **Rate limiting**: 5 login attempts/minute per IP, 3 registrations/hour per IP
- **Account lockout**: Progressive backoff after failed login attempts
- **Security headers**: CSP, X-Frame-Options, X-Content-Type-Options, etc.
- **Anti-enumeration**: Same error message whether username exists or not
- **HTTPS**: For production, put behind a reverse proxy (nginx/Caddy) with TLS
