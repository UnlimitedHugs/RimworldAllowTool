using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AllowTool.Context;
using AllowTool.Settings;
using Harmony;
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
	public class AllowToolController : ModBase {
		internal const string ModId = "AllowTool";
		internal const string DesignatorHandleNamePrefix = "show";
		internal const string ReverseDesignatorHandleNamePrefix = "showrev";
		internal const string HarmonyInstanceId = "HugsLib.AllowTool";
		private const string HaulWorktypeSettingName = "haulUrgentlyWorktype";
		private const string FinishOffWorktypeSettingName = "finishOffWorktype";

		public static FieldInfo GizmoGridGizmoListField;
		public static FieldInfo DraftControllerAutoUndrafterField;
		public static FieldInfo DesignatorHasDesignateAllFloatMenuOptionField;
		public static MethodInfo DesignatorGetDesignationMethod;
		public static MethodInfo DesignatorGetRightClickFloatMenuOptionsMethod;
		public static AllowToolController Instance { get; private set; }

		internal static HarmonyInstance HarmonyInstance { get; set; }

		// called before implied def generation
		public static void HideHaulUrgentlyWorkTypeIfDisabled() {
			try {
				// handles will be created later- just peek for now
				var pack = HugsLibController.SettingsManager.GetModSettings(ModId);
				var peekValue = pack.PeekValue(HaulWorktypeSettingName);
				if (peekValue == "False") {
					AllowToolDefOf.HaulingUrgent.visible = false;
				}
				peekValue = pack.PeekValue(FinishOffWorktypeSettingName);
				if (peekValue == "True") {
					AllowToolDefOf.FinishingOff.visible = true;
				}
			} catch (Exception e) {
				Log.Error("AllowTool failed to modify work type visibility: "+e);
			}
		}

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
		private readonly Dictionary<string, SettingHandle<bool>> designatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private readonly Dictionary<string, SettingHandle<bool>> reverseDesignatorToggleHandles = new Dictionary<string, SettingHandle<bool>>();
		private SettingHandle<bool> settingGlobalHotkeys;
		private bool expandToolSettings;
		private bool expandProviderSettings;
		private bool expandReverseToolSettings;
		
		public override string ModIdentifier {
			get { return ModId; }
		}

		private static ModLogger staticLogger;
		internal new static ModLogger Logger {
			get { return staticLogger ?? (staticLogger = new ModLogger(ModId)); }
		}

		protected override bool HarmonyAutoPatch {
			get { return false; } // we patch our stuff early on. See AllowToolEarlyInit
		}

		internal SettingHandle<int> SelectionLimitSetting { get; private set; }
		internal SettingHandle<bool> ContextOverlaySetting { get; private set; }
		internal SettingHandle<bool> ContextWatermarkSetting { get; private set; }
		internal SettingHandle<bool> ReplaceIconsSetting { get; private set; }
		internal SettingHandle<bool> ExtendedContextActionSetting { get; private set; }
		internal SettingHandle<bool> ReverseDesignatorPickSetting { get; private set; }
		internal SettingHandle<bool> FinishOffSkillRequirement { get; private set; }
		internal SettingHandle<bool> FinishOffUnforbidsSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntFinishSetting { get; private set; }
		internal SettingHandle<bool> PartyHuntDesignatedSetting { get; private set; }
		internal SettingHandle<bool> StorageSpaceAlertSetting { get; private set; }

		public UnlimitedDesignationDragger Dragger { get; private set; }
		public WorldSettings WorldSettings { get; private set; }

		private AllowToolController() {
			Instance = this;
		}

		public override void Initialize() {
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

		public override void DefsLoaded() {
			PrepareSettingsHandles();
			activeDesignators.Clear();
		}

		// we do our injections at world load because some mods overwrite ThingDesignatorDef.resolvedDesignators during init
		public override void WorldLoaded() {
			InjectDesignators();
			DesignatorContextMenuController.PrepareDesignatorContextMenus();
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
			settingGlobalHotkeys = Settings.GetHandle("globalHotkeys", "setting_globalHotkeys_label".Translate(), "setting_globalHotkeys_desc".Translate(), true);
			ContextOverlaySetting = Settings.GetHandle("contextOverlay", "setting_contextOverlay_label".Translate(), "setting_contextOverlay_desc".Translate(), true);
			ContextWatermarkSetting = Settings.GetHandle("contextWatermark", "setting_contextWatermark_label".Translate(), "setting_contextWatermark_desc".Translate(), true);
			ReplaceIconsSetting = Settings.GetHandle("replaceIcons", "setting_replaceIcons_label".Translate(), "setting_replaceIcons_desc".Translate(), true);
			Settings.GetHandle(HaulWorktypeSettingName, "setting_haulUrgentlyWorktype_label".Translate(), "setting_haulUrgentlyWorktype_desc".Translate(), true);
			Settings.GetHandle(FinishOffWorktypeSettingName, "setting_finishOffWorktype_label".Translate(), "setting_finishOffWorktype_desc".Translate(), false);
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
			FinishOffSkillRequirement.VisibilityPredicate = () => Prefs.DevMode;
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
				Logger.ReportException(e, null, false, $"instantiation of {(designatorType != null ? designatorType.FullName : "(null)")} with Def {designatorDef}");
			}
			return null;
		}

		private void InjectDesignators() {
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				if (designatorDef.Injected) continue;
				var resolvedDesignators = designatorDef.Category.AllResolvedDesignators;
				var insertIndex = -1;
				for (var i = 0; i < resolvedDesignators.Count; i++) {
					if(resolvedDesignators[i].GetType() != designatorDef.insertAfter) continue;
					insertIndex = i;
					break;
				}
				if (insertIndex >= 0) {
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
							Logger.Warning($"{designatorDef.defName} could not find {designatorDef.replaces} for replacement");		
						}
					}
					var designator = InstantiateDesignator(designatorDef.designatorClass, designatorDef, replacedDesignator);
					resolvedDesignators.Insert(insertIndex + 1, designator);
					designator.SetVisible(IsDesignatorEnabledInSettings(designatorDef));
					activeDesignators.Add(new DesignatorEntry(designator, designatorDef.hotkeyDef));
					numDesignatorsInjected++;
					
				} else {
					Logger.Error($"Failed to inject {designatorDef.defName} after {designatorDef.insertAfter.Name}");		
				}
				designatorDef.Injected = true;
			}
			if (numDesignatorsInjected > 0) {
				Logger.Trace("Injected " + numDesignatorsInjected + " designators");
			}
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