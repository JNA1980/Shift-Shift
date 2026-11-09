#!/usr/bin/env bash
# Upload builds to itch.io with butler (playbook 0.4).
# One-time: install butler, run `butler login`, copy tools/itch.env.example to tools/itch.env and fill it in.
# Usage: tools/upload.sh [html|windows|all]   (default: html)
set -euo pipefail
cd "$(dirname "$0")/.."

ENV_FILE=tools/itch.env
[[ -f "$ENV_FILE" ]] || { echo "Missing $ENV_FILE - copy tools/itch.env.example and fill it in." >&2; exit 1; }
ITCH_USER="$(sed -n 's/^ITCH_USER=//p' "$ENV_FILE" | tr -d '\r"' | head -1)"
ITCH_GAME="$(sed -n 's/^ITCH_GAME=//p' "$ENV_FILE" | tr -d '\r"' | head -1)"
ITCH_GAME="${ITCH_GAME:-shift-shift}"
[[ -n "$ITCH_USER" && "$ITCH_USER" != "your-itch-username" ]] || { echo "Set ITCH_USER in $ENV_FILE." >&2; exit 1; }
command -v butler >/dev/null || { echo "butler not found on PATH (https://itch.io/docs/butler/)." >&2; exit 1; }

WHICH="${1:-html}"
VERSION="$(git describe --tags --always --dirty 2>/dev/null || date +%Y%m%d-%H%M)"

push() { # path channel
  echo "Pushing $1 -> $ITCH_USER/$ITCH_GAME:$2 ($VERSION)"
  butler push "$1" "$ITCH_USER/$ITCH_GAME:$2" --userversion "$VERSION"
}

push_html() {
  [[ -f Builds/WebGL/index.html ]] || { echo "No Builds/WebGL/index.html - run Shift+Shift > Build WebGL first." >&2; exit 1; }
  push Builds/WebGL html
}

push_windows() {
  [[ -d Builds/Windows ]] || { echo "No Builds/Windows - run Shift+Shift > Build Windows first." >&2; exit 1; }
  rm -f Builds/ShiftShift-Windows.zip
  (cd Builds/Windows && zip -qr ../ShiftShift-Windows.zip .)
  push Builds/ShiftShift-Windows.zip windows
}

case "$WHICH" in
  html)    push_html ;;
  windows) push_windows ;;
  all)     push_html; push_windows ;;
  *) echo "Usage: $0 [html|windows|all]" >&2; exit 2 ;;
esac
