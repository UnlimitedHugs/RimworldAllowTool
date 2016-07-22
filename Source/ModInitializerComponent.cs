using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class ModInitializerComponent : MonoBehaviour {
		private static readonly FieldInfo resolvedDesignatorsField = typeof(DesignationCategoryDef).GetField("resolvedDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
		private static bool injectionPerformed;
		private static Action scheduledCallback;

		public static void ScheduleUpdateCallback(Action callback) {
			scheduledCallback = callback;
		}

		public void FixedUpdate() {
			if(injectionPerformed || Current.ProgramState != ProgramState.MapPlaying) return;
			// find a category with the Claim designator
			foreach (var designatorCategoryDef in DefDatabase<DesignationCategoryDef>.AllDefs) {
				var resolvedDesignators = (List<Designator>) resolvedDesignatorsField.GetValue(designatorCategoryDef);
				// tool may have been already injected during previous load
				foreach (var designator in resolvedDesignators) {
					if(designator is Designator_AllowTool) {
						injectionPerformed = true;
						break;
					}
				}

				if (injectionPerformed) break;
				for (int i = 0; i < resolvedDesignators.Count; i++) {
					var designator = resolvedDesignators[i];
					if(designator is Designator_Claim) {
						resolvedDesignators.Insert(i + 1, new Designator_AllowTool());
						injectionPerformed = true;
						Log.Message("AllowTool added to "+designatorCategoryDef.label+" category.");
						break;
					}
				}
			}
			if(!injectionPerformed) {
				Log.Error("AllowTool: failed to find a category to inject the tool. Category must contain the Claim designator.");
				injectionPerformed = true;
			}
		}

		public void Update() {
			if (scheduledCallback == null) return;
			var call = scheduledCallback;
			scheduledCallback = null;
			call();
		}

		public void OnLevelWasLoaded() {
			injectionPerformed = false;
		}
	}
}
