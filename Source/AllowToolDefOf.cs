using System.Reflection;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	[DefOf]
	public static class AllowToolDefOf {

		// designations
		public static DesignationDef HaulUgentlyDesignation;

		// designators
		public static ThingDesignatorDef HaulUrgentlyDesignator;

		// work types
		public static WorkTypeDef HaulingUrgent;

		[StaticConstructorOnStartup]
		public static class Textures {
			public static Texture2D rightClickOverlay;

			static Textures() {
				foreach (var fieldInfo in typeof(Textures).GetFields(HugsLibUtility.AllBindingFlags)) {
					fieldInfo.SetValue(null, ContentFinder<Texture2D>.Get(fieldInfo.Name));
				}
			}
		}
	}
}