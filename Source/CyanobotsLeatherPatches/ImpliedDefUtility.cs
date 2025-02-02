using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static CyanobotsLeather.Main;
using static CyanobotsLeather.LeatherColorUtility;
using static CyanobotsLeather.WoolUtility;
using static CyanobotsLeather.LogUtil;
using static CyanobotsLeather.TextUtility;
using ProcessorFramework;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Data;
using System.Reflection.Emit;

namespace CyanobotsLeather
{
    public static class ImpliedDefUtility
    {
        public static Dictionary<ThingDef, ThingDef> woolAnimals = new Dictionary<ThingDef, ThingDef>();
        public static Dictionary<ThingDef, ThingDef> leatherAnimals = new Dictionary<ThingDef, ThingDef>();
        public static List<ThingDef> leatherDefs = new List<ThingDef>();
        public static Dictionary<GeneDef, ThingDef> geneLeathers = new Dictionary<GeneDef, ThingDef>();
        public static List<ThingDef> humanLeathers = new List<ThingDef>();

        public static Dictionary<ThingDef, ProcessDef> tanningProcesses = new Dictionary<ThingDef, ProcessDef>();
        public static List<string> usedDefNames = new List<string>();
        public static Dictionary<string, ThingDef> generatedWoolHideDefs = new Dictionary<string, ThingDef>();
        public static Dictionary<ThingDef, ThingDef> basicHideByLeather = new Dictionary<ThingDef, ThingDef>();


        public static bool IsValidLeather(ThingDef def)
        {
            //anything blacklisted is not a valid leather
            if (CyanobotsLeather_DefOf.CYB_LeatherInclusion.blacklist.Contains(def)) return false;

            //anything whitelisted is definitey a valid leather
            if (CyanobotsLeather_DefOf.CYB_LeatherInclusion.whitelist.Contains(def)) return true;

            //if it's not an Item with stuffProps, it's probably not a normal leather
            if (def.category != ThingCategory.Item) return false;
            if (def.stuffProps == null) return false;

            //by default we want to exclude chitins, shells
            string defName = def.defName;
            if (defName.Contains("Chitin")
                || defName.Contains("chitin")
                || defName.Contains("Shell")
                || defName.Contains("shell")
                )
            {
                return false;
            }

            //otherwise probably a leather
            return true;
        }

        public static void ReadAllLeatherAndWoolDefs()
        {
            woolAnimals.Clear();
            leatherAnimals.Clear();
            leatherDefs.Clear();
            geneLeathers.Clear();
            humanLeathers.Clear();
            basicHideByLeather.Clear();
            tanningProcesses.Clear();
            usedDefNames.Clear();
            generatedWoolHideDefs.Clear();
            basicHideByLeather.Clear();

            DefListDef leatherInclusion = CyanobotsLeather_DefOf.CYB_LeatherInclusion;
            DebugLog("leatherInclusion.whitelist: " + leatherInclusion.whitelist.ToStringSafeEnumerable()
                + ", blacklist: " + leatherInclusion.blacklist.ToStringSafeEnumerable());

            //everything in leather whitelist should be logged as a leather
            //whether or not we find an animal that drops it
            foreach (Def def in leatherInclusion.whitelist)
            {
                if (def is ThingDef thingDef) leatherDefs.AddDistinct(thingDef);
            }

            //check all pawn defs for their leather and wool
            foreach (ThingDef pawnDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                //skip if not a pawn
                if (pawnDef.race == null) continue;

                ThingDef leatherDef = pawnDef.race.leatherDef;

                //ignore if no leather, if already logged, or not valid leather
                if (leatherDef != null
                    && IsValidLeather(leatherDef)
                    ) 
                {
                    leatherDefs.AddDistinct(leatherDef);
                    leatherAnimals.Add(pawnDef, leatherDef);
                }

                ThingDef woolDef = WoolDefFor(pawnDef);
                if (woolDef != null)
                {
                    woolAnimals.Add(pawnDef, woolDef);
                }
            }
        }

        public static ThingDef GenerateAndRegisterBasicHide(ThingDef leatherDef, bool hotReload = false)
        {
            ThingDef hideDef = GenerateHide(leatherDef, hotReload);
            if (usedDefNames.Contains(hideDef.defName))
            {
                Log.Error("[Cyanobot's Leather] Tried to generate multiple hides with defName: " + hideDef.defName);
                return null;
            }
            usedDefNames.Add(hideDef.defName);
            DefGenerator.AddImpliedDef(hideDef, hotReload);
            GenerateOrUpdateTanningProcess(hideDef, leatherDef, hotReload);

            if (!basicHideByLeather.ContainsKey(leatherDef))
            {
                basicHideByLeather.Add(leatherDef, hideDef);
            }

            return hideDef;
        }

