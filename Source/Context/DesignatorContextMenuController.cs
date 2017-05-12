using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	public static class DesignatorContextMenuController {

		private static readonly Dictionary<Command, BaseDesignatorMenuProvider> contextMenuHandlers = new Dictionary<Command, BaseDesignatorMenuProvider>(); 
		private static readonly Vector2 overlayIconOffset = new Vector2(59f, 2f);

		public static void PrepareContextMenus() {
			try {
				contextMenuHandlers.Clear();
				// instantiate context menu handlers
				var menuProviderTypes = typeof (BaseDesignatorMenuProvider).AllSubclassesNonAbstract();
				var providers = new List<BaseDesignatorMenuProvider>();
				foreach (var providerType in menuProviderTypes) {
					try {
						var provider = (BaseDesignatorMenuProvider) Activator.CreateInstance(providerType);
						providers.Add(provider);
					} catch (Exception e) {
						AllowToolController.Instance.Logger.ReportException(e, null, false, "instantiation of designator menu provider " + providerType);
					}
				}
				// assign handlers to designator instances
				// we can't do a direct type lookup here, since we want to support modded designators. 
				// i.e. Designator_Hunt -> Designator_ModdedHunt should also be supported.
				var allDesignators = DefDatabase<DesignationCategoryDef>.AllDefs.SelectMany(cat => (List<Designator>) AllowToolController.ResolvedDesignatorsField.GetValue(cat));
				// for each designator
				foreach (var designator in allDesignators) {
					// check if it matches the type required by any of the handlers
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
				if (!(gizmo is Designator)) return;
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
	}
}