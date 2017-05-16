using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Hub for everything related to designator context menus.
	/// Instantiates individual handlers, processess input events and draws overlay icons.
	/// </summary>
	public static class DesignatorContextMenuController {
		private static readonly Dictionary<Command, BaseDesignatorMenuProvider> designatorMenuProviders = new Dictionary<Command, BaseDesignatorMenuProvider>();
		private static readonly List<Command> reverseDesignatorsForRemoval = new List<Command>();
		private static readonly Vector2 overlayIconOffset = new Vector2(59f, 2f);

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
				var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefs
					.SelectMany(cat => (List<Designator>) AllowToolController.ResolvedDesignatorsField.GetValue(cat));
				foreach (var designator in allDesignators) {
					// check if designator matches the type required by any of the handlers
					TryBindDesignatorToHandler(designator, designator, providers);
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
		}

		// draws the "righclickable" icon over compatible designator buttons
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
				AllowToolController.Instance.Logger.ReportException(e);
			}
		}

		// try catch a right click on a supported designator. Left clicks should return false.
		public static bool TryProcessDesignatorInput(Designator designator) {
			try {
				if (Event.current.button != 1) return false;
				foreach (var provider in MenuProviderInstances) {
					if (provider.HandledDesignatorType.IsInstanceOfType(designator)) {
						provider.OpenContextMenu(designator);
						return true;
					}
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
			return false;
		}

		// called when a designator is selected and the context action key is pressed
		public static void DoContextMenuActionForActiveDesignator() {
			var selectedDesignator = Find.DesignatorManager.SelectedDesignator;
			if (selectedDesignator == null || !designatorMenuProviders.ContainsKey(selectedDesignator)) return;
			designatorMenuProviders[selectedDesignator].HotkeyAction(selectedDesignator);
		}

		// called every OnGUI- Commands for reverse designators are instantiated each time they are drawn, so we need to discard the old ones
		public static void ClearReverseDesignatorPairs() {
			if (reverseDesignatorsForRemoval.Count > 0) {
				foreach (var command in reverseDesignatorsForRemoval) {
					if (designatorMenuProviders.ContainsKey(command)) designatorMenuProviders.Remove(command);
				}
				reverseDesignatorsForRemoval.Clear();
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
			reverseDesignatorsForRemoval.Add(designatorButton);
		}

		public static void CheckForMemoryLeak() {
			// this should not happen, unless another mod patches out our ClearReverseDesignatorPairs call
			if (designatorMenuProviders.Count > 100000) {
				AllowToolController.Instance.Logger.Warning("Too many designator context menu providers! A mod interaction may have caused a memory leak.");
			}
		}

		private static List<BaseDesignatorMenuProvider> InstantiateProviders() {
			var providers = new List<BaseDesignatorMenuProvider>();
			try {
				var menuProviderTypes = typeof (BaseDesignatorMenuProvider).AllSubclassesNonAbstract();	
				foreach (var providerType in menuProviderTypes) {
					try {
						providers.Add((BaseDesignatorMenuProvider) Activator.CreateInstance(providerType));
					} catch (Exception e) {
						AllowToolController.Instance.Logger.Error("Exception while instantiating {0}: {1}", providerType, e);
					}
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
			providers.SortBy(p => p.SettingId);
			return providers;
		}

		/// <param name="designator">The designator that will be paired to a menu provider</param>
		/// <param name="commandToBind">The actual button that will display the overlay and trigger the menu</param>
		/// <param name="providers">All available handlers</param>
		private static void TryBindDesignatorToHandler(Designator designator, Command commandToBind, List<BaseDesignatorMenuProvider> providers) {
			if (designator == null || commandToBind == null) {
				AllowToolController.Instance.Logger.Trace("Tried to bind null designator|command: {0}|{1}", designator, commandToBind);
				return;
			}
			if (designatorMenuProviders.ContainsKey(commandToBind)) {
				AllowToolController.Instance.Logger.Trace("Tried to repeat binding for designator|command {0}|{1}", designator, commandToBind);
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