        public static ThingDef GenerateAndRegisterWoolHide(ThingDef animalDef, ThingDef woolDef, ThingDef shornDef, ThingDef tannedDef, bool hotReload)
        {
            LogUtil.DebugLog("GenerateAndRegisterWoolHide - "
                + "animalDef: " + animalDef
                + ", woolDef: " + woolDef
                + ", shornDef: " + shornDef
                + ", tannedDef: " + tannedDef
                );
            if (animalDef == null || woolDef == null || shornDef == null || tannedDef == null) return null;

            ThingDef hideDef = GenerateOrGetWoolHide(animalDef, woolDef, shornDef, tannedDef, hotReload);
            string key = WoolHideKey(woolDef, shornDef, tannedDef);
            if (!generatedWoolHideDefs.ContainsKey(key))
            {
                DefGenerator.AddImpliedDef(hideDef, hotReload);
                GenerateOrUpdateTanningProcess(hideDef, tannedDef, hotReload);
                generatedWoolHideDefs.Add(key, hideDef);
            }
            return hideDef;
        }

        public static void RegisterTanningProcess(ProcessDef processDef, bool hotReload)
        {
            DefGenerator.AddImpliedDef(processDef, hotReload);
        }

        public static ProcessDef GenerateOrUpdateTanningProcess(ThingDef hideDef, ThingDef leatherDef, bool hotReload)
        {
            ProcessDef processDef;
            if (tanningProcesses.ContainsKey(leatherDef))
            {
                processDef = tanningProcesses[leatherDef];
            }
            else
            {
                processDef = GenerateTanningProcess(hideDef, leatherDef, hotReload);
                tanningProcesses[leatherDef] = processDef;
            }

            processDef.ingredientFilter.SetAllow(hideDef, true);

            ThingDef vat = CyanobotsLeather_DefOf.CYB_TanningVat;
            CompProperties_Processor props_Processor = vat.GetCompProperties<CompProperties_Processor>();
            props_Processor.processes.AddDistinct(processDef);

            return processDef;
        }

        public static ProcessDef GenerateTanningProcess(ThingDef hideDef, ThingDef leatherDef, bool hotReload)
        {
            ProcessDef baseDef = CyanobotsLeather_DefOf.CYB_TanningProcess_Base;

            string defName = "CYB_TanningProcess_" + leatherDef.defName;
            ProcessDef processDef = (hotReload ? DefDatabase<ProcessDef>.GetNamed(defName) : new ProcessDef()) ?? new ProcessDef();

            processDef.defName = defName;
            processDef.thingDef = leatherDef;
            processDef.ingredientFilter = new ThingFilter();
            processDef.processDays = Settings.tanningDays;
            processDef.capacityFactor = baseDef.capacityFactor;
            processDef.efficiency = baseDef.efficiency;
            processDef.usesTemperature = baseDef.usesTemperature;
            processDef.temperatureSafe = baseDef.temperatureSafe;
            processDef.temperatureIdeal = baseDef.temperatureIdeal;
            processDef.ruinedPerDegreePerHour = baseDef.ruinedPerDegreePerHour;
            processDef.speedBelowSafe = baseDef.speedBelowSafe;
            processDef.speedAboveSafe = baseDef.speedAboveSafe;
            processDef.sunFactor = baseDef.sunFactor;
            processDef.rainFactor = baseDef.rainFactor;
            processDef.snowFactor = baseDef.snowFactor;
            processDef.windFactor = baseDef.windFactor;
            processDef.unpoweredFactor = baseDef.unpoweredFactor;
            processDef.unfueledFactor = baseDef.unfueledFactor;
            processDef.powerUseFactor = baseDef.powerUseFactor;
            processDef.fuelUseFactor = baseDef.fuelUseFactor;
            processDef.filledGraphicSuffix = baseDef.filledGraphicSuffix;
            processDef.usesQuality = baseDef.usesQuality;
            processDef.qualityDays = baseDef.qualityDays;
            processDef.customLabel = baseDef.customLabel;
            processDef.destroyChance = baseDef.destroyChance;
            processDef.bonusOutputs = baseDef.bonusOutputs;

            return processDef;
        }

