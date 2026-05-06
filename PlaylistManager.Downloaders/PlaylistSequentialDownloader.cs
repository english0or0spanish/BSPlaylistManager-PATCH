using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib.Types;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using IPA.Loader;
using PlaylistManager.Configuration;
using PlaylistManager.Types;
using PlaylistManager.Utilities;
using SiraUtil.Web;
using SiraUtil.Zenject;
using SongCore;
using Zenject;

namespace PlaylistManager.Downloaders;

internal class PlaylistSequentialDownloader : IInitializable, IDisposable
{
	private readonly IHttpService siraHttpService;

	private readonly BeatSaver beatSaverInstance;

	private readonly SemaphoreSlim downloadSemaphore;

	private static readonly HashSet<string> ownedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private DownloadQueueEntry currentDownload;

	private readonly SemaphoreSlim pauseSemaphore;

	private readonly SemaphoreSlim popupSemaphore;

	private bool preferCustomArchiveURL;

	private bool ignoredDiskWarning;

	private bool disposed;

	internal static readonly List<object> downloadQueue = new List<object>();

	private static readonly LinkedList<BeatSaberPlaylistsLib.Types.Playlist> coversToRefresh = new LinkedList<BeatSaberPlaylistsLib.Types.Playlist>();

	private PopupContents _pendingPopup;

	internal PopupContents PendingPopup
	{
		get
		{
			return _pendingPopup;
		}
		private set
		{
			_pendingPopup = value;
			this.PopupEvent?.Invoke();
		}
	}

	internal event Action PopupEvent;

	internal event Action QueueUpdatedEvent;

	public PlaylistSequentialDownloader(UBinder<Plugin, PluginMetadata> metadata, IHttpService siraHttpService)
	{
		this.siraHttpService = siraHttpService;
		BeatSaverOptions beatSaverOptions = new BeatSaverOptions(metadata.Value.Name, metadata.Value.HVersion.ToString());
		beatSaverInstance = new BeatSaver(beatSaverOptions);
		downloadSemaphore = new SemaphoreSlim(1, 1);
		pauseSemaphore = new SemaphoreSlim(0, 1);
		popupSemaphore = new SemaphoreSlim(0, 1);
		PendingPopup = null;
	}

	public void Initialize()
	{
		foreach (DownloadQueueEntry item in downloadQueue.OfType<DownloadQueueEntry>())
		{
			item.DownloadAbortedEvent += OnDownloadAborted;
		}
		foreach (DownloadQueueEntry item2 in downloadQueue)
		{
			_ = item2;
			IterateQueue();
		}
		PlaylistDownloader.PlaylistQueuedEvent += OnPlaylistQueued;
	}

	public void Dispose()
	{
		disposed = true;
		if (currentDownload != null && !currentDownload.cancellationTokenSource.IsCancellationRequested)
		{
			currentDownload.cancellationTokenSource.Cancel();
		}
		foreach (DownloadQueueEntry item in downloadQueue.OfType<DownloadQueueEntry>())
		{
			item.DownloadAbortedEvent -= OnDownloadAborted;
		}
		PlaylistDownloader.PlaylistQueuedEvent -= OnPlaylistQueued;
	}

	public void QueuePlaylist(DownloadQueueEntry downloadQueueEntry)
	{
		downloadQueue.Add(downloadQueueEntry);
		OnPlaylistQueued(downloadQueueEntry);
	}

	private void OnPlaylistQueued(DownloadQueueEntry downloadQueueEntry)
	{
		downloadQueueEntry.DownloadAbortedEvent += OnDownloadAborted;
		this.QueueUpdatedEvent?.Invoke();
		IterateQueue();
	}

	private void OnDownloadAborted(DownloadQueueEntry downloadQueueEntry)
	{
		downloadQueueEntry.DownloadAbortedEvent -= OnDownloadAborted;
		downloadQueue.Remove(downloadQueueEntry);
		this.QueueUpdatedEvent?.Invoke();
	}

