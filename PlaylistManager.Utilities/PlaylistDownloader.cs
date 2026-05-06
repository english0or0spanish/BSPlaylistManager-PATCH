using System;
using PlaylistManager.Downloaders;
using PlaylistManager.Types;

namespace PlaylistManager.Utilities;

public class PlaylistDownloader
{
	private readonly PlaylistSequentialDownloader playlistSequentialDownloader;

	internal static event Action<DownloadQueueEntry> PlaylistQueuedEvent;

	internal PlaylistDownloader(PlaylistSequentialDownloader playlistSequentialDownloader)
	{
		this.playlistSequentialDownloader = playlistSequentialDownloader;
	}

	public static void Queue(DownloadQueueEntry downloadQueueEntry)
	{
		PlaylistSequentialDownloader.downloadQueue.Add(downloadQueueEntry);
		PlaylistDownloader.PlaylistQueuedEvent?.Invoke(downloadQueueEntry);
	}

	public void QueuePlaylist(DownloadQueueEntry downloadQueueEntry)
	{
		playlistSequentialDownloader.QueuePlaylist(downloadQueueEntry);
	}
}