        public static ThingDef GenerateHide(ThingDef leatherDef, bool hotReload, string defName="")
        {
            if (defName == "") defName = BasicHideDefNameFor(leatherDef);
            ThingDef hideDef = (hotReload ? DefDatabase<ThingDef>.GetNamed(defName) : new ThingDef()) ?? new ThingDef();
            hideDef.defName = defName;
            hideDef.label = BasicHideLabelFor(leatherDef);
            hideDef.description = BasicHideDescriptionFor(leatherDef);
            hideDef.descriptionHyperlinks = new List<DefHyperlink>
            {
                new DefHyperlink(leatherDef)
            };
            hideDef.thingClass = typeof(ThingWithComps);
            hideDef.category = ThingCategory.Item;
            hideDef.stackLimit = 75;
            hideDef.comps = new List<CompProperties>
            {
                new CompProperties_Forbiddable(),
                new CompProperties(typeof(CompColorable)),
                new CompProperties_TannableHide()
                {
                    leatherDef = leatherDef
                }
            };
            hideDef.drawerType = DrawerType.MapMeshOnly;
            hideDef.resourceReadoutPriority = ResourceCountPriority.Middle;
            hideDef.useHitPoints = true;
            hideDef.selectable = true;
            hideDef.altitudeLayer = AltitudeLayer.Item;
            hideDef.statBases = GetHideStatBases(leatherDef);
            hideDef.alwaysHaulable = true;
            hideDef.drawGUIOverlay = true;
            hideDef.rotatable = false;
            hideDef.pathCost = 14;
            hideDef.allowedArchonexusCount = 80;
            hideDef.graphicData = new GraphicData()
            {
                texPath = "Things/Item/Resource/CYB_Hide",
                graphicClass = typeof(Graphic_StackCount),
                color = GetTextileColor(leatherDef)
            };
            hideDef.thingCategories = new List<ThingCategoryDef>
            {
                ThingCategoryDefOf.Leathers,
                CyanobotsLeather_DefOf.CYB_Hides
            };
            hideDef.burnableByRecipe = true;
            hideDef.healthAffectsPrice = false;
            hideDef.minRewardCount = 30;
            hideDef.tradeTags = new List<string>() { "Hides" };
            hideDef.stuffProps = GetHideStuffProps(leatherDef, hideDef);

            if (coloredLeatherLoaded)
            {
                hideDef.comps.Add(new CompProperties(t_CompColorNoStack));
                if (hideDef.modExtensions == null) hideDef.modExtensions = new List<DefModExtension>();
                if (t_DefModExtension_ReceivesButcherColor != null)
                {
                    hideDef.modExtensions.Add(Activator.CreateInstance(t_DefModExtension_ReceivesButcherColor) as DefModExtension);
                }
            }

            return hideDef;
        }

        public static ThingDef GenerateOrGetWoolHide(ThingDef animalDef, ThingDef woolDef, ThingDef shornDef, ThingDef tannedDef, bool hotReload)
        {
            string key = WoolHideKey(woolDef, shornDef, tannedDef);
            //DebugLog("woolHideKey: " + key);
            if (generatedWoolHideDefs.ContainsKey(key)) return generatedWoolHideDefs[key];

            string defName = WoolHideDefNameFor(woolDef, shornDef, tannedDef);
            if (usedDefNames.Contains(defName))
            {
                Log.Error("[Cyanobot's Leather] Tried to generate multiple hides with defName: " + defName);
                return null;
            }
            usedDefNames.Add(defName);

            ThingDef hideDef = GenerateHide(tannedDef, hotReload, defName);
            hideDef.defName = defName;
            hideDef.label = WoolHideLabelFor(animalDef, tannedDef);
            hideDef.description = WoolHideDescriptionFor(woolDef, shornDef, tannedDef);
            hideDef.descriptionHyperlinks.Add(new DefHyperlink(woolDef));
            hideDef.descriptionHyperlinks.Add(new DefHyperlink(shornDef));
            hideDef.comps.Add(
                new CompProperties_ShearableHide()
                {
                    woolDef = woolDef,
                    shornHideDef = shornDef,
                    defaultWoolPerUnit = 0.5f * WoolAmountPerLeatherFor(animalDef)
                });
            Color color = GetTextileColor(woolDef);
            hideDef.graphicData.color = color;
            hideDef.stuffProps.color = color;

            return hideDef;
        }