	private async void IterateQueue()
	{
		await downloadSemaphore.WaitAsync();
		if (downloadQueue.Count > 0 && !disposed)
		{
			DownloadQueueEntry toDownload = downloadQueue.OfType<DownloadQueueEntry>().FirstOrDefault();
			await DownloadPlaylist(toDownload);
			if (!disposed)
			{
				downloadQueue.Remove(toDownload);
			}
			this.QueueUpdatedEvent?.Invoke();
		}
		downloadSemaphore.Release();
	}

	internal void OnQueueClear()
	{
		if (downloadQueue.Count == 0)
		{
			Loader.SongsLoadedEvent += OnSongsLoaded;
			Loader.Instance.RefreshSongs(fullRefresh: false);
			ownedHashes.Clear();
		}
	}

	private void OnSongsLoaded(Loader loader, ConcurrentDictionary<string, BeatmapLevel> beatmapLevels)
	{
		Loader.SongsLoadedEvent -= OnSongsLoaded;
		foreach (BeatSaberPlaylistsLib.Types.Playlist item in coversToRefresh)
		{
			item.RaiseCoverImageChangedForDefaultCover();
		}
		coversToRefresh.Clear();
	}

	internal void PauseDownload()
	{
		if (currentDownload != null && !currentDownload.cancellationTokenSource.IsCancellationRequested)
		{
			currentDownload.cancellationTokenSource.Cancel();
		}
	}

	internal void ResumeDownload()
	{
		if (currentDownload != null && pauseSemaphore.CurrentCount == 0)
		{
			pauseSemaphore.Release();
		}
	}

	private async Task DownloadPlaylist(DownloadQueueEntry downloadQueueEntry)
	{
		currentDownload = downloadQueueEntry;
		List<IPlaylistSong> missingSongs = PlaylistLibUtils.GetMissingSongs(downloadQueueEntry.playlist, ownedHashes);
		downloadQueueEntry.SetMissingLevels(missingSongs.Count);
		downloadQueueEntry.SetTotalProgress(0);
		preferCustomArchiveURL = true;
		bool shownCustomArchiveWarning = false;
		for (int i = 0; i < missingSongs.Count; i++)
		{
			if (preferCustomArchiveURL && missingSongs[i].TryGetCustomData("customArchiveURL", out object value))
			{
				string customArchiveURL = (string)value;
				string identifier = PlaylistLibUtils.GetIdentifierForPlaylistSong(missingSongs[i]);
				if (identifier == "")
				{
					continue;
				}
				if (!shownCustomArchiveWarning)
				{
					shownCustomArchiveWarning = true;
					PendingPopup = new YesNoPopupContents("This playlist uses mirror download links. Would you like to use them?", delegate
					{
						SetCustomArchivePreference(preferCustomArchiveURL: true);
					}, "Yes", "No", delegate
					{
						SetCustomArchivePreference(preferCustomArchiveURL: false);
					}, animateParentCanvas: false);
					await popupSemaphore.WaitAsync();
					PendingPopup = null;
					if (!preferCustomArchiveURL)
					{
						i--;
						continue;
					}
				}
				await BeatmapDownloadByCustomURL(customArchiveURL, identifier, downloadQueueEntry.cancellationTokenSource.Token, downloadQueueEntry);
			}
			else if (!string.IsNullOrEmpty(missingSongs[i].Hash))
			{
				await BeatmapDownloadByHash(missingSongs[i].Hash, downloadQueueEntry.cancellationTokenSource.Token, downloadQueueEntry);
			}
			else if (!string.IsNullOrEmpty(missingSongs[i].Key))
			{
				string text = await BeatmapDownloadByKey(missingSongs[i].Key.ToLowerInvariant(), downloadQueueEntry.cancellationTokenSource.Token, downloadQueueEntry);
				if (!string.IsNullOrEmpty(text))
				{
					missingSongs[i].Hash = text;
				}
			}
			downloadQueueEntry.SetTotalProgress(i + 1);
			if (downloadQueueEntry.Aborted)
			{
				break;
			}
			if (disposed)
			{
				downloadQueueEntry.cancellationTokenSource = new CancellationTokenSource();
				return;
			}
			if (downloadQueueEntry.cancellationTokenSource.IsCancellationRequested)
			{
				await pauseSemaphore.WaitAsync();
				i--;
				downloadQueueEntry.cancellationTokenSource = new CancellationTokenSource();
			}
		}
		downloadQueueEntry.playlist.RaisePlaylistChanged();
		downloadQueueEntry.parentManager.StorePlaylist(downloadQueueEntry.playlist);
		if (downloadQueueEntry.playlist is BeatSaberPlaylistsLib.Types.Playlist value2)
		{
			coversToRefresh.AddLast(value2);
		}
		downloadQueueEntry.DownloadAbortedEvent -= OnDownloadAborted;
		currentDownload = null;
	}

