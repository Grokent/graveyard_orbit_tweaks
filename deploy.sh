#!/usr/bin/env bash
# Additively install BepInEx (if missing) and copy the tweaks plugin in.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAME="${GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/Graveyard Orbit}"
BEP_ZIP="$ROOT/tools/BepInEx_win_x64_5.4.23.5.zip"
DLL="$ROOT/src/GraveyardOrbit.Tweaks/bin/Release/GraveyardOrbit.Tweaks.dll"

[[ -d "$GAME" ]] || { echo "Game folder not found: $GAME" >&2; exit 1; }
[[ -f "$DLL" ]] || { echo "Plugin not built. Run ./build.sh first." >&2; exit 1; }

if [[ ! -d "$GAME/BepInEx" ]]; then
  echo "Installing BepInEx 5.4.23.5 (additive)"
  unzip -oq "$BEP_ZIP" -d "$GAME"
fi

DEST="$GAME/BepInEx/plugins/GraveyardOrbit.Tweaks"
mkdir -p "$DEST"
cp -f "$DLL" "$DEST/"

echo "Deployed -> $DEST/GraveyardOrbit.Tweaks.dll"
echo "Logs: $GAME/BepInEx/LogOutput.log"
