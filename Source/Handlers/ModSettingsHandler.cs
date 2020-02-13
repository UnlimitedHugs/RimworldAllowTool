using System;
using System.Collections.Generic;
using AllowTool.Context;
using AllowTool.Settings;
using HugsLib.Settings;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Creates and stores HugsLib setting handles for all configurable Allow Tool features
	/// </summary>
	public class ModSettingsHandler {
		private const string DesignatorHandleNamePrefix = "show";
		private const string ReverseDesignatorHandleNamePrefix = "showrev";

		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private readonly Dictionary<string, SettingHandle<bool>> reverseDesignatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();

		public event Action PackSettingsChanged;

		public SettingHandle<int> SelectionLimitSetting { get; private set; }
		public SettingHandle<bool> GlobalHotkeysSetting { get; private set; }
		public SettingHandle<bool> ContextOverlaySetting { get; private set; }
		public SettingHandle<bool> ContextWatermarkSetting { get; private set; }
		public SettingHandle<bool> ReplaceIconsSetting { get; private set; }
		public SettingHandle<bool> HaulWorktypeSetting { get; private set; }
		public SettingHandle<bool> FinishOffWorktypeSetting { get; private set; }
		public SettingHandle<bool> ExtendedContextActionSetting { get; private set; }
		public SettingHandle<bool> ReverseDesignatorPickSetting { get; private set; }
		public SettingHandle<bool> FinishOffSkillRequirement { get; private set; }
		public SettingHandle<bool> FinishOffUnforbidsSetting { get; private set; }
		public SettingHandle<bool> PartyHuntSetting { get; private set; }
		public SettingHandle<bool> StorageSpaceAlertSetting { get; private set; }
		
		public SettingHandle<StripMineGlobalSettings> StripMineSettings { get; private set; }

		private bool expandToolSettings;
		private bool expandProviderSettings;
		private bool expandReverseToolSettings;

		internal void PrepareSettingsHandles(ModSettingsPack pack) {
			GlobalHotkeysSetting = pack.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			ContextOverlaySetting = pack.GetHandle("contextOverlay", "setting_contextOverlay_label".Translate(), "setting_contextOverlay_desc".Translate(), true);
			ContextWatermarkSetting = pack.GetHandle("contextWatermark", "setting_contextWatermark_label".Translate(), "setting_contextWatermark_desc".Translate(), true);
			ReplaceIconsSetting = pack.GetHandle("replaceIcons", "setting_replaceIcons_label".Translate(), "setting_replaceIcons_desc".Translate(), false);
			HaulWorktypeSetting = pack.GetHandle("haulUrgentlyWorktype", "setting_haulUrgentlyWorktype_label".Translate(), "setting_haulUrgentlyWorktype_desc".Translate(), true);
			FinishOffWorktypeSetting = pack.GetHandle("finishOffWorktype", "setting_finishOffWorktype_label".Translate(), "setting_finishOffWorktype_desc".Translate(), false);
			ExtendedContextActionSetting = pack.GetHandle("extendedContextActionKey", "setting_extendedContextHotkey_label".Translate(), "setting_extendedContextHotkey_desc".Translate(), true);
			ReverseDesignatorPickSetting = pack.GetHandle("reverseDesignatorPick", "setting_reverseDesignatorPick_label".Translate(), "setting_reverseDesignatorPick_desc".Translate(), true);
			FinishOffUnforbidsSetting = pack.GetHandle("finishOffUnforbids", "setting_finishOffUnforbids_label".Translate(), "setting_finishOffUnforbids_desc".Translate(), true);
			
			PartyHuntSetting = pack.GetHandle("partyHunt", "setting_partyHunt_label".Translate(), "setting_partyHunt_desc".Translate(), true);

			StorageSpaceAlertSetting = pack.GetHandle("storageSpaceAlert", "setting_storageSpaceAlert_label".Translate(), "setting_storageSpaceAlert_desc".Translate(), true);
			
			SelectionLimitSetting = pack.GetHandle("selectionLimit", "setting_selectionLimit_label".Translate(), "setting_selectionLimit_desc".Translate(), 200, Validators.IntRangeValidator(50, 100000));
			SelectionLimitSetting.SpinnerIncrement = 50;
			// designators
			MakeSettingsCategoryToggle(pack, "setting_showToolToggles_label", () => expandToolSettings = !expandToolSettings);
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				var handleName = DesignatorHandleNamePrefix + designatorDef.defName;
				var handle = pack.GetHandle(handleName, "setting_showTool_label".Translate(designatorDef.label), null, true);
				handle.VisibilityPredicate = () => expandToolSettings;
				designatorToggleHandles[handleName] = handle;
			}
			// context menus
			MakeSettingsCategoryToggle(pack, "setting_showProviderToggles_label", () => expandProviderSettings = !expandProviderSettings);
			SettingHandle.ShouldDisplay menuEntryHandleVisibility = () => expandProviderSettings;
			foreach (var handle in DesignatorContextMenuController.RegisterMenuEntryHandles(pack)) {
				handle.VisibilityPredicate = menuEntryHandleVisibility;
			}
			// reverse designators
			MakeSettingsCategoryToggle(pack, "setting_showReverseToggles_label", () => expandReverseToolSettings = !expandReverseToolSettings);
			foreach (var reverseDef in DefDatabase<ReverseDesignatorDef>.AllDefs) {
				var handleName = ReverseDesignatorHandleNamePrefix + reverseDef.defName;
				var handle = pack.GetHandle(handleName, "setting_showTool_label".Translate(reverseDef.designatorDef.label), "setting_reverseDesignator_desc".Translate(), true);
				handle.VisibilityPredicate = () => expandReverseToolSettings;
				reverseDesignatorToggleHandles[handleName] = handle;
			}
			FinishOffSkillRequirement = pack.GetHandle("finishOffSkill", "setting_finishOffSkill_label".Translate(), "setting_finishOffSkill_desc".Translate(), true);
			FinishOffSkillRequirement.VisibilityPredicate = () => Prefs.DevMode;

			StripMineSettings = pack.GetHandle<StripMineGlobalSettings>("stripMineSettings", null, null);
			if (StripMineSettings.Value == null) StripMineSettings.Value = new StripMineGlobalSettings();
			// invisible but resettable
			StripMineSettings.VisibilityPredicate = () => false;

			RegisterPackHandlesChangedCallback(pack);
		}

		public bool IsDesignatorEnabled(ThingDesignatorDef def) {
			return GetToolHandleSettingValue(designatorToggleHandles, DesignatorHandleNamePrefix + def.defName);
		}

		public bool IsReverseDesignatorEnabled(ReverseDesignatorDef def) {
			return GetToolHandleSettingValue(reverseDesignatorToggleHandles, ReverseDesignatorHandleNamePrefix + def.defName);
		}

		private void MakeSettingsCategoryToggle(ModSettingsPack pack, string labelId, Action buttonAction) {
			var toolToggle = pack.GetHandle<bool>(labelId, labelId.Translate(), null);
			toolToggle.Unsaved = true;
			toolToggle.CustomDrawer = rect => {
				if (Widgets.ButtonText(rect, "setting_showToggles_btn".Translate())) buttonAction();
				return false;
			};
		}

		private bool GetToolHandleSettingValue(Dictionary<string, SettingHandle<bool>> handleDict, string handleName) {
			return handleDict.TryGetValue(handleName, out SettingHandle<bool> handle) && handle.Value;
		}

		private void RegisterPackHandlesChangedCallback(ModSettingsPack pack) {
			SettingHandle<bool>.ValueChanged onHandleValueChanged = val => PackSettingsChanged?.Invoke();
			foreach (var handle in pack.Handles) {
				if (handle is SettingHandle<bool> shb) {
					shb.OnValueChanged = onHandleValueChanged;
				}
			}
		}
	}
}