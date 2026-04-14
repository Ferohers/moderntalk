# UO Commander - Project Index

This document provides a complete overview of all files created for the UO Commander project.

## 📁 Directory Structure

```
uo-commander/
│
├── 📄 PLAN.md                      # Comprehensive project plan and architecture
├── 📄 README.md                    # Full documentation with API reference
├── 📄 QUICKSTART.md                # Quick start guide for users
├── 📄 INDEX.md                     # This file
│
│  👇 Server-Side (ModernUO HTTP API Module)
│
├── 📄 HttpApiServer.cs             # Main HTTP server implementation
│   ├── Authentication endpoints
│   ├── Server control endpoints
│   ├── Player management endpoints
│   ├── Account management endpoints
│   └── Firewall endpoints
│
├── 📄 JwtHelper.cs                 # JWT token generation and validation
│   ├── HMAC-SHA256 signature
│   ├── Token validation
│   └── Base64URL encoding
│
│  👇 Client-Side (macOS SwiftUI App)
│
├── 📄 Package.swift                # Swift Package Manager configuration
│
└── Sources/
    ├── 📄 UOCommanderApp.swift     # App entry point
    │
    ├── Models/
    │   └── 📄 DataModels.swift     # All data structures
    │       ├── ServerStatus
    │       ├── Player, PlayerDetail
    │       ├── Account
    │       ├── Item, Property, Skill
    │       ├── FirewallRule
    │       └── Request/Response models
    │
    ├── Services/
    │   └── 📄 UOCommanderAPI.swift # API client with all endpoints
    │       ├── Authentication methods
    │       ├── Server control methods
    │       ├── Player management methods
    │       ├── Account management methods
    │       └── Keychain helper
    │
    ├── ViewModels/
    │   └── 📄 AppState.swift       # Central state management
    │       ├── Authentication state
    │       ├── Server status caching
    │       ├── Player operations
    │       ├── Account operations
    │       └── Auto-refresh timer
    │
    └── Views/
        ├── 📄 LoginView.swift          # Login screen
        │   ├── Server URL input
        │   ├── Username/password form
        │   └── Error display
        │
        ├── 📄 MainContentView.swift    # Main app layout
        │   ├── Sidebar navigation
        │   ├── Toolbar with status
        │   └── Tab routing
        │
        ├── 📄 DashboardView.swift      # Dashboard
        │   ├── Server status cards
        │   ├── Quick action buttons
        │   └── Player overview
        │
        ├── 📄 PlayersView.swift        # Player management
        │   ├── Search functionality
        │   ├── Player table
        │   ├── Context menus
        │   └── Ban/Kick confirmations
        │
        ├── 📄 ServerControlView.swift  # Server control
        │   ├── Broadcast dialog
        │   ├── Restart countdown
        │   ├── Shutdown confirmation
        │   └── World save button
        │
        └── 📄 ManagementViews.swift    # Other management views
            ├── AccountsView
            ├── FirewallView
            └── LogsView
```

---

## 🎯 Feature Summary

### ✅ Implemented Features

#### Authentication & Security
- [x] JWT-based authentication
- [x] Username/password login
- [x] Access level validation (GameMaster+)
- [x] Secure token storage (Keychain)
- [x] Token auto-refresh
- [x] Automatic logout on expiry

#### Dashboard
- [x] Server status cards (players, uptime, memory, lockdown)
- [x] Quick action buttons
- [x] Online player overview
- [x] Real-time data refresh

#### Player Management
- [x] Player list with search
- [x] Player detail view (4 tabs)
  - [x] Overview (stats, location, account)
  - [x] Equipment viewer
  - [x] Skills list (sortable)
  - [x] Properties inspector
- [x] Kick player (with confirmation)
- [x] Ban player (with confirmation)
- [x] Status indicators (hidden, jailed, squelched)
- [x] Access level color coding

#### Server Control
- [x] Server status display
- [x] Broadcast to all players
- [x] Staff-only messages
- [x] World save
- [x] Restart with countdown
  - [x] Preset delays (30s, 1m, 2m, 5m)
  - [x] Custom delay input
  - [x] Save before restart toggle
- [x] Shutdown (with/without save)
- [x] Server lockdown view

#### Account Management
- [x] Account search
- [x] Account list with status
- [x] Ban/unban accounts
- [x] Access level display
- [x] Last login display

#### Firewall
- [x] Firewall rule list
- [x] Rule display with metadata
- [x] Add/remove rule UI

#### Logs
- [x] Log viewer with filtering
- [x] Log level selection
- [x] Search functionality
- [x] Color-coded log levels

---

## 🔌 API Endpoints Implemented

### Authentication (3 endpoints)
```
POST   /api/auth/login          - Login with credentials
POST   /api/auth/logout         - Logout
GET    /api/auth/verify         - Verify token
```

### Server Control (6 endpoints)
```
GET    /api/server/status       - Get server status
POST   /api/server/save         - Save world
POST   /api/server/shutdown     - Shutdown server
POST   /api/server/restart      - Restart with delay
POST   /api/server/broadcast    - Broadcast message
POST   /api/server/staff-message - Staff message
```

