using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using static CyanobotsLeather.Main;
using static CyanobotsLeather.WoolUtility;
using static CyanobotsLeather.ImpliedDefUtility;
using System.Linq;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(Pawn),nameof(Pawn.ButcherProducts))]
    [HarmonyAfter(new string[] { "OskarPotocki.VFECore" })]                    //vef core
    [HarmonyBefore(new string[] { "cyanobot.ColoredLeatherAndWool", "cyanobot.OncaAndUncia" })]    //colored leather, onca and uncia
    class Patch_ButcherProducts_Pawn
    {
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> results, Pawn __instance)
        {
            List<Thing> resultsList = results.ToList();
            bool humanlike = __instance.RaceProps.Humanlike;
            bool shearable = false;
            Thing newHide = null;

            ThingDef leatherDef = __instance.RaceProps.leatherDef;
            LogUtil.DebugLog("Patch_ButcherProducts_Pawn Postfix, leatherDef: " + leatherDef + ", results: " + resultsList.ToStringSafeEnumerable()); 
            
            if (leatherDef != null)
            {
                LogUtil.DebugLog("leatherDef != null");
                if (leatherDef.HasComp(typeof(CompShearableHide)))
                {
                    LogUtil.DebugLog("HasComp(CompShearableHide)");
                    shearable = true;
                }
            }

            LogUtil.DebugLog("about to start foreach");
            foreach (Thing thing in resultsList)
            {
                LogUtil.DebugLog("thing: " + thing);
                if (thing.def == leatherDef)
                {
                    ThingDef newHideDef = null;
                    if (humanlike && __instance.genes != null)
                    {
                        if (VEFLoaded || oncaUnciaLoaded)
                        {
                            foreach (Gene gene in __instance.genes.GenesListForReading)
                            {
                                if (gene.Active && !gene.def.modExtensions.NullOrEmpty())
                                {
                                    if (VEFLoaded)
                                    {
                                        DefModExtension geneExtension = gene.def.modExtensions.Find(dme => dme.GetType() == t_GeneExtension);
                                        if (geneExtension != null) newHideDef = f_customLeatherThingDef.GetValue(geneExtension) as ThingDef;
                                        //if (basicHideByLeather.ContainsKey(newLeatherDef)) newHideDef = basicHideByLeather[newLeatherDef];
                                    }
                                    if (newHideDef == null && oncaUnciaLoaded)
                                    {
                                        ThingDef newLeatherDef = null;
                                        DefModExtension leatherExtension = gene.def.modExtensions.Find(dme => dme.GetType() == t_CustomLeather);
                                        if (leatherExtension != null) newLeatherDef = f_CustomLeather_leatherDef.GetValue(leatherExtension) as ThingDef;
                                        if (basicHideByLeather.ContainsKey(newLeatherDef)) newHideDef = basicHideByLeather[newLeatherDef];
                                    }

                                    if (newHideDef != null) break;
                                }
                            }
                        }

                        if (newHideDef == null && humanButcheryLoaded)
                        {
                            if (__instance.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("Furskin")))
                            {
                                newHideDef = DefDatabase<ThingDef>.GetNamed("CYB_Hide_DoctorStupid_Leather_Human_Fur");
                            }
                            else if (__instance.genes.HasActiveGene(DefDatabase<GeneDef>.GetNamed("FireResistant")))
                            {
                                newHideDef = DefDatabase<ThingDef>.GetNamed("CYB_Hide_DoctorStupid_Leather_Human_Heat");
                            }
                        }

                        if (newHideDef != null)
                        {
                            newHide = ThingMaker.MakeThing(newHideDef);
                            newHide.stackCount = thing.stackCount;
                            newHide.SetColor(__instance.story.SkinColor);
                            LogUtil.DebugLog("newHide: " + newHide);
                            yield return newHide;
                            continue;
                        }
                        else
                        {
                            thing.SetColor(__instance.story.SkinColor);
                        }
                    }
                    if (shearable)
                    {
                        

                        CompShearableHide compShearableHide = thing.TryGetComp<CompShearableHide>();
                        LogUtil.DebugLog("Leather shearable, compHide: " + compShearableHide + "__instance.def: " + __instance.def);
                        //compHide.pawnDef = __instance.def;

                        ThingDef woolDef = compShearableHide.WoolDef;
                        float woolFullness = CurrentWoolFullness(__instance, woolDef);

                        if (woolFullness < 0.3f)
                        {
                            Thing shornHide = ThingMaker.MakeThing(compShearableHide.ShornHideDef);
                            shornHide.stackCount = thing.stackCount;
                            shornHide.SetColor(thing.DrawColor);
                            yield return shornHide;
                            continue;
                        }
                        else
                        {
                            compShearableHide.woolPerUnit = woolFullness * WoolAmountPerLeatherFor(__instance.def);
                        }

                    }
                    if (AALoaded && __instance.def.defName == "AA_ChameleonYak")
                    {
                        LogUtil.DebugLog("found chameleonyak");
                        HediffSet hediffs = __instance.health?.hediffSet;
                        string hideName = "CYB_Hide_Wool_AA_ChameleonYakWoolTemperate";
                        if (hediffs == null) ;
                        else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_WinterPelt")))
                        {
                            hideName = "CYB_Hide_Wool_AA_ChameleonYakWoolWinter";
                        }
                        else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_JunglePelt")))
                        {
                            hideName = "CYB_Hide_Wool_AA_ChameleonYakWoolJungle";
                        }
                        else if (hediffs.HasHediff(DefDatabase<HediffDef>.GetNamed("AA_DesertPelt")))
                        {
                            hideName = "CYB_Hide_Wool_AA_ChameleonYakWoolDesert";
                        }

                        ThingDef hideDef = DefDatabase<ThingDef>.GetNamed(hideName);

                        float fullness = CurrentWoolFullness(__instance, null);

                        if (fullness < 0.3f)
                        {
                            ThingDef shornHideDef = hideDef
                                .GetCompProperties<CompProperties_ShearableHide>()?.shornHideDef;
                            Thing shornHide = ThingMaker.MakeThing(shornHideDef);
                            shornHide.stackCount = thing.stackCount;
                            yield return shornHide;
                            continue;
                        }
                        else
                        {
                            Thing woolHide = ThingMaker.MakeThing(hideDef);
                            woolHide.stackCount = thing.stackCount;
                            woolHide.TryGetComp<CompShearableHide>().woolPerUnit = fullness * WoolAmountPerLeatherFor(__instance.def);
                            yield return woolHide;
                            continue;
                        }
                    }

                }
                yield return thing;
            }
        }
    }
}
