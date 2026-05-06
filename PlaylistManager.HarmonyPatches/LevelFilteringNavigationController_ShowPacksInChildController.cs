using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using PlaylistManager.Utilities;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(LevelFilteringNavigationController), "ShowPacksInSecondChildController")]
public class LevelFilteringNavigationController_ShowPacksInChildController
{
	internal static event Action AllPacksViewSelectedEvent;

	internal static void Prefix(LevelFilteringNavigationController __instance, ref IReadOnlyList<BeatmapLevelPack> beatmapLevelPacks)
	{
		SelectLevelCategoryViewController selectLevelCategoryViewController = __instance.GetPrivateField<SelectLevelCategoryViewController>("_selectLevelCategoryViewController");
		if (selectLevelCategoryViewController != null && selectLevelCategoryViewController.selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs)
		{
			beatmapLevelPacks = beatmapLevelPacks.ToArray().AddRangeToArray(PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks());
			AllPacksViewSelectedEvent?.Invoke();
		}
	}
}