        public static ThingDef GenerateWoolHideFromAnimal(ThingDef animalDef, ThingDef woolDef, bool hotReload)
        {
            ThingDef origLeatherDef = leatherAnimals.ContainsKey(animalDef) ? leatherAnimals[animalDef] : null;
            if (origLeatherDef == null) return null;

            DME_LeatherExtension leatherExtension = origLeatherDef.GetModExtension<DME_LeatherExtension>();
            
            ThingDef shornDef = leatherExtension?.shornEquivalent ?? origLeatherDef;
            shornDef = basicHideByLeather.ContainsKey(shornDef) ? basicHideByLeather[shornDef] : shornDef;

            ThingDef tannedDef = leatherExtension?.woollyEquivalent ?? CyanobotsLeather_DefOf.CYB_Leather_Wool;
            if (leatherExtension != null && leatherExtension.tanFromWoolHide) tannedDef = origLeatherDef;

            if (AALoaded && animalDef.defName == "AA_ChameleonYak")
            {
                DebugLog("Found chameleon yak.");

                GenerateAndRegisterChameleonYakHides(animalDef, shornDef, tannedDef, hotReload);

                return null;
            }

            return GenerateAndRegisterWoolHide(animalDef, woolDef, shornDef, tannedDef, hotReload);
        }
        public static void GenerateAndRegisterChameleonYakHides(ThingDef yakDef, ThingDef shornDef, ThingDef tannedDef, bool hotReload)
        {
            CompProperties props_AnimalProduct = yakDef.comps.Find(p => p.GetType() == t_CompProperties_AnimalProduct);
            List<string> seasonalItems = f_seasonalItems.GetValue(props_AnimalProduct) as List<string>;
            if (seasonalItems.NullOrEmpty()) return;

            List<ThingDef> woolDefs = seasonalItems.Select(s => DefDatabase<ThingDef>.GetNamed(s)).ToList();
            DebugLog("yak woolDefs: " + woolDefs.ToStringSafeEnumerable());
            foreach (ThingDef yakWool in woolDefs)
            {
                string labelSuffix = "";
                if (yakWool.defName == "AA_ChameleonYakWoolTemperate")
                {
                    labelSuffix = ", temperate";
                }
                else if (yakWool.defName == "AA_ChameleonYakWoolWinter")
                {
                    labelSuffix = ", winter";
                }
                else if (yakWool.defName == "AA_ChameleonYakWoolJungle")
                {
                    labelSuffix = ", jungle";
                }
                else if (yakWool.defName == "AA_ChameleonYakWoolDesert")
                {
                    labelSuffix = ", desert";
                }
                else
                {
                    continue;
                }

                ThingDef hideDef = GenerateAndRegisterWoolHide(yakDef, yakWool, shornDef, tannedDef, hotReload);
                hideDef.label += labelSuffix;
            }
        }
        public static List<StatModifier> GetHideStatBases(ThingDef leatherDef)
        {
            List<StatModifier> statBases = new List<StatModifier>();
            foreach (StatModifier statModifier in leatherDef.statBases)
            {
                StatDef statDef = statModifier.stat;
                float statValue = statModifier.value;

                if (statDef == StatDefOf.Beauty)
                {
                    statValue = -4f;
                }
                else if (statDef == StatDefOf.StuffPower_Insulation_Cold
                        || statDef == StatDefOf.StuffPower_Insulation_Heat)
                {
                    statValue *= 0.7f;
                }
                else if (statDef == StatDefOf.StuffPower_Armor_Blunt
                        || statDef == StatDefOf.StuffPower_Armor_Heat
                        || statDef == StatDefOf.StuffPower_Armor_Sharp)
                {
                    statValue *= 0.5f;
                }
                else if (statDef == StatDefOf.MarketValue
                        || statDef == StatDefOf.MaxHitPoints)
                {
                    statValue *= 0.5f;
                }
                else if (statDef == StatDefOf.Mass)
                {
                    statValue *= 1.2f;
                }

                StatModifier newStatModifier = new StatModifier()
                {
                    stat = statDef,
                    value = statValue
                };

                statBases.Add(newStatModifier);
            }
            return statBases;
        }

