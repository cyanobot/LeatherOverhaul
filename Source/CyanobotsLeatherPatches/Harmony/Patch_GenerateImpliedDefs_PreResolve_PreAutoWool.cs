using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CyanobotsLeather.Main;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    [HarmonyBefore(new string[] { "divineDerivative.AutoWool" })]
    public static class Patch_GenerateImpliedDefs_PreResolve_PreAutoWool
    {
        public static bool Prepare()
        {
            return betterWoolLoaded;
        }

        public static void Prefix()
        {
            harmony.Patch(
                Patch_BetterWool_DetermineButcherProducts.TargetMethod(),
                prefix: new HarmonyMethod(
                    AccessTools.Method(typeof(Patch_BetterWool_DetermineButcherProducts),
                    nameof(Patch_BetterWool_DetermineButcherProducts.Prefix)))
                );
        }
    }
}