	private void SetCustomArchivePreference(bool preferCustomArchiveURL)
	{
		this.preferCustomArchiveURL = preferCustomArchiveURL;
		popupSemaphore.Release();
	}

	private async Task BeatSaverBeatmapDownload(Beatmap song, BeatmapVersion songversion, CancellationToken token, IProgress<double> progress = null)
	{
		string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
		if (!Directory.Exists(customSongsPath))
		{
			Directory.CreateDirectory(customSongsPath);
		}
		if (!ownedHashes.Contains(songversion.Hash))
		{
			await ExtractZipAsync(await songversion.DownloadZIP(token, progress).ConfigureAwait(continueOnCapturedContext: false), customSongsPath, FolderNameForBeatsaverMap(song)).ConfigureAwait(continueOnCapturedContext: false);
			ownedHashes.Add(songversion.Hash);
		}
	}

	private async Task<string> BeatmapDownloadByKey(string key, CancellationToken token, IProgress<double> progress = null)
	{
		if (!token.IsCancellationRequested)
		{
			try
			{
				Beatmap song = await beatSaverInstance.Beatmap(key, token);
				if (song == null)
				{
					Plugin.Log.Error("Failed to download Song " + key + ". Unable to find a beatmap for that hash.");
					return "";
				}
				if (Loader.GetLevelByHash(song.LatestVersion.Hash) == null)
				{
					await BeatSaverBeatmapDownload(song, song.LatestVersion, token, progress);
				}
				return song.LatestVersion.Hash;
			}
			catch (Exception ex)
			{
				if (!(ex is TaskCanceledException))
				{
					Plugin.Log.Error($"Failed to download Song {key}. Exception: {ex}");
				}
			}
		}
		return "";
	}

