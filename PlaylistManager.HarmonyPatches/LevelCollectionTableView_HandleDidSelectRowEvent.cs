using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PlaylistManager.Utilities;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(LevelCollectionTableView), "HandleDidSelectCellWithIndex")]
public class LevelCollectionTableView_HandleDidSelectRowEvent
{
	internal static event Action<BeatmapLevel> DidSelectLevelEvent;

	internal static void Prefix(LevelCollectionTableView __instance, int index)
	{
		if (__instance.GetPrivateField<bool>("_showLevelPackHeader"))
		{
			index--;
		}
		if (index >= 0)
		{
			IEnumerable<BeatmapLevel> beatmapLevels = __instance.GetPrivateField<IEnumerable<BeatmapLevel>>("_beatmapLevels");
			if (beatmapLevels != null)
			{
				DidSelectLevelEvent?.Invoke(beatmapLevels.ElementAt(index));
			}
		}
	}
}
