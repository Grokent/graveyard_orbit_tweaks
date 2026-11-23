using GraveyardOrbit.UI;
using HarmonyLib;
using UnityEngine;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>
    /// Gives the refinery Igniter (MachineKind.Tagger) two upgrade levels:
    /// lv2 = 4.5 rocks/s for $200, lv3 = 6 rocks/s for $350.
    /// </summary>
    internal static class IgniterPatches
    {
        private static int LevelPrice(Machine m)
        {
            return m != null && m.level < 2 ? 200 : 350;
        }

        private static float Rate(int level)
        {
            if (level >= 3) return 6f;
            if (level == 2) return 4.5f;
            return 3f;
        }

        [HarmonyPatch(typeof(RefineryLine), nameof(RefineryLine.LevelUp))]
        internal static class LevelUpPatch
        {
            private static bool Prefix(Machine m)
            {
                if (m == null || m.kind != MachineKind.Tagger) return true;
                if (m.level >= 3) return true;
                m.level++;
                return false;
            }
        }

        [HarmonyPatch(typeof(RefineryLine), "RateOf")]
        internal static class RatePatch
        {
            private static void Postfix(Machine m, ref float __result)
            {
                if (m != null && m.kind == MachineKind.Tagger)
                {
                    __result = Rate(m.level);
                }
            }
        }

        [HarmonyPatch(typeof(RefineryScreen), "Levels")]
        internal static class LevelsPatch
        {
            private static void Postfix(Machine m, ref bool __result)
            {
                if (m != null && m.kind == MachineKind.Tagger) __result = true;
            }
        }

        [HarmonyPatch(typeof(RefineryScreen), "LevelPrice")]
        internal static class LevelPricePatch
        {
            private static void Postfix(Machine m, ref int __result)
            {
                if (m != null && m.kind == MachineKind.Tagger) __result = LevelPrice(m);
            }
        }

        // The live belt simulation hardcodes the igniter delay as 1/3 s in
        // RefinerySim.Tick; it does not call RefineryLine.RateOf. Retime each
        // igniter that fired this tick so levels 2 and 3 actually run faster.
        [HarmonyPatch(typeof(RefinerySim), "Tick")]
        internal static class SimTickPatch
        {
            private static void Postfix(RefinerySim __instance)
            {
                float now = __instance.Clock;
                var machines = __instance.Machines;
                for (int i = 0; i < machines.Count; i++)
                {
                    RefinerySim.MachineStat stat = machines[i];
                    if (stat?.m != null
                        && stat.m.kind == MachineKind.Tagger
                        && stat.lastFire == now)
                    {
                        stat.ready = now + 1f / Rate(stat.m.level);
                    }
                }
            }
        }

        // The machine card hardcodes "3 ROCKS / S" for the igniter as well, so
        // rebuild that one row from the same rate table used above.
        [HarmonyPatch(typeof(RefineryScreen), "Numbers")]
        internal static class NumbersPatch
        {
            private static void Postfix(RefineryScreen __instance, Machine m, ref string __result)
            {
                if (m == null || m.kind != MachineKind.Tagger) return;

                int haulHits = 0;
                try
                {
                    RefinerySim sim = Traverse.Create(__instance).Field("_sim").GetValue<RefinerySim>();
                    haulHits = sim?.StatOf(m)?.visitHits ?? 0;
                }
                catch
                {
                    // Keep the game's own text if the private field changes.
                    return;
                }

                __result = Loc.F(
                    "station.refinery.num_igniter",
                    "{rate} ROCKS / S · IGNITED {n} THIS HAUL",
                    ("rate", Rate(m.level).ToString("0.#")),
                    ("n", haulHits));
            }
        }

        [HarmonyPatch(typeof(RefineryScreen), nameof(RefineryScreen.State))]
        internal static class LevelCardPatch
        {
            private static void Postfix(RefineryScreen.CardKind kind, RefineryScreen __instance, ref RefineryScreen.CardState __result)
            {
                if (kind != RefineryScreen.CardKind.Level) return;
                Machine m = __instance.Selected;
                if (m == null || m.kind != MachineKind.Tagger) return;

                int cost = LevelPrice(m);
                bool maxed = m.level >= 3;
                int money = GameState.I != null ? GameState.I.Money : 0;

                __result.cost = cost;
                __result.maxed = maxed;
                __result.affordable = !maxed && money >= cost;
                __result.progress = $"LEVEL {m.level} / 3";
                __result.desc = maxed
                    ? ""
                    : (m.level < 2
                        ? "Triggers faster: 4.5 rocks / s."
                        : "Triggers faster: 6 rocks / s.");
            }
        }
    }
}
