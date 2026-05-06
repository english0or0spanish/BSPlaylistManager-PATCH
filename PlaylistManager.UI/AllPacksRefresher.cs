using System;
using System.Collections.Generic;
using System.Linq;
using PlaylistManager.Interfaces;
using PlaylistManager.Utilities;

namespace PlaylistManager.UI;

public class AllPacksRefresher : IPMRefreshable
{
	private readonly AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController;

	private readonly BeatmapLevelsModel beatmapLevelsModel;

	private readonly PlaylistUpdater playlistUpdater;

	private AllPacksRefresher(AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, BeatmapLevelsModel beatmapLevelsModel, PlaylistUpdater playlistUpdater)
	{
		this.annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
		this.beatmapLevelsModel = beatmapLevelsModel;
		this.playlistUpdater = playlistUpdater;
	}

	public void Refresh()
	{
		BeatmapLevelPack[] array = PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks();
		playlistUpdater.RefreshPlaylistChangedListeners(array);
		object customLevelsRepository = beatmapLevelsModel.GetPrivateField<object>("_customLevelsRepository");
		BeatmapLevelPack[] array2 = customLevelsRepository.GetMemberValue<IReadOnlyList<BeatmapLevelPack>>("beatmapLevelPacks").Concat<BeatmapLevelPack>(array).ToArray();
		int num = Array.FindIndex(array2, (BeatmapLevelPack pack) => pack.packID == annotatedBeatmapLevelCollectionsViewController.selectedAnnotatedBeatmapLevelPack.packID);
		if (num != -1)
		{
			annotatedBeatmapLevelCollectionsViewController.SetData(array2, num, hideIfOneOrNoPacks: false);
		}
	}
}
