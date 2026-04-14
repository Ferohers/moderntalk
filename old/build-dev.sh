#!/bin/bash
###############################################################################
# UO Commander - Development Build Script
# 
# Quick build for testing and development
#
# Usage:
#   ./build-dev.sh          # Build and run
#   ./build-dev.sh --build  # Build only
#   ./build-dev.sh --clean  # Clean and build
###############################################################################

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${BLUE}UO Commander - Development Build${NC}"
echo "=================================="
echo ""

# Parse arguments
BUILD_ONLY=false
CLEAN=false

for arg in "$@"; do
    case $arg in
        --build)
            BUILD_ONLY=true
            ;;
        --clean)
            CLEAN=true
            ;;
        --help|-h)
            echo "Usage:"
            echo "  ./build-dev.sh          # Build and run"
            echo "  ./build-dev.sh --build  # Build only"
            echo "  ./build-dev.sh --clean  # Clean and build"
            exit 0
            ;;
    esac
done

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo -e "${YELLOW}Cleaning build artifacts...${NC}"
    rm -rf .build
    echo ""
fi

# Build
echo -e "${GREEN}Building...${NC}"
swift build

echo ""
echo -e "${GREEN}✓ Build successful!${NC}"
echo ""

# Run unless build-only mode
if [ "$BUILD_ONLY" = false ]; then
    echo -e "${GREEN}Running UO Commander...${NC}"
    echo ""
    swift run UOCommander
fi
