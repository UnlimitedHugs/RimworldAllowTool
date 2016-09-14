using System;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class ThingDesignatorDef : Def {
		public Type designatorClass;
		public bool hidden = false;
		public string category;
		public Type insertAfter = null;
		public string iconTex;
		public string dragHighlightTex;
		public SoundDef soundSucceded = null;
		public KeyBindingDef hotkeyDef = null;
		public string messageSuccess = null;
		public string messageFailure = null;

		public bool Injected { get; set; }

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
			Assert(designatorClass != null && designatorClass.IsSubclassOf(typeof(Designator_SelectableThings)), "designatorClass must extend Designator_SelectableThings");
			Assert(category != null, "category field must be set");
			Assert(insertAfter != null, "insertAfter field must be set");
			Assert(iconTex!= null, "icon texture must be set");
			Assert(dragHighlightTex != null, "drag highlight texture must be set");
		}

		private void Assert(bool check, string errorMessage) {
			if (!check) AllowToolUtility.Error(string.Format("Invalid data in ThingDesignatorDef {0}: {1}", defName, errorMessage));
		}
	}
}