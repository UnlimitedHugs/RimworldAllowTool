using Verse;

namespace AllowTool.Comp {
	/// <summary>
	/// Stores the state of the "party hunt" toggle for each pawn.
	/// </summary>
	public class CompPartyHunt : ThingComp {
		public bool enabled;

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref enabled, "enabled");
		}
	}
}