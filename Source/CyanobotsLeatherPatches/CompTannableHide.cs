using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using static CyanobotsLeather.LeatherColorUtility;

namespace CyanobotsLeather
{
    public class CompTannableHide : ThingComp
    {
        public CompProperties_TannableHide Props => (CompProperties_TannableHide)this.props;

        public override string CompInspectStringExtra()
        {
            return "HideInspectString_Tanned".Translate(Props.leatherDef);
        }
    }

    public class CompProperties_TannableHide : CompProperties
    {
        public ThingDef leatherDef;

        public CompProperties_TannableHide()
        {
            this.compClass = typeof(CompTannableHide);
        }
    }
}