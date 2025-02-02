using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;
using RimWorld;
using static CyanobotsLeather.LogUtil;
using System.Reflection.Emit;

namespace CyanobotsLeather
{


    //thank you to Human Butchery 2.0 for this approach
    [HarmonyPatch(typeof(ThoughtWorker_HumanLeatherApparel),nameof(ThoughtWorker_HumanLeatherApparel.CurrentThoughtState))]
    public static class Patch_ThoughtWorker_HumanLeatherApparel_CurrentThoughtState
    {
        public static List<string> humanHideDefNames = new List<string>()
        {
            "Leather_Human",
            "CYB_Hide_DoctorStupid_Leather_Human_Fur",
            "CYB_Hide_DoctorStupid_Leather_Human_Heat",
            "CYB_Hide_CYB_Leather_Uncia",
            "CYB_Hide_CYB_Leather_Onca"
        };

        private static void Postfix(Pawn p, ref ThoughtState __result)
        {            
            int numWorn = __result.StageIndex + 1;
            if (numWorn < 0)
            {
                numWorn = 0;
            }
            
            DebugLog("Patch_ThoughtWorker_HumanLeatherApparel_CurrentThoughtState Postfix"
                + " - pawn: " + p
                + ", result: " + __result
                + ", StageIndex: " + __result.StageIndex
                + ", Reason: " + __result.Reason
                + ", Active: " + __result.Active
                + ", pre-existing numWorn: " + numWorn
                );
            
            string text = __result.Reason;

            List<Apparel> wornApparel = p.apparel.WornApparel;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (CountsAsHuman(wornApparel[i]))
                {
                    DebugLog("Found " + wornApparel[i] + " counts as human, stuff: " + wornApparel[i].Stuff);
                    if (text == null)
                    {
                        text = wornApparel[i].def.label;
                    }
                    numWorn++;
                }
            }
            if (numWorn == 0)
            {
                __result = ThoughtState.Inactive;
            }
            if (numWorn >= 5)
            {
                __result = ThoughtState.ActiveAtStage(4, text);
            }
            __result = ThoughtState.ActiveAtStage(numWorn - 1, text);
            DebugLog("Final numWorn: " + numWorn);
            
        }

        public static bool CountsAsHuman(Apparel apparel)
        {
            if (apparel == null) return false;
            if (apparel.Stuff == null) return false;
            if (humanHideDefNames.Contains(apparel.Stuff.defName)) return true;
            else return false;
        }
    }
}
