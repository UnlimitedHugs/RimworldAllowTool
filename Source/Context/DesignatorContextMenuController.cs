using System;
using System.Collections.Generic;
using System.Linq;
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

		private static readonly Dictionary<Command, BaseDesignatorMenuProvider> designatorMenuProviders = new Dictionary<Command, BaseDesignatorMenuProvider>();
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
			typeof(Designator_RearmTrap),
			typeof(Designator_Open)
		}; 

		private static List<BaseDesignatorMenuProvider> _providers;
		public static List<BaseDesignatorMenuProvider> MenuProviderInstances {
			get { return _providers ?? (_providers = InstantiateProviders()); }
		}

		public static void PrepareDesignatorContextMenus() {
			try {
				designatorMenuProviders.Clear();
				// bind handlers to designator instances
				// we can't do a direct type lookup here, since we want to support modded designators. 
				// i.e. Designator_Hunt -> Designator_ModdedHunt should also be supported.
				var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefs.ToArray()
					.SelectMany(cat => cat.AllResolvedDesignators.ToArray());
				foreach (var designator in allDesignators) {
					// check if designator matches the type required by any of the handlers
					TryBindDesignatorToHandler(designator, MenuProviderInstances);
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
		}

		public static void PrepareReverseDesignatorContextMenus() {
			try {
				ClearReverseDesignatorPairs();
				var allReverseDesignators = Find.ReverseDesignatorDatabase.AllDesignators;
				foreach (var reverseDesignator in allReverseDesignators) {
					TryBindDesignatorToHandler(reverseDesignator, MenuProviderInstances);
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
		}


		// draws the "rightclickable" icon over compatible designator buttons
		public static void DrawCommandOverlayIfNeeded(Command command, Vector2 topLeft) {
			var designator = TryResolveCommandToDesignator(command);
			if (designator != null) {
				try {
					if (!AllowToolController.Instance.ContextOverlaySetting.Value) return;
					BaseDesignatorMenuProvider provider;
					if (designatorMenuProviders.TryGetValue(designator, out provider) && provider.Enabled) {
						var overlay = AllowToolDefOf.Textures.rightClickOverlay;
						GUI.DrawTexture(new Rect(topLeft.x + overlayIconOffset.x, topLeft.y + overlayIconOffset.y, overlay.width, overlay.height), overlay);
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
				if (Event.current.button == (int)MouseButtons.Left && HugsLibUtility.ShiftIsHeld && AllowToolController.Instance.ReverseDesignatorPickSetting) {
					return TryPickDesignatorFromReverseDesignator(designator);
				} else if (Event.current.button == (int)MouseButtons.Right) {
					BaseDesignatorMenuProvider provider;
					if (designatorMenuProviders.TryGetValue(designator, out provider)) {
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
			if (selectedDesignator != null && designatorMenuProviders.ContainsKey(selectedDesignator)) {
				designatorMenuProviders[selectedDesignator].TryInvokeHotkeyAction(selectedDesignator);
			} else if(AllowToolController.Instance.ExtendedContextActionSetting.Value) {
				// activate hotkey action for first visible reverse designator
				foreach (var designator in currentDrawnReverseDesignators.Values) {
					if (designatorMenuProviders.ContainsKey(designator)) {
						if (designatorMenuProviders[designator].TryInvokeHotkeyAction(designator)) {
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

		public static void CheckForMemoryLeak() {
			// this should not happen, unless another mod patches out our ClearReverseDesignatorPairs call
			if (currentDrawnReverseDesignators.Count > 100000) {
				AllowToolController.Logger.Error("Too many reverse designators! A mod interaction may have caused a memory leak.");
			}
		}

		private static bool TryPickDesignatorFromReverseDesignator(Designator designator) {
			if (designator is Designator_SelectableThings || (designator!=null && reversePickingSupportedDesignators.Contains(designator.GetType()))) {
				Find.DesignatorManager.Select(designator);
				return true;
			}
			return false;
		}

		private static List<BaseDesignatorMenuProvider> InstantiateProviders() {
			var providers = new List<BaseDesignatorMenuProvider>();
			try {
				var menuProviderTypes = typeof (BaseDesignatorMenuProvider).AllSubclassesNonAbstract();	
				foreach (var providerType in menuProviderTypes) {
					try {
						providers.Add((BaseDesignatorMenuProvider) Activator.CreateInstance(providerType));
					} catch (Exception e) {
						AllowToolController.Logger.Error("Exception while instantiating {0}: {1}", providerType, e);
					}
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
			providers.SortBy(p => p.SettingId ?? string.Empty);
			return providers;
		}

		/// <param name="designator">The designator that will be paired to a menu provider</param>
		/// <param name="providers">All available handlers</param>
		private static void TryBindDesignatorToHandler(Designator designator, List<BaseDesignatorMenuProvider> providers) {
			if (designator == null || designatorMenuProviders.ContainsKey(designator)) {
				return;
			}
			var handlerBound = false;
			for (int i = 0; i < providers.Count; i++) {
				var provider = providers[i];
				if (provider.HandledDesignatorType != null && provider.HandledDesignatorType.IsInstanceOfType(designator)) {
					designatorMenuProviders.Add(designator, provider);
					handlerBound = true;
					break;
				}
			}
			if (!handlerBound && designator.GetType() != typeof(Designator_Build)) {
				try {
					// if designator has no handler but has a context menu, provide the generic one
					var hasDesignation = AllowToolController.DesignatorGetDesignationMethod.Invoke(designator, new object[0]) != null;
					var hasDesignateAll = (bool)AllowToolController.DesignatorHasDesignateAllFloatMenuOptionField.GetValue(designator);
					var getOptionsMethod = designator.GetType().GetMethod("get_RightClickFloatMenuOptions", HugsLibUtility.AllBindingFlags);
					var hasOptionsMethod = getOptionsMethod != null && getOptionsMethod.DeclaringType != typeof(Designator) && getOptionsMethod.DeclaringType != typeof(Designator_SelectableThings);
					var ATDesignator = designator as Designator_SelectableThings;
					var hasReplacedOptions = ATDesignator != null && ATDesignator.ReplacedDesignator != null;
					if (hasDesignation || hasDesignateAll || hasOptionsMethod || hasReplacedOptions) {
						// detection is not fool-proof, but it's good enough- and better than calling RightClickFloatMenuOptions
						designatorMenuProviders.Add(designator, providers.OfType<MenuProvider_Generic>().First());
					}
				} catch (Exception) {
					// no problem- the designator will just have no handler assigned
				}
			}
		}
	}
}