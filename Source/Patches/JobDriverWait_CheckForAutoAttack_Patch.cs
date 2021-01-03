using HarmonyLib;
using Verse.AI;

namespace AllowTool.Patches {
	[HarmonyPatch(typeof(JobDriver_Wait), "CheckForAutoAttack")]
	internal static class JobDriverWait_CheckForAutoAttack_Patch {
		[HarmonyPostfix]
		public static void DoPartyHunting(JobDriver_Wait __instance) {
			PartyHuntHandler.DoBehaviorForPawn(__instance);
		}
	}
}