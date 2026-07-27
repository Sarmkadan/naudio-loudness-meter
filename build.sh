#!/usr/bin/env bash
# Simple build script for the NAudio.LoudnessMeter solution.
# It runs `dotnet build` with the default configuration.

set -euo pipefail

# Change to the directory containing the .sln file (assumed to be the repository root)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Build the solution
dotnet build --configuration Release

echo "Build succeeded."
