# Graveyard Orbit Tweaks

Standalone BepInEx plugin with quality-of-life balance changes for
**Graveyard Orbit**. It does not require (and does not conflict with) the
co-op mod.

## Changes

- **Drone prices halved** — Miner / Hauler / Seeker buy prices and every
  upgrade level cost are cut 50%, including the progression multiplier.
- **Drone repair: $100** (was $300).
- **Igniter upgraded to level 3**:
  - Level 2 — 4.5 rocks/s, costs **$200**
  - Level 3 — 6 rocks/s, costs **$350**

The igniter uses the same upgrade flow as the laser and the press (LEVEL UP
card, hold-to-upgrade, tooltip and pad navigation).

## Build & install

```bash
./setup.sh    # portable .NET SDK + BepInEx compile references, project-local
./build.sh
./deploy.sh
```

`deploy.sh` adds BepInEx if missing and copies only the plugin DLL into
`BepInEx/plugins/GraveyardOrbit.Tweaks/`. No game files are modified.

On Linux/Steam Deck (Proton), add the launch option:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## Uninstall

Delete `BepInEx/plugins/GraveyardOrbit.Tweaks/` (plus BepInEx if unused).

## Repository

https://github.com/Grokent/graveyard_orbit_tweaks

## License

The plugin's own code is MIT licensed (see `LICENSE`). The redistributable ZIP
bundles BepInEx, which is LGPL-2.1; see `THIRD_PARTY_NOTICES.md` and
`licenses/LGPL-2.1.txt`. This is an unofficial fan mod; Graveyard Orbit and its
assets belong to Anchorhead Games.
