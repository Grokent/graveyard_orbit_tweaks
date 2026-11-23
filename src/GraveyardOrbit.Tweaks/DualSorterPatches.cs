using System.Collections.Generic;
using GraveyardOrbit.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UIElements;

namespace GraveyardOrbit.Tweaks
{
    /// <summary>
    /// Lets a Sorter hold two colours. The second colour is stored in the
    /// sorter's unused "lucky" field, which is already serialized by the game,
    /// so old saves and the base save format are unaffected.
    /// </summary>
    internal static class SorterColors
    {
        private const int NoSecondary = 7;

        internal static ColourRules.Colour Secondary(Machine m)
        {
            if (m == null || m.kind != MachineKind.Sorter) return ColourRules.Colour.None;
            int code = m.lucky;
            if (code >= (int)ColourRules.Colour.Black && code <= (int)ColourRules.Colour.Pink)
                return (ColourRules.Colour)code;
            return ColourRules.Colour.None;
        }

        internal static void SetSecondary(Machine m, ColourRules.Colour c)
        {
            if (m == null) return;
            m.lucky = (c >= ColourRules.Colour.Black && c <= ColourRules.Colour.Pink)
                ? (int)c
                : NoSecondary;
        }
    }

    internal static class DualSorterPatches
    {
        // Colour keys toggle: first pick sets primary, second pick sets
        // secondary, picking a selected colour removes it, and a third pick
        // replaces the secondary. This keeps mouse and pad navigation identical.
        [HarmonyPatch(typeof(RefineryLine), nameof(RefineryLine.SetSorterColour))]
        internal static class SetSorterColourPatch
        {
            private static bool Prefix(Machine m, ColourRules.Colour c)
            {
                if (m == null || m.kind != MachineKind.Sorter) return true;

                ColourRules.Colour primary = m.colour;
                ColourRules.Colour secondary = SorterColors.Secondary(m);

                if (c == ColourRules.Colour.None)
                {
                    m.colour = ColourRules.Colour.None;
                    SorterColors.SetSecondary(m, ColourRules.Colour.None);
                    return false;
                }

                if (c == primary)
                {
                    if (secondary != ColourRules.Colour.None)
                    {
                        m.colour = secondary;
                        SorterColors.SetSecondary(m, ColourRules.Colour.None);
                    }
                    else
                    {
                        m.colour = ColourRules.Colour.None;
                    }
                    return false;
                }

                if (c == secondary)
                {
                    SorterColors.SetSecondary(m, ColourRules.Colour.None);
                    return false;
                }

                if (primary == ColourRules.Colour.None)
                {
                    m.colour = c;
                    SorterColors.SetSecondary(m, ColourRules.Colour.None);
                }
                else
                {
                    // Fill the second slot, or replace it when both are full.
                    SorterColors.SetSecondary(m, c);
                }
                return false;
            }
        }

        // Keep the Dial button cycling the primary colour. A secondary that
        // would collide with the new primary is cleared.
        [HarmonyPatch(typeof(RefinerySim), nameof(RefinerySim.CycleSorterColour))]
        internal static class CycleSorterColourPatch
        {
            private static bool Prefix(RefinerySim __instance, Machine s)
            {
                if (s == null || s.kind != MachineKind.Sorter) return true;

                RefineryLine line = __instance.Line;
                if (line == null) return true;

                List<ColourRules.Colour> seen = new List<ColourRules.Colour>();
                foreach (ColourRules.Colour c in RefineryPalette.Substrates)
                {
                    if (line.SeenColours.Contains(c)) seen.Add(c);
                }
                if (seen.Count == 0) return false;

                int index = seen.IndexOf(s.colour);
                ColourRules.Colour next = seen[(index + 1) % seen.Count];
                s.colour = next;
                if (SorterColors.Secondary(s) == next)
                    SorterColors.SetSecondary(s, ColourRules.Colour.None);

                __instance.SaveState();
                __instance.Rebuild();
                __instance.OnEconomyChanged?.Invoke();
                return false;
            }
        }

