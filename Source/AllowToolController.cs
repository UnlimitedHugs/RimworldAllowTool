using System;
using AllowTool.Context;
using AllowTool.Settings;
using HugsLib;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// The hub of the mod. 
	/// </summary>
	[EarlyInit]
	public class AllowToolController : ModBase {
		public static AllowToolController Instance { get; private set; }

		public override string ModIdentifier {
			get { return "AllowTool"; }
		}

		// needed to access protected field from static getter below
		private ModLogger GetLogger {
			get { return base.Logger; }
		}
		internal new static ModLogger Logger {
			get { return Instance.GetLogger; }
		}

		public WorldSettings WorldSettings { get; private set; }
		public ModSettingsHandler Handles { get; private set; }
		public ReflectionHandler Reflection { get; private set; }
		internal HaulUrgentlyCacheHandler HaulUrgentlyCache { get; private set; }
		private HotKeyHandler hotKeys;
		private bool dependencyRefreshScheduled;
		private bool modSettingsHaveChanged;
		private int fixedUpdateCount;

		private AllowToolController() {
			Instance = this;
		}

		public override void EarlyInitialize() {
			Handles = new ModSettingsHandler();
			Handles.PackSettingsChanged += () => modSettingsHaveChanged = true;
			Reflection = new ReflectionHandler();
			Reflection.PrepareReflection();
			HaulUrgentlyCache = new HaulUrgentlyCacheHandler();
			hotKeys = new HotKeyHandler();
			// wait for other mods to be loaded
			LongEventHandler.QueueLongEvent(PickUpAndHaulCompatHandler.Apply, null, false, null);
		}

		public override void Update() {
			DesignatorContextMenuController.Update();
		}

		public override void FixedUpdate() {
			HaulUrgentlyCache.ProcessCacheEntries(fixedUpdateCount, Time.unscaledTime);
			fixedUpdateCount++;
		}

		public override void Tick(int currentTick) {
			DesignationCleanupHandler.Tick(currentTick);
		}

		public override void OnGUI() {
			hotKeys.OnGUI();
		}

		public override void WorldLoaded() {
			WorldSettings = Find.World.GetComponent<WorldSettings>();
			HaulUrgentlyCache.ClearCacheForAllMaps();
		}

		public override void MapLoaded(Map map) {
			// hidden worktypes can get disabled under unknown circumstances (other mods are involved)
			// make sure they always revert to being enabled.
			// Don't do this for visible work types- player could have disabled the worktype manually
			if (!Handles.HaulWorktypeSetting) {
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.HaulingUrgent, map);
			}
			if (!Handles.FinishOffWorktypeSetting) {
				AllowToolUtility.EnsureAllColonistsHaveWorkTypeEnabled(AllowToolDefOf.FinishingOff, map);
			}
		}

		public override void MapDiscarded(Map map) {
			HaulUrgentlyCache.ClearCacheForMap(map);
		}

		public override void SettingsChanged() {
			if (!modSettingsHaveChanged) return;
			modSettingsHaveChanged = false;
			ResolveAllDesignationCategories();
			if (AllowToolUtility.ReverseDesignatorDatabaseInitialized) {
				Find.ReverseDesignatorDatabase.Reinit();
			}
		}

		internal void OnBeforeImpliedDefGeneration() {
			try {
				// setting handles bust be created after language data is loaded
				// and before DesignationCategoryDef.ResolveDesignators is called
				// implied def generation is a good loading stage to do that on
				Handles.PrepareSettingsHandles(Instance.Settings);

				if (!Handles.HaulWorktypeSetting) {
					AllowToolDefOf.HaulingUrgent.visible = false;
				}
				if (Handles.FinishOffWorktypeSetting) {
					AllowToolDefOf.FinishingOff.visible = true;
				}
			} catch (Exception e) {
				Logger.Error("Error during early setting handle setup: "+e);
			}
		}

		internal void OnDesignationCategoryResolveDesignators() {
			ScheduleDesignatorDependencyRefresh();
		}

		internal void OnReverseDesignatorDatabaseInit(ReverseDesignatorDatabase database) {
			ReverseDesignatorHandler.InjectReverseDesignators(database);
			ScheduleDesignatorDependencyRefresh();
		}

		internal void ScheduleDesignatorDependencyRefresh() {
			if (dependencyRefreshScheduled) return;
			dependencyRefreshScheduled = true;
			// push the job to the next frame to avoid repeating this for every category as the game loads
			HugsLibController.Instance.DoLater.DoNextUpdate(() => {
				try {
					dependencyRefreshScheduled = false;
					hotKeys.RebindAllDesignators();
					AllowThingToggleHandler.ReinitializeDesignators();
					DesignatorContextMenuController.RebindAllContextMenus();
				} catch (Exception e) {
					Logger.Error($"Error during designator dependency refresh: {e}");
				}
			});
		}

		private void ResolveAllDesignationCategories() {
			foreach (var categoryDef in DefDatabase<DesignationCategoryDef>.AllDefs) {
				Reflection.DesignationCategoryDefResolveDesignatorsMethod.Invoke(categoryDef, new object[0]);
			}
		}
	}
}