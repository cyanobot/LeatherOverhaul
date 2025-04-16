using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static CyanobotsLeather.Main;
using static CyanobotsLeather.LeatherColorUtility;
using static CyanobotsLeather.ImpliedDefUtility;
using System.Security.Cryptography;
using ProcessorFramework;
using System.Reflection;
using UnityEngine;
using static CyanobotsLeather.LogUtil;
using static CyanobotsLeather.DefRemoval;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    [HarmonyAfter(new string[] { "divineDerivative.AutoWool", "cyanobot.CPEPatches" })]        //better wool production, cloth production expanded patches
    public static class Patch_GenerateImpliedDefs_PreResolve_Main
    {
        public static void Postfix(bool hotReload)
        {
            DebugLog("Patch_GenerateImpliedDefs_PreResolve Postfix");

            DefRemoval.LoadMerges();
            DefRemoval.ApplyMerges();

            //find out what leathers and wools we're working with
            ReadAllLeatherAndWoolDefs();
            ReadVEFGenes();

            DebugLog("leatherDefs: " + leatherDefs.ToStringSafeEnumerable());
            DebugLog("leatherAnimals: " + leatherAnimals.ToStringFullContents());
            DebugLog("woolAnimals: " + woolAnimals.ToStringFullContents());

            //generate an untanned hide for each leather
            foreach (ThingDef leatherDef in leatherDefs)
            {
                DME_LeatherExtension leatherExtension = leatherDef.GetModExtension<DME_LeatherExtension>();
                if (leatherExtension == null || leatherExtension.generateBasicHide)
                {
                    ThingDef hideDef = GenerateAndRegisterBasicHide(leatherDef, hotReload);
                    if (humanLeathers.Contains(leatherDef))
                    {
                        Patch_ThoughtWorker_HumanLeatherApparel_CurrentThoughtState
                            .humanHideDefNames.AddDistinct(hideDef.defName);
                    }
                }                
            }
            DebugLog("basicHideByLeather: " + basicHideByLeather.ToStringFullContents());
            DebugLog("humanHideDefNames: " + Patch_ThoughtWorker_HumanLeatherApparel_CurrentThoughtState
                            .humanHideDefNames.ToStringSafeEnumerable());

            //generate a wool hide for each woolly animal
            //and make it the dropped leather for that animal
            foreach (KeyValuePair<ThingDef,ThingDef> kvp in woolAnimals)
            {
                ThingDef hideDef = GenerateWoolHideFromAnimal(kvp.Key, kvp.Value, hotReload);
                if (hideDef != null)
                {
                    kvp.Key.race.leatherDef = hideDef;
                }
            }
            if (AALoaded)
            {
                ThingDef yakDef = DefDatabase<ThingDef>.GetNamed("AA_ChameleonYak", false);
                if (yakDef != null)
                {
                    GenerateWoolHideFromAnimal(yakDef, null, hotReload);
                }
            }
            DebugLog("generated woolHides: " + generatedWoolHideDefs.Values.ToStringSafeEnumerable());

            //for each animal that drops a leather we've replaced
            //set it to drop the untanned hide instead
            foreach (KeyValuePair<ThingDef,ThingDef> kvp in leatherAnimals)
            {
                ThingDef animalDef = kvp.Key;
                ThingDef leatherDef = kvp.Value;

                //skip any animals whose leather no longer matches up, eg because they're wool animals
                if (animalDef.race.leatherDef != leatherDef) continue;

                ThingDef hideDef = basicHideByLeather.ContainsKey(leatherDef) ? basicHideByLeather[leatherDef] : null;
                if (hideDef != null)
                {
                    animalDef.race.leatherDef = hideDef;
                }
            }

            //for each gene that defines a human leather
            if (VEFLoaded)
            {
                foreach (KeyValuePair<GeneDef, ThingDef> kvp in geneLeathers)
                {
                    GeneDef geneDef = kvp.Key;
                    ThingDef leatherDef = kvp.Value;

                    ThingDef hideDef = basicHideByLeather.ContainsKey(leatherDef) ? basicHideByLeather[leatherDef] : null;
                    if (hideDef != null)
                    {
                        AssignLeatherToGene(geneDef, hideDef);
                    }
                }
            }            

            //make sure the tanning processes are known the the DefDatabase
            //and assigned to the tanning vat
            foreach (ProcessDef processDef in tanningProcesses.Values)
            {
                RegisterTanningProcess(processDef,hotReload);
            }
        }
    }
}
