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

		private static readonly Dictionary<Command, ContextMenuProvider> designatorMenuProviders = new Dictionary<Command, ContextMenuProvider>();
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
		private static readonly ContextMenuProvider[] menuProviders = {
			new ContextMenuProvider(typeof(Designator_Cancel), 
				new MenuEntry_CancelSelected(), 
				new MenuEntry_CancelDesignations(), 
				new MenuEntry_CancelBlueprints()),
			new ContextMenuProvider(typeof(Designator_PlantsHarvest), 
				new MenuEntry_HarvestAll(), 
				new MenuEntry_HarvestHome()),
			new ContextMenuProvider(typeof(Designator_PlantsHarvestWood), 
				new MenuEntry_ChopAll(), 
				new MenuEntry_ChopHome()),
			new ContextMenuProvider(typeof(Designator_PlantsCut), 
				new MenuEntry_CutBlighted()),
			new ContextMenuProvider(typeof(Designator_HarvestFullyGrown), 
				new MenuEntry_HarvestGrownAll(), 
				new MenuEntry_HarvestGrownHome()),
			new ContextMenuProvider(typeof(Designator_FinishOff), 
				new MenuEntry_FinishOffAll()),
			new ContextMenuProvider(typeof(Designator_Haul), 
				new MenuEntry_HaulAll()),
			new ContextMenuProvider(typeof(Designator_HaulUrgently), 
				new MenuEntry_HaulUrgentAll(), 
				new MenuEntry_HaulUrgentVisible()),
			new ContextMenuProvider(typeof(Designator_Hunt), 
				new MenuEntry_HuntAll()),
			new ContextMenuProvider(typeof(Designator_Mine), 
				new MenuEntry_MineConnected(),
				new MenuEntry_MineSelectStripMine()),
			new ContextMenuProvider(typeof(Designator_SelectSimilar), 
				new MenuEntry_SelectSimilarAll(), 
				new MenuEntry_SelectSimilarVisible(),
				new MenuEntry_SelectSimilarHome()),
			new ContextMenuProvider(typeof(Designator_Strip), 
				new MenuEntry_StripAll()),
			new ContextMenuProvider(typeof(Designator_Allow),
				new MenuEntry_AllowVisible()),
			new ContextMenuProvider(typeof(Designator_Forbid),
				new MenuEntry_ForbidVisible())
		};
		private static readonly ContextMenuProvider fallbackMenuProvider = new ContextMenuProvider(null);

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
						var verticalOffset = command is Command_Toggle ? 
							56f : 0f; // checkmark/cross is in the way, use lower right corner
						AllowToolUtility.DrawRightClickIcon(topLeft.x + overlayIconOffset.x, 
							topLeft.y + overlayIconOffset.y + verticalOffset);
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
				if (Event.current.button == (int)MouseButtons.Left 
					&& HugsLibUtility.ShiftIsHeld 
					&& AllowToolController.Instance.Handles.ReverseDesignatorPickSetting) {
					return TryPickDesignatorFromReverseDesignator(designator);
				} else if (Event.current.button == (int)MouseButtons.Right) {
					if (designatorMenuProviders.TryGetValue(designator, out ContextMenuProvider provider)) {
						provider.OpenContextMenu(designator);
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
				if (!designatorMenuProviders.TryGetValue(selectedDesignator, out ContextMenuProvider provider)) {
					provider = GetMenuProviderForDesignator(selectedDesignator);
				}
				provider.TryInvokeHotkeyAction(selectedDesignator);
			} else if (AllowToolController.Instance.Handles.ExtendedContextActionSetting.Value) {
				// activate hotkey action for first visible reverse designator
				foreach (var reverseDesignator in currentDrawnReverseDesignators.Values) {
					if (designatorMenuProviders.TryGetValue(reverseDesignator, out ContextMenuProvider reverseProvider)) {
						if (reverseProvider.TryInvokeHotkeyAction(reverseDesignator)) {
							break;
						}
					}
				}
			}
		}

		// Pairs a Command_Action with its reverse designator. This is necessary to display the context menu icon,
		// as well as to intercept reverse designator right-clicks and shift-clicks
		public static void RegisterReverseDesignatorPair(Designator designator, Command designatorButton) {
			currentDrawnReverseDesignators.Add(designatorButton, designator);
		}

		// TODO: remove on next major update
		public static void RegisterReverseDesignatorPair(Designator designator, Command_Action designatorButton) {
			RegisterReverseDesignatorPair(designator, (Command)designatorButton);
		}
		
		public static void Update() {
			// Commands for reverse designators are instantiated each time an 
			// OnGUI event is processed, so we need to discard the old ones regularly
			ClearReverseDesignatorPairs();
		}

		internal static IEnumerable<SettingHandle<bool>> RegisterMenuEntryHandles(ModSettingsPack pack) {
			return menuProviders.SelectMany(p => p.RegisterEntryHandles(pack));
		}

		private static void PrepareReverseDesignatorContextMenus() {
			ClearReverseDesignatorPairs();
			foreach (var reverseDesignator in AllowToolUtility.EnumerateReverseDesignators()) {
				TryBindDesignatorToProvider(reverseDesignator);
			}
			foreach (var designator in AllowThingToggleHandler.GetImpliedReverseDesignators()) {
				TryBindDesignatorToProvider(designator);
			}
		}

		private static void ClearReverseDesignatorPairs() {
			currentDrawnReverseDesignators.Clear();
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
			var provider = GetMenuProviderForDesignator(designator);
			if (provider.HasCustomEnabledEntries || DesignatorShouldHaveFallbackContextMenuProvider(designator)) {
				designatorMenuProviders.Add(designator, provider);
			}
		}

		private static ContextMenuProvider GetMenuProviderForDesignator(Designator designator) {
			for (int i = 0; i < menuProviders.Length; i++) {
				if (menuProviders[i].HandledDesignatorType.IsInstanceOfType(designator)) {
					return menuProviders[i];
				}
			}
			return fallbackMenuProvider;
		}

		// if designator has no custom context menu entries but has a stock context menu, still show a right click icon
		// detection is not fool-proof, but it's good enough- and better than calling RightClickFloatMenuOptions
		private static bool DesignatorShouldHaveFallbackContextMenuProvider(Designator designator){
			try {
				if (designator.GetType() != typeof(Designator_Build)) {
					var hasDesignation = AllowToolController.Instance.Reflection.DesignatorGetDesignationMethod.Invoke(designator, new object[0]) != null;
					if (hasDesignation) return true;
					var hasDesignateAll = (bool)AllowToolController.Instance.Reflection.DesignatorHasDesignateAllFloatMenuOptionField.GetValue(designator);
					if (hasDesignateAll) return true;
					var getOptionsMethod = designator.GetType().GetMethod("get_RightClickFloatMenuOptions", HugsLibUtility.AllBindingFlags);
					var hasOptionsMethod = getOptionsMethod != null && getOptionsMethod.DeclaringType != typeof(Designator);
					if (hasOptionsMethod) return true;
				}
			} catch (Exception) {
				// no problem- the designator will just have no handler assigned
			}
			return false;
		}
	}
}