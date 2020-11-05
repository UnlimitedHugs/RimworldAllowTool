// ReSharper disable UnassignedField.Global
using System;
using HugsLib;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Base def for AllowTool designators.
	/// </summary>
	public class ThingDesignatorDef : Def {
		private readonly DeferredTextureResolver iconResolver = new DeferredTextureResolver();
		private readonly DeferredTextureResolver highlightResolver = new DeferredTextureResolver();
		
		public Type designatorClass;
		public string iconTex;
		public string dragHighlightTex;
		public SoundDef soundSucceeded = null;
		public KeyBindingDef hotkeyDef = null;
		public string messageSuccess = null;
		public string messageFailure = null;

		public void GetIconTexture(Action<Texture2D> onLoaded) {
			iconResolver.ResolveTexture(iconTex, onLoaded, "icon", defName);
		}

		public void GetDragHighlightTexture(Action<Texture2D> onLoaded) {
			highlightResolver.ResolveTexture(dragHighlightTex, onLoaded, "highlight", defName);
		}

		public override void PostLoad() {
			Assert(designatorClass != null, "designatorClass field must be set");
			Assert(typeof(Designator_DefBased).IsAssignableFrom(designatorClass),
				"designatorClass must extend " + nameof(Designator_DefBased));
		}

		private void Assert(bool check, string errorMessage) {
			if (!check) Log.Error($"[AllowTool] Invalid data in ThingDesignatorDef {defName}: {errorMessage}");
		}

		// ensures that textures are loaded in the main thread, since designators are created in a work thread while the game is loading
		private class DeferredTextureResolver {
			private bool resolved;
			private Texture2D texture;

			public void ResolveTexture(string path, Action<Texture2D> onLoaded, string textureTypeName, string defName) {
				if (resolved) {
					onLoaded(texture);
				} else {
					if (path.NullOrEmpty()) {
						AllowToolController.Logger.Error($"Missing ${textureTypeName} texture path for def ${defName}");
						resolved = true;
					} else {
						HugsLibController.Instance.DoLater.DoNextUpdate(() => {
							resolved = true;
							texture = ContentFinder<Texture2D>.Get(path);
							onLoaded(texture);
						});
					}
				}
			}
		}
	}
}