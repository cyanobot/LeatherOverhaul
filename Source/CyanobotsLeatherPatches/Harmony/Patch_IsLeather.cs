using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CyanobotsLeather
{
    [HarmonyPatch]
    public static class Patch_IsLeather
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(ThingDef), nameof(ThingDef.IsLeather));
        }

        public static bool Postfix(bool __result, ThingDef __instance)
        {
            if (__result) return true;

            if (__instance != null
                && !__instance.thingCategories.NullOrEmpty()
                && __instance.thingCategories.Contains(CyanobotsLeather_DefOf.CYB_Hides)) return true;

            return false;
        }
    }
}
