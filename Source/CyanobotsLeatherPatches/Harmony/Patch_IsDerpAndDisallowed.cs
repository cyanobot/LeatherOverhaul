using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(ThingSetMakerUtility),nameof(ThingSetMakerUtility.IsDerpAndDisallowed))]
    public static class Patch_IsDerpAndDisallowed
    {
        public static bool Postfix(bool __result, ThingDef thing, ThingDef stuff, QualityGenerator? qualityGenerator)
        {
            if (__result) return true;

            if (qualityGenerator == QualityGenerator.Reward
                || qualityGenerator == QualityGenerator.Gift
                || qualityGenerator == QualityGenerator.Super
                || qualityGenerator == QualityGenerator.Trader)
            {
                if (stuff?.stuffProps?.categories?.Contains(CyanobotsLeather_DefOf.CYB_Hide) ?? false) return true;
            }

            return false;
        }
    }
}
