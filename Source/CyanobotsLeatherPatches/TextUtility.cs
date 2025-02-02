using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static CyanobotsLeather.LogUtil;

namespace CyanobotsLeather
{
    public static class TextUtility
    {
        public const string hideDefNamePrefix = "CYB_Hide_";
        public const string woolDefNameInfix = "Wool_";

        public static string WoolHideKey(ThingDef woolDef, ThingDef shornDef, ThingDef tannedDef)
        {
            return shornDef.defName + woolDef.defName + tannedDef.defName;
        }

        public static string BasicHideDefNameFor(ThingDef leatherDef)
        {
            string leatherName = leatherDef.defName;
            if (leatherName.StartsWith("CYB_"))
            {
                leatherName = leatherName.Substring(4);
            }
            if (leatherName.StartsWith("Leather_"))
            {
                leatherName = leatherName.Substring(8);
            }
            return hideDefNamePrefix + leatherName;
        }

        public static string WoolHideDefNameFor(ThingDef woolDef, ThingDef shornDef, ThingDef tannedDef)
        {
            /*
            LogUtil.DebugLog("WoolHideDefNameFor - "
                + "woolDef: " + woolDef
                + ", shornDef: " + shornDef
                + ", tannedDef: " + tannedDef);
            */            string woolName = woolDef.defName;

            if (woolName.StartsWith("Wool_"))
            {
                woolName = woolName.Substring(5);
            }
            if (woolName.StartsWith("Wool"))
            {
                woolName = woolName.Substring(4);
            }
            //DebugLog("woolName:" + woolName);

            string defName = hideDefNamePrefix + woolDefNameInfix + woolName;
            //DebugLog("defName initial: " + defName);

            if (ImpliedDefUtility.usedDefNames.Contains(defName))
            {
                string shornName = shornDef.defName;
                if (shornName.StartsWith(hideDefNamePrefix))
                {
                    shornName = shornName.Substring(hideDefNamePrefix.Length);
                }
                defName += "_" + shornName;
                //DebugLog("defName already used, attempting with shornDef also: " + defName);
            }
            if (ImpliedDefUtility.usedDefNames.Contains(defName))
            {
                defName += "_" + tannedDef.defName;
                //DebugLog("defName already used, attempting with tannedDef also: " + defName);
            }
            return defName;
        }


        public static string BasicHideLabelFor(ThingDef leatherDef)
        {
            DME_LeatherExtension leatherExtension = leatherDef.GetModExtension<DME_LeatherExtension>();
            if (!(leatherExtension?.hideLabel).NullOrEmpty()) return leatherExtension.hideLabel;

            string leatherLabel = leatherDef.label;
            string hideLabel;

            if (leatherLabel.EndsWith("leather") || leatherLabel.EndsWith("Leather"))
            {
                leatherLabel = leatherLabel.Substring(0, leatherLabel.Length - 7);
                hideLabel = leatherLabel + "hide".Translate();
            }
            else if (leatherLabel.EndsWith("fur") || leatherLabel.EndsWith("Fur"))
            {
                leatherLabel = leatherLabel.Substring(0, leatherLabel.Length - 3);
                hideLabel = leatherLabel + "pelt".Translate();
            }
            else if (leatherLabel.EndsWith("skin") || leatherLabel.EndsWith("Skin"))
            {
                leatherLabel = leatherLabel.Substring(0, leatherLabel.Length - 4);
                hideLabel = leatherLabel + "hide".Translate();
            }
            else
            {
                hideLabel = leatherLabel;
            }

            hideLabel = "raw".Translate() + " " + hideLabel;
            return hideLabel;
        }

        public static string WoolHideLabelFor(ThingDef animalDef, ThingDef tannedDef)
        {
            DME_LeatherExtension leatherExtension = tannedDef.GetModExtension<DME_LeatherExtension>();
            if (leatherExtension != null && !leatherExtension.woolHideLabel.NullOrEmpty())
            {
                return leatherExtension.woolHideLabel;
            }
            else return "RawPeltOf".Translate(animalDef);
        }

        public static string BasicHideDescriptionFor(ThingDef leatherDef)
        {
            DME_LeatherExtension leatherExtension = leatherDef.GetModExtension<DME_LeatherExtension>();
            if (!(leatherExtension?.hideDescription).NullOrEmpty()) return leatherExtension.hideDescription;

            return "HideDesc_Generic".Translate(leatherDef.Named("TANNED"));
        }

        public static string WoolHideDescriptionFor(ThingDef woolDef, ThingDef shornDef, ThingDef tannedDef)
        {
            return "WoolPeltDesc".Translate(tannedDef.Named("TANNED"), shornDef.Named("SHORN"), woolDef.Named("WOOL"));
        }
    }
}
