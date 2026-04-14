# UO Commander - Quick Start Guide

## Overview
UO Commander is a two-part system:
1. **HTTP API Module** - Runs inside your ModernUO server
2. **macOS App** - Admin interface built with SwiftUI

---

## Step 1: Install HTTP API Module (5 minutes)

### 1.1 Copy Files
Copy these files to your ModernUO project:
```
HttpApiServer.cs  →  Projects/Server/HTTP/
JwtHelper.cs      →  Projects/Server/HTTP/
```

If the `HTTP` folder doesn't exist:
```bash
mkdir -p Projects/Server/HTTP
```

### 1.2 Register the Module

**File: `Projects/Server/Main.cs`**

Find the `Configure()` method and add:
```csharp
HttpApiServer.Configure();
```

Find the `EventSink_ServerStarted` handler and add:
```csharp
_ = HttpApiServer.Start();
```

Find the `EventSink_Shutdown` handler and add:
```csharp
HttpApiServer.Stop();
```

### 1.3 Enable in Configuration

**File: `Distribution/Config/modernuo.json`** (or your config location)

Add this section:
```json
{
  "httpApi": {
    "enabled": true,
    "port": 8080,
    "jwtExpiryHours": 24
  }
}
```

### 1.4 Build and Test

```bash
cd /path/to/ModernUO
dotnet build
./ModernUO
```

You should see in the console:
```
[HTTP API] Server started on port 8080
```

Test the login endpoint:
```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_ADMIN","password":"YOUR_PASSWORD"}'
```

---

## Step 2: Build macOS App (3 minutes)

### 2.1 Prerequisites
- macOS 14.0 (Sonoma) or later
- Xcode 15.0+ (or just command line tools)

### 2.2 Build

```bash
cd /path/to/uo-commander
swift build
```

### 2.3 Run

**Option 1: Command Line**
```bash
swift run UOCommander
```

**Option 2: Xcode**
```bash
# Generate Xcode project (optional)
swift package generate-xcodeproj
open UOCommander.xcodeproj
```
Then press ⌘+R to run.

**Option 3: Create .app Bundle**
```bash
# The app will be built to .build/debug/UOCommander.app
# You can drag it to your Applications folder
```

---

## Step 3: Use UO Commander

### 3.1 Login

1. Launch UO Commander
2. Enter Server URL: `http://localhost:8080`
3. Enter your admin username and password
4. Click "Login"

**Requirements:**
- Account must have GameMaster (2) or higher access level
- Account must not be banned
- ModernUO server must be running with HTTP API enabled

### 3.2 Dashboard

After login, you'll see the dashboard with:
- **Server Status**: Players, uptime, memory, version
- **Quick Actions**: Save, Broadcast, Restart, Shutdown
- **Online Players**: Quick overview of connected players

### 3.3 Player Management

Click "Players" in the sidebar to:
- **Search**: Type a name to filter players
- **Inspect**: Double-click a player to view details
- **Kick**: Right-click → Kick (disconnects player)
- **Ban**: Right-click → Ban (bans entire account)

**Player Detail View shows:**
- Overview: Stats, location, account info
- Equipment: All equipped items
- Skills: Sorted by value (highest first)
- Properties: Detailed character properties

### 3.4 Server Control

Click "Server" in the sidebar to:
- **Broadcast**: Send message to all players or staff
- **Save World**: Trigger manual world save
- **Restart**: Configure delay, save option, then restart
- **Shutdown**: Shut down server (with or without save)

**Restart Countdown:**
1. Select delay (30s, 1m, 2m, 5m, or custom)
2. Toggle "Save world before restart"
3. Click "Restart Now"
4. Server broadcasts warning to all players
5. Countdown begins
6. Can be cancelled during countdown

### 3.5 Accounts

Click "Accounts" to:
- Search accounts by username
- View account details
- Ban/unban accounts
- See login history

### 3.6 Firewall

Click "Firewall" to:
- View all firewall rules
- Add new rules
- Remove existing rules
- Check if IP is blocked

### 3.7 Logs

Click "Logs" to:
- View real-time server logs
- Filter by log level
- Search for specific entries

---

## Common Tasks

### Ban a Player
1. Go to Players tab
2. Find the player
3. Right-click → Ban
4. Confirm the action
5. Player is disconnected and account is banned

### Broadcast a Message
1. Click "Broadcast" on Dashboard or Server tab
2. Type your message
3. Toggle "Staff only" if needed
4. Click "Send"

### Restart Server
1. Go to Server tab
2. Click "Restart"
3. Select delay time
4. Toggle "Save world before restart"
5. Click "Restart Now"
6. All players receive broadcast warning
7. Server saves (if enabled) and restarts after countdown

### Search for a Player
1. Go to Players tab
2. Type name in search bar
3. Results filter in real-time
4. Click player to inspect

---

## Troubleshooting

### "Connection refused"
- Verify ModernUO is running
- Check HTTP API is enabled in `modernuo.json`
- Verify port number (default: 8080)

### "Invalid credentials"
- Check username/password
- Account must be GameMaster or higher
- Check ModernUO server logs

### "Insufficient privileges"
- Account AccessLevel must be ≥ GameMaster (2)
- Verify in ModernUO: `[StaffAccess` command

### App won't launch
- Check macOS version (14.0+ required)
- Try running from terminal: `swift run UOCommander`
- Check console for error messages

---

## Security Best Practices

1. **Use HTTPS in Production**
   - Configure TLS in HttpApiServer.cs
   - Use a reverse proxy (nginx, Caddy) for TLS termination

2. **Restrict API Access**
   - Use `allowedIPs` in configuration
   - Only allow localhost or trusted networks

3. **Change JWT Secret**
   - Generate a strong random secret
   - Store securely, don't commit to version control

4. **Monitor Logs**
   - Watch for failed login attempts
   - Review admin actions in server logs

5. **Use Short Token Expiry**
   - Default is 24 hours
   - Reduce to 4-8 hours for better security

---

## Next Steps

- Explore all features in the app
- Check the full README.md for detailed documentation
- Review API endpoints for custom integrations
- Contribute to the project with new features

---

**Need Help?**
- Check the FAQ in README.md
- Open an issue on GitHub
- Ask in the ModernUO community

**Happy administering! 🎮**
