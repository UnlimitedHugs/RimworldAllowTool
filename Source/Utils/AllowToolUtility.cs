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

		// due to other mods' worktypes, our worktype priority may start at zero. This should fix that.
		public static void EnsureAllColonistsHaveWorkTypeEnabled(WorkTypeDef def, Map map) {
			try {
				const int enabledWorkPriority = 3;
				var priorityChanges = new HashSet<(int prevValue, Pawn pawn)>();
				if (map?.mapPawns == null) return;
				var consideredPawns = map.mapPawns.PawnsInFaction(Faction.OfPlayer)
					.Concat(map.mapPawns.PrisonersOfColony); // included for prison labor mod compatibility
				foreach (var pawn in consideredPawns) {
					var workSettings = pawn.workSettings;
					if (workSettings != null && workSettings.EverWork && !pawn.WorkTypeIsDisabled(def)) {
						var prevValue = workSettings.GetPriority(def);
						if (prevValue != enabledWorkPriority) {
							workSettings.SetPriority(def, enabledWorkPriority);
							priorityChanges.Add((prevValue, pawn));
						}
					}
				}
				if (priorityChanges.Count > 0) {
					AllowToolController.Logger.Message("Adjusted work type priority of {0} for pawns:\n{1}", def.defName,
						priorityChanges
							.Select(t => $"{t.pawn.Name?.ToStringShort.ToStringSafe()}:{t.prevValue}->{enabledWorkPriority}")
							.ListElements()
					);
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
	}
}