# UO Commander - macOS Server Administration App

## Overview
A modern macOS application built with SwiftUI to administer ModernUO Ultima Online servers remotely via a custom HTTP API.

## Architecture

### Two-Part System
1. **Server-Side**: ModernUO HTTP API Module (C#) - Embedded HTTP server in ModernUO
2. **Client-Side**: UO Commander macOS App (Swift/SwiftUI) - Administration interface

---

## Part 1: ModernUO HTTP API Module

### Authentication
- **Method**: JWT (JSON Web Tokens) with admin account credentials
- **Flow**: 
  1. POST `/api/auth/login` with username/password
  2. Server validates against Account system (Argon2 password hash)
  3. Check account AccessLevel >= GameMaster (level 2)
  4. Return JWT token with 24-hour expiry
  5. All subsequent requests require `Authorization: Bearer <token>`

### API Endpoints

#### Authentication
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/logout` - Invalidate token
- `GET /api/auth/verify` - Verify token validity

#### Server Control
- `GET /api/server/status` - Server status (uptime, player count, memory, CPU)
- `POST /api/server/save` - Trigger world save
- `POST /api/server/shutdown?save=true` - Shutdown server (optional save flag)
- `POST /api/server/restart?save=true&delay=60` - Restart with countdown (seconds)
- `POST /api/server/broadcast` - Broadcast message to all players
- `POST /api/server/staff-message` - Message to staff only
- `GET /api/server/lockdown` - Get/set lockdown level

#### Player Management
- `GET /api/players` - List all online players
- `GET /api/players/search?name=X` - Search players by name
- `GET /api/players/{serial}` - Get player details (location, account, access level)
- `POST /api/players/{serial}/kick` - Kick player from server
- `POST /api/players/{serial}/ban` - Ban player's account
- `POST /api/players/{serial}/unban` - Unban player's account
- `GET /api/players/{serial}/equipment` - List equipped items
- `GET /api/players/{serial}/backpack` - List backpack contents
- `GET /api/players/{serial}/skills` - View player skills
- `GET /api/players/{serial}/properties` - Inspect mobile properties
- `GET /api/players/{serial}/speech-log` - Recent speech history
- `GET /api/players/{serial}/hardware-info` - Client hardware info

#### Account Management
- `GET /api/accounts` - List all accounts (paged)
- `GET /api/accounts/search?username=X` - Search accounts
- `GET /api/accounts/{username}` - Get account details
- `POST /api/accounts/{username}/ban` - Ban account
- `POST /api/accounts/{username}/unban` - Unban account
- `POST /api/accounts/{username}/access-level` - Change access level
- `POST /api/accounts/{username}/password` - Change password
- `GET /api/accounts/{username}/characters` - List all characters
- `GET /api/accounts/{username}/comments` - Account comments
- `POST /api/accounts/{username}/comments` - Add comment
- `GET /api/accounts/by-ip/{ip}` - Accounts sharing an IP

#### Firewall
- `GET /api/firewall` - List all firewall rules
- `POST /api/firewall` - Add firewall rule (IP, CIDR, range)
- `DELETE /api/firewall/{rule-id}` - Remove firewall rule
- `POST /api/firewall/check/{ip}` - Check if IP is blocked

#### World & Items
- `GET /api/world/stats` - World statistics (items count, mobiles count, map sizes)
- `GET /api/items/{serial}` - Inspect item properties
- `GET /api/items/{serial}/container` - List container contents
- `POST /api/items/{serial}/properties` - Modify item properties (Admin only)

#### Commands
- `POST /api/commands/execute` - Execute server command programmatically
- `GET /api/commands/list` - List available commands

#### Events & Logs
- `GET /api/logs/recent?lines=100` - Recent server logs
- `GET /api/events/connections` - Recent connection/disconnection events

### Security Features
- JWT tokens with configurable expiry
- IP whitelist for API access (optional)
- Rate limiting on login endpoint
- All admin actions logged with actor username and timestamp
- HTTPS support (TLS/SSL)
- CORS configuration for local development

### Technical Implementation
- Use `System.Net.HttpListener` for lightweight HTTP server
- Run on separate port (default: 8080) configured in `modernuo.json`
- All game-state operations marshaled to main thread via `EventLoopContext`
- JSON serialization using System.Text.Json
- No external dependencies beyond .NET standard library

---

## Part 2: UO Commander macOS App

### Technology Stack
- **Language**: Swift 5.9+
- **Framework**: SwiftUI (declarative UI)
- **Architecture**: MVVM with Observable pattern
- **Networking**: Async/Await with URLSession
- **Persistence**: SwiftData for local settings
- **UI Components**: SF Symbols, modern SwiftUI controls
- **Minimum macOS**: 14.0 (Sonoma)

### Features

#### 1. Authentication Screen
- Username/password login form
- Secure credential storage in Keychain
- Server URL configuration (default: http://localhost:8080)
- Connection status indicator
- Token auto-refresh mechanism
- Biometric authentication (Touch ID) option

#### 2. Dashboard
- **Server Status Card**:
  - Online/Offline indicator
  - Uptime counter
  - Player count (online / capacity)
  - Memory usage
  - CPU usage
  - World save status
  
- **Quick Actions**:
  - Broadcast message button
  - Save world button
  - Restart with countdown
  - Shutdown server
  
- **Recent Activity Feed**:
  - Player connections/disconnections
  - Bans/kicks
  - System messages
  
- **Charts**:
  - Player count over time
  - Memory usage trend
  - Network I/O

#### 3. Players Tab
- **Player List**:
  - Real-time list with avatars
  - Search bar (name, account, IP)
  - Sort by name, location, access level, playtime
  - Filter by access level
  - Badge indicators (staff, flagged, jailed)
  
- **Player Detail View**:
  - Character info (name, access level, location, playtime)
  - Account info (username, creation date, last login, IP)
  - Equipment viewer (visual grid with item icons)
  - Backpack contents (expandable tree)
  - Skills list (sortable table)
  - Properties inspector (collapsible sections)
  - Speech log (searchable list)
  - Hardware info (client version, OS, resolution)
  
- **Actions**:
  - Kick player (with reason)
  - Ban account (with duration selector: permanent, 1h, 6h, 24h, 7d, 30d)
  - Unban account
  - Teleport to player
  - Bring player to you (requires staff in-game)
  - Jail/unjail player
  - Squelch/unsquelch
  - View account details
  - View account characters

#### 4. Accounts Tab
- **Account Search**:
  - Search by username
  - Filter by access level
  - Filter by banned status
  - Search by IP address
  
- **Account Detail View**:
  - Username and access level
  - Ban status and history
  - Characters list (with stats)
  - Login IP history
  - Comments (add/edit/delete)
  - Tags
  - Shared IP accounts
  
- **Actions**:
  - Ban/unban account
  - Change access level
  - Change password
  - Add comment
  - View all characters
  - Delete account (with confirmation)

#### 5. Server Control Tab
- **Server Status**:
  - Uptime
  - World save status and last save time
  - Server version
  - Feature flags
  
- **Broadcast System**:
  - Text input for message
  - Preview button
  - Send to all players / staff only
  - Message history
  
- **Shutdown/Restart**:
  - **Countdown Timer** (visual circular progress):
    - Configurable delay (30s, 60s, 120s, 300s, custom)
    - Broadcast warning messages at intervals
    - Cancel button during countdown
    - Confirmation dialog before execution
  - Save before shutdown/restart toggle
  - Emergency shutdown (immediate, no save)
  
- **World Save**:
  - Manual save trigger
  - Auto-save status
  - Save history
  - Archive management
  
- **Server Lockdown**:
  - Set minimum access level for login
  - Purge unauthorized clients
  - Lockdown status indicator

#### 6. Firewall Tab
- **Rule Management**:
  - List all firewall rules
  - Add rule (single IP, CIDR, range)
  - Remove rule
  - Enable/disable rule toggle
  
- **Quick Actions**:
  - Block player IP
  - Unblock IP
  - Check if IP is blocked
  
- **Statistics**:
  - Blocked connection attempts
  - Top blocked IPs

#### 7. Logs Tab
- **Server Logs**:
  - Real-time log streaming
  - Filter by level (Info, Warning, Error, Critical)
  - Search functionality
  - Export logs
  - Auto-scroll toggle
  
- **Event Log**:
  - Player connections
  - Bans/kicks
  - Account logins
  - Command executions

#### 8. Settings
- **Server Configuration**:
  - Server URL
  - API port
  - Connection timeout
  - Auto-reconnect
  
- **App Preferences**:
  - Theme (Light, Dark, System)
  - Auto-refresh interval
  - Notification preferences
  - Sound effects
  
- **Credential Management**:
  - Saved servers
  - Clear credentials
  - Export/import settings

### UI/UX Design

#### Design Principles
- Clean, modern macOS native look
- SF Symbols for all icons
- Sidebar navigation
- Split-view for detail panels
- Keyboard shortcuts for common actions
- Touch Bar support (if available)
- Full-screen mode support
- Window state persistence

#### Color Scheme
- **Primary**: System blue
- **Success**: System green
- **Warning**: System orange
- **Danger**: System red
- **Staff**: Purple accent
- **Player**: Blue accent
- **Background**: System background with sidebar material

#### Layout
```
┌─────────────────────────────────────────────────────┐
│  [🔍] UO Commander                    [⚙️] [👤]    │  Toolbar
├─────────┬───────────────────────────────────────────┤
│         │                                           │
│  📊     │  Dashboard                                │
│  👥     │  ┌────────────┐  ┌────────────┐          │
│  📋     │  │Server Stats│  │Quick Action│          │
│  🔥     │  └────────────┘  └────────────┘          │
│  📝     │                                           │
│  ⚙️     │  ┌──────────────────────────────────┐    │
│         │  │     Player List / Detail View     │    │
│         │  └──────────────────────────────────┘    │
│ Sidebar │           Main Content Area              │
│         │                                           │
└─────────┴───────────────────────────────────────────┘
```

### Data Models

```swift
struct ServerStatus: Codable {
    let isRunning: Bool
    let uptime: TimeInterval
    let playerCount: Int
    let maxPlayers: Int
    let memoryUsage: Int64
    let cpuUsage: Double
    let worldSaveStatus: String
    let lastSaveTime: Date?
    let version: String
}

struct Player: Codable, Identifiable {
    let serial: Int
    let name: String
    let accessLevel: AccessLevel
    let location: Point3D
    let account: String
    let playtime: TimeInterval
    let isHidden: Bool
    let isSquelched: Bool
    let isJailed: Bool
    let mobileFlags: [String]
}

struct Account: Codable, Identifiable {
    let username: String
    let accessLevel: AccessLevel
    let isBanned: Bool
    let banExpiry: Date?
    let characters: [CharacterSummary]
    let loginIPs: [String]
    let lastLogin: Date?
    let creationDate: Date
    let comments: [AccountComment]
    let tags: [String]
}

struct Item: Codable, Identifiable {
    let serial: Int
    let name: String
    let itemID: Int
    let hue: Int
    let amount: Int
    let properties: [Property]
    let equipped: Bool
}

enum AccessLevel: Int, Codable {
    case player = 0
    case counselor = 1
    case gameMaster = 2
    case seer = 3
    case administrator = 4
    case developer = 5
    case owner = 6
}
```

### API Client

```swift
class UOCommanderAPI {
    private let baseURL: URL
    private var authToken: String?
    
    // Authentication
    func login(username: String, password: String) async throws
    func logout() async throws
    func verifyToken() async -> Bool
    
    // Server Control
    func getServerStatus() async throws -> ServerStatus
    func saveWorld() async throws
    func shutdown(save: Bool) async throws
    func restart(save: Bool, delay: Int) async throws
    func broadcast(message: String, staffOnly: Bool) async throws
    
    // Players
    func getOnlinePlayers() async throws -> [Player]
    func searchPlayers(name: String) async throws -> [Player]
    func getPlayer(serial: Int) async throws -> PlayerDetail
    func kickPlayer(serial: Int, reason: String?) async throws
    func banPlayer(serial: Int, duration: BanDuration?) async throws
    func unbanPlayer(serial: Int) async throws
    func getPlayerEquipment(serial: Int) async throws -> [Item]
    func getPlayerBackpack(serial: Int) async throws -> [Item]
    func getPlayerSkills(serial: Int) async throws -> [Skill]
    func getPlayerProperties(serial: Int) async throws -> [Property]
    
    // Accounts
    func searchAccounts(username: String) async throws -> [Account]
    func getAccount(username: String) async throws -> Account
    func banAccount(username: String) async throws
    func unbanAccount(username: String) async throws
    func setAccessLevel(username: String, level: AccessLevel) async throws
    
    // Firewall
    func getFirewallRules() async throws -> [FirewallRule]
    func addFirewallRule(entry: String) async throws
    func removeFirewallRule(id: String) async throws
}
```

### Key Implementation Details

#### Countdown Timer for Restart
```swift
struct RestartCountdownView: View {
    @State var remainingTime: TimeInterval
    @State var isRunning: Bool
    
    var body: some View {
        VStack {
            // Circular progress indicator
            ZStack {
                Circle()
                    .stroke(Color.gray.opacity(0.3), lineWidth: 20)
                Circle()
                    .trim(from: 0, to: progress)
                    .stroke(Color.orange, style: StrokeStyle(lineWidth: 20, lineCap: .round))
                    .rotationEffect(.degrees(-90))
                
                Text(formatTime(remainingTime))
                    .font(.system(size: 48, weight: .bold, design: .monospaced))
            }
            .frame(width: 200, height: 200)
            
            Text("Server will restart in")
                .font(.title2)
            
            Button("Cancel Restart") {
                cancelRestart()
            }
            .buttonStyle(.borderedProminent)
            .controlSize(.large)
        }
    }
}
```

#### Player Equipment Viewer
```swift
struct EquipmentViewer: View {
    let equipment: [Item]
    
    var body: some View {
        Grid {
            GridRow {
                // Head
                EquipmentSlot(item: equipment.first { $0.layer == .helm })
            }
            GridRow {
                // Left hand, torso, right hand
                EquipmentSlot(item: equipment.first { $0.layer == .oneHanded })
                EquipmentSlot(item: equipment.first { $0.layer == .torso })
                EquipmentSlot(item: equipment.first { $0.layer == .twoHanded })
            }
            // ... more slots
        }
    }
}
```

### Notifications
- Player banned/kicked
- Server shutdown/restart initiated
- Server crashed
- Unusual activity detected
- Auto-save completed

### Keyboard Shortcuts
- `⌘ + S` - Save world
- `⌘ + B` - Broadcast message
- `⌘ + R` - Restart server
- `⌘ + F` - Search players
- `⌘ + 1-7` - Switch tabs
- `⌘ + Q` - Quit (with confirmation if server running)

### Error Handling
- Network errors with retry mechanism
- Authentication errors with re-login prompt
- Server errors with detailed messages
- Offline mode with cached data indicator
- Graceful degradation for missing features

### Testing
- Unit tests for API client
- Mock server responses
- UI tests for critical flows
- Integration tests with test server

### Deployment
- Notarized macOS app
- Sparkle framework for auto-updates
- Minimum macOS 14.0
- Universal binary (Intel + Apple Silicon)

---

## Implementation Phases

### Phase 1: Foundation
1. Create ModernUO HTTP API module
2. Implement authentication endpoints
3. Set up SwiftUI app structure
4. Build login and authentication flow

### Phase 2: Core Features
1. Player list and search
2. Player details and inspection
3. Ban/unban functionality
4. Equipment and backpack viewer

### Phase 3: Server Control
1. Broadcast messages
2. Shutdown/restart with countdown
3. World save
4. Server status dashboard

### Phase 4: Advanced Features
1. Account management
2. Firewall management
3. Logs viewer
4. Real-time updates (WebSocket or polling)

### Phase 5: Polish
1. UI refinements
2. Keyboard shortcuts
3. Notifications
4. Settings and preferences
5. Error handling improvements

---

## Security Considerations

1. **API Authentication**: JWT with short expiry, refresh tokens
2. **Password Storage**: macOS Keychain for client credentials
3. **HTTPS**: Encrypt all API traffic
4. **IP Whitelist**: Optional restriction for API access
5. **Audit Logging**: All admin actions logged with timestamp and actor
6. **Rate Limiting**: Prevent brute force on login
7. **Access Control**: Enforce server access levels on all endpoints
8. **Token Revocation**: Logout invalidates token server-side

---

## Future Enhancements

1. **Real-time Updates**: WebSocket for live player count, chat
2. **Map View**: Visual player location display
3. **Spawner Management**: Edit spawn points
4. **Item Creation**: Spawn items in-game
5. **Multi-Server Support**: Manage multiple ModernUO servers
6. **Chat Integration**: Monitor in-game chat in real-time
7. **Performance Analytics**: Detailed server performance metrics
8. **Backup Management**: Automated backup creation and restoration
9. **Plugin System**: Extensible command system
10. **Mobile App**: iOS companion for on-the-go admin

---

## Dependencies

### Server-Side
- .NET 8+ (already in ModernUO)
- System.Net.HttpListener (built-in)
- System.IdentityModel.Tokens.Jwt (NuGet package)

### Client-Side (macOS App)
- SwiftUI (built-in)
- SwiftData (built-in, macOS 14+)
- LocalAuthentication framework (for Touch ID)
- UserNotifications framework
- No third-party dependencies required

---

## Configuration

### Server-Side (modernuo.json)
```json
{
  "httpApi": {
    "enabled": true,
    "port": 8080,
    "requireHttps": false,
    "jwtExpiryHours": 24,
    "jwtSecret": "<random-secret-key>",
    "allowedIPs": ["127.0.0.1", "192.168.1.0/24"],
    "rateLimit": {
      "loginAttempts": 5,
      "loginWindowMinutes": 15
    },
    "corsOrigins": []
  }
}
```

### Client-Side (App Settings)
- Server URL (with port)
- Auto-connect on launch
- Credential storage preference
- Refresh interval
- Theme preference
- Notification preferences

---

## Notes

- All server operations that modify game state must be thread-safe via EventLoopContext
- The HTTP API runs on a separate port from the game server
- Admin actions are logged via ModernUO's existing ILogger system
- The app should work with ModernUO's existing account and access level system
- No changes to ModernUO's core architecture required - the HTTP API is an additive module
