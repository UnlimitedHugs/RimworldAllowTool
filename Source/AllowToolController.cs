using System;
using System.Collections.Generic;
using System.Reflection;
using AllowTool.Context;
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

		public static FieldInfo ResolvedDesignatorsField;
		public static AllowToolController Instance { get; private set; }

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private SettingHandle<bool> settingGlobalHotkeys;
		private bool expandToolSettings;
		private bool expandProviderSettings;
		
		public override string ModIdentifier {
			get { return "AllowTool"; }
		}

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		internal SettingHandle<int> SelectionLimitSetting { get; private set; }

		internal SettingHandle<bool> ContextOverlaySetting { get; set; }

		internal SettingHandle<bool> ContextWatermarkSetting { get; private set; }

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

		// we do our injections at world load because some mods overwrite ThingDesignatorDef.resolvedDesignators during init
		public override void WorldLoaded() {
			InjectDesignators();
			DesignatorContextMenuController.PrepareContextMenus();
		}

		public override void SettingsChanged() {
			foreach (var entry in activeDesignators) {
				entry.designator.SetVisible(GetDesignatorHandleValue(entry.designator.def));
			}
			UpdateHaulingWorkTypeVisiblity();
		}

		private void PrepareSettingsHandles() {
			settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			ContextOverlaySetting = Settings.GetHandle("contextOverlay", "setting_contextOverlay_label".Translate(), "setting_contextOverlay_desc".Translate(), true);
			ContextWatermarkSetting = Settings.GetHandle("contextWatermark", "setting_contextWatermark_label".Translate(), "setting_contextWatermark_desc".Translate(), true);
			SelectionLimitSetting = Settings.GetHandle("selectionLimit", "setting_selectionLimit_label".Translate(), "setting_selectionLimit_desc".Translate(), 200, Validators.IntRangeValidator(50, 100000));
			SelectionLimitSetting.SpinnerIncrement = 50;
			// designators
			MakeSettingsCategoryToggle("setting_showToolToggles_label", () => expandToolSettings = !expandToolSettings);
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				var handleName = DesignatorHandleNamePrefix + designatorDef.defName;
				var handle = Settings.GetHandle(handleName, "setting_showTool_label".Translate(designatorDef.label), null, true);
				handle.VisibilityPredicate = () => expandToolSettings;
				designatorToggleHandles[handleName] = handle;
			}
			// context menus
			MakeSettingsCategoryToggle("setting_showProviderToggles_label", () => expandProviderSettings = !expandProviderSettings);
			foreach (var provider in DesignatorContextMenuController.MenuProviderInstances) {
				if (provider.SettingId == null) continue;
				provider.ProviderHandle = Settings.GetHandle(provider.SettingId, "setting_providerPrefix".Translate(provider.EntryTextKey.Translate()), "setting_provider_desc".Translate(), true);
				provider.ProviderHandle.VisibilityPredicate = () => expandProviderSettings;
			}
			UpdateHaulingWorkTypeVisiblity();
		}

		private void MakeSettingsCategoryToggle(string labelId, Action buttonAction) {
			var toolToggle = Settings.GetHandle<bool>(labelId, labelId.Translate(), null);
			toolToggle.Unsaved = true;
			toolToggle.CustomDrawer = rect => {
				if (Widgets.ButtonText(rect, "setting_showToggles_btn".Translate())) buttonAction();
				return false;
			};
		}

		private void InjectDesignators() {
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				if (designatorDef.Injected) continue;
				var resolvedDesignators = (List<Designator>)ResolvedDesignatorsField.GetValue(designatorDef.Category);
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
			ResolvedDesignatorsField = typeof (DesignationCategoryDef).GetField("resolvedDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
			if (ResolvedDesignatorsField == null) Logger.Error("failed to reflect DesignationCategoryDef.resolvedDesignators");
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