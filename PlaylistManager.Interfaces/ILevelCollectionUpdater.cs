using BeatSaberPlaylistsLib;

namespace PlaylistManager.Interfaces;

internal interface ILevelCollectionUpdater
{
	void LevelCollectionUpdated(BeatmapLevelPack annotatedBeatmapLevelCollection, BeatSaberPlaylistsLib.PlaylistManager parentManager);
}