	private async Task BeatmapDownloadByHash(string hash, CancellationToken token, IProgress<double> progress = null)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}
		try
		{
			Beatmap beatmap = await beatSaverInstance.BeatmapByHash(hash, token);
			if (beatmap == null)
			{
				Plugin.Log.Error("Failed to download Song " + hash + ". Unable to find a beatmap for that hash.");
				return;
			}
			BeatmapVersion beatmapVersion = null;
			foreach (BeatmapVersion version in beatmap.Versions)
			{
				if (string.Equals(hash, version.Hash, StringComparison.OrdinalIgnoreCase))
				{
					beatmapVersion = version;
				}
			}
			if (beatmapVersion != null)
			{
				await BeatSaverBeatmapDownload(beatmap, beatmapVersion, token, progress);
			}
			else
			{
				await BeatmapDownloadByCustomURL("https://cdn.beatsaver.com/" + hash.ToLowerInvariant() + ".zip", FolderNameForBeatsaverMap(beatmap), token, progress as IProgress<float>);
			}
		}
		catch (Exception ex)
		{
			if (!(ex is TaskCanceledException))
			{
				Plugin.Log.Error($"Failed to download Song {hash}. Exception: {ex}");
			}
		}
	}

	private async Task BeatmapDownloadByCustomURL(string url, string songName, CancellationToken token, IProgress<float> progress = null)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}
		try
		{
			string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
			if (!Directory.Exists(customSongsPath))
			{
				Directory.CreateDirectory(customSongsPath);
			}
			IHttpResponse httpResponse = await siraHttpService.GetAsync(url, progress, token);
			if (httpResponse.Successful)
			{
				await ExtractZipAsync(await httpResponse.ReadAsByteArrayAsync(), customSongsPath, songName).ConfigureAwait(continueOnCapturedContext: false);
			}
			else
			{
				Plugin.Log.Error("Failed to download Song " + url);
			}
		}
		catch (Exception ex)
		{
			if (!(ex is TaskCanceledException))
			{
				Plugin.Log.Error("Failed to download Song " + url);
			}
		}
	}

	private string FolderNameForBeatsaverMap(Beatmap song)
	{
		return (song.ID + " (" + song.Metadata.SongName + " - " + song.Metadata.LevelAuthorName).Truncate(49, appendEllipsis: true) + ")";
	}

	private async Task ExtractZipAsync(byte[] zip, string customSongsPath, string songName, bool overwrite = false)
	{
		Stream zipStream = new MemoryStream(zip);
		try
		{
			ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
			try
			{
				string path = string.Join("", songName.Split(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray()));
				string path2 = Path.Combine(customSongsPath, path);
				if (!overwrite && Directory.Exists(path2))
				{
					int i;
					for (i = 1; Directory.Exists(path2 + $" ({i})"); i++)
					{
					}
					path2 += $" ({i})";
				}
				if (PluginConfig.Instance.DriveFullProtection)
				{
					DriveInfo driveInfo = new DriveInfo(Path.GetPathRoot(path2));
					long num = 0L;
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						num += entry.Length;
					}
					if (driveInfo.AvailableFreeSpace - num < 104857600 && !ignoredDiskWarning)
					{
						CreateDrivePopup();
						await popupSemaphore.WaitAsync();
						PendingPopup = null;
						if (!ignoredDiskWarning)
						{
							currentDownload.AbortDownload();
							downloadQueue.Clear();
							downloadQueue.Add(currentDownload);
							return;
						}
					}
				}
				if (!Directory.Exists(path2))
				{
					Directory.CreateDirectory(path2);
				}
				await Task.Run(delegate
				{
					foreach (ZipArchiveEntry entry2 in archive.Entries)
					{
						if (!string.IsNullOrWhiteSpace(entry2.Name) && entry2.Name == entry2.FullName)
						{
							string text = Path.Combine(path2, entry2.Name);
							if (overwrite || !File.Exists(text))
							{
								entry2.ExtractToFile(text, overwrite);
							}
						}
					}
				}).ConfigureAwait(continueOnCapturedContext: false);
				archive.Dispose();
			}
			finally
			{
				if (archive != null)
				{
					((IDisposable)archive).Dispose();
				}
			}
		}
		catch (Exception arg)
		{
			Plugin.Log.Error($"Unable to extract ZIP! Exception: {arg}");
			return;
		}
		zipStream.Close();
	}

	private void CreateDrivePopup()
	{
		string message = "You are running out of disk space (less than 100MB), continuing the download can cause issues such as corrupt game configs (as there may not be enough space to save them).";
		if (PluginConfig.Instance.EasterEggs && PluginConfig.Instance.AuthorName.IndexOf("SKALX", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			message = "Remember the October 26th, 2021 \"JoeSaber\" incident? Wanna do it again?";
		}
		PendingPopup = new YesNoPopupContents(message, delegate
		{
			ignoredDiskWarning = true;
			popupSemaphore.Release();
		}, "Continue", "Abort", delegate
		{
			ignoredDiskWarning = false;
			popupSemaphore.Release();
		}, animateParentCanvas: false);
	}
}
