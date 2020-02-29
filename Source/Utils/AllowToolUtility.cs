using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AllowTool {
	public static class AllowToolUtility {
		const int DisabledWorkPriority = 0;
		const int DefaultWorkPriority = 3;

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
				if (thing != null && thing.def.selectable) {
					var comp = thing.GetComp<CompForbiddable>();
					if (comp != null && comp.Forbidden != makeForbidden) {
						comp.Forbidden = makeForbidden;
						hitCount++;
					}
				}
			}
			return hitCount;
		}

		// Allows to add WorkTypeDefs to an existing saved game without causing exceptions in the Work tab and work scheduler.
		// Returns true if the work type array had to be padded for at least one pawn.
		public static bool EnsureAllColonistsKnowAllWorkTypes(Map map) {
			try {
				var injectedPawns = new HashSet<Pawn>();
				if (map?.mapPawns == null) return false;
				foreach (var pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer)) {
					if (pawn?.workSettings == null) continue;
					var priorityList = GetWorkPriorityListForPawn(pawn);
					if (priorityList != null && priorityList.Count > 0) {
						var cyclesLeft = 100;
						// the priority list must be padded to accommodate all available WorkTypeDef.index
						// pad by the maximum index available to make provisions for other mods' worktypes
						var maxIndex = DefDatabase<WorkTypeDef>.AllDefs.Max(d => d.index);
						while (priorityList.Count <= maxIndex && cyclesLeft > 0) {
							cyclesLeft--;
							priorityList.Add(DisabledWorkPriority);
							injectedPawns.Add(pawn);
						}
						if (cyclesLeft == 0) {
							throw new Exception($"Ran out of cycles while trying to pad work priorities list:  {pawn.Name} {priorityList.Count}");
						}
					}
				}
				if (injectedPawns.Count > 0) {
					AllowToolController.Logger.Message("Padded work priority lists for pawns: {0}", injectedPawns.Join(", ", true));
					return true;
				}
			} catch (Exception e) {
				AllowToolController.Logger.Error("Exception while injecting WorkTypeDef into colonist pawns: " + e);
			}
			return false;
		}

		// due to other mods' worktypes, our worktype priority may start at zero. This should fix that.
		public static void EnsureAllColonistsHaveWorkTypeEnabled(WorkTypeDef def, Map map) {
			try {
				var activatedPawns = new HashSet<Pawn>();
				if (map?.mapPawns == null) return;
				foreach (var pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer).Concat(map.mapPawns.PrisonersOfColony)) {
					var priorityList = GetWorkPriorityListForPawn(pawn);
					if (priorityList != null && priorityList.Count > 0) {
						var curValue = priorityList[def.index];
						if (curValue == DisabledWorkPriority) {
							var adjustedValue = GetWorkTypePriorityForPawn(def, pawn);
							if (adjustedValue != curValue) {
								priorityList[def.index] = adjustedValue;
								activatedPawns.Add(pawn);
							}
						}	
					}
				}
				if (activatedPawns.Count > 0) {
					AllowToolController.Logger.Message("Adjusted work type priority of {0} to default for pawns: {1}", def.defName, activatedPawns.Join(", ", true));
				}
			} catch (Exception e) {
				AllowToolController.Logger.Error("Exception while adjusting work type priority in colonist pawns: " + e);
			}
		}

		public static bool PawnIsFriendly(Thing t) {
			var pawn = t as Pawn;
			return pawn?.Faction != null && (pawn.IsPrisonerOfColony || !pawn.Faction.HostileTo(Faction.OfPlayer));
		}

		public static void DrawMouseAttachedLabel(string text) {
			DrawMouseAttachedLabel(text, Color.white);
		}

		public static void DrawMouseAttachedLabel(string text, Color textColor) {
			const float AttachedIconHeight = 32f;
			const float LabelWidth = 200f;
			var cursorOffset = new Vector2(8f, 8f + 12f); // see GenUI.DrawMouseAttachment
			var mousePosition = Event.current.mousePosition;
			if (!text.NullOrEmpty()) {
				var rect = new Rect(mousePosition.x + cursorOffset.x, mousePosition.y + cursorOffset.y + AttachedIconHeight, LabelWidth, 9999f);
				Text.Font = GameFont.Small;
				var prevColor = GUI.color;
				GUI.color = textColor;
				Widgets.Label(rect, text);
				GUI.color = prevColor;
			}
		}

		public static bool PawnCapableOfViolence(Pawn pawn) {
			return !pawn.WorkTagIsDisabled(WorkTags.Violent);
		}

		public static void DrawRightClickIcon(float x, float y) {
			var overlay = AllowToolDefOf.Textures.rightClickOverlay;
			GUI.DrawTexture(new Rect(x, y, overlay.width, overlay.height), overlay);
		}

		public static ATFloatMenuOption MakeCheckmarkOption(string labelKey, string descriptionKey, Func<bool> getter, Action<bool> setter) {
			const float checkmarkButtonSize = 24f;
			const float labelMargin = 10f;
			bool checkOn = getter();
			return new ATFloatMenuOption(labelKey.Translate(), () => {
				setter(!getter());
				checkOn = getter();
				HugsLibController.SettingsManager.SaveChanges();
				var feedbackSound = checkOn ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff;
				feedbackSound.PlayOneShotOnCamera();
			}, MenuOptionPriority.Default, null, null, checkmarkButtonSize + labelMargin, rect => {
				Widgets.Checkbox(rect.x + labelMargin, rect.height / 2f - checkmarkButtonSize / 2f + rect.y, ref checkOn);
				return false;
			}, null, descriptionKey?.Translate());
		}

		public static CellRect GetVisibleMapRect() {
			// code swiped from ThingSelectionUtility
			var screenRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
			var screenLoc1 = new Vector2(screenRect.x, UI.screenHeight - screenRect.y);
			var screenLoc2 = new Vector2(screenRect.x + screenRect.width, UI.screenHeight - (screenRect.y + screenRect.height));
			var corner1 = UI.UIToMapPosition(screenLoc1);
			var corner2 = UI.UIToMapPosition(screenLoc2);
			return new CellRect {
				minX = Mathf.FloorToInt(corner1.x),
				minZ = Mathf.FloorToInt(corner2.z),
				maxX = Mathf.FloorToInt(corner2.x),
				maxZ = Mathf.FloorToInt(corner1.z)
			};
		}

		public static IEnumerable<Designator> EnumerateResolvedDirectDesignators() {
			return DefDatabase<DesignationCategoryDef>.AllDefs
				.SelectMany(cat => cat.AllResolvedDesignators).ToArray();
		}

		public static IEnumerable<Designator> EnumerateReverseDesignators() {
			return ReverseDesignatorDatabaseInitialized ? Find.ReverseDesignatorDatabase.AllDesignators : new List<Designator>();
		}

		public static bool ReverseDesignatorDatabaseInitialized {
			get { return Current.Root?.uiRoot is UIRoot_Play uiPlay && uiPlay.mapUI?.reverseDesignatorDatabase != null; }
		}

		private static List<int> GetWorkPriorityListForPawn(Pawn pawn) {
			try {
				if (pawn?.workSettings != null) {
					var workDefMap = (DefMap<WorkTypeDef, int>)AllowToolController.Instance.Reflection.PawnWorkSettingsPriorities.GetValue(pawn.workSettings);
					// could be null if pawn is not capable of work (enemies, prisoners)
					if (workDefMap != null) {
						var priorityList = (List<int>)AllowToolController.Instance.Reflection.WorkDefMapValues.GetValue(workDefMap);
						return priorityList;
					}
				}
			} catch (Exception e) {
				AllowToolController.Logger.Error($"Caught exception while trying to retrieve work priorities for pawn {pawn.ToStringSafe()}: {e}");
				throw;
			}
			return null;
		}
		
		// returns a work priority based on disabled work types and tags for that pawn
		private static int GetWorkTypePriorityForPawn(WorkTypeDef workDef, Pawn pawn) {
			if (pawn.story != null){
				if (pawn.WorkTypeIsDisabled(workDef) || pawn.WorkTagIsDisabled(workDef.workTags)) {
					return DisabledWorkPriority;
				}
			}
			return DefaultWorkPriority;
		}
	}
}