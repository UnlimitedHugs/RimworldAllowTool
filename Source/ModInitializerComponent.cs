using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class ModInitializerComponent : MonoBehaviour {
		private static bool injectionPerformed;
		private static Action scheduledCallback;

		public static void ScheduleUpdateCallback(Action callback) {
			scheduledCallback = callback;
		}

		public void FixedUpdate() {
			if(injectionPerformed || Game.Mode != GameMode.MapPlaying) return;

			// find a category with the Claim designator
			foreach (var designatorCategoryDef in DefDatabase<DesignationCategoryDef>.AllDefs) {
				// tool may have been already injected during previous load
				foreach (var designator in designatorCategoryDef.resolvedDesignators) {
					if(designator is Designator_AllowTool) {
						injectionPerformed = true;
						break;
					}
				}

				if (injectionPerformed) break;
				for (int i = 0; i < designatorCategoryDef.resolvedDesignators.Count; i++) {
					var designator = designatorCategoryDef.resolvedDesignators[i];
					if(designator is Designator_Claim) {
						designatorCategoryDef.resolvedDesignators.Insert(i+1, new Designator_AllowTool());
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
