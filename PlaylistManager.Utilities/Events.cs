using System;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;

namespace PlaylistManager.Utilities;

public class Events
{
	public static event Action<IPlaylistSong> playlistSongSelected;

	public static event Action<IPlaylist, BeatSaberPlaylistsLib.PlaylistManager> playlistSelected;

	public static event Action<IPlaylistSong, IPlaylist> playlistSongAdded;

	public static event Action<IPlaylistSong, IPlaylist> playlistSongRemoved;

	public static event Action<IPlaylist, BeatSaberPlaylistsLib.PlaylistManager> playlistRenamed;

	internal static void RaisePlaylistSongSelected(IPlaylistSong playlistSong)
	{
		Events.playlistSongSelected?.Invoke(playlistSong);
	}

	internal static void RaisePlaylistSelected(IPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		Events.playlistSelected?.Invoke(playlist, parentManager);
	}

	internal static void RaisePlaylistSongAdded(IPlaylistSong playlistSong, IPlaylist playlist)
	{
		Events.playlistSongAdded?.Invoke(playlistSong, playlist);
	}

	internal static void RaisePlaylistSongRemoved(IPlaylistSong playlistSong, IPlaylist playlist)
	{
		Events.playlistSongRemoved?.Invoke(playlistSong, playlist);
	}

	internal static void RaisePlaylistRenamed(IPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		Events.playlistRenamed?.Invoke(playlist, parentManager);
	}
}
