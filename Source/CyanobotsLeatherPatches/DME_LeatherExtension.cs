using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace CyanobotsLeather
{
    public class DME_LeatherExtension : DefModExtension
    {
        public ThingDef shornEquivalent;
        public ThingDef woollyEquivalent;
        public bool tanFromWoolHide;
        public bool generateBasicHide = true;

        public string woolHideLabel;
        public string hideLabel;
        public string hideDescription;
    }
}
