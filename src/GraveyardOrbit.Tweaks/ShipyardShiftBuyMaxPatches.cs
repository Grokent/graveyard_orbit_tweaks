using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardOrbit.Audio;
using GraveyardOrbit.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>
    /// Shift+click "buy max" for shipyard upgrades.
    ///
    /// Works on three surfaces:
    ///  - the hex ship grid (shift+click a placed part),
    ///  - the advanced upgrade keys (shift+click the LEVEL UP / UPGRADE key),
    ///  - the part info card's level-up row (shift+click the row).
    ///
    /// The reactor is treated as the magnet/boost engine: a shift+click buys
    /// the cheapest affordable core level across both tracks until neither is
    /// affordable. Other parts buy every level that is currently legal and
    /// affordable, stopping at a max level, a fit/energy block, or the wallet.
    /// </summary>
    internal static class ShipyardShiftBuy
    {
        internal static string Hint =>
            Loc.T("tweaks.shipyard.shift_buy_max", "SHIFT+CLICK TO BUY MAX");

        internal static bool ShiftHeld
        {
            get
            {
                Keyboard keyboard = Keyboard.current;
                return keyboard != null &&
                    (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            }
        }

        internal static bool CanBuyMax(GameState gs, AccretionShip.PlacedPart part)
        {
            if (gs == null || part == null) return false;
            if (part.type == AccretionShip.ReactorType) return CanBuyReactor(gs);
            return PartLevels.CanBuy(gs, part);
        }

        private static bool CanBuyReactor(GameState gs)
        {
            AccretionShip ship = gs?.Ship;
            if (ship == null) return false;
            return PartLevels.CanBuyReactorUpgrade(gs, magnet: true, ship.magnetLevel) ||
                   PartLevels.CanBuyReactorUpgrade(gs, magnet: false, ship.boostLevel);
        }

        internal static bool BuyMax(GameState gs, AccretionShip.PlacedPart part)
        {
            if (gs == null || part == null) return false;
            if (part.type == AccretionShip.ReactorType) return BuyMaxReactor(gs);
            return BuyMaxPart(gs, part);
        }

        private static bool BuyMaxPart(GameState gs, AccretionShip.PlacedPart part)
        {
            bool bought = false;
            // 15 ladder levels + a little slack; flat parts stop at 5.
            for (int guard = 0; guard < 18 && PartLevels.CanBuy(gs, part); guard++)
            {
                if (!PartLevels.Buy(gs, part)) break;
                bought = true;
            }
            return bought;
        }

        private static bool BuyMaxReactor(GameState gs)
        {
            AccretionShip ship = gs?.Ship;
            if (ship == null) return false;

            bool bought = false;
            // Both tracks are five levels each; alternate on cheapest-next.
            for (int guard = 0; guard < 12; guard++)
            {
                bool magnetOk = PartLevels.CanBuyReactorUpgrade(gs, magnet: true, ship.magnetLevel);
                bool boostOk = PartLevels.CanBuyReactorUpgrade(gs, magnet: false, ship.boostLevel);
                if (!magnetOk && !boostOk) break;

                bool magnet;
                if (magnetOk && boostOk)
                {
                    int magnetCost = PartLevels.Cost(PartLevels.ReactorModel, ship.magnetLevel);
                    int boostCost = PartLevels.Cost(PartLevels.ReactorModel, ship.boostLevel);
                    magnet = magnetCost <= boostCost;
                }
                else
                {
                    magnet = magnetOk;
                }

                if (!PartLevels.BuyReactorUpgrade(gs, magnet)) break;
                bought = true;
            }
            return bought;
        }

        internal static void Apply(ShipyardAccretionScreen screen, bool bought)
        {
            if (!bought) return;
            if (Application.isPlaying)
            {
                Sfx.Play("buy");
            }
            screen.ProbeRefresh();
            try
            {
                // Keep the hovered info card in step with the new level.
                Traverse.Create(screen).Method("RefreshPartCardForWallet").GetValue();
            }
            catch
            {
                // Older/newer builds may not have the helper; harmless.
            }
        }
    }

    /// <summary>
    /// Shift+click an advanced upgrade key. Buying through BuyUpgrade covers
    /// weapons, cargo and drives; BuyCoreUpgrade covers magnet/boost.
    /// </summary>
    internal static class ShipyardShiftBuyKeyPatches
    {
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "BuyUpgrade")]
        internal static class BuyUpgradePatch
        {
            private static bool Prefix(ShipyardAccretionScreen __instance, List<int> uids)
            {
                if (!ShipyardShiftBuy.ShiftHeld) return true;
                try
                {
                    GameState gs = GameState.I;
                    AccretionShip ship = gs?.Ship;
                    if (gs == null || ship == null) return true;

                    bool bought = false;
                    foreach (int uid in uids)
                    {
                        AccretionShip.PlacedPart part =
                            ship.placed.FirstOrDefault(p => p.uid == uid);
                        if (part == null) continue;
                        if (ShipyardShiftBuy.BuyMax(gs, part)) bought = true;
                    }

                    if (!bought) return true;
                    ShipyardShiftBuy.Apply(__instance, bought: true);
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(ShipyardAccretionScreen), "BuyCoreUpgrade")]
        internal static class BuyCoreUpgradePatch
        {
            private static bool Prefix(ShipyardAccretionScreen __instance, bool magnet)
            {
                if (!ShipyardShiftBuy.ShiftHeld) return true;
                try
                {
                    GameState gs = GameState.I;
                    if (gs?.Ship == null) return true;

                    bool bought = false;
                    for (int guard = 0; guard < 6; guard++)
                    {
                        if (!PartLevels.BuyReactorUpgrade(gs, magnet)) break;
                        bought = true;
                    }

                    if (!bought) return true;
                    ShipyardShiftBuy.Apply(__instance, bought: true);
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// Shift+click a placed part on the hex grid. Vanilla shift+click is
    /// multi-select, so this only takes over when the part actually has
    /// something to buy; maxed/blocked parts still multi-select as before.
    /// Shift+drag and marquee selection are untouched.
    /// </summary>
    internal static class ShipyardShiftBuyGridPatches
    {
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "ClickAction")]
        internal static class ClickActionPatch
        {
            private static bool Prefix(ShipyardAccretionScreen __instance)
            {
                try
                {
                    object down = Traverse.Create(__instance).Field("_down").GetValue();
                    if (down == null) return true;

                    Traverse t = Traverse.Create(down);
                    string kind = t.Field("kind").GetValue<string>();
                    bool shift = t.Field("shift").GetValue<bool>();
                    int uid = t.Field("uid").GetValue<int>();
                    if (!shift || kind != "ship") return true;

                    GameState gs = GameState.I;
                    AccretionShip ship = gs?.Ship;
                    if (ship == null || ship.pick != null) return true;
                    AccretionShip.PlacedPart part =
                        ship.placed.FirstOrDefault(p => p.uid == uid);
                    if (part == null || !ShipyardShiftBuy.CanBuyMax(gs, part)) return true;

                    if (ShipyardShiftBuy.BuyMax(gs, part))
                    {
                        // Make the bought part the selection so the upgrade
                        // column and its card describe what just changed.
                        // The reactor has no part card and selecting it would
                        // hide the magnet/boost core cards, so clear instead.
                        try
                        {
                            if (part.type == AccretionShip.ReactorType)
                            {
                                __instance.ClearSelection();
                            }
                            else
                            {
                                __instance.SelectPartsForUpgrades(new List<int> { uid });
                            }
                        }
                        catch
                        {
                            // Selection is a nicety; the purchase already landed.
                        }
                        ShipyardShiftBuy.Apply(__instance, bought: true);
                        return false;
                    }
                    return true;
                }
                catch
                {
                    return true;
                }
            }
        }
    }

    /// <summary>
    /// Adds the shortcut to the part info card. The level-up row is also made
    /// shift-clickable; it routes to the same buy-max helper.
    /// </summary>
    internal static class ShipyardShiftBuyCardPatches
    {
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "ShowPartCard")]
        internal static class ShowPartCardPatch
        {
            private static void Postfix(
                ShipyardAccretionScreen __instance,
                AccretionShip.PlacedPart p)
            {
                try
                {
                    if (PadNav.Active || p == null) return;
                    if (p.type == AccretionShip.ReactorType) return;

                    GameState gs = GameState.I;
                    if (gs == null || !ShipyardShiftBuy.CanBuyMax(gs, p)) return;

                    TipCardKit.Card card =
                        Traverse.Create(__instance).Field("_card").GetValue<TipCardKit.Card>();
                    if (card?.rows == null || card.rows.Length == 0) return;

                    TipCardKit.Row row = card.rows[0];
                    if (row?.root == null ||
                        row.root.style.display == DisplayStyle.None) return;

                    row.sub.text += " · " + ShipyardShiftBuy.Hint;
                }
                catch
                {
                    // A UI change must not break the card.
                }
            }
        }

        /// <summary>
        /// The card sits ~28px from the part, so moving the pointer from the
        /// part to the card normally hides it before the row can be clicked.
        /// Hold Shift and the part card stays put; release Shift and normal
        /// hover-hide resumes.
        /// </summary>
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "HideCard")]
        internal static class HideCardPatch
        {
            private static bool Prefix(ShipyardAccretionScreen __instance)
            {
                if (!ShipyardShiftBuy.ShiftHeld) return true;
                try
                {
                    if (Traverse.Create(__instance).Field("_selecting").GetValue<bool>())
                    {
                        return true;
                    }
                    if (Traverse.Create(__instance).Field("_drag").GetValue() != null)
                    {
                        return true;
                    }
                    int uid = Traverse.Create(__instance)
                        .Field("_cardUid").GetValue<int>();
                    string shop = Traverse.Create(__instance)
                        .Field("_cardShopId").GetValue<string>();
                    // Keep only a real part card; shop/blocker cards still hide.
                    return uid < 0 || shop != null;
                }
                catch
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(ShipyardAccretionScreen), "BuildCard")]
        internal static class BuildCardPatch
        {
            private static void Postfix(ShipyardAccretionScreen __instance)
            {
                try
                {
                    TipCardKit.Card card =
                        Traverse.Create(__instance).Field("_card").GetValue<TipCardKit.Card>();
                    if (card?.rows == null || card.rows.Length == 0) return;

                    VisualElement row = card.rows[0]?.root;
                    if (row == null) return;

                    // TipCardKit builds the card click-through so it does not
                    // block the grid. Only make the card/row pickable while
                    // Shift is held, which is exactly when the row is usable.
                    card.root.pickingMode = PickingMode.Ignore;
                    row.pickingMode = PickingMode.Ignore;
                    card.root.schedule.Execute((Action)delegate
                    {
                        try
                        {
                            bool live = ShipyardShiftBuy.ShiftHeld &&
                                card.root.style.display != DisplayStyle.None &&
                                row.panel != null;
                            PickingMode mode = live
                                ? PickingMode.Position
                                : PickingMode.Ignore;
                            if (card.root.pickingMode != mode)
                            {
                                card.root.pickingMode = mode;
                            }
                            if (row.pickingMode != mode)
                            {
                                row.pickingMode = mode;
                            }
                        }
                        catch
                        {
                            // Leave the card click-through on any surprise.
                        }
                    }).Every(50L);
                    row.RegisterCallback<ClickEvent>(delegate(ClickEvent evt)
                    {
                        if (!evt.shiftKey) return;
                        try
                        {
                            int uid = Traverse.Create(__instance)
                                .Field("_cardUid").GetValue<int>();
                            string shop = Traverse.Create(__instance)
                                .Field("_cardShopId").GetValue<string>();
                            if (uid < 0 || shop != null) return;

                            GameState gs = GameState.I;
                            AccretionShip ship = gs?.Ship;
                            AccretionShip.PlacedPart part =
                                ship?.placed.FirstOrDefault(q => q.uid == uid);
                            // The reactor card has no level row; use its core keys.
                            if (part == null ||
                                part.type == AccretionShip.ReactorType ||
                                !ShipyardShiftBuy.CanBuyMax(gs, part)) return;

                            evt.StopPropagation();
                            ShipyardShiftBuy.Apply(
                                __instance,
                                ShipyardShiftBuy.BuyMax(gs, part));
                        }
                        catch
                        {
                            // Ignore a malformed click.
                        }
                    });
                }
                catch
                {
                    // BuildCard changing shape must not break the shipyard.
                }
            }
        }
    }

    /// <summary>
    /// The magnet/boost engine has no part info card; put the shortcut on its
    /// two core upgrade keys instead.
    /// </summary>
    internal static class ShipyardShiftBuyCoreKeyPatches
    {
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "UpKey")]
        internal static class UpKeyPatch
        {
            private static void Postfix(VisualElement __result, object[] __args)
            {
                try
                {
                    if (__result == null || __args == null || __args.Length < 2) return;
                    if (!Traverse.Create(__args[0]).Field("affordable").GetValue<bool>()) return;
                    if (!Traverse.Create(__args[1]).Property("IsCore").GetValue<bool>()) return;
                    if (__result.Q<Label>("shift-buy-hint") != null) return;

                    Label hint = MetaUi.Text(
                        ShipyardShiftBuy.Hint,
                        MetaUi.Face.Display,
                        11f,
                        MetaUi.Fade(MetaUi.Ink, 0.65f),
                        1f);
                    hint.name = "shift-buy-hint";
                    hint.pickingMode = PickingMode.Ignore;
                    hint.style.unityTextAlign = TextAnchor.MiddleCenter;
                    hint.style.marginTop = 2f;
                    __result.Add(hint);
                    __result.style.height = StyleKeyword.Auto;
                    __result.style.minHeight = 49f;
                    __result.style.paddingTop = 6f;
                    __result.style.paddingBottom = 8f;
                }
                catch
                {
                    // Ignore: the core key still works without the hint.
                }
            }
        }
    }
}
