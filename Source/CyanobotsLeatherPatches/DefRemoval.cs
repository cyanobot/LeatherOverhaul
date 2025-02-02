using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime;
using Verse;
using static CyanobotsLeather.LogUtil;
using static CyanobotsLeather.Main;
using RimWorld;
using RimWorld.Planet;
using Verse.Noise;
using System.Reflection;
using HarmonyLib;

namespace CyanobotsLeather
{
    public static class DefRemoval
    {
        public static Dictionary<ThingDef, List<ThingDef>> merges = new Dictionary<ThingDef, List<ThingDef>>();
        public static Dictionary<ThingDef, ThingDef> toReplace = new Dictionary<ThingDef, ThingDef>();
        public static List<ThingDef> removedDefs = new List<ThingDef>();

        public static FieldInfo f_ScenPart_thingDef = AccessTools.Field(typeof(ScenPart_ThingCount), "thingDef");
        public static FieldInfo f_ScenPart_stuff = AccessTools.Field(typeof(ScenPart_ThingCount), "stuff");
        public static FieldInfo f_StockGenerator_MultiDef_thingDefs = AccessTools.Field(typeof(StockGenerator_MultiDef), "thingDefs");
        public static FieldInfo f_StockGenerator_SingleDef_thingDef = AccessTools.Field(typeof(StockGenerator_SingleDef), "thingDef");
        public static MethodInfo m_ThingDefDatabase_Remove = AccessTools.Method(typeof(DefDatabase<ThingDef>), "Remove");

        public static void LoadMerges()
        {
            merges.Clear();
            toReplace.Clear();

            //first load the merges from the defs into a single dictionary sorted by mergeTo
            foreach (LeatherMergeDef mergeDef in DefDatabase<LeatherMergeDef>.AllDefsListForReading)
            {
                if (mergeDef.mergeTo == null || mergeDef.mergeFrom.NullOrEmpty())
                {
                    Log.Warning("[Cyanobot's Leather] Improperly formatted LeatherMergeDef " + mergeDef.defName
                        + ". LeatherMergeDef must define mergeTo and at least one mergeFrom.");
                    continue;
                }

                mergeDef.mergeFrom.Remove(mergeDef.mergeTo);
                mergeDef.mergeFrom.RemoveDuplicates();

                if (mergeDef.mergeFrom.Empty())
                {
                    Log.Warning("[Cyanobot's Leather] Improperly formatted LeatherMergeDef " + mergeDef.defName
                        + ". LeatherMergeDef must define mergeTo and at least one mergeFrom, "
                        + "which may not be the same as the mergeTo.");
                    continue;
                }

                if (merges.ContainsKey(mergeDef.mergeTo))
                {
                    foreach (ThingDef def in mergeDef.mergeFrom)
                    {
                        merges[mergeDef.mergeTo].AddDistinct(def);
                    }
                }
                else
                {
                    merges.Add(mergeDef.mergeTo, mergeDef.mergeFrom);
                }

            }

            StringBuilder sb_merges = new StringBuilder();
            sb_merges.Append("DefRemoval.LoadMerges - initial merges: ");
            foreach (ThingDef key in merges.Keys)
            {
                sb_merges.AppendInNewLine(key.defName);
                sb_merges.Append(" : ");
                sb_merges.Append(merges[key].ToStringSafeEnumerable());
            }
            DebugLog(sb_merges.ToString());

            //second load that dictionary into a list sorted by mergeFrom
            foreach (KeyValuePair<ThingDef,List<ThingDef>> kvp in merges)
            {
                ThingDef mergeTo = kvp.Key;
                List<ThingDef> mergeFrom = kvp.Value;

                //check for multiple merges trying to override the same leather
                foreach (ThingDef fromDef in mergeFrom)
                {
                    if (toReplace.ContainsKey(fromDef))
                    {
                        Log.Warning("[Cyanobot's Leather] Found multiple LeatherMergeDefs attempting to override "
                            + fromDef.defName + ". The first loaded merge wins. " + fromDef.defName 
                            + " will be replaced with " + toReplace[fromDef].defName);
                        continue;
                    }
                    else
                    {
                        toReplace.Add(fromDef, mergeTo);
                    }
                }
            }

            //second check for any recursive merges
            int i = 0;
            bool finished = false;
            bool recurseWarningSent = false;
            do
            {
                List<ThingDef> mergeFroms = toReplace.Keys.ToList();
                finished = true;
                foreach (ThingDef mergeFrom in mergeFroms)
                {
                    ThingDef mergeTo = toReplace[mergeFrom];
                    if (mergeFroms.Contains(mergeTo))
                    {
                        ThingDef newMergeTo = toReplace[mergeTo];
                        if (newMergeTo == mergeTo) continue;
                        if (!recurseWarningSent)
                        {
                            Log.Warning("[Cyanobot's Leather] Found multiple LeatherMergeDefs such that "
                            + mergeFrom + " is merged into " + mergeTo + ", which is itself merged into " + newMergeTo
                            + ". Attempting to resolve. Suppressing further warnings.");
                            recurseWarningSent = true;
                        }
                        DebugLog("Replacing mergeTo " + mergeTo + " with newMergeTo " + newMergeTo);
                        toReplace[mergeFrom] = newMergeTo;
                        finished = false;
                    }
                }
                i++;
            }
            while (!finished && i < 10);
            if (i >= 10)
            {
                Log.Warning("[Cyanobot's Leather] Recursive LeatherMergeDef processing exceeded 10 iterations,"
                     + " some defs may not be correctly merged.");
            }

            //finally, tidy up any entries that would replace with themselves
            toReplace.RemoveAll(kvp => kvp.Key == kvp.Value);

            DebugLog("Final toReplace: " + toReplace.ToStringFullContents());

        }
    
