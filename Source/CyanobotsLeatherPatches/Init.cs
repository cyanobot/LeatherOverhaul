using HarmonyLib;
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
using static CyanobotsLeather.Main;

namespace CyanobotsLeather
{
    [StaticConstructorOnStartup]
    class Init
    {
        static Init()
        {
            ApplySettings();

            LogUtil.DebugLog("VEFLoaded: " + VEFLoaded
                + ", AALoaded: " + AALoaded
                + ", humanButcheryLoaded: " + humanButcheryLoaded
                + ", coloredLeatherLoaded: " + coloredLeatherLoaded);

            
        }

    }
}
