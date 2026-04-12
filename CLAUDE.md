# ModernTalk — Web Portal Overlay for ModernUO

This repo is an **overlay**, not a fork. It contains only the WebPortal project. The Dockerfile clones upstream ModernUO at build time and injects the WebPortal into it.

## Project Structure

- `Projects/WebPortal/` — The entire WebPortal project (our code)
- `Dockerfile` — Clones ModernUO, injects WebPortal, builds
- `compose.yml` — Docker Compose config (ports 2593 + 8080)
- `configuration/` — Sample configuration files

## WebPortal Architecture

- **Runtime**: ASP.NET Core Kestrel web server running on a background thread
- **Integration**: ModernUO's `AssemblyHandler` auto-discovers `WebPortal.dll` in `Assemblies/` and calls its `Configure()` and `Initialize()` methods
- **Thread safety**: All game state access goes through `GameThreadDispatcher` which dispatches to the single game thread via `Core.LoopContext.Post()`
- **Auth**: JWT tokens in HttpOnly cookies, Argon2 password hashing
- **Frontend**: Static HTML/CSS/JS in `wwwroot/` with retro UO theme

## Code Conventions

- **No `Console.WriteLine`** — use `LogFactory.GetLogger(typeof(MyClass))` → `logger.Information(...)`
- **No concurrency primitives in game code** — the server is single-threaded; `GameThreadDispatcher` bridges web threads to game thread
- **`PascalCase`** for public members, `_camelCase` for private fields
- **Braces required** on all control flow

## Key Files

| File | Purpose |
|------|---------|
| `WebPortalHost.cs` | Entry point — `Configure()` and `Initialize()` called by AssemblyHandler |
| `WebPortalConfiguration.cs` | Reads settings from `modernuo.json` |
| `GameThreadDispatcher.cs` | Dispatches work from web threads to game thread |
| `AuthService.cs` | Login, register, token management |
| `TokenService.cs` | JWT generation and validation |
