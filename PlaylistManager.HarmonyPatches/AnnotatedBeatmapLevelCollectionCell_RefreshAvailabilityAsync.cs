using BeatSaberPlaylistsLib.Types;
using HarmonyLib;
using PlaylistManager.Configuration;
using PlaylistManager.Utilities;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(AnnotatedBeatmapLevelCollectionCell), "RefreshAvailabilityAsync")]
internal class AnnotatedBeatmapLevelCollectionCell_RefreshAvailabilityAsync
{
	private static void Postfix(AnnotatedBeatmapLevelCollectionCell __instance)
	{
		BeatmapLevelPack beatmapLevelPack = __instance.GetPrivateField<BeatmapLevelPack>("_beatmapLevelPack");
		if (beatmapLevelPack is PlaylistLevelPack playlistLevelPack)
		{
			__instance.InvokeMethod("SetDownloadIconVisible", PluginConfig.Instance.ShowDownloadIcon && PlaylistLibUtils.GetMissingSongs(playlistLevelPack.playlist).Count > 0);
		}
	}
}
