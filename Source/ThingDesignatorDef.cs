// ReSharper disable UnassignedField.Global
using System;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Base def for AllowTool designators. These are automatically instantiated and injected.
	/// </summary>
	public class ThingDesignatorDef : Def {
		public Type designatorClass;
		public string category;
		public Type insertAfter = null;
		public Type replaces = null;
		public string iconTex;
		public string dragHighlightTex;
		public SoundDef soundSucceeded = null;
		public KeyBindingDef hotkeyDef = null;
		public string messageSuccess = null;
		public string messageFailure = null;

		public Texture2D IconTex { get; private set; }
		public Texture2D DragHighlightTex { get; private set; }

		public DesignationCategoryDef Category { get; private set; }

		public override void ResolveReferences() {
			base.ResolveReferences();
			Category = DefDatabase<DesignationCategoryDef>.GetNamed(category);
		}

		public void ResolveTextures() {
			IconTex = ContentFinder<Texture2D>.Get(iconTex);
			DragHighlightTex = ContentFinder<Texture2D>.Get(dragHighlightTex);
		}

		public override void PostLoad() {
			Assert(designatorClass != null, "designatorClass field must be set");
			Assert(designatorClass != null && typeof (Designator_SelectableThings).IsAssignableFrom(designatorClass), "designatorClass must extend Designator_SelectableThings");
			Assert(category != null, "category field must be set");
			Assert(insertAfter != null, "insertAfter field must be set");
			Assert(iconTex != null, "icon texture must be set");
			Assert(dragHighlightTex != null, "drag highlight texture must be set");
		}

		private void Assert(bool check, string errorMessage) {
			if (!check) Log.Error($"[AllowTool] Invalid data in ThingDesignatorDef {defName}: {errorMessage}");
		}
	}
}