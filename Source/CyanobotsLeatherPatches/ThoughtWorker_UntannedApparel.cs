using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;


namespace CyanobotsLeather
{
    class ThoughtWorker_UntannedApparel : ThoughtWorker
	{
		public static ThoughtState CurrentThoughtState(Pawn p)
		{
			string text = null;
			int num = 0;
			List<Apparel> wornApparel = p.apparel.WornApparel;
			for (int i = 0; i < wornApparel.Count; i++)
			{
				if (wornApparel[i].Stuff?.stuffProps?.categories?.Contains(CyanobotsLeather_DefOf.CYB_Hide) ?? false)
				{
					if (text == null)
					{
						text = wornApparel[i].def.label;
					}
					num++;
				}
			}
			if (num == 0)
			{
				return ThoughtState.Inactive;
			}
			if (num >= 3)
			{
				return ThoughtState.ActiveAtStage(2, text);
			}
			return ThoughtState.ActiveAtStage(num - 1, text);
		}

		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			return CurrentThoughtState(p);
		}
	}

	public class ThoughtWorker_UntannedApparel_Social : ThoughtWorker
	{
		protected override ThoughtState CurrentSocialStateInternal(Pawn p, Pawn other)
		{
			if (!p.RaceProps.Humanlike)
			{
				return false;
			}
			if (!other.RaceProps.Humanlike)
			{
				return false;
			}
			if (!RelationsUtility.PawnsKnowEachOther(p, other))
			{
				return false;
			}
			List<Apparel> wornApparel = other.apparel.WornApparel;
			if (wornApparel.Any(a => a.Stuff?.stuffProps?.categories?.Contains(CyanobotsLeather_DefOf.CYB_Hide) ?? false))
            {
				return true;
            }
			return false;
		}
	}
	class ThoughtWorker_UntannedFurniture : ThoughtWorker
	{
		public static ThoughtState CurrentThoughtState(Pawn p)
		{
			if (p.Spawned)
            {
				List<Thing> posThings = p.Position.GetThingList(p.Map);
				if (posThings.Any(t => t.Stuff?.stuffProps?.categories?.Contains(CyanobotsLeather_DefOf.CYB_Hide) ?? false))
                {
					return ThoughtState.ActiveAtStage(0);
				}
				return ThoughtState.Inactive;
			}

			Caravan caravan = CaravanUtility.GetCaravan(p);
			if (caravan != null)
            {
				if (caravan.beds.GetBedUsedBy(p)?.Stuff?.stuffProps?.categories?.Contains(CyanobotsLeather_DefOf.CYB_Hide) ?? false)
                {
					return ThoughtState.ActiveAtStage(0);
				}
            }
			return ThoughtState.Inactive;
		}

		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			return CurrentThoughtState(p);
		}
	}
}