### Players (10 endpoints)
```
GET    /api/players             - List all players
GET    /api/players/search      - Search players
GET    /api/players/{serial}    - Get player details
POST   /api/players/{serial}/kick    - Kick player
POST   /api/players/{serial}/ban     - Ban player
POST   /api/players/{serial}/unban   - Unban player
GET    /api/players/{serial}/equipment - Get equipment
GET    /api/players/{serial}/backpack  - Get backpack
GET    /api/players/{serial}/skills    - Get skills
GET    /api/players/{serial}/properties - Get properties
```

### Accounts (3 endpoints)
```
GET    /api/accounts/search           - Search accounts
POST   /api/accounts/{username}/ban   - Ban account
POST   /api/accounts/{username}/unban - Unban account
```

### Firewall (1 endpoint)
```
GET    /api/firewall            - List firewall rules
```

**Total: 23 API endpoints**

---

## 🎨 UI Components

### Views (8 screens)
1. **LoginView** - Authentication screen
2. **MainContentView** - App shell with sidebar
3. **DashboardView** - Server overview
4. **PlayersView** - Player management
5. **ServerControlView** - Server operations
6. **AccountsView** - Account management
7. **FirewallView** - Firewall rules
8. **LogsView** - Server logs

### Sub-Views (10+ components)
- PlayerDetailView
- PlayerOverviewTab
- PlayerEquipmentTab
- PlayerSkillsTab
- PlayerPropertiesTab
- ServerStatusCard
- QuickActionButton
- PlayerQuickView
- SearchBar
- BroadcastMessageView
- RestartCountdownView
- ConnectionStatusView
- LogEntryView
- DelayOptionButton

---

## 📊 Data Models

### Core Models
- `ServerStatus` - Server state and metrics
- `Player` - Online player information
- `PlayerDetail` - Detailed player data
- `Account` - Account information
- `Item` - Equipment/backpack items
- `Property` - Item properties
- `Skill` - Player skills
- `FirewallRule` - Firewall entries

### Enums
- `AccessLevel` - Player access levels (0-6)

### Request/Response
- `LoginRequest` / `LoginResponse`
- `BroadcastRequest`
- `MessageResponse`

---

## 🔧 Technical Stack

### Server-Side
- **Language**: C# (.NET 8)
- **Framework**: ModernUO
- **HTTP**: HttpListener (built-in)
- **Auth**: JWT (HMAC-SHA256)
- **JSON**: System.Text.Json
- **Threading**: EventLoopContext (game thread safety)

### Client-Side
- **Language**: Swift 5.9+
- **Framework**: SwiftUI
- **Architecture**: MVVM
- **Networking**: Async/await + URLSession
- **Storage**: SwiftData + Keychain
- **Minimum OS**: macOS 14.0 (Sonoma)
- **Architecture**: Universal (Intel + Apple Silicon)

---

## 🚀 Getting Started

### Quick Start (10 minutes)
1. Install HTTP API module in ModernUO (5 min)
2. Build macOS app (3 min)
3. Login and start administering (2 min)

See **QUICKSTART.md** for detailed instructions.

### Full Documentation
See **README.md** for complete documentation including:
- Architecture diagrams
- API reference
- Configuration options
- Security best practices
- Troubleshooting
- Development guide

---

## 🎯 Design Principles

### Modern macOS Native Look
- ✅ SwiftUI declarative UI
- ✅ SF Symbols for icons
- ✅ Sidebar navigation
- ✅ Native controls (buttons, forms, dialogs)
- ✅ Light/Dark mode support
- ✅ Window state persistence
- ✅ Keyboard shortcuts
- ✅ Touch Bar support (future)

### No Deprecated Technologies
- ✅ No AppKit (unless necessary)
- ✅ No Interface Builder (.xib/.storyboard)
- ✅ No Objective-C
- ✅ No CocoaPods/SwiftPackage old patterns
- ✅ Pure Swift 5.9+ with modern concurrency

---

## 🔮 Future Enhancements

### Phase 2
- [ ] Real-time updates via WebSocket
- [ ] Visual map with player locations
- [ ] Item spawning/creation
- [ ] Spawner management
- [ ] Multi-server support

### Phase 3
- [ ] Live chat monitoring
- [ ] Performance analytics
- [ ] Backup management
- [ ] iOS companion app
- [ ] Plugin/extensibility system

### Phase 4
- [ ] Advanced reporting
- [ ] Player behavior analytics
- [ ] Automated moderation
- [ ] Web dashboard (alternative to macOS app)
- [ ] Mobile app (iOS/Android)

---

## 📝 Notes

### What's Production-Ready
✅ Authentication system
✅ Player management
✅ Server control
✅ Basic account management
✅ Dashboard

### What Needs Enhancement
⚠️ Firewall management (UI ready, API needs expansion)
⚠️ Logs viewer (UI ready, needs streaming API)
⚠️ Item inspection (needs visual asset mapping)
⚠️ Real-time updates (currently 30s polling)

### Security Considerations
⚠️ HTTP only (needs HTTPS for production)
⚠️ No IP whitelist enforcement
⚠️ Rate limiting not implemented
⚠️ Audit logging basic

---

## 🤝 Contributing

To add new features:

1. **Server-Side**: Add endpoint to `HttpApiServer.cs`
2. **Client-Side**: 
   - Add data model to `DataModels.swift`
   - Add API method to `UOCommanderAPI.swift`
   - Add state method to `AppState.swift`
   - Create SwiftUI view

See **README.md** Development section for details.

---

**Project created: April 2026**
**Version: 1.0.0**
**License: MIT**
