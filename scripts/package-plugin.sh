#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="${1:-0.1.0.0}"
CONFIGURATION="${CONFIGURATION:-Release}"
PROJECT_DIR="$ROOT_DIR/Jellyfin.Plugin.Jellystream"
PUBLISH_DIR="$PROJECT_DIR/bin/$CONFIGURATION/net9.0/publish"
DIST_DIR="$ROOT_DIR/dist"
ZIP_NAME="jellystream_${VERSION}.zip"
ZIP_PATH="$DIST_DIR/$ZIP_NAME"

dotnet publish "$ROOT_DIR/Jellyfin.Plugin.Jellystream.sln" -c "$CONFIGURATION"

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

(
  cd "$PUBLISH_DIR"
  zip -r "$ZIP_PATH" .
)

if command -v md5sum >/dev/null 2>&1; then
  md5sum "$ZIP_PATH" | tee "$ZIP_PATH.md5"
else
  md5 -r "$ZIP_PATH" | tee "$ZIP_PATH.md5"
fi

printf 'Created %s\n' "$ZIP_PATH"
