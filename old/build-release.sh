#!/bin/bash
###############################################################################
# UO Commander - Release Build Script
# 
# This script builds a production-ready release of the UO Commander macOS app
# and optionally packages the ModernUO HTTP API module.
#
# Usage:
#   ./build-release.sh                  # Build macOS app only
#   ./build-release.sh --with-server    # Build app + package server module
#   ./build-release.sh --help           # Show help
###############################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
APP_NAME="UOCommander"
BUILD_DIR=".build/release"
OUTPUT_DIR="dist"
VERSION="1.0.0"
MIN_MACOS_VERSION="14.0"

###############################################################################
# Helper Functions
###############################################################################

print_header() {
    echo -e "\n${BLUE}================================================${NC}"
    echo -e "${BLUE}  $1${NC}"
    echo -e "${BLUE}================================================${NC}\n"
}

print_step() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

check_prerequisites() {
    print_header "Checking Prerequisites"
    
    # Check if we're in the right directory
    if [ ! -f "Package.swift" ]; then
        print_error "Package.swift not found. Run this script from the uo-commander directory."
        exit 1
    fi
    
    # Check Swift version
    if ! command -v swift &> /dev/null; then
        print_error "Swift is not installed. Please install Xcode 15+ or Swift 5.9+."
        exit 1
    fi
    
    SWIFT_VERSION=$(swift --version | head -n 1)
    print_step "Swift found: $SWIFT_VERSION"
    
    # Check macOS version
    MACOS_VERSION=$(sw_vers -productVersion)
    print_step "macOS version: $MACOS_VERSION"
    
    # Check if macOS 14+
    MACOS_MAJOR=$(echo $MACOS_VERSION | cut -d. -f1)
    if [ "$MACOS_MAJOR" -lt 14 ]; then
        print_error "macOS 14.0 (Sonoma) or later is required. Found: $MACOS_VERSION"
        exit 1
    fi
    
    print_step "All prerequisites met"
}

###############################################################################
# Build Functions
###############################################################################

clean_build() {
    print_header "Cleaning Build Artifacts"
    
    if [ -d ".build" ]; then
        rm -rf .build
        print_step "Cleaned .build directory"
    fi
    
    if [ -d "$OUTPUT_DIR" ]; then
        rm -rf "$OUTPUT_DIR"
        print_step "Cleaned $OUTPUT_DIR directory"
    fi
}

build_release() {
    print_header "Building Release"
    
    echo "Building $APP_NAME v$VERSION..."
    swift build -c release
    
    print_step "Release build completed successfully"
}

create_app_bundle() {
    print_header "Creating App Bundle"
    
    mkdir -p "$OUTPUT_DIR"
    
    APP_BUNDLE="$OUTPUT_DIR/$APP_NAME.app"
    
    # Create app bundle structure
    mkdir -p "$APP_BUNDLE/Contents/MacOS"
    mkdir -p "$APP_BUNDLE/Contents/Resources"
    
    # Copy executable
    cp "$BUILD_DIR/$APP_NAME" "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
    chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"
    print_step "Copied executable"
    
    # Create Info.plist
    cat > "$APP_BUNDLE/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string></string>
    <key>CFBundleIdentifier</key>
    <string>com.uocommander.macos</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
    <key>CFBundleVersion</key>
    <string>$VERSION</string>
    <key>LSMinimumSystemVersion</key>
    <string>$MIN_MACOS_VERSION</string>
    <key>NSHumanReadableCopyright</key>
    <string>Copyright © 2026 UO Commander. All rights reserved.</string>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSAppleEventsUsageDescription</key>
    <string>UO Commander needs access to manage server connections.</string>
</dict>
</plist>
EOF
    print_step "Created Info.plist"
    
    # Create PkgInfo
    echo -n "APPL????" > "$APP_BUNDLE/Contents/PkgInfo"
    
    print_step "App bundle created: $APP_BUNDLE"
}

