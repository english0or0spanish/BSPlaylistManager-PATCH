using System;
using HarmonyLib;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(LevelFilteringNavigationController), "UpdateSecondChildControllerContent")]
public class LevelFilteringNavigationController_UpdateSecondChildControllerContent
{
	internal static event Action SecondChildControllerUpdatedEvent;

	internal static void Postfix()
	{
		LevelFilteringNavigationController_UpdateSecondChildControllerContent.SecondChildControllerUpdatedEvent?.Invoke();
	}
}
