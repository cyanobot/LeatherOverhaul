using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;
using static CyanobotsLeather.LeatherColorUtility;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.ButcherProducts))]
    [HarmonyBefore(new string[] { "cyanobot.ColoredLeatherAndWool" })]
    class Patch_ButcherProducts_Thing
    {
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> results, Thing __instance)
        {
            LogUtil.DebugLog("Patch_ButcherProducts_Thing, __instance: " + __instance + ", stackCount: " + __instance.stackCount);

            CompShearableHide compShearableHide = __instance.TryGetComp<CompShearableHide>();
            if (compShearableHide != null)
            {
                Color color = GetTextileColor(__instance);

                Thing shornHide = ThingMaker.MakeThing(compShearableHide.ShornHideDef);
                shornHide.stackCount = __instance.stackCount;
                //SetTextileColor(shornHide, color);
                yield return shornHide;

                Thing wool = ThingMaker.MakeThing(compShearableHide.WoolDef);
                wool.stackCount = (int)(__instance.stackCount * compShearableHide.woolPerUnit);
                SetTextileColor(shornHide, color);
                if (wool.stackCount > 0) yield return wool;
            }
            else
            {
                foreach (Thing thing in results) yield return thing;
            }
        }

    }
}
