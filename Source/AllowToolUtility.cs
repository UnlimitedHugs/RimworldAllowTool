using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool {
	public static class AllowToolUtility {
		// unforbids forbidden things in a cell and returns the number of hits
		public static int ToggleForbiddenInCell(IntVec3 cell, Map map, bool makeForbidden) {
			if(map == null) throw new NullReferenceException("map is null");
			var hitCount = 0;
			List<Thing> cellThings;
			try {
				cellThings = map.thingGrid.ThingsListAtFast(cell);
			} catch (IndexOutOfRangeException e) {
				throw new IndexOutOfRangeException("Cell out of bounds: "+cell, e);
			}
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i] as ThingWithComps;
				if (thing != null && thing.def.selectable && thing.IsForbidden(Faction.OfPlayer) != makeForbidden) {
					thing.SetForbidden(makeForbidden);
					hitCount++;
				}
			}
			return hitCount;
		}

		// Allows to add WorkTypeDefs to an existing saved game without causing exceptions in the Work tab and work scheduler.
		public static void EnsureAllColonistsKnowWorkType(WorkTypeDef def, Map map) {
			const int disabledWorkPriority = 0;
			try {
				var injectedPawns = new HashSet<Pawn>();
				if (map == null || map.mapPawns == null) return;
				foreach (var pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer)) {
					if (pawn == null || pawn.workSettings == null) continue;
					var workDefMap = Traverse.Create(pawn.workSettings).Field("priorities").GetValue<DefMap<WorkTypeDef, int>>();
					if (workDefMap == null) throw new Exception("Failed to retrieve workDefMap for pawn: " + pawn);
					var priorityList = Traverse.Create(workDefMap).Field("values").GetValue<List<int>>();
					if (priorityList == null) throw new Exception("Failed to retrieve priority list for pawn: " + pawn);
					if (priorityList.Count > 0) {
						var cyclesLeft = 100;
						// the priority list must be padded to accomodate our WorkTypeDef.index
						// the value added will be the priority for our work type
						// more than one element may need to be added (other modded work types taking up indices)
						// pad by the maximum index available to make provisions for ther mods' worktypes
						var maxIndex = DefDatabase<WorkTypeDef>.AllDefs.Max(d => d.index);
						while (priorityList.Count <= maxIndex && cyclesLeft > 0) {
							cyclesLeft--;
							var nowAddingSpecifiedWorktype = priorityList.Count == maxIndex;
							int priority = disabledWorkPriority;
							if (nowAddingSpecifiedWorktype) {
								priority = GetWorkTypePriorityForPawn(def, pawn);
							}
							priorityList.Add(priority);
							injectedPawns.Add(pawn);
						}
						if (cyclesLeft == 0) {
							throw new Exception(String.Format("Ran out of cycles while trying to pad work priorities array:  {0} {1} {2}", def.defName, pawn.Name, priorityList.Count));
						}
					}
				}
				if (injectedPawns.Count > 0) {
					AllowToolController.Instance.Logger.Message("Injected work type {0} into pawns: {1}", def.defName, injectedPawns.Join(", ", true));
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.Error("Exception while injecting WorkTypeDef into colonist pawns: " + e);
			}
		}

		public static bool PawnIsFriendly(Thing t) {
			var pawn = t as Pawn;
			return pawn != null && pawn.Faction != null && (pawn.IsPrisonerOfColony || !pawn.Faction.HostileTo(Faction.OfPlayer));
		}

		// returns a work priority based on disabled work types and tags for that pawn
		private static int GetWorkTypePriorityForPawn(WorkTypeDef workDef, Pawn pawn) {
			const int disabledWorkPriority = 0;
			const int defaultWorkPriority = 3;
			if (pawn.story != null){
				if (pawn.story.WorkTypeIsDisabled(workDef) || pawn.story.WorkTagIsDisabled(workDef.workTags)) {
					return disabledWorkPriority;
				}
			}
			return defaultWorkPriority;
		}
	}
}