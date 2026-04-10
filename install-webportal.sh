#!/bin/bash
# =============================================================================
# install-webportal.sh - Injects the Web Portal into an existing ModernUO install
#
# Usage: ./install-webportal.sh [modernuo_root]
#
# If modernuo_root is not specified, uses the current directory.
# This script should be run AFTER extracting webportal-inject.tar.gz
#
# What it does:
#   1. Verifies this is a ModernUO installation
#   2. Copies the Web Portal project files
#   3. Patches Application.csproj (adds ASP.NET Core + WebPortal reference)
#   4. Patches assemblies.json (adds WebPortal.dll)
#   5. Patches ModernUO.slnx (adds WebPortal project)
#   6. Optionally patches Dockerfile and compose.yml
#   7. Rebuilds the project
# =============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

info()  { echo -e "${BLUE}[INFO]${NC}  $1"; }
ok()    { echo -e "${GREEN}[OK]${NC}    $1"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $1"; }
error() { echo -e "${RED}[ERROR]${NC} $1"; exit 1; }

# Determine ModernUO root directory
MUO_ROOT="${1:-.}"
MUO_ROOT="$(cd "$MUO_ROOT" && pwd)"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo ""
echo "=== ModernUO Web Portal Installer ==="
echo ""

# ---- Step 1: Verify this is a ModernUO installation ----
info "Verifying ModernUO installation at: $MUO_ROOT"

[ -f "$MUO_ROOT/Projects/Application/Application.csproj" ] || error "Application.csproj not found. Is this a ModernUO root directory?"
[ -f "$MUO_ROOT/Projects/Server/Server.csproj" ] || error "Server.csproj not found. Is this a ModernUO root directory?"
[ -f "$MUO_ROOT/Distribution/Data/assemblies.json" ] || error "assemblies.json not found. Is this a ModernUO root directory?"

ok "ModernUO installation verified"

# ---- Step 2: Check if Web Portal is already installed ----
if [ -d "$MUO_ROOT/Projects/WebPortal" ]; then
    warn "Web Portal directory already exists at Projects/WebPortal/"
    read -p "Overwrite? [y/N] " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        error "Aborted. Remove Projects/WebPortal/ manually to reinstall."
    fi
fi

# ---- Step 3: Copy new files ----
info "Copying Web Portal project files..."

if [ -d "$SCRIPT_DIR/new/Projects/WebPortal" ]; then
    # Running from extracted tar archive
    cp -r "$SCRIPT_DIR/new/Projects/WebPortal" "$MUO_ROOT/Projects/WebPortal"
else
    error "Web Portal source files not found. Ensure you extracted the tar archive correctly."
fi

ok "Web Portal project files copied"

# ---- Step 4: Patch Application.csproj ----
info "Patching Application.csproj..."

APP_CSPROJ="$MUO_ROOT/Projects/Application/Application.csproj"

# Check if already patched
if grep -q "WebPortal" "$APP_CSPROJ"; then
    warn "Application.csproj already contains WebPortal reference. Skipping."
else
    # Add FrameworkReference for ASP.NET Core
    if ! grep -q "Microsoft.AspNetCore.App" "$APP_CSPROJ"; then
        sed -i '/<\/PropertyGroup>/a\    <ItemGroup>\n        <FrameworkReference Include="Microsoft.AspNetCore.App" />\n    </ItemGroup>' "$APP_CSPROJ"
    fi

    # Add WebPortal project reference
    if ! grep -q "WebPortal.csproj" "$APP_CSPROJ"; then
        sed -i '/<\/ItemGroup>/a\        <ProjectReference Include="..\\WebPortal\\WebPortal.csproj" Private="false" PrivateAssets="All" IncludeAssets="None">\n            <IncludeInPackage>false</IncludeInPackage>\n        </ProjectReference>' "$APP_CSPROJ"
    fi

    ok "Application.csproj patched"
fi

# ---- Step 5: Patch assemblies.json ----
info "Patching assemblies.json..."

ASSEMBLIES_JSON="$MUO_ROOT/Distribution/Data/assemblies.json"

if grep -q "WebPortal.dll" "$ASSEMBLIES_JSON"; then
    warn "assemblies.json already contains WebPortal.dll. Skipping."
else
    # Add WebPortal.dll to the JSON array
    # Handle both single-line and multi-line formats
    if grep -q 'UOContent.dll"' "$ASSEMBLIES_JSON"; then
        sed -i 's/"UOContent.dll"/"UOContent.dll",\n  "WebPortal.dll"/' "$ASSEMBLIES_JSON"
    else
        # Fallback: add before the closing bracket
        sed -i 's/\]/  "WebPortal.dll"\n]/' "$ASSEMBLIES_JSON"
    fi

    ok "assemblies.json patched"
fi

# ---- Step 6: Patch ModernUO.slnx ----
info "Patching ModernUO.slnx..."

SLNX="$MUO_ROOT/ModernUO.slnx"

if [ -f "$SLNX" ]; then
    if grep -q "WebPortal" "$SLNX"; then
        warn "ModernUO.slnx already contains WebPortal. Skipping."
    else
        # Add WebPortal project after UOContent project line
        sed -i '/UOContent\/UOContent.csproj/a\  <Project Path="Projects/WebPortal/WebPortal.csproj" />' "$SLNX"
        ok "ModernUO.slnx patched"
    fi
else
    warn "ModernUO.slnx not found. Skipping solution file patch."
fi

# ---- Step 7: Optionally patch Dockerfile ----
info "Checking for Dockerfile..."

DOCKERFILE="$MUO_ROOT/Dockerfile"

if [ -f "$DOCKERFILE" ]; then
    DOCKER_PATCHED=false

    # Check if aspnet base image is already used
    if grep -q "aspnet:10.0" "$DOCKERFILE"; then
        warn "Dockerfile already uses aspnet:10.0 base image."
    else
        read -p "Patch Dockerfile to use aspnet:10.0 base image? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            sed -i 's|mcr.microsoft.com/dotnet/runtime:10.0|mcr.microsoft.com/dotnet/aspnet:10.0|' "$DOCKERFILE"
            DOCKER_PATCHED=true
        fi
    fi

    # Check if port 8080 is already exposed
    if grep -q "EXPOSE 8080" "$DOCKERFILE"; then
        warn "Dockerfile already exposes port 8080."
    else
        read -p "Add EXPOSE 8080 to Dockerfile? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            # Add EXPOSE 8080 after the last EXPOSE line
            sed -i '/^EXPOSE/a EXPOSE 8080' "$DOCKERFILE"
            DOCKER_PATCHED=true
        fi
    fi

    if [ "$DOCKER_PATCHED" = true ]; then
        ok "Dockerfile patched"
    fi
else
    warn "No Dockerfile found. Skipping Docker patches."
fi

# ---- Step 8: Optionally patch compose.yml ----
info "Checking for compose.yml..."

COMPOSE_FILE=""
for f in "$MUO_ROOT/compose.yml" "$MUO_ROOT/docker-compose.yml" "$MUO_ROOT/compose.yaml" "$MUO_ROOT/docker-compose.yaml"; do
    if [ -f "$f" ]; then
        COMPOSE_FILE="$f"
        break
    fi
done

if [ -n "$COMPOSE_FILE" ]; then
    if grep -q "8080" "$COMPOSE_FILE"; then
        warn "compose file already contains port 8080. Skipping."
    else
        read -p "Add port 8080:8080 to compose file? [y/N] " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            # Add web portal port after the game server port line
            sed -i '/2593:2593/a\      - "8080:8080"    # Web portal' "$COMPOSE_FILE"
            ok "compose file patched"
        fi
    fi
else
    warn "No compose file found. Skipping compose patches."
fi

# ---- Step 9: Rebuild ----
echo ""
info "All patches applied. You now need to rebuild ModernUO."
echo ""
echo "To rebuild:"
echo "  cd $MUO_ROOT"
echo "  ./publish.sh release linux x64"
echo ""
echo "Or with Docker:"
echo "  docker compose build"
echo ""
echo "The web portal will be available at http://localhost:8080"
echo "The game server will be available at localhost:2593"
echo ""
ok "Web Portal installation complete!"