        // Both selected colours route down this sorter's lane.
        [HarmonyPatch(typeof(RefineryLine), "SortedTo")]
        internal static class SortedToPatch
        {
            private static void Postfix(RefineryLine __instance, int lane, ColourRules.Colour c, ref bool __result)
            {
                if (__result || c == ColourRules.Colour.None) return;
                Machine sorter = __instance.SorterOn(lane);
                if (sorter != null && SorterColors.Secondary(sorter) == c)
                    __result = true;
            }
        }

        [HarmonyPatch(typeof(RefineryPalette), nameof(RefineryPalette.LabelOf))]
        internal static class LabelOfPatch
        {
            private static void Postfix(Machine m, ref string __result)
            {
                if (m == null || m.kind != MachineKind.Sorter) return;
                ColourRules.Colour secondary = SorterColors.Secondary(m);
                if (secondary == ColourRules.Colour.None) return;

                string primaryName = m.colour == ColourRules.Colour.None
                    ? Loc.T("station.refinery.color_any", "ANY")
                    : RefineryPalette.Name(m.colour);

                __result = Loc.F(
                    "station.refinery.machine_sorter_dual",
                    "{primary}+{secondary}\nSORTER",
                    ("primary", primaryName),
                    ("secondary", RefineryPalette.Name(secondary)));
            }
        }

        [HarmonyPatch(typeof(RefineryScreen), "PartDesc")]
        internal static class PartDescPatch
        {
            private static void Postfix(RefineryScreen.CardKind kind, ref string __result)
            {
                if (kind != RefineryScreen.CardKind.Sorter) return;
                __result = Loc.T(
                    "station.refinery.desc_sorter_dual",
                    "Sits at the start of a lane and sends one or two colours down it. Click a colour to add it; click it again to remove it. Max two. One lane always stays open.");
            }
        }

        [HarmonyPatch(typeof(RefineryScreen), nameof(RefineryScreen.State))]
        internal static class DialStatePatch
        {
            private static void Postfix(
                RefineryScreen.CardKind kind,
                RefineryScreen __instance,
                ref RefineryScreen.CardState __result)
            {
                if (kind != RefineryScreen.CardKind.Dial) return;

                Machine m = __instance.Selected;
                if (m == null || m.kind != MachineKind.Sorter) return;

                ColourRules.Colour secondary = SorterColors.Secondary(m);
                __result.desc = Loc.T(
                    "station.refinery.dial_desc_dual",
                    "Click a colour to select it; click it again to remove it. Up to two colours go down this lane.");

                string primaryName = m.colour == ColourRules.Colour.None
                    ? Loc.T("station.refinery.dial_no_color", "NO COLOR")
                    : RefineryPalette.Name(m.colour);

                __result.progress = secondary == ColourRules.Colour.None
                    ? primaryName
                    : primaryName + " + " + RefineryPalette.Name(secondary);
            }
        }

        // Mark the secondary key in the sorter popover and explain the toggle.
        [HarmonyPatch(typeof(RefineryScreen), nameof(RefineryScreen.OpenSocket))]
        internal static class SorterPopoverPatch
        {
            private static void Postfix(RefineryScreen __instance, Machine m)
            {
                if (m == null || m.kind != MachineKind.Sorter) return;

                VisualElement pop = __instance.Popover;
                if (pop == null) return;

                Label desc = pop.Q<Label>(null, "rl-opt__desc");
                if (desc != null)
                {
                    desc.text = Loc.T(
                        "station.refinery.dial_desc_dual",
                        "Click a colour to select it; click it again to remove it. Up to two colours go down this lane.");
                }

                ColourRules.Colour secondary = SorterColors.Secondary(m);
                foreach (ColourRules.Colour colour in RefineryPalette.Substrates)
                {
                    Button key = pop.Q<Button>("pop-dial-" + colour);
                    if (key == null) continue;
                    key.text = RefineryPalette.Name(colour) + (secondary == colour ? "  +" : "");
                }
            }
        }

        // Repaint the sorter octagon as a two-tone diagonal: upper-left is the
        // primary colour, lower-right is the secondary colour.
        [HarmonyPatch(typeof(RefineryLineElement), "DrawMachine")]
        internal static class SorterDrawPatch
        {
            private static readonly List<Vector2> Poly = new List<Vector2>(8);
            private static readonly List<Vector2> Upper = new List<Vector2>(12);
            private static readonly List<Vector2> Lower = new List<Vector2>(12);

