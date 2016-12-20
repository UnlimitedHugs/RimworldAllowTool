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
		private static FieldInfo resolvedDesignatorsField;
		public static AllowToolController Instance { get; private set; }

		private readonly List<DesignatorEntry> activeDesignators = new List<DesignatorEntry>();
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
			LongEventHandler.ExecuteWhenFinished(InjectDesignators); // DesignationCategoryDef has delayed designator resolution, so we do, too
			PrepareSettingsHandles();
		}

		public override void SettingsChanged() {
			foreach (var entry in activeDesignators) {
				entry.designator.SetVisible(entry.visibilitySetting.Value);
			}
		}

		private void InjectDesignators() {
			activeDesignators.Clear();
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
					var handle = Settings.GetHandle("show" + designatorDef.defName, "setting_showTool_label".Translate(designatorDef.label), null, true);
					designator.SetVisible(handle.Value);
					activeDesignators.Add(new DesignatorEntry(designator, designatorDef.hotkeyDef, handle));
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
		}

		private void CheckForHotkeyPresses() {
			if (!settingGlobalHotkeys || Find.VisibleMap == null) return;
			for (int i = 0; i < activeDesignators.Count; i++) {
				var entry = activeDesignators[i];
				if(entry.key == null || !entry.key.JustPressed || !entry.visibilitySetting.Value) continue;
				activeDesignators[i].designator.ProcessInput(Event.current);
				break;
			}
		}

		private class DesignatorEntry {
			public readonly Designator_SelectableThings designator;
			public readonly KeyBindingDef key;
			public readonly SettingHandle<bool> visibilitySetting;
			public DesignatorEntry(Designator_SelectableThings designator, KeyBindingDef key, SettingHandle<bool> visibilitySetting) {
				this.designator = designator;
				this.key = key;
				this.visibilitySetting = visibilitySetting;
			}
		}
	}
}