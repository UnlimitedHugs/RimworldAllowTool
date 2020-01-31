using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllowTool.Context;
using AllowTool.Settings;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// The hub of the mod. 
	/// </summary>
	[EarlyInit]
	public class AllowToolController : ModBase {
		public static FieldInfo GizmoGridGizmoListField;
		public static FieldInfo DraftControllerAutoUndrafterField;
		public static FieldInfo DesignatorHasDesignateAllFloatMenuOptionField;
		public static MethodInfo DesignatorGetDesignationMethod;
		public static MethodInfo DesignatorGetRightClickFloatMenuOptionsMethod;
		public static MethodInfo DesignationCategoryDefResolveDesignatorsMethod;
		public static AllowToolController Instance { get; private set; }

		// called before implied def generation
		public static void BeforeImpliedDefGeneration() {
			try {
				// setting handles bust be created after language data is loaded
				// and before DesignationCategoryDef.ResolveDesignators is called
				// implied def generation is a good loading stage to do that on
				Instance.Handles.PrepareSettingsHandles(Instance.Settings);

				if (!Instance.Handles.HaulWorktypeSetting) {
					AllowToolDefOf.HaulingUrgent.visible = false;
				}
				if (Instance.Handles.FinishOffWorktypeSetting) {
					AllowToolDefOf.FinishingOff.visible = true;
				}
			} catch (Exception e) {
				Log.Error("Error during early setting handle setup: "+e);
			}
		}

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();

		private bool dependencyRefreshScheduled;
		
		public override string ModIdentifier {
			get { return "AllowTool"; }
		}

		// needed to access protected field from static getter below
		private ModLogger GetLogger {
			get { return base.Logger; }
		}
		internal new static ModLogger Logger {
			get { return Instance.GetLogger; }
		}

		public UnlimitedDesignationDragger Dragger { get; private set; }
		public WorldSettings WorldSettings { get; private set; }
		public ModSettingsHandler Handles { get; private set; }

		private AllowToolController() {
			Instance = this;
		}

		public override void EarlyInitalize() {
			Dragger = new UnlimitedDesignationDragger();
			Handles = new ModSettingsHandler();
			PrepareReflection();
			Compat_PickUpAndHaul.Apply();
		}

		public override void Update() {
			Dragger.Update();
			DesignatorContextMenuController.Update();
		}

		public override void Tick(int currentTick) {
			DesignationCleanupManager.Tick(currentTick);
		}

		public override void OnGUI() {
			if (Current.Game == null || Current.Game.CurrentMap == null) return;
			if (Event.current.type == EventType.KeyDown) {
				CheckForHotkeyPresses();
			}
		}

		public override void WorldLoaded() {
			WorldSettings = UtilityWorldObjectManager.GetUtilityWorldObject<WorldSettings>();
		}

		public override void MapLoaded(Map map) {
			// necessary when adding the mod to existing saves
			var injected = AllowToolUtility.EnsureAllColonistsKnowAllWorkTypes(map);
			if (injected) {
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.HaulingUrgent, map);
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.FinishingOff, map);
			}
		}

		public override void SettingsChanged() {
			ResolveAllDesignationCategories();
		}

		

		public Designator_SelectableThings TryGetDesignator(ThingDesignatorDef def) {
			return activeDesignators.Select(e => e.designator).FirstOrDefault(d => d.Def == def);
		}

		internal void InjectDuringResolveDesignators() {
			ScheduleDesignatorDependencyRefresh();
		}

		internal void ScheduleDesignatorDependencyRefresh() {
			if (dependencyRefreshScheduled) return;
			dependencyRefreshScheduled = true;
			activeDesignators.Clear();
			// push the job to the next frame to avoid repeating this for every category as the game loads
			HugsLibController.Instance.DoLater.DoNextUpdate(() => {
				try {
					dependencyRefreshScheduled = false;
					var resolvedDesignators = AllowToolUtility.GetAllResolvedDesignators().ToArray();
					foreach (var designator in resolvedDesignators.OfType<Designator_SelectableThings>()) {
						activeDesignators.Add(new DesignatorEntry(designator, designator.Def.hotkeyDef));
					}
					DesignatorContextMenuController.RebindAllContextMenus();
				} catch (Exception e) {
					Logger.Error($"Error during designator dependency refresh: {e}");
				}
			});
		}

		private void PrepareReflection() {
			var gizmoGridType = GenTypes.GetTypeInAnyAssemblyNew("InspectGizmoGrid", "RimWorld");
			if (gizmoGridType != null) {
				GizmoGridGizmoListField = gizmoGridType.GetField("gizmoList", HugsLibUtility.AllBindingFlags);
			}
			DesignatorGetDesignationMethod = typeof(Designator).GetMethod("get_Designation", HugsLibUtility.AllBindingFlags);
			DesignatorHasDesignateAllFloatMenuOptionField = typeof(Designator).GetField("hasDesignateAllFloatMenuOption", HugsLibUtility.AllBindingFlags);
			DesignatorGetRightClickFloatMenuOptionsMethod = typeof(Designator).GetMethod("get_RightClickFloatMenuOptions", HugsLibUtility.AllBindingFlags);
			DraftControllerAutoUndrafterField = typeof(Pawn_DraftController).GetField("autoUndrafter", HugsLibUtility.AllBindingFlags);
			DesignationCategoryDefResolveDesignatorsMethod = typeof(DesignationCategoryDef).GetMethod("ResolveDesignators", HugsLibUtility.AllBindingFlags);
			if (GizmoGridGizmoListField == null || GizmoGridGizmoListField.FieldType != typeof(List<Gizmo>)
				|| DesignatorGetDesignationMethod == null || DesignatorGetDesignationMethod.ReturnType != typeof(DesignationDef)
				|| DesignatorHasDesignateAllFloatMenuOptionField == null || DesignatorHasDesignateAllFloatMenuOptionField.FieldType != typeof(bool)
				|| DesignatorGetRightClickFloatMenuOptionsMethod == null || DesignatorGetRightClickFloatMenuOptionsMethod.ReturnType != typeof(IEnumerable<FloatMenuOption>)
				|| DraftControllerAutoUndrafterField == null || DraftControllerAutoUndrafterField.FieldType != typeof(AutoUndrafter)
				|| DesignationCategoryDefResolveDesignatorsMethod == null
				) {
				Logger.Error("Failed to reflect required members");
			}
		}

		private void CheckForHotkeyPresses() {
			if (Event.current.keyCode == KeyCode.None) return;
			if (AllowToolDefOf.ToolContextMenuAction.JustPressed) {
				DesignatorContextMenuController.ProcessContextActionHotkeyPress();
			}
			if (!Handles.GlobalHotkeysSetting || Find.CurrentMap == null) return;
			for (int i = 0; i < activeDesignators.Count; i++) {
				var entry = activeDesignators[i];
				if(entry.key == null || !entry.key.JustPressed || !entry.designator.Visible) continue;
				Find.DesignatorManager.Select(entry.designator);
				break;
			}
		}

		private class DesignatorEntry {
			public readonly Designator_SelectableThings designator;
			public readonly KeyBindingDef key;
			public DesignatorEntry(Designator_SelectableThings designator, KeyBindingDef key) {
				this.designator = designator;
				this.key = key;
			}
		}

		private void ResolveAllDesignationCategories() {
			foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefs) {
				DesignationCategoryDefResolveDesignatorsMethod.Invoke(categoryDef, new object[0]);
			}
		}
	}
}