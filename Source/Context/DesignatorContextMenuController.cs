using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	public static class DesignatorContextMenuController {

		private static readonly Dictionary<Command, BaseDesignatorMenuProvider> contextMenuHandlers = new Dictionary<Command, BaseDesignatorMenuProvider>(); 
		private static readonly Vector2 overlayIconOffset = new Vector2(59f, 2f);

		private static List<BaseDesignatorMenuProvider> _providers;
		public static List<BaseDesignatorMenuProvider> MenuProviderInstances {
			get { return _providers ?? (_providers = InstantiateProviders()); }
		}

		public static void PrepareContextMenus() {
			try {
				contextMenuHandlers.Clear();

				var providers = MenuProviderInstances;
				// assign handlers to designator instances
				// we can't do a direct type lookup here, since we want to support modded designators. 
				// i.e. Designator_Hunt -> Designator_ModdedHunt should also be supported.
				var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefs.SelectMany(cat => (List<Designator>) AllowToolController.ResolvedDesignatorsField.GetValue(cat));
				foreach (var designator in allDesignators) {
					// check if designator matches the type required by any of the handlers
					for (int i = 0; i < providers.Count; i++) {
						if (providers[i].HandledDesignatorType.IsInstanceOfType(designator)) {
							contextMenuHandlers.Add(designator, providers[i]);
							break;
						}
					}
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
		}

		// draws the "righclickable" icon over compatible designator buttons
		public static void DrawCommandOverlayIfNeeded(Command gizmo, Vector2 topLeft) {
			try {
				if (!(gizmo is Designator) || !AllowToolController.Instance.ContextOverlaySetting.Value) return;
				BaseDesignatorMenuProvider provider;
				if (contextMenuHandlers.TryGetValue(gizmo, out provider) && provider.Enabled) {
					var overlay = AllowToolDefOf.Textures.rightClickOverlay;
					GUI.DrawTexture(new Rect(topLeft.x + overlayIconOffset.x, topLeft.y + overlayIconOffset.y, overlay.width, overlay.height), overlay);
				}
			} catch (Exception e) {
				if (contextMenuHandlers.ContainsKey(gizmo)) contextMenuHandlers.Remove(gizmo);
				AllowToolController.Instance.Logger.ReportException(e);
			}
		}

		/// <returns>true if a supported designator was right-clicked</returns>
		public static bool TryProcessRightClickOnDesignator(Designator designator) {
			BaseDesignatorMenuProvider provider;
			if (contextMenuHandlers.TryGetValue(designator, out provider) && provider.Enabled) {
				contextMenuHandlers[designator].OpenContextMenu(designator);
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
						AllowToolController.Instance.Logger.Error("Exception while instantiating {0}: {1}", providerType, e);
					}
				}
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
			providers.SortBy(p => p.SettingId);
			return providers;
		}
	}
}