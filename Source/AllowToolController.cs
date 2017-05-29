using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllowTool.Context;
using Harmony;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// The hub of the mod. 
	/// Injects the custom designators and handles hotkey presses.
	/// </summary>
	public class AllowToolController : ModBase {
		internal const string ModId = "AllowTool";
		internal const string DesignatorHandleNamePrefix = "show";
		internal const string HarmonyInstanceId = "HugsLib.AllowTool";
		private const string HaulWorktypeSettingName = "haulUrgentlyWorktype";

		public static FieldInfo ResolvedDesignatorsField;
		public static FieldInfo ReverseDesignatorDatabaseDesListField;
		public static AllowToolController Instance { get; private set; }

		internal static HarmonyInstance HarmonyInstance { get; set; }

		// called before implied def generation
		public static void HideHaulUrgentlyWorkTypeIfDisabled() {
			var peekValue = HugsLibController.SettingsManager.GetModSettings(ModId).PeekValue(HaulWorktypeSettingName); // handles will be created later- just peek for now
			if (peekValue == "False") {
				AllowToolDefOf.HaulingUrgent.visible = false;
			}
		}

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private SettingHandle<bool> settingGlobalHotkeys;
		private bool expandToolSettings;
		private bool expandProviderSettings;
		
		public override string ModIdentifier {
			get { return ModId; }
		}

		internal new ModLogger Logger {
			get { return base.Logger; }
		}

		protected override bool HarmonyAutoPatch {
			get { return false; } // we patch out stuff early on. See AllowToolEarlyInit
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
			PrepareReflection();
		}

		public override void Update() {
			Dragger.Update();
			if (Time.frameCount % (60*60) == 0) { // 'bout every minute
				DesignatorContextMenuController.CheckForMemoryLeak();
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

		// we do our injections at world load because some mods overwrite ThingDesignatorDef.resolvedDesignators during init
		public override void WorldLoaded() {
			InjectDesignators();
			DesignatorContextMenuController.PrepareContextMenus();
		}

		public override void MapLoaded(Map map) {
			// necessary when adding the mod to existing saves
			AllowToolUtility.EnsureAllColonistsKnowWorkType(AllowToolDefOf.HaulingUrgent, map);
		}

		public override void SettingsChanged() {
			foreach (var entry in activeDesignators) {
				entry.designator.SetVisible(IsDesignatorEnabledInSettings(entry.designator.def));
			}
		}

		public bool IsDesignatorEnabledInSettings(ThingDesignatorDef def) {
			SettingHandle<bool> handle;
			designatorToggleHandles.TryGetValue(DesignatorHandleNamePrefix + def.defName, out handle);
			return handle == null || handle.Value;
		}

		public Designator_SelectableThings TryGetDesignator(ThingDesignatorDef def) {
			return activeDesignators.Select(e => e.designator).FirstOrDefault(d => d.def == def);
		}

		private void PrepareSettingsHandles() {
			settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			ContextOverlaySetting = Settings.GetHandle("contextOverlay", "setting_contextOverlay_label".Translate(), "setting_contextOverlay_desc".Translate(), true);
			ContextWatermarkSetting = Settings.GetHandle("contextWatermark", "setting_contextWatermark_label".Translate(), "setting_contextWatermark_desc".Translate(), true);
			Settings.GetHandle(HaulWorktypeSettingName, "setting_haulUrgentlyWorktype_label".Translate(), "setting_haulUrgentlyWorktype_desc".Translate(), true);
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
					designator.SetVisible(IsDesignatorEnabledInSettings(designatorDef));
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

		private void PrepareReflection() {
			ResolvedDesignatorsField = typeof(DesignationCategoryDef).GetField("resolvedDesignators", HugsLibUtility.AllBindingFlags);
			ReverseDesignatorDatabaseDesListField = typeof(ReverseDesignatorDatabase).GetField("desList", HugsLibUtility.AllBindingFlags);
			if (ResolvedDesignatorsField == null || ResolvedDesignatorsField.FieldType != typeof(List<Designator>)
			    || ReverseDesignatorDatabaseDesListField == null || ReverseDesignatorDatabaseDesListField.FieldType != typeof(List<Designator>)) {
				Logger.Error("Failed to reflect required members");
			}
		}

		private void CheckForHotkeyPresses() {
			if (Event.current.keyCode == KeyCode.None) return;
			if (AllowToolDefOf.ToolContextMenuAction.JustPressed) {
				DesignatorContextMenuController.DoContextMenuActionForActiveDesignator();
			}
			if (!settingGlobalHotkeys || Find.VisibleMap == null) return;
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
	}
}