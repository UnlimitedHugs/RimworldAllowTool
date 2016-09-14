using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace AllowTool {
	/**
	 * The hub of the mod.
	 * Injects the custom designators and handles hotkey presses.
	 */
	public class AllowToolController {
		private const int MapSceneIndex = 1;

		private static FieldInfo resolvedDesignatorsField;
		private static AllowToolController instance;
		public static AllowToolController Instance {
			get { return instance ?? (instance = new AllowToolController()); }
		}

		private readonly List<HotkeyEntry> activeHotkeys = new List<HotkeyEntry>(); 

		public UnlimitedDesignationDragger Dragger { get; private set; }
		
		private AllowToolController() {
			Dragger = new UnlimitedDesignationDragger();
			InitReflectionFields();
		}

		public void Update() {
			Dragger.Update();
		}

		public void OnGUI() {
			if(Event.current.type != EventType.KeyDown) return;
			CheckForHotkeyPresses();
		}

		public void OnLevelLoaded(int level) {
			if(level != MapSceneIndex) return;
			if (DefDatabase<ThingDesignatorDef>.DefCount == 0) {
				activeHotkeys.Clear(); // mod was unloaded
			}
			TryInjectDesignators();
		}

		private void TryInjectDesignators() {
			var numDesignatorsInjected = 0;
			foreach (var designatorDef in DefDatabase<ThingDesignatorDef>.AllDefs) {
				if (designatorDef.hidden || designatorDef.Injected) continue;
				var resolvedDesignators = (List<Designator>)resolvedDesignatorsField.GetValue(designatorDef.Category);
				var insertIndex = -1;
				for (var i = 0; i < resolvedDesignators.Count; i++) {
					if(resolvedDesignators[i].GetType() != designatorDef.insertAfter) continue;
					insertIndex = i;
					break;
				}
				if (insertIndex >= 0) {
					var designator = (Designator) Activator.CreateInstance(designatorDef.designatorClass, designatorDef);
					resolvedDesignators.Insert(insertIndex + 1, designator);
					if (designatorDef.hotkeyDef != null) {
						activeHotkeys.Add(new HotkeyEntry(designatorDef.hotkeyDef, designator));
					}
					numDesignatorsInjected++;
				} else {
					AllowToolUtility.Error(string.Format("Failed to inject {0} after {1}", designatorDef.defName, designatorDef.insertAfter.Name));		
				}
				designatorDef.Injected = true;
			}
			if (numDesignatorsInjected > 0) {
				AllowToolUtility.Log("Injected " + numDesignatorsInjected + " designators");
			}
		}

		private void InitReflectionFields() {
			resolvedDesignatorsField = typeof (DesignationCategoryDef).GetField("resolvedDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
			if (resolvedDesignatorsField == null) AllowToolUtility.Error("failed to reflect DesignationCategoryDef.resolvedDesignators");
		}

		private void CheckForHotkeyPresses() {
			for (int i = 0; i < activeHotkeys.Count; i++) {
				if(!activeHotkeys[i].key.JustPressed) continue;
				activeHotkeys[i].designator.ProcessInput(Event.current);
				break;
			}
		}

		private class HotkeyEntry {
			public readonly KeyBindingDef key;
			public readonly Designator designator;
			public HotkeyEntry(KeyBindingDef key, Designator designator) {
				this.key = key;
				this.designator = designator;
			}
		}
	}
}