using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_MineConnected : BaseContextMenuEntry {
		private delegate bool InitialCandidateFilter(IntVec3 cell, Map map);
		private delegate bool ExpansionCandidateFilter(IntVec3 fromCell, IntVec3 toCell, Map map);

		protected override string BaseTextKey => "Designator_context_mine";
		protected override string SettingHandleSuffix => "mineConnected";

		public override ActivationResult Activate(Designator designator, Map map) {
			MineDesignateSelectedOres(map);
			var anyMineDesignations = map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Mine).Any();
			if (!anyMineDesignations) {
				return ActivationResult.Failure(BaseMessageKey);
			}
			// expand designations, excluding designated fogged cells to prevent exposing completely hidden ores
			var hits = FloodExpandDesignationType(DesignationDefOf.Mine, map, (cell, m) => !m.fogGrid.IsFogged(cell), MineDesignationExpansionIsValid);
			return ActivationResult.Success(BaseMessageKey, hits);
		}

		private bool MineDesignationExpansionIsValid(IntVec3 cellFrom, IntVec3 cellTo, Map map) {
			var oreFrom = TryGetMineableAtPos(cellFrom, map);
			var oreTo = TryGetMineableAtPos(cellTo, map);
			return oreFrom != null && oreTo != null && oreFrom.def == oreTo.def;
		}

		private Thing TryGetMineableAtPos(IntVec3 pos, Map map) {
			var thing = map.edificeGrid[pos];
			return thing?.def.building != null && thing.def.mineable && thing.def.building.isResourceRock ? thing : null;
		}

		// ensure all selected ores are Mine designated
		private void MineDesignateSelectedOres(Map map) {
			var toDesignate = Find.Selector.SelectedObjects.OfType<Thing>().Where(o => TryGetMineableAtPos(o.Position, map) != null);
			foreach (var thing in toDesignate) {
				thing.Position.ToggleDesignation(DesignationDefOf.Mine, true);
			}
		}

		// Expands cell designations iteratively to adjacent cells that are deemed valid by the expansionFilter. 
		// Returns the number of additional cells designated.
		private int FloodExpandDesignationType(DesignationDef designationDef, Map  map, InitialCandidateFilter initialFilter, ExpansionCandidateFilter expansionFilter) {
			var designatedCells = map.designationManager.SpawnedDesignationsOfDef(designationDef).Where(d => !d.target.HasThing).Select(d => d.target.Cell).ToList();
			var markedCells = new HashSet<IntVec3>(designatedCells);
			var cellsToProcess = new Queue<IntVec3>(designatedCells.Where(c => initialFilter(c, map)));
			var adjacent = GenAdj.AdjacentCellsAround;
			var cyclesLimit = 1000000;
			var hitCount = 0;
			while (cellsToProcess.Count > 0 && cyclesLimit > 0) {
				cyclesLimit--;
				var baseCell = cellsToProcess.Dequeue();
				for (int i = 0; i < adjacent.Length; i++) {
					var adjacentCell = baseCell + adjacent[i];
					try {
						if (!markedCells.Contains(adjacentCell) && expansionFilter(baseCell, adjacentCell, map)) {
							map.designationManager.AddDesignation(new Designation(adjacentCell, designationDef));
							markedCells.Add(adjacentCell);
							hitCount++;
							cellsToProcess.Enqueue(adjacentCell);
						}
					} catch (Exception e) {
						markedCells.Add(adjacentCell);
						AllowToolController.Logger.Warning($"Exception while trying to designate cell {adjacentCell} via \"mine connected\": {e}");
					}
				}
			}
			if (cyclesLimit == 0) {
				AllowToolController.Logger.Error("Ran out of cycles while expanding designations: " + Environment.StackTrace);
			}
			return hitCount;
		}
	}
}