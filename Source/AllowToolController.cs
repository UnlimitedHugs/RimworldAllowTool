using System;
using System.Collections.Generic;
using System.Reflection;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AllowTool {
	/**
	 * The hub of the mod.
	 * Injects the custom designators and handles hotkey presses.
	 */
	public class AllowToolController : ModBase {
		private const string DesignatorHandleNamePrefix = "show";

		private static FieldInfo resolvedDesignatorsField;
		public static AllowToolController Instance { get; private set; }

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>(); 
		private SettingHandle<bool> settingGlobalHotkeys;

		public override string ModIdentifier {
			get { return "AllowTool"; }
		}

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		public UnlimitedDesignationDragger Dragger { get; private set; }

		private AllowToolController() {
			Instance = this;
		}

		public override void Initialize() {
			Dragger = new UnlimitedDesignationDragger();
			InitReflectionFields();
		}

		public override void Update() {
			Dragger.Update();
		}

		public override void Tick(int currentTick) {
			if (currentTick % 10 == 0) {
				PruneUrgentHaulingDesignations();
			}
		}

		// TODO: scrap this for A17, replace with harmony patch
		private void PruneUrgentHaulingDesignations() {
			// look on all maps
			for (int i = 0; i < Find.Maps.Count; i++) {
				var map = Find.Maps[i];
				if(map == null || map.designationManager == null) continue;
				// iterate through all designations
				for (int j = 0; j < map.designationManager.allDesignations.Count; j++) {
					var designation = map.designationManager.allDesignations[j];
					// drop designations on cells without haulables
					if (designation.def == AllowToolDefOf.HaulUgentlyDesignation) {
						if(Designator_HaulUrgently.CountDesignateableThingsInCell(designation.target.Cell, map)==0){
							map.designationManager.RemoveDesignation(designation);
						}
					}
				}
			}
		}

		public override void OnGUI() {
			if (Current.Game == null || Current.Game.VisibleMap == null) return;
			var selectedDesignator = Find.MapUI.designatorManager.SelectedDesignator;
			for (int i = 0; i < activeDesignators.Count; i++) {
				var designator = activeDesignators[i].designator;
				if (selectedDesignator != designator) continue;
				designator.SelectedOnGUI();
			}
			if (Event.current.type == EventType.KeyDown) {
				CheckForHotkeyPresses();
			}
		}

		public override void DefsLoaded() {
			PrepareSettingsHandles();
			activeDesignators.Clear();
		}

		public override void WorldLoaded() {
			InjectDesignators();
		}

		public override void SettingsChanged() {
			foreach (var entry in activeDesignators) {
				entry.designator.SetVisible(GetDesignatorHandleValue(entry.designator.def));
			}
			UpdateHaulingWorkTypeVisiblity();
		}

		private void InjectDesignators() {
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				if (designatorDef.Injected) continue;
				var resolvedDesignators = (List<Designator>)resolvedDesignatorsField.GetValue(designatorDef.Category);
				var insertIndex = -1;
				for (var i = 0; i < resolvedDesignators.Count; i++) {
					if(resolvedDesignators[i].GetType() != designatorDef.insertAfter) continue;
					insertIndex = i;
					break;
				}
				if (insertIndex >= 0) {
					var designator = (Designator_SelectableThings)Activator.CreateInstance(designatorDef.designatorClass, designatorDef);
					resolvedDesignators.Insert(insertIndex + 1, designator);
					designator.SetVisible(GetDesignatorHandleValue(designatorDef));
					activeDesignators.Add(new DesignatorEntry(designator, designatorDef.hotkeyDef));
					numDesignatorsInjected++;
				} else {
					Logger.Error(string.Format("Failed to inject {0} after {1}", designatorDef.defName, designatorDef.insertAfter.Name));		
				}
				designatorDef.Injected = true;
			}
			if (numDesignatorsInjected > 0) {
				Logger.Trace("Injected " + numDesignatorsInjected + " designators");
			}
		}

		private void InitReflectionFields() {
			resolvedDesignatorsField = typeof (DesignationCategoryDef).GetField("resolvedDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
			if (resolvedDesignatorsField == null) Logger.Error("failed to reflect DesignationCategoryDef.resolvedDesignators");
		}

		private void PrepareSettingsHandles() {
			settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				var handleName = DesignatorHandleNamePrefix + designatorDef.defName;
				var handle = Settings.GetHandle(handleName, "setting_showTool_label".Translate(designatorDef.label), null, true);
				designatorToggleHandles[handleName] = handle;
			}
			UpdateHaulingWorkTypeVisiblity();
		}

		private void UpdateHaulingWorkTypeVisiblity() {
			AllowToolDefOf.HaulingUrgent.visible = GetDesignatorHandleValue(AllowToolDefOf.HaulUrgentlyDesignator);
		}

		private bool GetDesignatorHandleValue(ThingDesignatorDef def) {
			SettingHandle<bool> handle;
			designatorToggleHandles.TryGetValue(DesignatorHandleNamePrefix + def.defName, out handle);
			return handle == null || handle.Value;
		}

		private void CheckForHotkeyPresses() {
			if (!settingGlobalHotkeys || Find.VisibleMap == null) return;
			for (int i = 0; i < activeDesignators.Count; i++) {
				var entry = activeDesignators[i];
				if(entry.key == null || !entry.key.JustPressed || !entry.designator.Visible) continue;
				activeDesignators[i].designator.ProcessInput(Event.current);
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
	}
}