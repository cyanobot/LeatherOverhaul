using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static CyanobotsLeather.Main;
using static CyanobotsLeather.LogUtil;
using static CyanobotsLeather.ImpliedDefUtility;
using static CyanobotsLeather.WoolUtility;

namespace CyanobotsLeather
{
    public enum YakSeason
    {
        Temperate,
        Winter,
        Jungle,
        Desert,
        Error
    }

    public static class YakUtility
    {
        public static Dictionary<YakSeason, string> labelSuffices = new Dictionary<YakSeason, string>
        {
            { YakSeason.Temperate, ", temperate" },
            { YakSeason.Winter, ", winter" },
            { YakSeason.Jungle, ", jungle" },
            { YakSeason.Desert, ", desert" },
            { YakSeason.Error, " ERROR THIS SHOULD NOT EXIST" }
        };
        public static Dictionary<YakSeason, string> defKeys = new Dictionary<YakSeason, string>
        {
            { YakSeason.Temperate, "Temperate" },
            { YakSeason.Winter, "Winter" },
            { YakSeason.Jungle, "Jungle" },
            { YakSeason.Desert, "Desert" },
            { YakSeason.Error, "Error" }
        };
        public static YakSeason YakSeasonFromString(string str)
        {
            if (str.Contains(defKeys[YakSeason.Temperate])) return YakSeason.Temperate;
            if (str.Contains(defKeys[YakSeason.Winter])) return YakSeason.Winter;
            if (str.Contains(defKeys[YakSeason.Jungle])) return YakSeason.Jungle;
            if (str.Contains(defKeys[YakSeason.Desert])) return YakSeason.Desert;
            return YakSeason.Error;
        }

        public static Dictionary<YakSeason, ThingDef> yakHideDefs = new Dictionary<YakSeason, ThingDef>();

        public static void GenerateAndRegisterChameleonYakHides(ThingDef yakDef, ThingDef shornDef, ThingDef tannedDef, bool hotReload)
        {
            CompProperties props_AnimalProduct = yakDef.comps.Find(p => p.GetType() == t_CompProperties_AnimalProduct);
            List<string> seasonalItems = f_seasonalItems.GetValue(props_AnimalProduct) as List<string>;
            if (seasonalItems.NullOrEmpty()) return;

            List<ThingDef> woolDefs = seasonalItems.Select(s => DefDatabase<ThingDef>.GetNamed(s)).ToList();
            DebugLog("yak woolDefs: " + woolDefs.ToStringSafeEnumerable());
            foreach (ThingDef yakWool in woolDefs)
            {
                YakSeason yakSeason = YakSeasonFromString(yakWool.defName);
                if (yakSeason == YakSeason.Error) continue;

                string labelSuffix = labelSuffices[yakSeason];

                ThingDef hideDef = GenerateAndRegisterWoolHide(yakDef, yakWool, shornDef, tannedDef, hotReload);
                hideDef.label += labelSuffix;

                if (yakHideDefs.ContainsKey(yakSeason))
                {
                    yakHideDefs[yakSeason] = hideDef;
                }
                else
                {
                    yakHideDefs.Add(yakSeason, hideDef);
                }
            }
        }

        public static Thing MakeHideFromYak(Pawn yak, int stackCount)
        {
            HediffSet hediffs = yak.health?.hediffSet;
            YakSeason yakSeason = YakSeason.Temperate;

            if (hediffs == null) ;
            else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_WinterPelt")))
            {
                yakSeason = YakSeason.Winter;
            }
            else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_JunglePelt")))
            {
                yakSeason = YakSeason.Jungle;
            }
            else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_DesertPelt")))
            {
                yakSeason = YakSeason.Desert;
            }

            ThingDef woolHideDef;
            if (!yakHideDefs.TryGetValue(yakSeason, out woolHideDef)) return null;

            float fullness = CurrentWoolFullness(yak, null);

            if (fullness < 0.3f)
            {
                ThingDef shornHideDef = woolHideDef
                    .GetCompProperties<CompProperties_ShearableHide>()?.shornHideDef;
                Thing shornHide = ThingMaker.MakeThing(shornHideDef);
                shornHide.stackCount = stackCount;
                return shornHide;
            }
            else
            {
                Thing woolHide = ThingMaker.MakeThing(woolHideDef);
                woolHide.stackCount = stackCount;
                woolHide.TryGetComp<CompShearableHide>().woolPerUnit = fullness 
                    * WoolAmountPerLeatherFor(yak.def);
                return woolHide;
            }
        }
    }
}
