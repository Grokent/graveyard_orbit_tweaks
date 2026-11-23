using System.Collections.Generic;
using System.Text;
using GraveyardOrbit.UI;
using HarmonyLib;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>
    /// Refunds the colour shards spent unlocking a Miner's colour specs when
    /// that drone is scrapped. Levels and the purchase price are already
    /// refunded by DroneEconomy.ScrapValue; this adds the missing shards.
    /// </summary>
    internal static class ScrapRefund
    {
        private static readonly List<(string key, int amount)> Refunded = new List<(string, int)>();

        internal static bool HasRefund => Refunded.Count > 0;

        internal static void Clear()
        {
            Refunded.Clear();
        }

        internal static void GrantFor(DroneData d)
        {
            Refunded.Clear();
            if (d == null) return;

            int amount = DroneCatalog.ColourShards;
            if (amount <= 0) return;

            HashSet<string> seen = new HashSet<string>();
            foreach (DroneSpec spec in d.Specs)
            {
                if (!DroneCatalog.IsColourSpec(spec)) continue;

                string key = ColourTokens.KeyOfColour(DroneCatalog.SpecColour(spec));
                if (string.IsNullOrEmpty(key) || !seen.Add(key)) continue;

                ColourTokens.Grant(key, amount);
                Refunded.Add((key, amount));
            }
        }

        internal static string Describe()
        {
            if (Refunded.Count == 0) return null;

            StringBuilder sb = new StringBuilder();
            foreach ((string key, int amount) in Refunded)
            {
                if (sb.Length > 0) sb.Append(" · ");
                sb.Append('+').Append(amount).Append(' ');
                sb.Append(amount == 1 ? ColourTokens.Name(key) : ColourTokens.NamePlural(key));
            }
            return sb.ToString();
        }
    }

    internal static class ScrapRefundPatches
    {
        [HarmonyPatch(typeof(DroneEconomy), nameof(DroneEconomy.Scrap))]
        internal static class ScrapPatch
        {
            private static void Postfix(DroneData d, bool __result)
            {
                ScrapRefund.Clear();
                if (!__result) return;

                ScrapRefund.GrantFor(d);
                if (ScrapRefund.HasRefund)
                {
                    // Scrap() has already saved the money refund; persist the
                    // shard grant as well.
                    GameState.I?.Save();
                }
            }
        }

        [HarmonyPatch(typeof(DroneBayScreen), nameof(DroneBayScreen.PressScrap))]
        internal static class ScrapMessagePatch
        {
            private static void Prefix()
            {
                ScrapRefund.Clear();
            }

            private static void Postfix(DroneBayScreen __instance, bool __result)
            {
                if (!__result || !ScrapRefund.HasRefund) return;

                string shards = ScrapRefund.Describe();
                if (string.IsNullOrEmpty(shards)) return;

                try
                {
                    Traverse state = Traverse.Create(__instance);
                    string message = state.Field("_message").GetValue<string>();
                    if (string.IsNullOrEmpty(message)) return;

                    state.Field("_message").SetValue(message + " · " + shards);
                    __instance.Refresh();
                }
                catch
                {
                    // If the private field changes, keep the game's own message.
                }
            }
        }
    }
}
