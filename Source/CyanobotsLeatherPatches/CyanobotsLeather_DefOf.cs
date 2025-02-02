using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using ProcessorFramework;

namespace CyanobotsLeather
{
    [RimWorld.DefOf]
    public class CyanobotsLeather_DefOf
    {
        public static ThingDef CYB_Leather_Wool;

        public static ThingDef CYB_TanningVat;

        public static ThingDef CYB_Tannin;
        
        public static StuffCategoryDef CYB_Hide;
        public static ThingCategoryDef CYB_Hides;

        public static ProcessDef CYB_TanningProcess_Base;

        public static DefListDef CYB_LeatherInclusion;
        public static DefListDef CYB_WoolInclusion;
    }
}
