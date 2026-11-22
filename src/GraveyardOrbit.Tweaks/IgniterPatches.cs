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
