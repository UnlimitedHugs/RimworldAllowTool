using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Hub for everything related to designator context menus.
	/// Instantiates individual handlers, processes input events and draws overlay icons.
	/// </summary>
	public static class DesignatorContextMenuController {
		private enum MouseButtons {
			Left = 0, Right = 1
		}

		private static readonly Dictionary<Command, MenuProvider> designatorMenuProviders = new Dictionary<Command, MenuProvider>();
		private static readonly Dictionary<Command, Designator> currentDrawnReverseDesignators = new Dictionary<Command, Designator>();
		private static readonly Vector2 overlayIconOffset = new Vector2(59f, 2f);
		private static readonly HashSet<Type> reversePickingSupportedDesignators = new HashSet<Type> {
			typeof(Designator_Cancel),
			typeof(Designator_Claim),
			typeof(Designator_Deconstruct),
			typeof(Designator_Uninstall),
			typeof(Designator_Haul),
			typeof(Designator_Hunt),
			typeof(Designator_Slaughter),
			typeof(Designator_Tame),
			typeof(Designator_PlantsCut),
			typeof(Designator_PlantsHarvest),
			typeof(Designator_PlantsHarvestWood),
			typeof(Designator_Mine),
			typeof(Designator_Strip),
			typeof(Designator_Open)
		};

		private static readonly List<BaseContextMenuEntry> menuEntries = new List<BaseContextMenuEntry> {
			new MenuEntry_CancelSelected(), 
			new MenuEntry_CancelDesignations(), 
			new MenuEntry_CancelBlueprints(),
			new MenuEntry_ChopAll(),
			new MenuEntry_ChopHome(),
			new MenuEntry_CutBlighted(),
			new MenuEntry_FinishOffAll(),
			new MenuEntry_HarvestAll(),
			new MenuEntry_HarvestHome(),
			new MenuEntry_HarvestGrownAll(),
			new MenuEntry_HarvestGrownHome(),
			new MenuEntry_HaulAll(),
			new MenuEntry_HaulUrgentAll(),
			new MenuEntry_HaulUrgentVisible(),
			new MenuEntry_HuntAll(),
			new MenuEntry_MineConnected(),
			new MenuEntry_SelectSimilarAll(),
			new MenuEntry_SelectSimilarVisible(),
			new MenuEntry_StripAll()
		};

		public static void RebindAllContextMenus() {
			try {
				designatorMenuProviders.Clear();
				// bind handlers to designator instances
				// we can't do a direct type lookup here, since we want to support modded designators. 
				// i.e. Designator_Hunt -> Designator_ModdedHunt should also be supported.
				var allDesignators = AllowToolUtility.EnumerateResolvedDirectDesignators();
				foreach (var designator in allDesignators) {
					// check if designator matches the type required by any of the handlers
					TryBindDesignatorToProvider(designator);
				}
				PrepareReverseDesignatorContextMenus();
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
		}

		// draws the "rightclickable" icon over compatible designator buttons
		public static void DrawCommandOverlayIfNeeded(Command command, Vector2 topLeft) {
			var designator = TryResolveCommandToDesignator(command);
			if (designator != null) {
				try {
					if (!AllowToolController.Instance.Handles.ContextOverlaySetting.Value) return;
					if (designatorMenuProviders.ContainsKey(designator)) {
						AllowToolUtility.DrawRightClickIcon(topLeft.x + overlayIconOffset.x, topLeft.y + overlayIconOffset.y);
					}
				} catch (Exception e) {
					designatorMenuProviders.Remove(designator);
					AllowToolController.Logger.ReportException(e);
				}
			}
		}

		// catch right-clicks and shift-clicks on supported designators and reverse designators. Left clicks return false.
		public static bool TryProcessDesignatorInput(Designator designator) {
			try {
				if (Event.current.button == (int)MouseButtons.Left && HugsLibUtility.ShiftIsHeld && AllowToolController.Instance.Handles.ReverseDesignatorPickSetting) {
					return TryPickDesignatorFromReverseDesignator(designator);
				} else if (Event.current.button == (int)MouseButtons.Right) {
					if (designatorMenuProviders.TryGetValue(designator, out MenuProvider provider)) {
						provider.OpenContextMenu();
						return true;
					}
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
			return false;
		}

		// resolves reverse designators to designators and calls TryProcessDesignatorInput
		public static Designator TryResolveCommandToDesignator(Command command) {
			if (command != null) {
				// for regular designators
				var designator = command as Designator;
				if (designator != null) {
					return designator;
				}
				// for reverse designators
				if (currentDrawnReverseDesignators.TryGetValue(command, out designator)) {
					return designator;
				}
			}
			return null;
		}

		public static void ProcessContextActionHotkeyPress() {
			var selectedDesignator = Find.DesignatorManager.SelectedDesignator;
			if (selectedDesignator != null) {
				// get an existing provider or make a temporary one (e.g.: for shift-picked Select Similar which is never registered)
				var selectedProvider = designatorMenuProviders.TryGetValue(selectedDesignator) 
					?? MakeMenuProviderForDesignator(selectedDesignator);
				selectedProvider.TryInvokeHotkeyAction();
			} else if (AllowToolController.Instance.Handles.ExtendedContextActionSetting.Value) {
				// activate hotkey action for first visible reverse designator
				foreach (var reverseDesignator in currentDrawnReverseDesignators.Values) {
					if (designatorMenuProviders.TryGetValue(reverseDesignator, out MenuProvider reverseProvider)) {
						if (reverseProvider.TryInvokeHotkeyAction()) {
							break;
						}
					}
				}
			}
		}

		// called every OnGUI- Commands for reverse designators are instantiated each time they are drawn, so we need to discard the old ones
		public static void ClearReverseDesignatorPairs() {
			currentDrawnReverseDesignators.Clear();
		}

		// Pairs a Command_Action with its reverse designator. This is necessary to display the context menu icon,
		// as well as to intercept reverse designator right-clicks and shift-clicks
		public static void RegisterReverseDesignatorPair(Designator designator, Command_Action designatorButton) {
			currentDrawnReverseDesignators.Add(designatorButton, designator);
		}

		
		public static void Update() {
			if (Time.frameCount % (60*60) == 0) { // 'bout every minute
				CheckForMemoryLeak();
			}
		}

		private static void CheckForMemoryLeak() {
			// this should not happen, unless another mod patches out our ClearReverseDesignatorPairs call
			if (currentDrawnReverseDesignators.Count > 100000) {
				AllowToolController.Logger.Error("Too many reverse designators! A mod interaction may have caused a memory leak.");
			}
		}

		internal static IEnumerable<SettingHandle<bool>> RegisterMenuEntryHandles(ModSettingsPack pack) {
			foreach (var menuEntry in menuEntries) {
				yield return menuEntry.RegisterSettingHandle(pack);
			}
		}

		private static void PrepareReverseDesignatorContextMenus() {
			ClearReverseDesignatorPairs();
			foreach (var reverseDesignator in AllowToolUtility.EnumerateReverseDesignators()) {
				TryBindDesignatorToProvider(reverseDesignator);
			}
		}

		private static bool TryPickDesignatorFromReverseDesignator(Designator designator) {
			var interfaceSupport = false;
			if (designator != null && designator is IReversePickableDesignator rp) {
				designator = rp.PickUpReverseDesignator();
				interfaceSupport = true;
			}
			if (designator != null) {
				if (interfaceSupport || reversePickingSupportedDesignators.Contains(designator.GetType())) {
					Find.DesignatorManager.Select(designator);
					return true;
				}
			}
			return false;
		}

		private static void TryBindDesignatorToProvider(Designator designator) {
			if (designator == null || designatorMenuProviders.ContainsKey(designator)) {
				return;
			}
			var provider = MakeMenuProviderForDesignator(designator);
			if (provider.EntryCount > 0 || DesignatorShouldHaveDefaultContextMenuProvider(designator)) {
				designatorMenuProviders.Add(designator, provider);
			}
		}

		private static MenuProvider MakeMenuProviderForDesignator(Designator designator) {
			return new MenuProvider(designator, menuEntries.Where(e => e.HandledDesignatorType.IsInstanceOfType(designator)));
		}

		// if designator has no custom context menu entries but has a stock context menu, still show a right click icon
		// detection is not fool-proof, but it's good enough- and better than calling RightClickFloatMenuOptions
		private static bool DesignatorShouldHaveDefaultContextMenuProvider(Designator designator){
			try {
				if (designator.GetType() != typeof(Designator_Build)) {
					var hasDesignation = AllowToolController.Instance.Reflection.DesignatorGetDesignationMethod.Invoke(designator, new object[0]) != null;
					var hasDesignateAll = (bool)AllowToolController.Instance.Reflection.DesignatorHasDesignateAllFloatMenuOptionField.GetValue(designator);
					var getOptionsMethod = designator.GetType().GetMethod("get_RightClickFloatMenuOptions", HugsLibUtility.AllBindingFlags);
					var hasOptionsMethod = getOptionsMethod != null && getOptionsMethod.DeclaringType != typeof(Designator) &&
											getOptionsMethod.DeclaringType != typeof(Designator_SelectableThings);
					return hasDesignation || hasDesignateAll || hasOptionsMethod;
				}
			} catch (Exception) {
				// no problem- the designator will just have no handler assigned
			}
			return false;
		}

		private class MenuProvider {
			private readonly Designator designator;
			private readonly BaseContextMenuEntry[] entries;

			public int EntryCount {
				get { return entries.Count(e => e.Enabled); }
			}

			public MenuProvider(Designator designator, IEnumerable<BaseContextMenuEntry> entries) {
				this.designator = designator;
				this.entries = entries.ToArray();
			}

			public void OpenContextMenu() {
				var menuOptions = entries.Where(e => e.Enabled)
					.Select(e => e.MakeMenuOption(designator))
					.Concat(designator.RightClickFloatMenuOptions).ToList();
				if (menuOptions.Count > 0) {
					Find.WindowStack.Add(new FloatMenu(menuOptions));
				}
			}

			public bool TryInvokeHotkeyAction() {
				// stock right click menu options can't be activated by hotkey
				var firstEnabledEntry = entries.FirstOrDefault(e => e.Enabled);
				if (firstEnabledEntry != null) {
					firstEnabledEntry.ActivateAndHandleResult(designator);
					return true;
				}
				return false;
			}
		}
	}
}