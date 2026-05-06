using System;
using HarmonyLib;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(BeatmapDifficultySegmentedControlController), "SetData")]
public class BeatmapDifficultySegmentedControlController_SetData
{
	internal static event Action CharacteristicsSegmentedControllerDataSetEvent;

	private static void Postfix()
	{
		BeatmapDifficultySegmentedControlController_SetData.CharacteristicsSegmentedControllerDataSetEvent?.Invoke();
	}
}
