# Graveyard Orbit Tweaks

Standalone BepInEx plugin with balance changes for **Graveyard Orbit**:
cheaper drones, cheaper repairs, and an upgrade path for the Igniter. It does
not require the co-op mod, and both can be installed together.

## ⚠️ Disclaimer

This project is **100% vibe coded** and **100% the product of AI use**. It is an
unofficial fan mod. I make **no claims of ownership** over Graveyard Orbit, its
code, assets, names, or anything derived from it. **All credit and all rights
belong to Anchorhead Games.** If you own the game, you are welcome to use this
mod for private play. No game code or game asset files are redistributed here.

## Install (Windows and Linux are the same)

You only need the release zip — no compiler, no setup, no separate Linux build.

1. Download the latest `GraveyardOrbit-Tweaks-*.zip` from the
   [`release/`](release) folder (or the repository's Releases page).
2. Close Graveyard Orbit.
3. Open the Graveyard Orbit game folder:
   - **Windows:** Steam → right-click *Graveyard Orbit* → *Manage* → *Browse local files*
   - **Linux / Steam Deck:** same Steam menu, or
     `~/.local/share/Steam/steamapps/common/Graveyard Orbit`
4. Extract **everything** from the zip into that folder. It only adds files
   (`winhttp.dll`, `doorstop_config.ini`, `.doorstop_version`, `changelog.txt`,
   `BepInEx/`); nothing in the original game is replaced.
5. **Linux / Proton only:** add this launch option once (game → Properties →
   Launch Options). Windows needs no launch options:

   ```
   WINEDLLOVERRIDES="winhttp=n,b" %command%
   ```

6. Launch the game through Steam normally.

If you also use the co-op mod, its files and this plugin's folder coexist:
`BepInEx/plugins/GraveyardOrbit.Coop/` and `BepInEx/plugins/GraveyardOrbit.Tweaks/`.

### Uninstall

Delete `BepInEx/plugins/GraveyardOrbit.Tweaks/`. Remove `winhttp.dll`,
`doorstop_config.ini`, `.doorstop_version`, `changelog.txt` and `BepInEx/` only
if you do not use any other BepInEx mod.

## Changes

### Drone prices and repairs

| | Vanilla | Tweaked |
|---|---:|---:|
| Miner (buy) | $800 | **$400** |
| Hauler (buy) | $1,200 | **$600** |
| Seeker (buy) | $1,500 | **$750** |
| Upgrade steps | $200 / $350 / $550 / $850 / $1,300 | **$100 / $175 / $275 / $425 / $650** |
| Repair | $300 | **$100** |

Every level cost is halved **after** the per-contract progression multiplier is
applied, so later contracts scale from the reduced base too.

**Reasoning:** the purchase, upgrade and repair prices were too high for the
utility the drones actually provide. A single lost Miner cost a four-figure sum
to replace, and a $300 repair made recovering a damaged drone feel worse than
letting it die. Drones are fragile, their work is incremental, and the early
game cannot absorb those costs — the drone bay became a money sink instead of a
progression choice. Halving the buy/upgrade curve and dropping repairs to $100
keeps drones as a real investment without making them a trap.

### Igniter upgrades

The refinery Igniter (internally `MachineKind.Tagger`) now has two upgrade
levels, using the same upgrade flow as the laser and the press (LEVEL UP card,
hold-to-upgrade, tooltip and pad navigation):

| Igniter level | Rate | Upgrade cost |
|---|---|---:|
| 1 (vanilla) | 3 rocks/s | — |
| 2 | 4.5 rocks/s | **$200** |
| 3 | 6 rocks/s | **$350** |

**Reasoning:** the press already has a two-step upgrade curve at **$180** and
**$300**, while the igniter stopped dead at level 1. Pricing its upgrades at
**$200 / $350** puts it in line with the press — the same shape of cost for the
same kind of throughput gain — and gives the igniter a progression path instead
of being a one-and-done purchase. The card maxes at level 3.

### Future tweaks

- Add an upgrade path for the **Kickback** (internally `MachineKind.Rebate`), the
  only refinery machine with no progression today.
- Further cost tweaks as more of the economy is played through.

## Logs and settings

- Log: `BepInEx/LogOutput.log` (look for `[Graveyard Orbit Tweaks] Tweaks loaded`)
- BepInEx config: `BepInEx/config/`

## Build from source (developers)

Requires a legitimate copy of the game, plus `unzip`, `curl` and Python 3.

```bash
./setup.sh      # project-local .NET SDK + BepInEx compile references
./build.sh      # builds the plugin
./deploy.sh     # additively installs BepInEx and copies the plugin
./package.sh    # rebuilds release/GraveyardOrbit-Tweaks-<version>.zip
```

Set `GAME_DIR` if the game is not in the default Steam library. Everything is
installed project-locally under `tools/`; nothing is installed system-wide.

`build.sh` prefers `tools/dotnet` and falls back to the co-op project's portable
SDK. Reference game assemblies are read from the installed game and are never
committed.

## Layout

```
src/GraveyardOrbit.Tweaks/   plugin source
release/                     redistributable install zip
licenses/                    third-party license texts shipped in the zip
```

## License and credits

The plugin's own code is MIT licensed (see `LICENSE`). The release zip bundles
BepInEx, which is LGPL-2.1 (see `THIRD_PARTY_NOTICES.md` and
`licenses/LGPL-2.1.txt`).

Graveyard Orbit and all related content are © Anchorhead Games. This is an
unofficial, fan-made, AI-generated mod with no affiliation with or endorsement
from Anchorhead Games. All rights to the game and its assets remain with them.

Repository: <https://github.com/Grokent/graveyard_orbit_tweaks>
