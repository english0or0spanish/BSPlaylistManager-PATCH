using System;
using System.Collections.Generic;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using PlaylistManager.Configuration;
using PlaylistManager.UI;
using PlaylistManager.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches;

internal class LevelCollectionCellSetDataPatch : IAffinity
{
	private readonly Dictionary<IPlaylist, AnnotatedBeatmapLevelCollectionCell> eventTable = new Dictionary<IPlaylist, AnnotatedBeatmapLevelCollectionCell>();

	private readonly HoverHintController hoverHintController;

	private readonly PlaylistUpdater playlistUpdater;

	public LevelCollectionCellSetDataPatch(HoverHintController hoverHintController, PlaylistUpdater playlistUpdater)
	{
		this.hoverHintController = hoverHintController;
		this.playlistUpdater = playlistUpdater;
	}

	[AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionCell), "SetData", AffinityMethodType.Normal, null, new Type[] { })]
	private void Patch(AnnotatedBeatmapLevelCollectionCell __instance, ref BeatmapLevelPack beatmapLevelPack)
	{
		if (beatmapLevelPack is PlaylistLevelPack playlistLevelPack)
		{
			IPlaylist playlist = playlistLevelPack.playlist;
			eventTable.Remove(playlist);
			eventTable.Add(playlist, __instance);
			playlist.SpriteLoaded -= OnSpriteLoaded;
			playlist.SpriteLoaded += OnSpriteLoaded;
		}
		if (PluginConfig.Instance.PlaylistHoverHints)
		{
			HoverHint hoverHint = ((Component)(object)__instance).GetComponent<HoverHint>();
			if (hoverHint == null)
			{
				hoverHint = ((Component)(object)__instance).gameObject.AddComponent<HoverHint>();
				Accessors.HoverHintControllerAccessor.Invoke(ref hoverHint) = hoverHintController;
			}
			hoverHint.text = beatmapLevelPack.packName;
		}
	}

	private void OnSpriteLoaded(object sender, EventArgs e)
	{
		if (sender is IPlaylist playlist)
		{
			playlist.SpriteLoaded -= OnSpriteLoaded;
			if (eventTable.TryGetValue(playlist, out var value) && !((UnityEngine.Object)(object)value == null))
			{
				BeatmapLevelPack beatmapLevelPack = value.GetPrivateField<BeatmapLevelPack>("_beatmapLevelPack");
				if (beatmapLevelPack is PlaylistLevelPack)
				{
					value.GetPrivateField<ImageView>("_coverImage").sprite = playlist.SmallSprite;
					playlistUpdater.RefreshAnnotatedBeatmapCollection(beatmapLevelPack);
				}
			}
		}
	}
}
