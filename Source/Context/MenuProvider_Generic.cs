using System;
using System.Collections.Generic;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// An empty menu provider for designators that have not been assigned a specific one.
	/// Allows vanilla and other modded designators to benefit from the right-click icon
	/// and context menus on reverse designators.
	/// </summary>
	public class MenuProvider_Generic : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return string.Empty; }
		}

		public override Type HandledDesignatorType {
			get { return null; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			// BaseDesignatorMenuProvider will fill in vanilla entries
			yield break;
		}
	}
}