        public static void ApplyMerges()
        {
            //thingdefs - remove leather drops
            //a bunch of random replacements for things that could conceivably reference leather
            ThingDef replacement = null;

            foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                ReplaceAllInList(thingDef.costList);
                ReplaceAllInList(thingDef.killedLeavingsRanges);
                ReplaceAllInList(thingDef.killedLeavings);
                ReplaceAllInList(thingDef.killedLeavingsPlayerHostile);
                ReplaceAllInList(thingDef.smeltProducts);
                ReplaceAllInList(thingDef.butcherProducts);
                if (ReplacementFor(thingDef.defaultStuff, out replacement)) thingDef.defaultStuff = replacement;
                if (thingDef.building != null)
                {
                    if (ReplacementFor(thingDef.building.groundSpawnerThingToSpawn, out replacement))
                        thingDef.building.groundSpawnerThingToSpawn = replacement;
                }
                if (thingDef.race != null)
                {
                    if (ReplacementFor(thingDef.race.specificMeatDef, out replacement))
                        thingDef.race.specificMeatDef = replacement;
                    if (ReplacementFor(thingDef.race.leatherDef, out replacement))
                        thingDef.race.leatherDef = replacement;
                    if (ReplacementFor(thingDef.race.meatDef, out replacement))
                        thingDef.race.meatDef = replacement;
                }
                if (thingDef.plant != null)
                {
                    if (ReplacementFor(thingDef.plant.harvestedThingDef, out replacement))
                        thingDef.plant.harvestedThingDef = replacement;
                }
            }
            //recipe defs - ingredients, filters, products
            foreach (RecipeDef recipeDef in DefDatabase<RecipeDef>.AllDefsListForReading)
            {
                ReplaceAllInList(recipeDef.ingredients);
                ReplaceInFilter(recipeDef.fixedIngredientFilter);
                ReplaceInFilter(recipeDef.defaultIngredientFilter);
                ReplaceAllInList(recipeDef.products);
            }
            //terrain defs - cost list
            foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
            {
                ReplaceAllInList(terrainDef.costList);
            }
            //faction defs - apparel stuff
            foreach (FactionDef factionDef in DefDatabase<FactionDef>.AllDefsListForReading)
            {
                ReplaceInFilter(factionDef.apparelStuffFilter);
            }
            //scenario defs - starting things
            foreach (ScenarioDef scenarioDef in DefDatabase<ScenarioDef>.AllDefsListForReading)
            {
                if (!(scenarioDef.scenario?.AllParts).EnumerableNullOrEmpty())
                {
                    foreach (ScenPart scenPart in scenarioDef.scenario.AllParts)
                    {
                        if (scenPart is ScenPart_ThingCount)
                        {
                            ThingDef thingDef = (ThingDef)f_ScenPart_thingDef.GetValue(scenPart);
                            ThingDef stuff = (ThingDef)f_ScenPart_stuff.GetValue(scenPart);
                            if (ReplacementFor(thingDef, out replacement))
                                f_ScenPart_thingDef.SetValue(scenPart, replacement);
                            if (ReplacementFor(stuff, out replacement))
                                f_ScenPart_stuff.SetValue(scenPart, replacement);
                        }
                    }
                }
            }
            //trader kind defs - stock
            foreach (TraderKindDef traderKindDef in DefDatabase<TraderKindDef>.AllDefsListForReading)
            {
                if (traderKindDef.stockGenerators.NullOrEmpty()) continue;
                foreach (StockGenerator stockGenerator in traderKindDef.stockGenerators)
                {
                    if (stockGenerator is StockGenerator_BuySingleDef buySingleDef)
                    {
                        if (ReplacementFor(buySingleDef.thingDef, out replacement))
                            buySingleDef.thingDef = replacement;
                    }
                    else if (stockGenerator is StockGenerator_MultiDef multiDef)
                    {
                        List<ThingDef> thingDefs = (List<ThingDef>)f_StockGenerator_MultiDef_thingDefs.GetValue(multiDef);
                        ReplaceAllInList(thingDefs);
                    }
                    else if (stockGenerator is StockGenerator_SingleDef singleDef)
                    {
                        ThingDef thingDef = (ThingDef)f_StockGenerator_SingleDef_thingDef.GetValue(singleDef);
                        if (ReplacementFor(thingDef, out replacement))
                            f_StockGenerator_SingleDef_thingDef.SetValue(singleDef, replacement);
                    }
                }
            }
            //vef custom leather genes
            if (VEFLoaded)
            {
                foreach (GeneDef geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
                {
                    ReplaceInGeneExtension(geneDef);
                }
            }


            foreach(KeyValuePair<ThingDef,ThingDef> kvp in toReplace)
            {
                //ThingDef replace = kvp.Key;
                //ThingDef replaceWith = kvp.Value;

                RemoveThingDef(kvp.Key);
            }
        
        }

