using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using IPA.Utilities.Async;
using PlaylistManager.Configuration;
using UnityEngine;

namespace PlaylistManager.Utilities;

public static class PlaylistLibUtils
{
	private const string ICON_PATH = "PlaylistManager.Icons.DefaultIcon.png";

	private const string EASTER_EGG_URL = "https://raw.githubusercontent.com/rithik-b/PlaylistManager/master/img/easteregg.bplist";

	public static BeatSaberPlaylistsLib.PlaylistManager playlistManager => BeatSaberPlaylistsLib.PlaylistManager.DefaultManager;

	public static IPlaylist CreatePlaylistWithConfig(string playlistName, BeatSaberPlaylistsLib.PlaylistManager playlistManager)
	{
		string authorName = PluginConfig.Instance.AuthorName;
		bool easterEgg = authorName.IndexOf("BINTER", StringComparison.OrdinalIgnoreCase) >= 0 && playlistName.IndexOf("TECH", StringComparison.OrdinalIgnoreCase) >= 0 && PluginConfig.Instance.EasterEggs;
		return CreatePlaylist(playlistName, authorName, playlistManager, !PluginConfig.Instance.DefaultImageDisabled, PluginConfig.Instance.DefaultAllowDuplicates, easterEgg);
	}

	public static IPlaylist CreatePlaylist(string playlistName, string playlistAuthorName, BeatSaberPlaylistsLib.PlaylistManager playlistManager, bool defaultCover = true, bool allowDups = true, bool easterEgg = false)
	{
		IPlaylist playlist = playlistManager.CreatePlaylist("", playlistName, playlistAuthorName, "");
		if (defaultCover)
		{
			using Stream cover = Assembly.GetExecutingAssembly().GetManifestResourceStream("PlaylistManager.Icons.DefaultIcon.png");
			playlist.SetCover(cover);
		}
		if (!allowDups)
		{
			playlist.AllowDuplicates = false;
		}
		if (easterEgg)
		{
			playlist.SetCustomData("syncURL", "https://raw.githubusercontent.com/rithik-b/PlaylistManager/master/img/easteregg.bplist");
		}
		playlist.RaisePlaylistChanged();
		playlistManager.StorePlaylist(playlist);
		PlaylistLibUtils.playlistManager.RequestRefresh("PlaylistManager (plugin)");
		return playlist;
	}

	public static string GetIdentifierForPlaylistSong(IPlaylistSong playlistSong)
	{
		if (playlistSong.Identifiers.HasFlag(Identifier.Hash))
		{
			return playlistSong.Hash;
		}
		if (playlistSong.Identifiers.HasFlag(Identifier.Key))
		{
			return playlistSong.Key;
		}
		if (playlistSong.Identifiers.HasFlag(Identifier.LevelId))
		{
			return playlistSong.LevelId;
		}
		return "";
	}

	public static List<IPlaylistSong> GetMissingSongs(IPlaylist playlist, HashSet<string> ownedHashes = null)
	{
		if (playlist != null)
		{
			return playlist.Where((IPlaylistSong s) => s.BeatmapLevel == null && !(ownedHashes?.Contains(s.Hash) ?? false)).Distinct(IPlaylistSongComparer<IPlaylistSong>.Default).ToList();
		}
		return new List<IPlaylistSong>();
	}

	public static IPlaylist[] TryGetAllPlaylists()
	{
		AggregateException e;
		IPlaylist[] allPlaylists = playlistManager.GetAllPlaylists(includeChildren: true, out e);
		if (e != null)
		{
			Plugin.Log.Error(e.Message);
			foreach (Exception innerException in e.InnerExceptions)
			{
				Plugin.Log.Error(innerException.ToString());
			}
		}
		return allPlaylists;
	}

	public static BeatmapLevelPack[] TryGetAllPlaylistsAsLevelPacks()
	{
		IPlaylist[] array = TryGetAllPlaylists();
		BeatmapLevelPack[] array2 = new BeatmapLevelPack[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = array[i].PlaylistLevelPack;
		}
		return array2;
	}

	private static Stream GetFolderImageStream()
	{
		return Assembly.GetExecutingAssembly().GetManifestResourceStream("PlaylistManager.Icons.FolderIcon.png");
	}

	internal static async Task<Sprite> GeneratePlaylistIcon(IPlaylist playlist)
	{
		Stream coverStream = await playlist.GetDefaultCoverStream();
		try
		{
			if (coverStream != null)
			{
				Sprite sprite = null;
				await UnityMainThreadTaskScheduler.Factory.StartNew(() => sprite = SpriteUtils.CreateSprite(coverStream.ToArray(), 100f));
				return sprite ? sprite : BeatSaberPlaylistsLib.Utilities.DefaultSprite;
			}
			return BeatSaberPlaylistsLib.Utilities.DefaultSprite;
		}
		finally
		{
			if (coverStream != null)
			{
				((IDisposable)coverStream).Dispose();
			}
		}
	}
}
