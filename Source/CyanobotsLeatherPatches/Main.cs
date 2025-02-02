using HarmonyLib;
using ProcessorFramework;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using static CyanobotsLeather.Settings;

namespace CyanobotsLeather
{
    public class Main : Mod
    {
        public static bool VEFLoaded = false;
        public static bool AALoaded = false;
        public static bool humanButcheryLoaded = false;
        public static bool coloredLeatherLoaded = false;
        public static bool betterWoolLoaded = false;
        public static bool oncaUnciaLoaded = false;

        //public static Dictionary<ThingDef, List<ThingDef>> shearableHideAnimals = new Dictionary<ThingDef, List<ThingDef>>();


        public static Type t_CompProperties_AnimalProduct;
        public static FieldInfo f_resourceDef;
        public static FieldInfo f_resourceAmount;
        public static FieldInfo f_seasonalItems;
        public static Type t_CompAnimalProduct;
        public static FieldInfo f_fullness;
        public static Type t_GeneExtension; 
        public static FieldInfo f_customLeatherThingDef; 

        public static Type t_CompColorNoStack;
        public static Type t_CompTanningColor;
        public static PropertyInfo p_BaseColor;
        public static PropertyInfo p_Weight;
        public static Type t_DefModExtension_ReceivesButcherColor;

        public static Type t_CustomLeather;
        public static FieldInfo f_CustomLeather_leatherDef;

        public static Harmony harmony;

        public Main(ModContentPack mcp) : base(mcp)
        {
            GetSettings<Settings>();

            VEFLoaded = ModsConfig.IsActive("OskarPotocki.VanillaFactionsExpanded.Core");
            AALoaded = ModsConfig.IsActive("sarg.alphaanimals");
            coloredLeatherLoaded = ModsConfig.IsActive("cyanobot.coloredleatherandwool");
            humanButcheryLoaded = ModsConfig.IsActive("DoctorStupid.PrettySkin");
            betterWoolLoaded = ModsConfig.IsActive("divineDerivative.AutoWool");
            oncaUnciaLoaded = ModsConfig.IsActive("cyanobot.OncaAndUncia");

            t_CompProperties_AnimalProduct = VEFLoaded ? AccessTools.TypeByName("AnimalBehaviours.CompProperties_AnimalProduct") : null;
            t_CompAnimalProduct = VEFLoaded ? AccessTools.TypeByName("AnimalBehaviours.CompAnimalProduct") : null;
            f_resourceDef = VEFLoaded ? AccessTools.Field(t_CompProperties_AnimalProduct, "resourceDef") : null;
            f_resourceAmount = VEFLoaded ? AccessTools.Field(t_CompProperties_AnimalProduct, "resourceAmount") : null;
            f_seasonalItems = VEFLoaded ? AccessTools.Field(t_CompProperties_AnimalProduct, "seasonalItems") : null;
            f_fullness = AccessTools.Field(typeof(CompHasGatherableBodyResource), "fullness");
            t_GeneExtension = VEFLoaded ? AccessTools.TypeByName("VanillaGenesExpanded.GeneExtension") : null;
            f_customLeatherThingDef = VEFLoaded ? AccessTools.Field(t_GeneExtension, "customLeatherThingDef") : null;            

            t_CompColorNoStack = coloredLeatherLoaded ? AccessTools.TypeByName("ColoredLeatherAndWool.CompColorNoStack") : null;
            t_CompTanningColor = coloredLeatherLoaded ? AccessTools.TypeByName("ColoredLeatherAndWool.CompTanningColor") : null;
            p_BaseColor = coloredLeatherLoaded ? AccessTools.Property(t_CompTanningColor, "BaseColor") : null;
            p_Weight = coloredLeatherLoaded ? AccessTools.Property(t_CompTanningColor, "Weight") : null;
            t_DefModExtension_ReceivesButcherColor = coloredLeatherLoaded ? AccessTools.TypeByName("ColoredLeatherAndWool.DefModExtension_ReceivesButcherColor") : null;

            t_CustomLeather = oncaUnciaLoaded ? AccessTools.TypeByName("OncaAndUncia.CustomLeather") : null;
            f_CustomLeather_leatherDef = oncaUnciaLoaded ? AccessTools.Field(t_CustomLeather, "leatherDef") : null;

            harmony = new Harmony("cyanobot.CyanobotsLeatherPatches");
            harmony.PatchAll();
        }

        public override string SettingsCategory()
        {
            return "Cyanobot's Leather Overhaul";
        }

        static public void ApplySettings()
        {
            foreach (ProcessDef processDef in ImpliedDefUtility.tanningProcesses.Values)
            {
                processDef.processDays = tanningDays;
            }
        }

        public override void DoSettingsWindowContents(Rect inRect) => Settings.DoSettingsWindowContents(inRect);
    }

}
