using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(ThoughtWorker_DreadLeatherApparel),"ApparelCounts")]
    public static class Patch_ThoughtWorker_DreadLeatherApparel_ApparelCounts
    {
        public static bool Postfix(bool __result, Apparel apparel)
        {
            return (__result || apparel.Stuff?.defName == "CYB_Hide_Dread");
        }
    }
}
