# Orbit Tweaks

Balance changes for **Graveyard Orbit**, as a small standalone plugin.

## Installation

**Mod manager (recommended):** install with r2modman or Gale. `BepInEx` is pulled
in automatically.

**Manual:** install `BepInEx-BepInExPack` first, then extract this package into
the game folder (the one containing `GraveyardOrbit.exe`).

**Linux / Steam Proton:** add this launch option once, under
Properties → Launch Options:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## What it changes

| Area | Change |
|---|---|
| Drones | Buy and every upgrade level cost **half**; repair costs **$100** |
| Refinery Igniter | Levels 2 and 3 unlocked: **4.5/s for $200**, then **6/s for $350** |
| Sorters | Each sorter handles **two colours** at no extra cost |
| Scrap | Refunds shards spent on miner colour specifications |
| Shipyard | Adds a **BUY ALL** button in the simple helper view; **shift+click** any upgrade to buy max |

Halving is applied to the final computed price, so progression scaling and JSON
overrides are included.

## Notes

- Independent of `Orbit Coop` and `Orbit Quotas`; install any combination.
- No game files are modified; everything is a runtime Harmony patch.

## Credits

Unofficial fan mod, not affiliated with Anchorhead Games. All rights to
Graveyard Orbit belong to Anchorhead Games. This mod was built with heavy AI
assistance.
