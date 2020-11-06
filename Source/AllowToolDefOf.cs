// ReSharper disable UnassignedField.Global
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Automatically filled with AllowTool Defs
	/// </summary>
	[DefOf]
	public static class AllowToolDefOf {

		// designations
		public static DesignationDef HaulUrgentlyDesignation;
		public static DesignationDef FinishOffDesignation;

		// designators
		public static ThingDesignatorDef AllowDesignator;
		public static ThingDesignatorDef ForbidDesignator;
		public static ThingDesignatorDef AllowAllDesignator;
		public static ThingDesignatorDef SelectSimilarDesignator;
		public static ThingDesignatorDef HaulUrgentlyDesignator;
		public static ThingDesignatorDef FinishOffDesignator;
		public static ThingDesignatorDef HarvestFullyGrownDesignator;
		public static ThingDesignatorDef StripMineDesignator;

		// reverse designators
		public static ReverseDesignatorDef ReverseFinishOff;

		// work types
		public static WorkTypeDef HaulingUrgent;
		public static WorkTypeDef FinishingOff;
		
		// work giver
		public static WorkGiverDef FinishOff;

		// jobs
		public static JobDef FinishOffPawn;

		// effecters
		public static EffecterDef EffecterWeaponGlint;

		// key bindings
		public static KeyBindingDef ToolContextMenuAction;

		[StaticConstructorOnStartup]
		public static class Textures {
			public static Texture2D rightClickOverlay;
			public static Texture2D contextMenuWatermark;
			public static Texture2D designatorSelectionOption;
			public static Texture2D partyHunt;

			static Textures() {
				foreach (var fieldInfo in typeof(Textures).GetFields(HugsLibUtility.AllBindingFlags)) {
					fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
				}
			}
		}
	}
}