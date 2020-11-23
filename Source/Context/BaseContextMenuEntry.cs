using System;
using HugsLib.Settings;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Base type for all custom right-click menu entries added by Allow Tool
	/// </summary>
	public abstract class BaseContextMenuEntry {
		private const string SettingHandlePrefix = "contextEntry_";

		protected delegate void MenuActionMethod(Designator designator, Map map);

		private SettingHandle<bool> enabledHandle;

		public bool Enabled {
			get { return enabledHandle == null || enabledHandle.Value; }
		}

		public SettingHandle<bool> RegisterSettingHandle(ModSettingsPack pack) {
			return enabledHandle = pack.GetHandle(SettingHandlePrefix + SettingHandleSuffix,
				"setting_providerPrefix".Translate(Label),
				"setting_provider_desc".Translate(), true);
		}

		protected abstract string BaseTextKey { get; }
		protected abstract string SettingHandleSuffix { get; }

		protected virtual string Label {
			get { return BaseTextKey.Translate(); }
		}

		protected virtual string BaseMessageKey {
			get { return BaseTextKey; }
		}

		// preliminary filter for map things when DesignateAllThings is used
		protected virtual ThingRequestGroup DesignationRequestGroup {
			get { return ThingRequestGroup.Everything; }
		}
		
		public virtual ActivationResult Activate(Designator designator, Map map){
			return ActivateWithFilter(designator, map, null);
		}

		public virtual FloatMenuOption MakeMenuOption(Designator designator) {
			return MakeStandardOption(designator);
		}

		protected ActivationResult ActivateInHomeArea(Designator designator, Map map,
			Predicate<Thing> extraFilter = null) {
			var inHomeArea = GetHomeAreaFilter(map);
			return ActivateWithFilter(designator, map, 
				thing => inHomeArea(thing) && (extraFilter == null || extraFilter(thing)));
		}

		protected ActivationResult ActivateInVisibleArea(Designator designator, Map map,
			Predicate<Thing> extraFilter = null) {
			var thingIsVisible = GetVisibleThingFilter();
			return ActivateWithFilter(designator, map,
				thing => thingIsVisible(thing) && (extraFilter == null || extraFilter(thing)));
		}

		protected ActivationResult ActivateWithFilter(Designator designator, Map map, Predicate<Thing> thingFilter) {
			var hitCount = DesignateAllThings(designator, map, thingFilter);
			return ActivationResult.FromCount(hitCount, BaseMessageKey);
		}

		protected int DesignateAllThings(Designator designator, Map map, Predicate<Thing> thingFilter) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesignationRequestGroup)) {
				if (ThingIsValidForDesignation(thing) && (thingFilter == null || thingFilter(thing)) && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			return hitCount;
		}
		
		// TODO: remove this on next major update
		[Obsolete]
		protected int DesignateAllThingsInHomeArea(Designator designator, Map map) {
			return DesignateAllThings(designator, map, GetHomeAreaFilter(map));
		}

		protected FloatMenuOption MakeStandardOption(Designator designator, string descriptionKey = null, Texture2D extraIcon = null) {
			const float extraIconsSize = 24f;
			const float labelMargin = 10f;
			Func<Rect, bool> extraIconOnGUI = null;
			var extraPartWidth = 0f;
			if (extraIcon != null) {
				extraIconOnGUI = rect => {
					Graphics.DrawTexture(new Rect(rect.x + labelMargin, rect.height / 2f - extraIconsSize / 2f + rect.y, extraIconsSize, extraIconsSize), extraIcon);
					return false;
				};
				extraPartWidth = extraIconsSize + labelMargin;
			}
			return new ATFloatMenuOption(Label, () => {
				ActivateAndHandleResult(designator);
			}, MenuOptionPriority.Default, null, null, extraPartWidth, extraIconOnGUI, null, descriptionKey?.Translate());
		}

		public void ActivateAndHandleResult(Designator designator) {
			try {
				var currentMap = Find.CurrentMap;
				if (currentMap == null) return;
				var result = Activate(designator, currentMap);
				result?.ShowMessage();
			} catch (Exception e) {
				AllowToolController.Logger.Error("Exception while processing context menu action: " + e);
			}
		}

		protected static bool ThingIsValidForDesignation(Thing thing) {
			return thing?.def != null && thing.Map != null && !thing.Map.fogGrid.IsFogged(thing.Position);
		}

		protected Predicate<Thing> GetHomeAreaFilter(Map map) {
			var homeArea = map.areaManager.Home;
			return thing => homeArea.GetCellBool(map.cellIndices.CellToIndex(thing.Position));
		}

		protected Predicate<Thing> GetVisibleThingFilter() {
			var visibleRect = AllowToolUtility.GetVisibleMapRect();
			return t => visibleRect.Contains(t.Position);
		}

		protected static Predicate<Thing> GetExceptAnimaTreeFilter() {
			return t => !AnimaTreeMassDesignationFix.IsAnimaTree(t);
		}
	}
}