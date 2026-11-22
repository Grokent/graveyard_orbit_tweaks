# Third-party notices

This repository contains only the Graveyard Orbit Tweaks plugin's own source
code. No game files or third-party binaries are committed here. The build
output (`dist/`) and project-local toolchain (`tools/dotnet`, `tools/BepInEx`)
are gitignored.

The redistributable ZIP produced by `package.sh` bundles the BepInEx mod loader
and its runtime dependencies:

## BepInEx 5.4.23.6 - LGPL-2.1

- Project: https://github.com/BepInEx/BepInEx
- License: GNU Lesser General Public License v2.1
- Full text: [`licenses/LGPL-2.1.txt`](licenses/LGPL-2.1.txt)
- Source for the shipped binaries: https://github.com/BepInEx/BepInEx/tree/v5.4.23.6

BepInEx is redistributed unmodified as separate DLLs inside the ZIP and is
loaded dynamically at runtime. The corresponding source and build instructions
are available at the link above.

## HarmonyX / 0Harmony - MIT

- Project: https://github.com/BepInEx/HarmonyX
- License: MIT

## MonoMod - MIT

- Project: https://github.com/MonoMod/MonoMod
- License: MIT

## Mono.Cecil - MIT

- Project: https://github.com/jbevain/cecil
- License: MIT

---

This is an unofficial fan mod. Graveyard Orbit and its assets belong to
Anchorhead Games.
