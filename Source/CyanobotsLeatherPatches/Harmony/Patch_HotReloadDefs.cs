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
    [HarmonyPatch(typeof(PlayDataLoader),nameof(PlayDataLoader.HotReloadDefs))]
    public static class Patch_HotReloadDefs
    {
        public static void Prefix()
        {
            //put back all the defs we took out of the database
            //so that errors aren't thrown during loading
            foreach(ThingDef leatherDef in removedDefs)
            {
                DefDatabase<ThingDef>.Add(leatherDef);
            }
        }
    }
}