        public static StuffProperties GetHideStuffProps(ThingDef leatherDef, ThingDef hideDef)
        {
            StuffProperties oldStuffProperties = leatherDef.stuffProps;
            if (oldStuffProperties == null)
            {
                Log.Error("[Cyanobot's Leather Overhaul] Could not find stuff properties for leather: " + leatherDef);
                return null;
            }

            //LogUtil.DebugLog("GetHideStuffProps - leatherDef: " + leatherDef + ", hideDef: " + hideDef
            //    + ", oldStuffProperties: " + oldStuffProperties + ", old statFactors: " + oldStuffProperties?.statFactors
            //    + ", old statOffsets: " + oldStuffProperties?.statOffsets);

            List<StatModifier> statOffsets = new List<StatModifier>();
            if (!oldStuffProperties.statOffsets.NullOrEmpty())
            {
                foreach (StatModifier statOffset in oldStuffProperties.statOffsets)
                {
                    StatDef statDef = statOffset.stat;
                    float statValue = statOffset.value;

                    StatModifier newStatModifier = new StatModifier()
                    {
                        stat = statDef,
                        value = statValue
                    };

                    statOffsets.Add(newStatModifier);
                }
            }

            List<StatModifier> statFactors = new List<StatModifier>();
            if (!oldStuffProperties.statFactors.NullOrEmpty())
            {
                foreach (StatModifier statFactor in oldStuffProperties.statFactors)
                {
                    StatDef statDef = statFactor.stat;
                    float statValue = statFactor.value;

                    if (statDef == StatDefOf.MaxHitPoints)
                    {
                        statValue *= 0.5f;
                    }
                    else if (statDef == StatDefOf.Beauty)
                    {
                        statValue *= 0.2f;
                    }
                    else if (statDef == StatDefOf.Mass
                            || statDef == StatDefOf.EquipDelay)
                    {
                        statValue *= 1.2f;
                    }
                    else if (statDef == StatDefOf.Comfort)
                    {
                        statValue *= 0.6f;
                    }

                    StatModifier newStatModifier = new StatModifier()
                    {
                        stat = statDef,
                        value = statValue
                    };

                    statFactors.Add(newStatModifier);
                }
            }

            //LogUtil.DebugLog("About to generate new stuffprops");
            StuffProperties stuffProperties = new StuffProperties()
            {
                parent = hideDef,
                stuffAdjective = hideDef.label,
                commonality = oldStuffProperties.commonality * 0.05f,
                categories = new List<StuffCategoryDef>()
                {
                    CyanobotsLeather_DefOf.CYB_Hide
                },
                allowedInStuffGeneration = true,
                statOffsets = statOffsets,
                statFactors = statFactors,
                color = hideDef.graphicData.color,
                constructEffect = oldStuffProperties.constructEffect,
                appearance = oldStuffProperties.appearance,
                allowColorGenerators = oldStuffProperties.allowColorGenerators,
                canSuggestUseDefaultStuff = oldStuffProperties.canSuggestUseDefaultStuff,
                soundImpactBullet = oldStuffProperties.soundImpactBullet,
                soundImpactMelee = oldStuffProperties.soundImpactMelee,
                soundMeleeHitSharp = oldStuffProperties.soundMeleeHitSharp,
                soundMeleeHitBlunt = oldStuffProperties.soundMeleeHitBlunt
            };

            return stuffProperties;
        }

        public static void ReadVEFGenes()
        {
            if (!VEFLoaded) return;

            foreach (GeneDef geneDef in DefDatabase<GeneDef>.AllDefsListForReading)
            {
                DefModExtension vefExtension = geneDef.modExtensions?.Find(dme => dme.GetType() == t_GeneExtension);
                if (vefExtension == null) continue;

                ThingDef leatherDef = f_customLeatherThingDef.GetValue(vefExtension) as ThingDef;
                if (leatherDef != null
                    && IsValidLeather(leatherDef)
                    )
                {
                    leatherDefs.AddDistinct(leatherDef);
                    geneLeathers.Add(geneDef, leatherDef);
                    if (!leatherDefs.Contains(leatherDef))
                    {
                        leatherDefs.Add(leatherDef);
                        //if it wasn't already registered by an animal its probably unique to humans
                        humanLeathers.Add(leatherDef);
                    }
                }
            }
        }

        public static void AssignLeatherToGene(GeneDef geneDef, ThingDef leatherDef)
        {
            DefModExtension vefExtension = geneDef.modExtensions?.Find(dme => dme.GetType() == t_GeneExtension);
            if (vefExtension == null) return;

            f_customLeatherThingDef.SetValue(vefExtension, leatherDef);
        }
    }
}
