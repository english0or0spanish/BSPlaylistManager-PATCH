using System;
using HarmonyLib;
using SongCore;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(Loader), "RefreshLevelPacks")]
internal class SongCore_RefreshLevelPacks
{
	public static event Action PacksToBeRefreshedEvent;

	private static void Prefix()
	{
		SongCore_RefreshLevelPacks.PacksToBeRefreshedEvent?.Invoke();
	}
}
