#!/usr/bin/env bash
# Upload builds to itch.io with butler. One-time setup: install butler, then `butler login`.
# Usage: ITCH_USER=yourname tools/butler-push.sh [html|windows|all]
set -euo pipefail
cd "$(dirname "$0")/.."

: "${ITCH_USER:?Set ITCH_USER to your itch.io username}"
GAME="${ITCH_GAME:-shift-shift}"
WHICH="${1:-html}"
VERSION="$(git describe --tags --always --dirty 2>/dev/null || date +%Y%m%d-%H%M)"

push() { # dir channel
  if [[ ! -d "$1" ]]; then echo "Missing $1 — build it first (Shift+Shift menu in Unity)." >&2; exit 1; fi
  echo "Pushing $1 -> $ITCH_USER/$GAME:$2 ($VERSION)"
  butler push "$1" "$ITCH_USER/$GAME:$2" --userversion "$VERSION"
}

case "$WHICH" in
  html)    push Builds/WebGL html ;;
  windows) push Builds/Windows windows ;;
  all)     push Builds/WebGL html; push Builds/Windows windows ;;
  *) echo "Usage: $0 [html|windows|all]" >&2; exit 2 ;;
esac
