using Verse;

namespace AllowTool {
	/// <summary>
	/// Base class for designators that use the <see cref="UnlimitedAreaDragger"/>
	/// rather than just the plain <see cref="DesignationDragger"/>
	/// </summary>
	public abstract class Designator_UnlimitedDragger : Designator_DefBased {
		protected UnlimitedAreaDragger Dragger { get; }

		protected Designator_UnlimitedDragger() {
			Dragger = new UnlimitedAreaDragger();
		}

		public override int DraggableDimensions {
			get { return 2; }
		}

		public override bool DragDrawMeasurements {
			get { return true; }
		}

		public override void Selected() {
			Dragger.BeginListening(this);
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			// returning false prevents the vanilla dragger from selecting any of the cells
			// while still drawing the dimension labels and playing sounds
			return false;
		}
	}
}