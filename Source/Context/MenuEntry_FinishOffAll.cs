using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_FinishOffAll : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "finishOffAll";
		protected override string BaseTextKey => "Designator_context_finish";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Pawn;

		public override ActivationResult Activate(Designator designator, Map map) {
			int hitCount = 0;
			bool friendliesFound = false;
			foreach (var thing in map.listerThings.ThingsInGroup(DesignationRequestGroup)) {
				if (ThingIsValidForDesignation(thing) && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
					if (AllowToolUtility.PawnIsFriendly(thing)) {
						friendliesFound = true;
					}
				}
			}
			if (hitCount>0 && friendliesFound) {
				Messages.Message("Designator_context_finish_allies".Translate(hitCount), MessageTypeDefOf.CautionInput);
			}
			return ActivationResult.FromCount(hitCount, BaseTextKey);
		}
	}
}