package_server_module() {
    print_header "Packaging ModernUO HTTP API Module"
    
    SERVER_PACKAGE="$OUTPUT_DIR/modernuo-http-api-module"
    mkdir -p "$SERVER_PACKAGE"
    
    # Copy server files
    cp HttpApiServer.cs "$SERVER_PACKAGE/"
    cp JwtHelper.cs "$SERVER_PACKAGE/"
    
    # Create installation guide
    cat > "$SERVER_PACKAGE/INSTALL.md" << 'EOF'
# ModernUO HTTP API Module - Installation Guide

## Quick Install (5 minutes)

### 1. Copy Files to ModernUO

```bash
cd /path/to/ModernUO
mkdir -p Projects/Server/HTTP
cp HttpApiServer.cs Projects/Server/HTTP/
cp JwtHelper.cs Projects/Server/HTTP/
```

### 2. Register the Module

Edit `Projects/Server/Main.cs`:

**In `Configure()` method:**
```csharp
HttpApiServer.Configure();
```

**In `EventSink_ServerStarted`:**
```csharp
_ = HttpApiServer.Start();
```

**In `EventSink_Shutdown`:**
```csharp
HttpApiServer.Stop();
```

### 3. Enable in Configuration

Add to `modernuo.json`:
```json
{
  "httpApi": {
    "enabled": true,
    "port": 8080,
    "jwtExpiryHours": 24
  }
}
```

### 4. Build and Run

```bash
dotnet build
./ModernUO
```

You should see: `[HTTP API] Server started on port 8080`

### 5. Test the API

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"YOUR_ADMIN","password":"YOUR_PASSWORD"}'
```

## API Endpoints

See the full API documentation in the UO Commander README.md.

## Security Notes

1. Change the JWT secret in production
2. Use HTTPS for remote access
3. Restrict API access to trusted IPs
4. Monitor logs for unauthorized access attempts

## Troubleshooting

**"Connection refused"**
- Verify HTTP API is enabled in modernuo.json
- Check port number is correct
- Ensure ModernUO server is running

**"Invalid credentials"**
- Account must have GameMaster+ access level
- Check username/password spelling
- Review ModernUO server logs

## Support

For issues or questions:
- Check the UO Commander documentation
- Open an issue on GitHub
- Ask in the ModernUO community
EOF
    
    print_step "Created installation guide"
    
    # Create a simple setup script
    cat > "$SERVER_PACKAGE/setup.sh" << 'EOF'
#!/bin/bash
# Quick setup script for ModernUO HTTP API Module

echo "ModernUO HTTP API Module Setup"
echo "================================"
echo ""

# Check if we're in ModernUO directory
if [ ! -f "ModernUO.slnx" ] && [ ! -f "Directory.Build.props" ]; then
    echo "Error: This script must be run from the ModernUO root directory."
    exit 1
fi

# Create HTTP directory
echo "Creating HTTP module directory..."
mkdir -p Projects/Server/HTTP

# Copy files
echo "Copying module files..."
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cp "$SCRIPT_DIR/HttpApiServer.cs" Projects/Server/HTTP/
cp "$SCRIPT_DIR/JwtHelper.cs" Projects/Server/HTTP/

echo ""
echo "✓ Files copied successfully!"
echo ""
echo "Next steps:"
echo "1. Edit Projects/Server/Main.cs and add:"
echo "   - HttpApiServer.Configure() in Configure()"
echo "   - _ = HttpApiServer.Start() in EventSink_ServerStarted"
echo "   - HttpApiServer.Stop() in EventSink_Shutdown"
echo ""
echo "2. Add to modernuo.json:"
echo '   {"httpApi": {"enabled": true, "port": 8080}}'
echo ""
echo "3. Build and run: dotnet build && ./ModernUO"
EOF
    
    chmod +x "$SERVER_PACKAGE/setup.sh"
    print_step "Created setup script"
    
    # Create README
    cat > "$SERVER_PACKAGE/README.md" << EOF
# ModernUO HTTP API Module v$VERSION

This module provides a REST API for the UO Commander macOS application.

## Contents

- \`HttpApiServer.cs\` - Main HTTP server implementation
- \`JwtHelper.cs\` - JWT authentication helper
- \`setup.sh\` - Quick setup script (optional)
- \`INSTALL.md\` - Installation guide

## Quick Start

Run the setup script:
\`\`\`bash
./setup.sh
\`\`\`

Or follow the manual installation guide in INSTALL.md.

## Features

- 27 API endpoints for complete server management
- JWT-based authentication
- Player management (search, ban, kick, inspect)
- Server control (save, shutdown, restart, broadcast)
- Account management
- Firewall management
- Server lockdown
- Real-time status monitoring

## Requirements

- ModernUO server
- .NET 8+
- GameMaster+ account for API access

## Documentation

See the UO Commander documentation for:
- Complete API reference
- Usage examples
- Security best practices
- Troubleshooting guide
EOF
    
    print_step "Created README"
    print_step "Server module packaged: $SERVER_PACKAGE"
}

create_archive() {
    print_header "Creating Release Archive"
    
    ARCHIVE="$OUTPUT_DIR/uocommander-${VERSION}.zip"
    
    # Create zip archive
    cd "$OUTPUT_DIR"
    zip -r "../$(basename $ARCHIVE)" . -x "*.DS_Store" "._*"
    cd ..
    
    print_step "Archive created: $ARCHIVE"
    
    # Show archive contents
    echo ""
    echo "Archive contents:"
    unzip -l "$ARCHIVE" | tail -n +4 | head -n -2
    echo ""
}

generate_checksums() {
    print_header "Generating Checksums"
    
    cd "$OUTPUT_DIR"
    
    # Generate SHA256 checksums
    if command -v shasum &> /dev/null; then
        shasum -a 256 *.zip > checksums.txt
        print_step "SHA256 checksums generated"
        cat checksums.txt
    fi
    
    cd ..
}

print_release_notes() {
    print_header "Release Notes - v$VERSION"
    
    cat << EOF
UO Commander v$VERSION
======================

What's New:
-----------
✓ Complete macOS administration app for ModernUO servers
✓ 27 API endpoints for comprehensive server management
✓ JWT-based authentication with secure token storage
✓ Real-time server monitoring and status
✓ Player management (search, inspect, ban, kick)
✓ Server control (broadcast, save, shutdown, restart)
✓ Account management and firewall controls
✓ Equipment viewer with intelligent icon mapping
✓ Skills inspector with sortable display
✓ Server lockdown management
✓ Logs viewer with filtering

Components:
-----------
• UOCommander.app - macOS administration client
• ModernUO HTTP API Module - Server-side API (optional)

Requirements:
-------------
• macOS 14.0 (Sonoma) or later
• ModernUO server with HTTP API module installed
• GameMaster+ account for API access

Installation:
-------------
1. Install HTTP API module on your ModernUO server (see modernuo-http-api-module/)
2. Copy UOCommander.app to your Applications folder
3. Launch and login with your admin credentials

Documentation:
--------------
• README.md - Full documentation
• QUICKSTART.md - Quick start guide
• PLAN.md - Architecture and design
• modernuo-http-api-module/INSTALL.md - Server module setup

EOF
}

###############################################################################
# Main Script
###############################################################################

# Parse arguments
WITH_SERVER=false

for arg in "$@"; do
    case $arg in
        --with-server)
            WITH_SERVER=true
            shift
            ;;
        --help|-h)
            echo "UO Commander Release Build Script"
            echo ""
            echo "Usage:"
            echo "  ./build-release.sh [options]"
            echo ""
            echo "Options:"
            echo "  --with-server    Also package the ModernUO HTTP API module"
            echo "  --help, -h       Show this help message"
            echo ""
            echo "Examples:"
            echo "  ./build-release.sh                 # Build app only"
            echo "  ./build-release.sh --with-server   # Build app + server module"
            exit 0
            ;;
        *)
            print_error "Unknown option: $arg"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Main execution
print_header "UO Commander Release Build v$VERSION"

check_prerequisites
clean_build
build_release
create_app_bundle

if [ "$WITH_SERVER" = true ]; then
    package_server_module
fi

create_archive
generate_checksums
print_release_notes

print_header "Build Completed Successfully!"

echo -e "${GREEN}Output directory: $OUTPUT_DIR/${NC}"
echo ""
echo "Next steps:"
echo "1. Test the app: open $OUTPUT_DIR/UOCommander.app"
if [ "$WITH_SERVER" = true ]; then
    echo "2. Install server module: see $OUTPUT_DIR/modernuo-http-api-module/INSTALL.md"
fi
echo "3. Distribute: $OUTPUT_DIR/uocommander-${VERSION}.zip"
echo ""
echo -e "${GREEN}Happy administering! 🎮${NC}"
