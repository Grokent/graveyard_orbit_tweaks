using HarmonyLib;
using System;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>Cut drone buy and upgrade prices in half, and repair to $100.</summary>
    internal static class DronePricePatches
    {
        private static DronePrice Half(DronePrice p)
        {
            int money = (int)Math.Round(p.Money * 0.5, MidpointRounding.AwayFromZero);
            return new DronePrice(Math.Max(1, money), p.Green, p.ShardKey, p.Shards);
        }

        [HarmonyPatch(typeof(DroneEconomy), nameof(DroneEconomy.BuyPriceAt))]
        internal static class BuyHalfPatch
        {
            private static void Postfix(ref DronePrice __result)
            {
                try { __result = Half(__result); } catch { }
            }
        }

        [HarmonyPatch(typeof(DroneEconomy), nameof(DroneEconomy.LevelPriceAt))]
        internal static class LevelHalfPatch
        {
            private static void Postfix(ref DronePrice __result)
            {
                try { __result = Half(__result); } catch { }
            }
        }

        [HarmonyPatch(typeof(DroneEconomy), nameof(DroneEconomy.RepairPriceAt))]
        internal static class RepairPatch
        {
            private static void Postfix(ref DronePrice __result)
            {
                try { __result = new DronePrice(100, __result.Green, __result.ShardKey, __result.Shards); }
                catch { }
            }
        }
    }
}
