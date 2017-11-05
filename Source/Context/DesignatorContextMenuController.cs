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
		private static readonly List<KeyValuePair<Command, Designator>> currentDrawnReverseDesignators = new List<KeyValuePair<Command, Designator>>();
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
			typeof(Designator_Mine),
			typeof(Designator_Strip),
			typeof(Designator_RearmTrap),
			typeof(Designator_Open)
		}; 

		private static List<BaseDesignatorMenuProvider> _providers;
		public static List<BaseDesignatorMenuProvider> MenuProviderInstances {
			get { return _providers ?? (_providers = InstantiateProviders()); }
		}

		public static void PrepareContextMenus() {
			try {
				ClearReverseDesignatorPairs();
				designatorMenuProviders.Clear();

				var providers = MenuProviderInstances;
				// bind handlers to designator instances
				// we can't do a direct type lookup here, since we want to support modded designators. 
				// i.e. Designator_Hunt -> Designator_ModdedHunt should also be supported.
				var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefs.ToArray()
					.SelectMany(cat => (List<Designator>) AllowToolController.ResolvedDesignatorsField.GetValue(cat));
				foreach (var designator in allDesignators) {
					// check if designator matches the type required by any of the handlers
					TryBindDesignatorToHandler(designator, designator, providers);
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
		}

		// draws the "rightclickable" icon over compatible designator buttons
		public static void DrawCommandOverlayIfNeeded(Command gizmo, Vector2 topLeft) {
			try {
				if (!AllowToolController.Instance.ContextOverlaySetting.Value) return;
				if (gizmo is Designator || gizmo is Command_Action) {
					BaseDesignatorMenuProvider provider;
					if (designatorMenuProviders.TryGetValue(gizmo, out provider) && provider.Enabled) {
						var overlay = AllowToolDefOf.Textures.rightClickOverlay;
						GUI.DrawTexture(new Rect(topLeft.x + overlayIconOffset.x, topLeft.y + overlayIconOffset.y, overlay.width, overlay.height), overlay);
					}
				}
			} catch (Exception e) {
				if (designatorMenuProviders.ContainsKey(gizmo)) designatorMenuProviders.Remove(gizmo);
				AllowToolController.Logger.ReportException(e);
			}
		}

		// try catch a right click on a supported designator. Left clicks should return false.
		public static bool TryProcessDesignatorInput(Designator designator) {
			try {
				if (Event.current.button == (int)MouseButtons.Left && HugsLibUtility.ShiftIsHeld && AllowToolController.Instance.ReverseDesignatorPickSetting) {
					return TryPickDesignatorFromReverseDesignator(designator);
				} else if (Event.current.button == (int)MouseButtons.Right) {
					foreach (var provider in MenuProviderInstances) {
						if (provider.HandledDesignatorType.IsInstanceOfType(designator)) {
							provider.OpenContextMenu(designator);
							return true;
						}
					}
				}
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e);
			}
			return false;
		}

		public static void ProcessContextActionHotkeyPress() {
			var selectedDesignator = Find.DesignatorManager.SelectedDesignator;
			if (selectedDesignator != null && designatorMenuProviders.ContainsKey(selectedDesignator)) {
				designatorMenuProviders[selectedDesignator].HotkeyAction(selectedDesignator);
			} else if(AllowToolController.Instance.ExtendedContextActionSetting.Value) {
				// activate hotkey action for first visible reverse designator
				foreach (var pair in currentDrawnReverseDesignators) {
					if (designatorMenuProviders.ContainsKey(pair.Key)) {
						designatorMenuProviders[pair.Key].HotkeyAction(pair.Value);
						break;
					}
				}
			}
		}

		// called every OnGUI- Commands for reverse designators are instantiated each time they are drawn, so we need to discard the old ones
		public static void ClearReverseDesignatorPairs() {
			if (currentDrawnReverseDesignators.Count > 0) {
				foreach (var pair in currentDrawnReverseDesignators) {
					if (designatorMenuProviders.ContainsKey(pair.Key)) designatorMenuProviders.Remove(pair.Key);
				}
				currentDrawnReverseDesignators.Clear();
			}
		}

		// Pairs a Command_Action with its reverse designator. This is necessary to display the context menu icon.
		// Also replaces the action property so that we can intercept the right click interaction
		public static void RegisterReverseDesignatorPair(Designator designator, Command_Action designatorButton) {
			var originalAction = designatorButton.action;
			designatorButton.action = () => {
				if (!TryProcessDesignatorInput(designator)) {
					originalAction();
				}
			};
			var providers = MenuProviderInstances;
			TryBindDesignatorToHandler(designator, designatorButton, providers);
			currentDrawnReverseDesignators.Add(new KeyValuePair<Command, Designator>(designatorButton, designator));
		}

		public static void CheckForMemoryLeak() {
			// this should not happen, unless another mod patches out our ClearReverseDesignatorPairs call
			if (designatorMenuProviders.Count > 100000) {
				AllowToolController.Logger.Warning("Too many designator context menu providers! A mod interaction may have caused a memory leak.");
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
			providers.SortBy(p => p.SettingId);
			return providers;
		}

		/// <param name="designator">The designator that will be paired to a menu provider</param>
		/// <param name="commandToBind">The actual button that will display the overlay and trigger the menu</param>
		/// <param name="providers">All available handlers</param>
		private static void TryBindDesignatorToHandler(Designator designator, Command commandToBind, List<BaseDesignatorMenuProvider> providers) {
			if (designator == null || commandToBind == null) {
				AllowToolController.Logger.Trace("Tried to bind null designator|command: {0}|{1}", designator, commandToBind);
				return;
			}
			if (designatorMenuProviders.ContainsKey(commandToBind)) {
				AllowToolController.Logger.Trace("Tried to repeat binding for designator|command {0}|{1}", designator, commandToBind);
				return;
			}
			for (int i = 0; i < providers.Count; i++) {
				if (providers[i].HandledDesignatorType.IsInstanceOfType(designator)) {
					designatorMenuProviders.Add(commandToBind, providers[i]);
					break;
				}
			}
		}
	}
}