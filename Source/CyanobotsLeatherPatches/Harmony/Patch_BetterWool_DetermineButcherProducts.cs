using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CyanobotsLeather.Main;

namespace CyanobotsLeather
{
    //[HarmonyPatch]
    public static class Patch_BetterWool_DetermineButcherProducts
    {
        public static bool Prepare()
        {
            return betterWoolLoaded;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(AccessTools.TypeByName("AutoWool.GeneratorUtility"), "DetermineButcherProducts");
        }

        //simply blocks addition of fleece to animal butcher products
        //because our own system handles it
        public static bool Prefix()
        {
            LogUtil.DebugLog("Patch_BetterWool_DetermineButcherProducts.Prefix");
            return false;
        }
    }
}
