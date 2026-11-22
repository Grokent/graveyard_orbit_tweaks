# AGENTS.md — Graveyard Orbit Tweaks

## Purpose

Standalone BepInEx/Harmony balance plugin for **Graveyard Orbit**. Separate from
the co-op mod; both can be installed together.

- **Repo:** https://github.com/Grokent/graveyard_orbit_tweaks (branch `main`)
- **License:** MIT (`LICENSE`); BepInEx LGPL-2.1 notices in `THIRD_PARTY_NOTICES.md`

## Changes implemented

- **Drone prices halved** (buy + every upgrade level) via postfixes on
  `DroneEconomy.BuyPriceAt` and `DroneEconomy.LevelPriceAt`. Halving happens on
  the final computed price, so progression scaling and JSON overrides are
  included.
- **Drone repair: $100** via `DroneEconomy.RepairPriceAt` postfix.
- **Igniter (refinery `MachineKind.Tagger`) levels 2–3**:
  - lv2 = 4.5 rocks/s for **$200**
  - lv3 = 6 rocks/s for **$350**
  - Uses the existing laser/press upgrade flow:
    `RefineryLine.LevelUp` (allow Tagger < 3), `RateOf` (level-scaled),
    `RefineryScreen.Levels` (true for Tagger), `LevelPrice` (200/350), and
    `State(CardKind.Level)` postfix for cost/maxed/affordable/desc.

## Environment

- Linux (CachyOS); game via **Steam/Proton**, AppID **4654120**, Unity
  **6000.5.0f1**, Mono; BepInEx 5.4.23.6 additive.
- Proton launch option: `WINEDLLOVERRIDES="winhttp=n,b" %command%`.
- Game path: `~/.local/share/Steam/steamapps/common/Graveyard Orbit`.
- Reference/decompiled game code for recon lives in the co-op project:
  `~/Projects/agent/Graveyard Orbit MP/decompiled/` (do not redistribute).

## Build / deploy / package

```bash
./setup.sh     # portable .NET 9 SDK + BepInEx compile refs (project-local)
./build.sh
./deploy.sh    # -> BepInEx/plugins/GraveyardOrbit.Tweaks/
./package.sh   # dist/GraveyardOrbit-Tweaks-0.1.0-win64.zip
```

- `build.sh` prefers `tools/dotnet`, then falls back to the co-op project's
  portable SDK.
- No dependency on the co-op plugin; no shared state.

## Gotchas

- `RefineryScreen.CardState` / `CardKind` are **nested public types**
  (`RefineryScreen.CardState`); use the nested names in patch signatures.
- `RateOf`, `Levels`, `LevelPrice` are private — patch by string name; Harmony
  binds by name and `ref __result`. If PatchAll logs an error, a target name
  changed after a game update.
- Igniter rate table is `3 / 4.5 / 6` (levels 1–3); costs are 200 then 350.
- Upgrade purchase money comes from `CardState.cost`, so the `State` postfix is
  what actually charges 200/350.
- Do not use `SetEnabled(false)`-style theme tricks in this plugin; not needed.
- Never modify base game files. All changes are runtime patches.
- Restart testing by killing the Steam launch client, not `pgrep -f
  GraveyardOrbit` (kills the shell).
- Logs: `BepInEx/LogOutput.log`; expect:
  `[Graveyard Orbit Tweaks] Tweaks loaded: ...`.

## Verification checklist

1. Drone Bay: Miner $400, Hauler $600, Seeker $750; upgrade steps halved;
   repair shows $100.
2. Refinery Igniter: LEVEL UP card appears; lv2 costs $200 and rate 4.5,
   lv3 costs $350 and rate 6; card maxes at lv3.
3. Co-exists with the co-op mod (both plugins load, no Harmony errors).

## Working conventions

- Keep patches defensive (try/catch, no exceptions into gameplay).
- Bump `PluginVersion` in `TweaksPlugin.cs` when shipping.
- Commit/push to the repo when asked; keep `dist/`, `tools/`, `bin/obj`
  gitignored.
