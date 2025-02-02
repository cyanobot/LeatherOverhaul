using System.Text;
using Verse;

namespace CyanobotsLeather
{
    class CompShearableHide : ThingComp
    {
        public float woolPerUnit = 0f;

        public CompProperties_ShearableHide Props => (CompProperties_ShearableHide)this.props;

        public ThingDef ShornHideDef => Props.shornHideDef;
        public ThingDef WoolDef => Props.woolDef;

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("HideInspectString_Wool".Translate(WoolDef)
                + " (" + ((int)(woolPerUnit * parent.stackCount)).ToString() + ")");
            sb.Append("HideInspectString_Shorn".Translate(ShornHideDef));

            return sb.ToString();
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            base.PreAbsorbStack(otherStack, count);
            CompShearableHide compShearableHide = otherStack.TryGetComp<CompShearableHide>();
            woolPerUnit = GenMath.WeightedAverage(woolPerUnit, parent.stackCount, compShearableHide.woolPerUnit, count);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //LogUtil.DebugLog("PostSpawnSetup for shearable hide");

            if (woolPerUnit == 0f)
            {
                woolPerUnit = Props.defaultWoolPerUnit;
            }
        }
        
        public override void PostSplitOff(Thing piece)
        {
            CompShearableHide pieceComp = piece.TryGetComp<CompShearableHide>();
            pieceComp.woolPerUnit = this.woolPerUnit;
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref woolPerUnit, "woolAmount");
        }

    }

    class CompProperties_ShearableHide : CompProperties
    {
        public ThingDef shornHideDef = null;
        public ThingDef woolDef = null;
        public float defaultWoolPerUnit = 0f;

        public CompProperties_ShearableHide()
        {
            this.compClass = typeof(CompShearableHide);
        }
    }
}
