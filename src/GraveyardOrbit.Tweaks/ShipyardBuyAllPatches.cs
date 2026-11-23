using System;
using System.Collections.Generic;
using System.Linq;
using GraveyardOrbit.Audio;
using GraveyardOrbit.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>
    /// Buys as many shipyard upgrades as the current money and energy allow.
    /// Placed parts are bought lowest-cost-first. When a tier upgrade is
    /// blocked by energy, the lowest-level drive is upgraded and the part is
    /// retried. Reactor magnet/boost upgrades are fallback spending only.
    /// </summary>
    internal static class ShipyardBuyAll
    {
        private sealed class Candidate
        {
            internal AccretionShip.PlacedPart part;
            internal long cost;
            internal int level;
            internal int uid;
            internal bool magnet;
        }

        internal static bool CanBuyAnything()
        {
            GameState gs = GameState.I;
            AccretionShip ship = gs?.Ship;
            if (gs == null || ship == null) return false;

            foreach (Candidate c in NormalCandidates(ship))
            {
                if (c.cost > gs.Money) continue;
                if (IsLegal(c.part, gs, ship, out bool _)) return true;
            }
            foreach (Candidate c in DriveCandidates(ship))
            {
                if (c.cost <= gs.Money && PartLevels.CanBuy(gs, c.part)) return true;
            }
            return ReactorCandidates(gs, ship).Count > 0;
        }

        internal static void Run(ShipyardAccretionScreen screen)
        {
            GameState gs = GameState.I;
            AccretionShip ship = gs?.Ship;
            if (gs == null || ship == null) return;

            bool bought = false;
            BuyNormal(gs, ship, ref bought);

            // Leftover money goes to drives after normal upgrades run out.
            while (TryBuyDrive(gs, ship)) bought = true;

            // Then magnet/boost as the final money sink.
            while (TryBuyReactor(gs, ship)) bought = true;

            if (bought)
            {
                Sfx.Play("buy");
                screen.ProbeRefresh();
            }
            else
            {
                UpdateButton(screen);
            }
        }

        private static void BuyNormal(GameState gs, AccretionShip ship, ref bool bought)
        {
            HashSet<int> skipped = new HashSet<int>();
            int guard = 0;

            while (guard++ < 500)
            {
                List<Candidate> list = NormalCandidates(ship);
                list.Sort(CompareNormal);
                bool acted = false;

                foreach (Candidate c in list)
                {
                    if (skipped.Contains(c.uid)) continue;
                    if (c.cost > gs.Money) break;

                    if (!IsLegal(c.part, gs, ship, out bool energyBlocked))
                    {
                        if (energyBlocked)
                        {
                            if (TryBuyDrive(gs, ship))
                            {
                                bought = true;
                                skipped.Clear();
                                acted = true;
                                break;
                            }

                            if (TryBuyReactor(gs, ship)) bought = true;
                        }

                        skipped.Add(c.uid);
                        continue;
                    }

                    if (PartLevels.Buy(gs, c.part))
                    {
                        bought = true;
                        skipped.Clear();
                        acted = true;
                        break;
                    }

                    skipped.Add(c.uid);
                }

                if (!acted) break;
            }
        }

        private static bool IsLegal(
            AccretionShip.PlacedPart p,
            GameState gs,
            AccretionShip ship,
            out bool energyBlocked)
        {
            energyBlocked = false;
            if (PartLevels.IsMaxed(p)) return false;

            if (!PartTiers.UsesLadder(p))
            {
                return PartLevels.CanBuy(gs, p);
            }

            if (PartTiers.IsLadderMaxed(p)) return false;
            if (PartTiers.CanLevelUpIgnoringMoney(gs, p, out string reason)) return true;

            // Only treat this as energy-blocked if it is not a fit/other
            // refusal. A drive must not be spent on an unfixable upgrade.
            if (reason != Loc.T("data.parts.level_no_fit", "Does not fit"))
            {
                int step = PartTiers.EnergyStepOfNext(p);
                energyBlocked = step > 0 && ship.EnergyUsed + step > ship.EnergyCap;
            }
            return false;
        }

        private static List<Candidate> NormalCandidates(AccretionShip ship)
        {
            List<Candidate> list = new List<Candidate>();
            foreach (UpgradeCards.CardModel card in UpgradeCards.BuildCards())
            {
                if (card.kind != UpgradeCards.KindPart) continue;

                AccretionShip.PlacedPart p = ship.placed.FirstOrDefault(x => x.uid == card.uid);
                if (p == null || AccretionShip.IsDrive(p) || PartLevels.IsMaxed(p)) continue;
                if (!PartTiers.UsesLadder(p) && !PartLevels.HasLevels(card.modelId)) continue;

                list.Add(new Candidate
                {
                    part = p,
                    cost = PartLevels.CostOf(p),
                    level = p.level,
                    uid = p.uid
                });
            }
            return list;
        }

        private static List<Candidate> DriveCandidates(AccretionShip ship)
        {
            List<Candidate> list = new List<Candidate>();
            foreach (AccretionShip.PlacedPart p in ship.placed)
            {
                if (!AccretionShip.IsDrive(p) || p.level >= 5) continue;
                list.Add(new Candidate
                {
                    part = p,
                    cost = PartLevels.CostOf(p),
                    level = p.level,
                    uid = p.uid
                });
            }
            return list;
        }

        private static List<Candidate> ReactorCandidates(GameState gs, AccretionShip ship)
        {
            List<Candidate> list = new List<Candidate>();
            AddReactor(gs, ship, magnet: true, list);
            AddReactor(gs, ship, magnet: false, list);
            return list;
        }

        private static void AddReactor(
            GameState gs,
            AccretionShip ship,
            bool magnet,
            List<Candidate> list)
        {
            int level = magnet ? ship.magnetLevel : ship.boostLevel;
            if (level >= 5) return;
            if (!PartLevels.CanBuyReactorUpgrade(gs, magnet, level)) return;

            list.Add(new Candidate
            {
                cost = PartLevels.Cost("REACTOR", level),
                level = level,
                magnet = magnet
            });
        }

        private static int CompareNormal(Candidate a, Candidate b)
        {
            int c = a.cost.CompareTo(b.cost);
            if (c != 0) return c;
            c = a.level.CompareTo(b.level);
            return c != 0 ? c : a.uid.CompareTo(b.uid);
        }

        private static int CompareDrive(Candidate a, Candidate b)
        {
            int c = a.level.CompareTo(b.level);
            if (c != 0) return c;
            c = a.cost.CompareTo(b.cost);
            return c != 0 ? c : a.uid.CompareTo(b.uid);
        }

        private static int CompareReactor(Candidate a, Candidate b)
        {
            int c = a.cost.CompareTo(b.cost);
            if (c != 0) return c;
            c = (a.magnet ? 0 : 1).CompareTo(b.magnet ? 0 : 1);
            return c != 0 ? c : a.level.CompareTo(b.level);
        }

        private static bool TryBuyDrive(GameState gs, AccretionShip ship)
        {
            List<Candidate> list = DriveCandidates(ship);
            list.Sort(CompareDrive);
            foreach (Candidate c in list)
            {
                if (c.cost > gs.Money) continue;
                if (PartLevels.Buy(gs, c.part)) return true;
            }
            return false;
        }

        private static bool TryBuyReactor(GameState gs, AccretionShip ship)
        {
            List<Candidate> list = ReactorCandidates(gs, ship);
            list.Sort(CompareReactor);
            foreach (Candidate c in list)
            {
                if (c.cost > gs.Money) break;
                if (PartLevels.BuyReactorUpgrade(gs, c.magnet)) return true;
            }
            return false;
        }

        internal static void UpdateButton(ShipyardAccretionScreen screen)
        {
            try
            {
                VisualElement hlp = Traverse.Create(screen).Field("_hlp").GetValue<VisualElement>();
                Button button = hlp?.Q<Button>("buy-all");
                if (button == null) return;

                bool can = CanBuyAnything();
                button.SetEnabled(can);
                button.style.opacity = can ? 1f : 0.45f;
                if (can) button.RemoveFromClassList("pad-skip");
                else button.AddToClassList("pad-skip");
            }
            catch
            {
                // Leave the button alone if a private field changes.
            }
        }
    }

    internal static class ShipyardBuyAllPatches
    {
        [HarmonyPatch(typeof(ShipyardAccretionScreen), "BuildHelperUi")]
        internal static class BuildHelperPatch
        {
            private static void Postfix(ShipyardAccretionScreen __instance)
            {
                try
                {
                    VisualElement hlp = Traverse.Create(__instance).Field("_hlp").GetValue<VisualElement>();
                    VisualElement head = Traverse.Create(__instance).Field("_hlpHead").GetValue<VisualElement>();
                    VisualElement scroll = Traverse.Create(__instance).Field("_hlpScroll").GetValue<VisualElement>();
                    if (hlp == null || hlp.Q<Button>("buy-all") != null) return;

                    VisualElement host = new VisualElement
                    {
                        name = "buy-all-host"
                    };
                    host.style.flexShrink = 0f;
                    host.style.paddingLeft = 4f;
                    host.style.paddingRight = 16f;
                    host.style.paddingTop = 4f;
                    host.style.paddingBottom = 12f;

                    Button button = new Button
                    {
                        name = "buy-all"
                    };
                    button.AddToClassList("pad-nav");
                    button.style.flexDirection = FlexDirection.Row;
                    button.style.alignItems = Align.Center;
                    button.style.flexShrink = 0f;
                    button.style.paddingLeft = 20f;
                    button.style.paddingRight = 18f;
                    button.style.paddingTop = 16f;
                    button.style.paddingBottom = 16f;
                    button.style.marginBottom = 0f;
                    button.style.backgroundColor = Color.clear;
                    button.style.borderLeftWidth = 0f;
                    button.style.borderRightWidth = 0f;
                    button.style.borderTopWidth = 0f;
                    button.style.borderBottomWidth = 0f;

                    SlabFrame frame = new SlabFrame
                    {
                        Notch = 14f,
                        Stroke = 2f,
                        StrokeColor = MetaUi.Fade(MetaUi.Orange, 0.55f),
                        FillColor = MetaUi.Hex(1512207),
                        TintColor = Color.clear
                    };
                    frame.Behind(button);

                    Label label = MetaUi.Text(
                        Loc.T("station.shipyard.buy_all", "BUY ALL"),
                        MetaUi.Face.Display,
                        24f,
                        MetaUi.Ink,
                        1.5f);
                    label.style.flexGrow = 1f;
                    button.Add(label);

                    void Paint(bool on)
                    {
                        frame.Stroke = on ? 4f : 2f;
                        frame.StrokeColor = on
                            ? Color.Lerp(MetaUi.OrangeLt, Color.white, 0.45f)
                            : MetaUi.Fade(MetaUi.Orange, 0.55f);
                        frame.FillColor = on ? MetaUi.Hex(3021834) : MetaUi.Hex(1512207);
                        frame.TintColor = on ? MetaUi.Fade(MetaUi.Orange, 0.3f) : Color.clear;
                    }

                    Action action = delegate { ShipyardBuyAll.Run(__instance); };
                    button.clicked += action;
                    PadNav.Register(
                        button,
                        action,
                        delegate { if (button.enabledSelf) Paint(on: true); },
                        delegate { Paint(on: false); },
                        null,
                        null,
                        null,
                        null,
                        null,
                        () => new string[4]
                        {
                            Loc.T("station.shipyard.buy_all", "BUY ALL"),
                            null,
                            null,
                            PadNav.ExitWord
                        });
                    button.RegisterCallback<PointerEnterEvent>(delegate
                    {
                        if (button.enabledSelf) Paint(on: true);
                    });
                    button.RegisterCallback<PointerLeaveEvent>(delegate { Paint(on: false); });

                    host.Add(button);
                    // Keep BUY ALL above the helper head, which holds the
                    // back/collapse row on subpages. Otherwise opening a
                    // submenu reorders it below that row.
                    int index = head != null
                        ? hlp.IndexOf(head)
                        : (scroll != null ? hlp.IndexOf(scroll) : -1);
                    if (index < 0) hlp.Add(host);
                    else hlp.Insert(index, host);

                    ShipyardBuyAll.UpdateButton(__instance);
                }
                catch
                {
                    // A missing helper field must not break the shipyard.
                }
            }
        }

        [HarmonyPatch(typeof(ShipyardAccretionScreen), "RenderHelper")]
        internal static class RenderHelperPatch
        {
            private static void Postfix(ShipyardAccretionScreen __instance)
            {
                ShipyardBuyAll.UpdateButton(__instance);
            }
        }
    }
}
