#!/bin/bash
# =============================================================================
# pack-webportal.sh - Packs the Web Portal project into a portable tar archive
#
# Usage: ./pack-webportal.sh [output_path]
#
# Creates a tar.gz archive containing:
#   - All Web Portal source files
#   - Install script for injecting into an existing ModernUO installation
#   - Manifest of all affected files
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT_PATH="${1:-webportal-inject.tar.gz}"

echo "=== ModernUO Web Portal Packer ==="
echo ""

# Create a temp directory for staging (outside the project tree)
STAGE_DIR="$(mktemp -d /tmp/webportal-stage.XXXXXX)"
trap "rm -rf '$STAGE_DIR'" EXIT

echo "Staging directory: $STAGE_DIR"
echo "Staging files..."

# --- New files (Web Portal project) ---
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/Configuration"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/Endpoints"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/Middleware"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/Models"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/Services"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/wwwroot/css"
mkdir -p "$STAGE_DIR/new/Projects/WebPortal/wwwroot/js"

# Copy with -f to avoid identical file errors on macOS
cp -f "$SCRIPT_DIR/Projects/WebPortal/WebPortal.csproj" \
   "$STAGE_DIR/new/Projects/WebPortal/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/WebPortalHost.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/Configuration/WebPortalConfiguration.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Configuration/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/Endpoints/AuthEndpoints.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Endpoints/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Endpoints/AccountEndpoints.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Endpoints/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Endpoints/ServerEndpoints.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Endpoints/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/Middleware/AccountLockoutService.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Middleware/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Middleware/RateLimitingMiddleware.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Middleware/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Middleware/SecurityHeadersMiddleware.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Middleware/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/Models/Requests.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Models/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Models/Responses.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Models/"

cp -f "$SCRIPT_DIR/Projects/WebPortal/Services/GameThreadDispatcher.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Services/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Services/TokenService.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Services/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Services/AuthService.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Services/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/Services/AccountService.cs" \
   "$STAGE_DIR/new/Projects/WebPortal/Services/"

# Frontend files
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/index.html" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/login.html" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/register.html" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/dashboard.html" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/css/uo-theme.css" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/css/"
cp -f "$SCRIPT_DIR/Projects/WebPortal/wwwroot/js/app.js" \
   "$STAGE_DIR/new/Projects/WebPortal/wwwroot/js/"

# --- Install script ---
cp -f "$SCRIPT_DIR/install-webportal.sh" "$STAGE_DIR/"

# --- Manifest ---
cp -f "$SCRIPT_DIR/WEB_PORTAL_CHANGES.md" "$STAGE_DIR/"

# Create the tar archive
echo "Creating archive: $OUTPUT_PATH"
tar -czf "$OUTPUT_PATH" -C "$STAGE_DIR" .

# Calculate size and file count
SIZE=$(du -h "$OUTPUT_PATH" | cut -f1)
FILES=$(tar -tzf "$OUTPUT_PATH" | wc -l | tr -d ' ')

echo ""
echo "=== Pack Complete ==="
echo "Archive: $OUTPUT_PATH"
echo "Size:    $SIZE"
echo "Files:   $FILES"
echo ""
echo "To install on a Linux machine with ModernUO:"
echo "  1. Copy $OUTPUT_PATH to the target machine"
echo "  2. Extract: tar -xzf $OUTPUT_PATH"
echo "  3. Run:     ./install-webportal.sh /path/to/modernuo"
