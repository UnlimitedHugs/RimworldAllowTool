using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllowTool.Context;
using AllowTool.Settings;
using HugsLib;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// The hub of the mod. 
	/// Injects the custom designators and handles hotkey presses.
	/// </summary>
	[EarlyInit]
	public class AllowToolController : ModBase {
		internal const string ModId = "AllowTool";
		internal const string DesignatorHandleNamePrefix = "show";
		internal const string ReverseDesignatorHandleNamePrefix = "showrev";

		public static FieldInfo GizmoGridGizmoListField;
		public static FieldInfo DraftControllerAutoUndrafterField;
		public static FieldInfo DesignatorHasDesignateAllFloatMenuOptionField;
		public static MethodInfo DesignatorGetDesignationMethod;
		public static MethodInfo DesignatorGetRightClickFloatMenuOptionsMethod;
		public static AllowToolController Instance { get; private set; }

		// called before implied def generation
		public static void BeforeImpliedDefGeneration() {
			try {
				// setting handles bust be created after language data is loaded
				// and before DesignationCategoryDef.ResolveDesignators is called
				// implied def generation is a good loading stage to do that on
				Instance.PrepareSettingsHandles();

				if (!Instance.HaulWorktypeSetting) {
					AllowToolDefOf.HaulingUrgent.visible = false;
				}
				if (Instance.FinishOffWorktypeSetting) {
					AllowToolDefOf.FinishingOff.visible = true;
				}
			} catch (Exception e) {
				Log.Error("Error during early setting handle setup: "+e);
			}
		}

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private readonly Dictionary<string, SettingHandle<bool>> reverseDesignatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private SettingHandle<bool> settingGlobalHotkeys;
		private bool expandToolSettings;
		private bool expandProviderSettings;
		private bool expandReverseToolSettings;
		private bool dependencyRefreshScheduled;
		
		public override string ModIdentifier {
			get { return ModId; }
		}

		// needed to access protected field from static getter below
		private ModLogger GetLogger {
			get { return base.Logger; }
		}
		internal new static ModLogger Logger {
			get { return Instance.GetLogger; }
		}

		internal SettingHandle<int> SelectionLimitSetting { get; private set; }
		internal SettingHandle<bool> ContextOverlaySetting { get; private set; }
		internal SettingHandle<bool> ContextWatermarkSetting { get; private set; }
		internal SettingHandle<bool> ReplaceIconsSetting { get; private set; }
		internal SettingHandle<bool> HaulWorktypeSetting { get; private set; }
		internal SettingHandle<bool> FinishOffWorktypeSetting { get; private set; }
		internal SettingHandle<bool> ExtendedContextActionSetting { get; private set; }
		internal SettingHandle<bool> ReverseDesignatorPickSetting { get; private set; }
		internal SettingHandle<bool> FinishOffSkillRequirement { get; private set; }
		internal SettingHandle<bool> FinishOffUnforbidsSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntFinishSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntDesignatedSetting { get; private set; }
		internal SettingHandle<bool> StorageSpaceAlertSetting { get; private set; }
		internal SettingHandle<bool> LegacyInjectionSetting { get; private set; }

		public UnlimitedDesignationDragger Dragger { get; private set; }
		public WorldSettings WorldSettings { get; private set; }

		private AllowToolController() {
			Instance = this;
		}

		public override void EarlyInitalize() {
			Dragger = new UnlimitedDesignationDragger();
			PrepareReflection();
			Compat_PickUpAndHaul.Apply();
		}

		public override void Update() {
			Dragger.Update();
			if (Time.frameCount % (60*60) == 0) { // 'bout every minute
				DesignatorContextMenuController.CheckForMemoryLeak();
			}
		}

		public override void Tick(int currentTick) {
			DesignationCleanupManager.Tick(currentTick);
		}

		public override void OnGUI() {
			if (Current.Game == null || Current.Game.CurrentMap == null) return;
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

		public override void WorldLoaded() {
			if (LegacyInjectionSetting) {
				// we do our injections at world load because some mods overwrite ThingDesignatorDef.resolvedDesignators during init
				InjectDesignators();
				ScheduleDesignatorDependencyRefresh();
			}
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
			foreach (var entry in activeDesignators) {
				entry.designator.SetVisible(IsDesignatorEnabledInSettings(entry.designator.def));
			}
		}

		public bool IsDesignatorEnabledInSettings(ThingDesignatorDef def) {
			return GetToolHandleSettingValue(designatorToggleHandles, DesignatorHandleNamePrefix + def.defName);
		}

		public bool IsReverseDesignatorEnabledInSettings(ReverseDesignatorDef def) {
			return GetToolHandleSettingValue(reverseDesignatorToggleHandles, ReverseDesignatorHandleNamePrefix + def.defName);
		}

		public Designator_SelectableThings TryGetDesignator(ThingDesignatorDef def) {
			return activeDesignators.Select(e => e.designator).FirstOrDefault(d => d.def == def);
		}

		private void PrepareSettingsHandles() {
			bool DevModeOnVisibilityPredicate() => Prefs.DevMode;

			settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			ContextOverlaySetting = Settings.GetHandle("contextOverlay", "setting_contextOverlay_label".Translate(), "setting_contextOverlay_desc".Translate(), true);
			ContextWatermarkSetting = Settings.GetHandle("contextWatermark", "setting_contextWatermark_label".Translate(), "setting_contextWatermark_desc".Translate(), true);
			ReplaceIconsSetting = Settings.GetHandle("replaceIcons", "setting_replaceIcons_label".Translate(), "setting_replaceIcons_desc".Translate(), true);
			HaulWorktypeSetting = Settings.GetHandle("haulUrgentlyWorktype", "setting_haulUrgentlyWorktype_label".Translate(), "setting_haulUrgentlyWorktype_desc".Translate(), true);
			FinishOffWorktypeSetting = Settings.GetHandle("finishOffWorktype", "setting_finishOffWorktype_label".Translate(), "setting_finishOffWorktype_desc".Translate(), false);
			ExtendedContextActionSetting = Settings.GetHandle("extendedContextActionKey", "setting_extendedContextHotkey_label".Translate(), "setting_extendedContextHotkey_desc".Translate(), true);
			ReverseDesignatorPickSetting = Settings.GetHandle("reverseDesignatorPick", "setting_reverseDesignatorPick_label".Translate(), "setting_reverseDesignatorPick_desc".Translate(), true);
			FinishOffUnforbidsSetting = Settings.GetHandle("finishOffUnforbids", "setting_finishOffUnforbids_label".Translate(), "setting_finishOffUnforbids_desc".Translate(), true);
			
			// party hunt
			PartyHuntSetting = Settings.GetHandle("partyHunt", "setting_partyHunt_label".Translate(), "setting_partyHunt_desc".Translate(), true);
			PartyHuntFinishSetting = Settings.GetHandle("partyHuntFinish", "setting_partyHuntFinish_label".Translate(), null, true);
			PartyHuntDesignatedSetting = Settings.GetHandle("partyHuntDesignated", "setting_partyHuntDesignated_label".Translate(), null, false);
			PartyHuntFinishSetting.VisibilityPredicate = PartyHuntDesignatedSetting.VisibilityPredicate = () => false;

			StorageSpaceAlertSetting = Settings.GetHandle("storageSpaceAlert", "setting_storageSpaceAlert_label".Translate(), "setting_storageSpaceAlert_desc".Translate(), true);
			
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
			// reverse designators
			MakeSettingsCategoryToggle("setting_showReverseToggles_label", () => expandReverseToolSettings = !expandReverseToolSettings);
			foreach (var reverseDef in DefDatabase<ReverseDesignatorDef>.AllDefs) {
				var handleName = ReverseDesignatorHandleNamePrefix + reverseDef.defName;
				var handle = Settings.GetHandle(handleName, "setting_showTool_label".Translate(reverseDef.designatorDef.label), "setting_reverseDesignator_desc".Translate(), true);
				handle.VisibilityPredicate = () => expandReverseToolSettings;
				reverseDesignatorToggleHandles[handleName] = handle;
			}
			FinishOffSkillRequirement = Settings.GetHandle("finishOffSkill", "setting_finishOffSkill_label".Translate(), "setting_finishOffSkill_desc".Translate(), true);
			FinishOffSkillRequirement.VisibilityPredicate = DevModeOnVisibilityPredicate;

			// TODO: remove this thing
			LegacyInjectionSetting = Settings.GetHandle("legacyInjectionMode", "Legacy tool injection", 
				"If enabled, causes Allow Tool designators to be added at world load time, instead of during designator category refresh. " +
				"This can temporarily fix compatibility issues with certain mods.\n" +
				"This setting will be removed, when the new injection mode is considered stable.", false);
			LegacyInjectionSetting.VisibilityPredicate = DevModeOnVisibilityPredicate;
		}

		private void MakeSettingsCategoryToggle(string labelId, Action buttonAction) {
			var toolToggle = Settings.GetHandle<bool>(labelId, labelId.Translate(), null);
			toolToggle.Unsaved = true;
			toolToggle.CustomDrawer = rect => {
				if (Widgets.ButtonText(rect, "setting_showToggles_btn".Translate())) buttonAction();
				return false;
			};
		}

		public Designator_SelectableThings InstantiateDesignator(Type designatorType, ThingDesignatorDef designatorDef, Designator replacedDesignator = null) {
			try {
				var des = (Designator_SelectableThings) Activator.CreateInstance(designatorType, designatorDef);
				des.ReplacedDesignator = replacedDesignator;
				return des;
			} catch (Exception e) {
				throw new Exception($"Failed to instantiate designator {designatorType.FullName} (def {designatorDef.defName})", e);
			}
		}

		internal void InjectDuringResolveDesignators(DesignationCategoryDef processedCategory) {
			if (LegacyInjectionSetting) return;
			InjectDesignators(processedCategory);
			ScheduleDesignatorDependencyRefresh();
		}

		private void InjectDesignators(DesignationCategoryDef onlyInCategory = null) {
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				try {
					var designatorIsForThisCategory = onlyInCategory == null || designatorDef.Category == onlyInCategory;
					if (designatorDef.Injected || !designatorIsForThisCategory) continue;
					var resolvedDesignators = designatorDef.Category.AllResolvedDesignators;
					var insertIndex = -1;
					for (var i = 0; i < resolvedDesignators.Count; i++) {
						if(resolvedDesignators[i].GetType() != designatorDef.insertAfter) continue;
						insertIndex = i + 1;
						break;
					}
				
					if(insertIndex < 1) {
						if(Prefs.DevMode) Logger.Warning($"Could not find {designatorDef.insertAfter.Name} to inject {designatorDef.defName} after. " +
														$"Appending to {designatorDef.Category.label} category instead.");
						insertIndex = resolvedDesignators.Count;
					}

					Designator replacedDesignator = null;
					if (designatorDef.replaces != null) {
						// remove the designator to replace, if specified
						var replacedIndex = resolvedDesignators.FindIndex(des => designatorDef.replaces.IsInstanceOfType(des));
						if (replacedIndex >= 0) {
							replacedDesignator = resolvedDesignators[replacedIndex];
							resolvedDesignators.RemoveAt(replacedIndex);
							// adjust index to compensate for removed element
							if (replacedIndex < insertIndex) {
								insertIndex--;
							}
						} else {
							if(Prefs.DevMode) Logger.Warning($"{designatorDef.defName} could not find {designatorDef.replaces} for replacement");		
						}
					}
					var designator = InstantiateDesignator(designatorDef.designatorClass, designatorDef, replacedDesignator);
					resolvedDesignators.Insert(insertIndex, designator);
					designator.SetVisible(IsDesignatorEnabledInSettings(designatorDef));
					numDesignatorsInjected++;
					
					if(LegacyInjectionSetting) designatorDef.Injected = true;
				} catch (Exception e) {
					Logger.Error($"Failed to inject designator {designatorDef}: {e}");
					throw;
				}
			}
			if (numDesignatorsInjected > 0) {
				if(Prefs.DevMode) Logger.Trace("Refreshed " + numDesignatorsInjected + " designators");
			}
		}

		private void ScheduleDesignatorDependencyRefresh() {
			if (dependencyRefreshScheduled) return;
			dependencyRefreshScheduled = true;
			activeDesignators.Clear();
			// push the job to the next frame to avoid repeating this for every category as the game loads
			HugsLibController.Instance.DoLater.DoNextUpdate(() => {
				try {
					dependencyRefreshScheduled = false;
					var thingDesignators = AllowToolUtility.GetAllResolvedDesignators()
						.OfType<Designator_SelectableThings>();
					foreach (var designator in thingDesignators) {
						activeDesignators.Add(new DesignatorEntry(designator, designator.def.hotkeyDef));
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
			if (GizmoGridGizmoListField == null || GizmoGridGizmoListField.FieldType != typeof(List<Gizmo>)
				|| DesignatorGetDesignationMethod == null || DesignatorGetDesignationMethod.ReturnType != typeof(DesignationDef)
				|| DesignatorHasDesignateAllFloatMenuOptionField == null || DesignatorHasDesignateAllFloatMenuOptionField.FieldType != typeof(bool)
				|| DesignatorGetRightClickFloatMenuOptionsMethod == null || DesignatorGetRightClickFloatMenuOptionsMethod.ReturnType != typeof(IEnumerable<FloatMenuOption>)
				|| DraftControllerAutoUndrafterField == null || DraftControllerAutoUndrafterField.FieldType != typeof(AutoUndrafter)
				) {
				Logger.Error("Failed to reflect required members");
			}
		}

		private void CheckForHotkeyPresses() {
			if (Event.current.keyCode == KeyCode.None) return;
			if (AllowToolDefOf.ToolContextMenuAction.JustPressed) {
				DesignatorContextMenuController.ProcessContextActionHotkeyPress();
			}
			if (!settingGlobalHotkeys || Find.CurrentMap == null) return;
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

		private bool GetToolHandleSettingValue(Dictionary<string, SettingHandle<bool>> handleDict, string handleName) {
			SettingHandle<bool> handle;
			handleDict.TryGetValue(handleName, out handle);
			return handle == null || handle.Value;
		}
	}
}