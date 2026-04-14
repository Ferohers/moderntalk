# UO Commander - Build Guide

This guide explains how to build and release UO Commander.

---

## Quick Start

### Development Build (for testing)

```bash
cd uo-commander
./build-dev.sh
```

This will build and immediately run the app.

### Release Build (for distribution)

```bash
cd uo-commander
./build-release.sh --with-server
```

This creates a complete release package with both the macOS app and server module.

---

## Build Scripts

### build-dev.sh

**Purpose:** Quick development builds for testing

**Options:**
```bash
./build-dev.sh              # Build and run
./build-dev.sh --build      # Build only (don't run)
./build-dev.sh --clean      # Clean build artifacts, then build
./build-dev.sh --help       # Show help
```

**Use when:**
- Testing changes
- Quick iteration during development
- Checking for compile errors

**Output:**
- Builds to `.build/debug/`
- No app bundle created
- Fast build times

---

### build-release.sh

**Purpose:** Production-ready release builds

**Options:**
```bash
./build-release.sh                  # Build macOS app only
./build-release.sh --with-server    # Build app + package server module
./build-release.sh --help           # Show help
```

**Use when:**
- Creating releases for distribution
- Testing release builds
- Packaging for end users

**Output:**
- `dist/UOCommander.app` - Complete macOS app bundle
- `dist/modernuo-http-api-module/` - Server module (with `--with-server`)
- `dist/uocommander-{version}.zip` - Release archive
- `dist/checksums.txt` - SHA256 checksums

---

## Manual Build Commands

### Development Build

```bash
# Build debug
swift build

# Run
swift run UOCommander

# Build and run in one command
swift run UOCommander
```

### Release Build

```bash
# Build release (optimized)
swift build -c release

# Build app bundle
mkdir -p dist/UOCommander.app/Contents/MacOS
cp .build/release/UOCommander dist/UOCommander.app/Contents/MacOS/
```

### Clean Build

```bash
# Remove all build artifacts
rm -rf .build

# Clean and rebuild
swift package clean
swift build
```

### Generate Xcode Project

```bash
# Generate Xcode project file
swift package generate-xcodeproj

# Open in Xcode
open UOCommander.xcodeproj
```

Then in Xcode:
- Press `⌘+B` to build
- Press `⌘+R` to run
- Select "Any Mac (My Mac)" as destination
- Choose "Release" configuration for optimized builds

---

## Build Requirements

### Minimum Requirements

- **macOS:** 14.0 (Sonoma) or later
- **Swift:** 5.9 or later
- **Xcode:** 15.0 or later (optional, for GUI development)

### Check Your Environment

```bash
# Check Swift version
swift --version

# Check macOS version
sw_vers -productVersion

# Check Xcode version (if installed)
xcodebuild -version
```

### Install Swift/Xcode

**Option 1: Command Line Tools Only**
```bash
xcode-select --install
```

**Option 2: Full Xcode (from App Store)**
1. Open App Store
2. Search for "Xcode"
3. Click "Get" and install
4. Launch Xcode once to complete setup

---

## Build Outputs

### Development Build

```
.build/
└── debug/
    ├── UOCommander              # Executable
    └── ...                       # Build intermediates
```

### Release Build

```
dist/
├── UOCommander.app/             # macOS app bundle
│   └── Contents/
│       ├── MacOS/UOCommander    # Executable
│       ├── Info.plist           # App metadata
│       └── PkgInfo
│
├── modernuo-http-api-module/    # Server module (optional)
│   ├── HttpApiServer.cs
│   ├── JwtHelper.cs
│   ├── setup.sh
│   ├── INSTALL.md
│   └── README.md
│
├── uocommander-1.0.0.zip        # Release archive
└── checksums.txt                # SHA256 checksums
```

---

## Testing Builds

### Test Development Build

```bash
# Quick build and test
./build-dev.sh

# Should launch the app in terminal
# Look for the login window
```

### Test Release Build

```bash
# Build release
./build-release.sh

# Test the app bundle
open dist/UOCommander.app

# Check app bundle integrity
codesign -vv dist/UOCommander.app
```

### Test Server Module

```bash
# Extract server module
cd /path/to/ModernUO
unzip /path/to/uocommander-1.0.0.zip

# Run setup
./dist/modernuo-http-api-module/setup.sh

# Build ModernUO
dotnet build

# Run and check for HTTP API startup message
./ModernUO
# Should see: [HTTP API] Server started on port 8080
```

---

## Troubleshooting Builds

### "swift: command not found"

