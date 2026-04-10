# Web Portal - Affected Files List

This document lists all files that were **created** or **modified** to add the Web Portal feature to ModernUO.
This is intended to help with merging changes when updating from the upstream ModernUO repository.

> **Repository**: https://github.com/Ferohers/moderntalk.git
> **Date**: 2026-04-10

---

## New Files (Web Portal Only)

These files are entirely new and do not exist in the upstream ModernUO repository. They can be safely added during a merge without conflict.

### Project Configuration
| File | Description |
|------|-------------|
| `Projects/WebPortal/WebPortal.csproj` | ASP.NET Core Minimal API project file with JWT NuGet package, Server/UOContent references, wwwroot copy targets |

### Core Integration
| File | Description |
|------|-------------|
| `Projects/WebPortal/WebPortalHost.cs` | Entry point with `Configure()`/`Initialize()` static methods auto-discovered by ModernUO's assembly handler. Starts Kestrel on a background thread. |

### Configuration
| File | Description |
|------|-------------|
| `Projects/WebPortal/Configuration/WebPortalConfiguration.cs` | Reads all settings from `ServerConfiguration.GetOrUpdateSetting()` (port, JWT secret, rate limits, lockout, server name, connection info) |

### Services
| File | Description |
|------|-------------|
| `Projects/WebPortal/Services/GameThreadDispatcher.cs` | Core dispatch mechanism using `Core.LoopContext.Post()` with `TaskCompletionSource<T>` to safely access game state from web threads |
| `Projects/WebPortal/Services/TokenService.cs` | JWT generation/validation using HMAC-SHA256, refresh token management with `ConcurrentDictionary` |
| `Projects/WebPortal/Services/AuthService.cs` | Registration, login, logout, refresh. Uses `GameThreadDispatcher.Enqueue()` for all game state access. Anti-enumeration (generic error messages) |
| `Projects/WebPortal/Services/AccountService.cs` | Account info retrieval and password change via `Account.CheckPassword()`/`Account.SetPassword()` (Argon2) |

### Middleware
| File | Description |
|------|-------------|
| `Projects/WebPortal/Middleware/AccountLockoutService.cs` | Progressive backoff lockout (5→15min, 10→1hr, 15→24hr) with `ConcurrentDictionary` + per-entry locking |
| `Projects/WebPortal/Middleware/RateLimitingMiddleware.cs` | Per-IP sliding window rate limiting: 5/min auth, 60/min general, 3/hour registration |
| `Projects/WebPortal/Middleware/SecurityHeadersMiddleware.cs` | CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy headers |

### API Endpoints
| File | Description |
|------|-------------|
| `Projects/WebPortal/Endpoints/AuthEndpoints.cs` | POST `/api/auth/register`, `/login`, `/refresh`, `/logout` with HttpOnly Secure SameSite=Strict cookies |
| `Projects/WebPortal/Endpoints/AccountEndpoints.cs` | GET `/api/account/info`, POST `/api/account/change-password` (require authorization) |
| `Projects/WebPortal/Endpoints/ServerEndpoints.cs` | GET `/api/server/info` (public - server name, player count, client version, connection info) |

### Models
| File | Description |
|------|-------------|
| `Projects/WebPortal/Models/Requests.cs` | `RegisterRequest`, `LoginRequest`, `ChangePasswordRequest`, `RefreshRequest` with data annotations |
| `Projects/WebPortal/Models/Responses.cs` | `AuthResponse`, `AccountInfoResponse`, `ServerInfoResponse`, `ErrorResponse`, `SuccessResponse` |