        public static bool ReplacementFor(ThingDef thingDef, out ThingDef replacement)
        {
            replacement = null;
            if (thingDef == null) return false;
            if (toReplace.ContainsKey(thingDef))
            {
                replacement = toReplace[thingDef];
                return true;
            }
            else return false;
        }

        public static void ReplaceInFilter(ThingFilter filter)
        {
            if (filter == null) return;
            foreach (ThingDef replace in toReplace.Keys)
            {
                if (filter.Allows(replace))
                {
                    filter.SetAllow(replace, false);
                    filter.SetAllow(toReplace[replace], true);
                }
            }
        }

        public static void ReplaceAllInList(List<IngredientCount> list)
        {
            if (list.NullOrEmpty()) return;
            foreach (IngredientCount ingredient in list)
            {
                ReplaceInFilter(ingredient.filter);
            }
        }

        public static void ReplaceAllInList(List<ThingDefCountRangeClass> list)
        {
            if (list.NullOrEmpty()) return;
            foreach (ThingDefCountRangeClass tdcrc in list)
            {
                if (ReplacementFor(tdcrc.thingDef, out ThingDef replacement)) tdcrc.thingDef = replacement;
            }
        }

        public static void ReplaceAllInList(List<ThingDefCountClass> list)
        {
            if (list.NullOrEmpty()) return;
            foreach (ThingDefCountClass tdcc in list)
            {
                if (ReplacementFor(tdcc.thingDef, out ThingDef replacement)) tdcc.thingDef = replacement;
            }
        }

        public static void ReplaceAllInList(List<ThingDef> list)
        {
            if (list.NullOrEmpty()) return;
            List<ThingDef> newList = list.ToList();
            foreach (ThingDef def in newList)
            {
                if (ReplacementFor(def, out ThingDef replacement))
                {
                    list.Remove(def);
                    list.AddDistinct(replacement);
                }
            }
        }

        public static void ReplaceInGeneExtension(GeneDef geneDef)
        {
            DefModExtension vefExtension = geneDef.modExtensions?.Find(dme => dme.GetType() == t_GeneExtension);
            if (vefExtension == null) return; 
            
            ThingDef customLeatherThingDef = f_customLeatherThingDef.GetValue(vefExtension) as ThingDef;
            if (ReplacementFor(customLeatherThingDef, out ThingDef replacement))
                f_customLeatherThingDef.SetValue(vefExtension, replacement);
        }

        public static void RemoveThingDef(ThingDef thingDef)
        {
            thingDef.tradeability = Tradeability.None;
            thingDef.destroyOnDrop = true;
            thingDef.deepCommonality = 0f;
            thingDef.generateCommonality = 0f;
            thingDef.generateAllowChance = 0f;
            thingDef.relicChance = 0f;

            m_ThingDefDatabase_Remove.Invoke(null, new object[] { thingDef });
            removedDefs.AddDistinct(thingDef);
        }
    }
}
