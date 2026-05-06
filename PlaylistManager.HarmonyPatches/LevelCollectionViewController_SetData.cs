using HarmonyLib;
using PlaylistManager.Utilities;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(LevelCollectionViewController), "SetData")]
internal class LevelCollectionViewController_SetData
{
	private static void Postfix(LevelCollectionViewController __instance, BeatmapLevel[] beatmapLevels)
	{
		if (beatmapLevels == null || beatmapLevels.Length == 0)
		{
			LevelCollectionTableView levelCollectionTableView = __instance.GetPrivateField<LevelCollectionTableView>("_levelCollectionTableView");
			levelCollectionTableView?.gameObject.SetActive(value: true);
		}
	}
}
