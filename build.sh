#!/usr/bin/env bash
# Wrapper build script for the NAudio.LoudnessMeter solution.
# This file exists to satisfy external tooling that expects a build.sh
# in a directory named `sql-index-advisor`. It simply forwards to the
# actual build script located at the repository root.

set -euo pipefail

# Resolve the repository root (the parent directory of this wrapper)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Execute the real build script
"$REPO_ROOT/build.sh"
