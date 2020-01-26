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

		private Texture2D resolvedIconTex;
		public Texture2D IconTex {
			get { return resolvedIconTex; }
		}

		private Texture2D resolvedDragHighlightTex;
		public Texture2D DragHighlightTex {
			get { return resolvedDragHighlightTex; }
		}

		private DesignationCategoryDef resolvedCategory;
		public DesignationCategoryDef Category {
			get { return resolvedCategory; }
		}

		public override void ResolveReferences() {
			base.ResolveReferences();
			resolvedCategory = DefDatabase<DesignationCategoryDef>.GetNamed(category);
			// load textures in main thread
			LongEventHandler.ExecuteWhenFinished(() => {
				resolvedIconTex = ContentFinder<Texture2D>.Get(iconTex);
				resolvedDragHighlightTex = ContentFinder<Texture2D>.Get(dragHighlightTex);
			});
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