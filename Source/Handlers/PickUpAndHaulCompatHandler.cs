using System;
using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Compatibility patch for Pick Up And Haul mod.
	/// Uses the included work giver to assign hauling jobs scheduled by haul urgently.
	/// </summary>
	public static class PickUpAndHaulCompatHandler {
		public static void Apply() {
			try {
				var workGiverType = GenTypes.GetTypeInAnyAssembly("PickUpAndHaul.WorkGiver_HaulToInventory");
				if (workGiverType == null) return;
				if(!typeof(WorkGiver_HaulGeneral).IsAssignableFrom(workGiverType)) 
					throw new Exception("Expected work giver to extend "+nameof(WorkGiver_HaulGeneral));
				if(workGiverType.GetConstructor(Type.EmptyTypes) == null)
					throw new Exception("Expected work giver to have parameterless constructor");
				var haulWorkGiver = (WorkGiver_HaulGeneral)Activator.CreateInstance(workGiverType);

				WorkGiver_HaulUrgently.JobOnThingDelegate = (pawn, thing, forced) => {
					if (haulWorkGiver.ShouldSkip(pawn, forced)) return null;
					return haulWorkGiver.JobOnThing(pawn, thing, forced);
				};

				AllowToolController.Logger.Message("Applied compatibility patch for \"Pick Up And Haul\"");
			} catch (Exception e) {
				AllowToolController.Logger.ReportException(e, null, false, "Pick Up And Haul compatibility layer application");
			}
		}
	}
}