#!/usr/bin/env bash
# Build the tweaks plugin with a local .NET SDK (shared with the co-op project).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

DOTNET="$ROOT/tools/dotnet"
if [[ ! -x "$DOTNET/dotnet" ]]; then
  SHARED="$HOME/Projects/agent/Graveyard Orbit MP/tools/dotnet"
  if [[ -x "$SHARED/dotnet" ]]; then
    DOTNET="$SHARED"
  else
    DOTNET="$(command -v dotnet || true)"
  fi
fi

[[ -n "$DOTNET" ]] || { echo "No dotnet SDK found (expected tools/dotnet or the co-op project's tools/dotnet)." >&2; exit 1; }

export DOTNET_ROOT="$DOTNET"
export PATH="$DOTNET:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

dotnet build "$ROOT/src/GraveyardOrbit.Tweaks/GraveyardOrbit.Tweaks.csproj" -c Release "$@"