### Frontend
| File | Description |
|------|-------------|
| `Projects/WebPortal/wwwroot/index.html` | Welcome page with "How to Connect" guide (primary), server status badge, sign up/login links (secondary) |
| `Projects/WebPortal/wwwroot/login.html` | Login page with username/password fields |
| `Projects/WebPortal/wwwroot/register.html` | Registration page with password strength indicator, username validation hints |
| `Projects/WebPortal/wwwroot/dashboard.html` | Account dashboard with status, character count, change password form |
| `Projects/WebPortal/wwwroot/css/uo-theme.css` | Retro UO theme with dark stone textures, gold accents (#C8A848), Cinzel + Crimson Text fonts, ornate borders |
| `Projects/WebPortal/wwwroot/js/app.js` | API client with automatic token refresh on 401, rate limit handling |

### Planning
| File | Description |
|------|-------------|
| `plans/web-portal-plan.md` | Detailed architecture plan document covering all design decisions, API endpoints, security design, frontend design |

---

## Modified Files (Existing ModernUO Files)

These files already exist in the upstream ModernUO repository and were **modified** to support the Web Portal. These may cause merge conflicts when updating from upstream.

### Project Configuration
| File | Change | Merge Risk |
|------|--------|------------|
| `Projects/Application/Application.csproj` | Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` and WebPortal project reference | **Medium** - If upstream adds new project references, may conflict in the `<ItemGroup>` |
| `ModernUO.slnx` | Added `<Project Path="Projects/WebPortal/WebPortal.csproj" />` | **Low** - If upstream adds new projects, may conflict |
| `Distribution/Data/assemblies.json` | Added `"WebPortal.dll"` to the assemblies array | **Low** - If upstream modifies this file, may conflict |

### Docker / Deployment
| File | Change | Merge Risk |
|------|--------|------------|
| `Dockerfile` | Changed `git clone` URL to `https://github.com/Ferohers/moderntalk.git`, changed base image from `runtime:10.0` to `aspnet:10.0`, added `EXPOSE 8080` | **High** - If upstream modifies the Dockerfile, significant merge conflict likely |
| `compose.yml` | Added port `8080:8080` for web portal | **Medium** - If upstream modifies compose.yml, may conflict |
| `.dockerignore` | Created new file to exclude bin/obj, IDE files, docs from Docker build context | **Low** - New file, no upstream equivalent |

---

## Merge Strategy

### When updating from upstream ModernUO:

1. **New files** (entire `Projects/WebPortal/` directory, `plans/`, `.dockerignore`): These will not conflict. Simply ensure they are preserved during the merge.

2. **`Projects/Application/Application.csproj`**: The key additions are:
   - `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in a new `<ItemGroup>`
   - `<ProjectReference Include="..\WebPortal\WebPortal.csproj" .../>` in the existing `<ItemGroup>`
   
   If there's a conflict, re-add these two lines to the merged file.

3. **`ModernUO.slnx`**: The key addition is:
   - `<Project Path="Projects/WebPortal/WebPortal.csproj" />`
   
   If there's a conflict, re-add this line to the merged file.

4. **`Distribution/Data/assemblies.json`**: The key addition is:
   - `"WebPortal.dll"` entry in the JSON array
   
   If there's a conflict, ensure `"WebPortal.dll"` remains in the array alongside any upstream additions.

5. **`Dockerfile`**: The key changes are:
   - `git clone https://github.com/Ferohers/moderntalk.git` instead of the upstream ModernUO repo (line 17)
   - `aspnet:10.0` instead of `runtime:10.0` (line 26)
   - `EXPOSE 8080` added (line 44)
   
   This is the highest-risk file for merge conflicts. If upstream modifies the Dockerfile significantly, you may need to manually re-apply these three changes.

6. **`compose.yml`**: The key addition is:
   - `"8080:8080"` port mapping with `# Web portal` comment
   
   If there's a conflict, re-add this port mapping line.

---

## Configuration Reference

The Web Portal reads these settings from ModernUO's `modernuo.json` configuration (auto-created with defaults on first run):

| Key | Default | Description |
|-----|---------|-------------|
| `webPortal.enabled` | `true` | Enable/disable the web portal |
| `webPortal.port` | `8080` | Port for the web portal HTTP listener |
| `webPortal.jwtSecret` | *(auto-generated)* | HMAC-SHA256 key for JWT tokens. Auto-generates a 256-bit random key if not set. |
| `webPortal.maxLoginAttemptsPerMinute` | `5` | Max login attempts per minute per IP |
| `webPortal.accountLockoutMinutes` | `15` | Base lockout duration in minutes (progressive: 5→15min, 10→1hr, 15→24hr) |
| `webPortal.accessTokenExpiryMinutes` | `15` | Access token (JWT) expiry in minutes |
| `webPortal.refreshTokenExpiryDays` | `7` | Refresh token expiry in days |
| `webPortal.connectionHost` | `localhost` | Host shown in "How to Connect" guide |
| `webPortal.connectionPort` | `2593` | Port shown in "How to Connect" guide |
| `server.name` | `ModernUO` | Server name displayed on the welcome page |
