using BeatSaberPlaylistsLib.Types;
using HarmonyLib;
using HMUI;
using PlaylistManager.Configuration;
using PlaylistManager.Utilities;
using UnityEngine;

namespace PlaylistManager.HarmonyPatches;

[HarmonyPatch(typeof(LevelPackDetailViewController), "ShowContent")]
internal class LevelPackDetailViewController_ShowContent
{
    private static bool Prefix(LevelPackDetailViewController __instance, object contentType)
    {
        if (contentType == null || contentType.ToString() != "NonBuyable")
        {
            return true;
        }

        BeatmapLevelPack pack = __instance.GetPrivateField<BeatmapLevelPack>("_pack");
        if (pack is not PlaylistLevelPack)
        {
            return true;
        }

        ImageView packImage = __instance.GetPrivateField<ImageView>("_packImage");
        Sprite blurredPackArtwork = __instance.GetPrivateField<Sprite>("_blurredPackArtwork");
        LoadingControl loadingControl = __instance.GetPrivateField<LoadingControl>("_loadingControl");
        GameObject detailWrapper = __instance.GetPrivateField<GameObject>("_detailWrapper");

        if (packImage != null)
        {
            if (PluginConfig.Instance.BlurredArt && blurredPackArtwork != null)
            {
                packImage.sprite = blurredPackArtwork;
            }
            packImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        loadingControl?.Hide();
        detailWrapper?.SetActive(true);
        return false;
    }
}