using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static CyanobotsLeather.LogUtil;
using static CyanobotsLeather.DefRemoval;

namespace CyanobotsLeather
{
    public class LeatherMergeDef : Def
    {
        public ThingDef mergeTo;
        public List<ThingDef> mergeFrom;

    }
}
