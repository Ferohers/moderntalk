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
