using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CyanobotsLeather
{
    class Settings : ModSettings
    {
        public static float tanningDays = 5f;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref tanningDays, "tanningDays", tanningDays, true);
        }

        public static void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard l = new Listing_Standard(GameFont.Small)
            {
                ColumnWidth = rect.width
            };

            l.Begin(rect);

            l.Label("TanningTimeDays".Translate() + " [" + "Default".Translate()
                    + " : " + 5.ToString() + "] : " + tanningDays.ToString("F1"));
            tanningDays = l.Slider(tanningDays, 0.1f, 15f);

            l.End();

            Main.ApplySettings();
        }
    }
}