            private static void Postfix(Painter2D p, Machine m, Vector2 c, bool ghost)
            {
                if (m == null || m.kind != MachineKind.Sorter) return;
                ColourRules.Colour secondaryColour = SorterColors.Secondary(m);
                if (secondaryColour == ColourRules.Colour.None) return;

                float scale = RefineryLine.Board != null ? RefineryLine.Board.Scale : 1f;
                float size = 124f * scale;
                if (size <= 1f) return;

                float notch = size * 0.09f;
                Color primary = m.colour == ColourRules.Colour.None
                    ? RefineryPalette.Sorter
                    : RefineryPalette.Of(m.colour);
                Color secondary = RefineryPalette.Of(secondaryColour);

                BuildOctagon(Poly, c, size, notch);
                float split = c.x + c.y;
                ClipHalf(Poly, Upper, split, upper: true);
                ClipHalf(Poly, Lower, split, upper: false);

                float alpha = ghost
                    ? 0.5f
                    : 0.5f + 0.15f * Mathf.Sin(Time.realtimeSinceStartup * 9f);

                // Cover the game's single-colour fill first, otherwise the
                // secondary half would blend with the primary underneath it.
                p.fillColor = new Color(0.055f, 0.06f, 0.07f, ghost ? 0.5f : 0.95f);
                FillPoly(p, Poly);

                Color primaryFill = Color.Lerp(primary, Color.white, 0.25f);
                Color secondaryFill = Color.Lerp(secondary, Color.white, 0.25f);
                p.fillColor = new Color(primaryFill.r, primaryFill.g, primaryFill.b, alpha);
                FillPoly(p, Upper);
                p.fillColor = new Color(secondaryFill.r, secondaryFill.g, secondaryFill.b, alpha);
                FillPoly(p, Lower);

                Color blend = Color.Lerp(primary, secondary, 0.5f);
                p.strokeColor = Color.Lerp(blend, Color.white, 0.3f);
                p.lineWidth = 3f;
                RefineryLineElement.Octagon(p, c, size, notch);
                p.Stroke();

                Color ink = Color.Lerp(blend, Color.white, 0.45f);
                RefineryLineElement.Glyph(p, m.kind, c, size / 78f, m.level, ink);
            }

            private static void BuildOctagon(List<Vector2> into, Vector2 c, float size, float notch)
            {
                float n = size * 0.5f;
                into.Clear();
                into.Add(new Vector2(c.x - n + notch, c.y - n));
                into.Add(new Vector2(c.x + n - notch, c.y - n));
                into.Add(new Vector2(c.x + n, c.y - n + notch));
                into.Add(new Vector2(c.x + n, c.y + n - notch));
                into.Add(new Vector2(c.x + n - notch, c.y + n));
                into.Add(new Vector2(c.x - n + notch, c.y + n));
                into.Add(new Vector2(c.x - n, c.y + n - notch));
                into.Add(new Vector2(c.x - n, c.y - n + notch));
            }

            private static void ClipHalf(List<Vector2> src, List<Vector2> into, float split, bool upper)
            {
                into.Clear();
                for (int i = 0; i < src.Count; i++)
                {
                    Vector2 a = src[i];
                    Vector2 b = src[(i + 1) % src.Count];
                    float da = a.x + a.y - split;
                    float db = b.x + b.y - split;
                    bool aInside = upper ? da <= 0f : da >= 0f;
                    bool bInside = upper ? db <= 0f : db >= 0f;

                    if (aInside) into.Add(a);
                    if (aInside != bInside)
                    {
                        float t = da / (da - db);
                        into.Add(new Vector2(
                            a.x + (b.x - a.x) * t,
                            a.y + (b.y - a.y) * t));
                    }
                }
            }

            private static void FillPoly(Painter2D p, List<Vector2> pts)
            {
                if (pts.Count < 3) return;
                p.BeginPath();
                p.MoveTo(pts[0]);
                for (int i = 1; i < pts.Count; i++) p.LineTo(pts[i]);
                p.ClosePath();
                p.Fill();
            }
        }
    }
}