**Solution:** Install Xcode or Command Line Tools
```bash
xcode-select --install
```

### Build Errors

**Clean and rebuild:**
```bash
swift package clean
rm -rf .build
swift build
```

**Check for syntax errors:**
```bash
# Swift syntax check
swiftc -parse Sources/**/*.swift
```

### "Package.swift not found"

**Solution:** Run build scripts from the `uo-commander` directory
```bash
cd /path/to/uo-commander
./build-dev.sh
```

### App Won't Launch

**Check console for errors:**
```bash
# Run from terminal to see errors
swift run UOCommander
```

**Check macOS compatibility:**
```bash
# Verify minimum OS version
/usr/libexec/PlistBuddy -c "Print :LSMinimumSystemVersion" dist/UOCommander.app/Contents/Info.plist
```

### Codesign Errors (Release Builds)

**For local testing (not distribution):**
```bash
# Ad-hoc sign the app
codesign --force --deep --sign - dist/UOCommander.app
```

**For distribution:**
- Use Xcode's automatic signing
- Configure provisioning profiles
- Use a valid Apple Developer certificate

---

## Advanced Build Options

### Build for Specific Architecture

```bash
# Apple Silicon only
swift build -c release --arch arm64

# Intel only
swift build -c release --arch x86_64

# Universal binary (both architectures)
swift build -c release
```

### Enable Address Sanitizer (Debug Builds)

```bash
# Build with memory error detection
swift build -Xswiftc -sanitize=address
```

### Enable Thread Sanitizer (Debug Builds)

```bash
# Build with data race detection
swift build -Xswiftc -sanitize=thread
```

### Build with Custom Swift Flags

```bash
# Add compilation flags
swift build -c release -Xswiftc -Ounchecked
```

---

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: macos-14
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Select Xcode
      run: sudo xcode-select -s /Applications/Xcode_15.app
    
    - name: Build
      run: |
        cd uo-commander
        swift build -c release
    
    - name: Test
      run: |
        cd uo-commander
        swift test
```

### Azure Pipelines Example

```yaml
trigger:
- main

pool:
  vmImage: 'macOS-14'

steps:
- task: Swift@0
  inputs:
    workingDirectory: 'uo-commander'
    command: 'build'
    configuration: 'release'
```

---

## Version Management

### Update Version Number

Edit these files:
1. `Package.swift` - No version field (uses git tags)
2. `build-release.sh` - Update `VERSION="1.0.0"`
3. `Info.plist` template in `build-release.sh`

### Tag a Release

```bash
# Tag the release
git tag -a v1.0.0 -m "Release v1.0.0"

# Push tag
git push origin v1.0.0

# Verify tag
git tag -l -n1
```

---

## Distribution

### For Local Testing

```bash
# Just open the app
open dist/UOCommander.app
```

### For Team Distribution

```bash
# Share the zip
# Team members extract and run:
unzip uocommander-1.0.0.zip
cd dist
open UOCommander.app
```

### For Public Release

1. Build with `--with-server` flag
2. Test thoroughly
3. Upload zip to GitHub Releases
4. Update README with download link
5. Create release notes

### Notarization (for Public Distribution)

```bash
# Archive for notarization
ditto -c -k --keepParent dist/UOCommander.app uocommander.zip

# Submit to Apple for notarization
xcrun notarytool submit uocommander.zip \
  --apple-id "your@apple.id" \
  --password "app-specific-password" \
  --team-id "TEAM_ID" \
  --wait

# Staple the ticket
xcrun stapler staple dist/UOCommander.app
```

---

## Build Performance

### Typical Build Times

| Build Type | Time | Notes |
|------------|------|-------|
| Debug (incremental) | 5-15s | Fast, for development |
| Debug (clean) | 30-60s | Full rebuild |
| Release | 1-3min | Optimized, slower |
| Universal Release | 2-5min | Both architectures |

### Speed Up Development Builds

```bash
# Build only changed files
swift build

# Use build cache (default behavior)
# Don't run `swift package clean` unless necessary

# Build specific target
swift build --target UOCommander
```

---

## Next Steps

After building:

1. **Test the app** - Run and verify all features work
2. **Install server module** - Follow INSTALL.md in server module
3. **Configure ModernUO** - Enable HTTP API in modernuo.json
4. **Login and test** - Use GameMaster+ account
5. **Distribute** - Share the release archive

---

**Need help?**
- Check the troubleshooting section
- Review the QUICKSTART.md
- Open an issue on GitHub

**Happy building! 🔨**
