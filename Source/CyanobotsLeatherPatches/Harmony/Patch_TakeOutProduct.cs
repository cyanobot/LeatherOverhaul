using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ProcessorFramework;
using UnityEngine;
using Verse;
using static CyanobotsLeather.Main;

namespace CyanobotsLeather
{
    [HarmonyPatch(typeof(CompProcessor),nameof(CompProcessor.TakeOutProduct))]
    public static class Patch_TakeOutProduct
    {
        //surrender color-handling to Colored Leather And Wool if loaded
        public static bool Prepare()
        {
            return !coloredLeatherLoaded;
        }

        public static void Prefix(ThingWithComps ___parent, ActiveProcess activeProcess, out Color __state)
        {
            __state = Color.white;
            if (___parent.def != CyanobotsLeather_DefOf.CYB_TanningVat) return;

            List<Thing> ingredientThings = activeProcess.ingredientThings;
            if (ingredientThings.NullOrEmpty()) return;

            Thing mainIngredient = ingredientThings[0];
            if (mainIngredient == null) return;

            __state = mainIngredient.DrawColor;
        }

        static Thing Postfix(Thing product, ThingWithComps ___parent, Color __state)
        {
            if (___parent.def != CyanobotsLeather_DefOf.CYB_TanningVat) return product;
            if (__state == Color.white) return product;

            if (!(product is ThingWithComps thingWithComps)) return product;
            if (thingWithComps.GetComp<CompColorable>() == null) return product;

            product.SetColor(__state);

            return product;

        }
    }
}
