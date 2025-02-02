using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static CyanobotsLeather.Main;

namespace CyanobotsLeather
{
    static class LeatherColorUtility
    {
        public static Color GetTextileColor(ThingDef textileDef)
        {
            //LogUtil.DebugLog("GetTextileColor - textileDef: " + textileDef
            //    + ", stuffProps.color: " + textileDef.stuffProps?.color
            //    + ", graphicData.color: " + textileDef.graphicData.color);
            if (textileDef.stuffProps != null) return textileDef.stuffProps.color;
            else return textileDef.graphicData.color;
        }

        public static Color GetTextileColor(Thing textile)
        {
            CompColorable compColorable = textile.TryGetComp<CompColorable>();
            if (compColorable != null) return compColorable.Color;
            else if (textile.def.stuffProps != null) return textile.def.stuffProps.color;
            else return textile.DrawColor;
        }

        public static void SetTextileColor(ThingDef textileDef, Color color)
        {
            textileDef.graphicData.color = color;
            textileDef.stuffProps.color = color;
        }

        public static void SetTextileColor(Thing textile, Color color)
        {
            textile.SetColor(color);
        }
    }
}
