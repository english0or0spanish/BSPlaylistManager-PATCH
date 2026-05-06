using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using IPA.Loader;
using PlaylistManager.Configuration;
using PlaylistManager.Downloaders;
using PlaylistManager.Interfaces;
using PlaylistManager.Types;
using PlaylistManager.Utilities;
using SiraUtil.Web;
using SiraUtil.Zenject;
using SongCore;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

public class PlaylistDetailViewButtonsController : IInitializable, IDisposable, INotifyPropertyChanged, ILevelCollectionUpdater, ILevelCategoryUpdater, ILevelCollectionsTableUpdater
{
	private readonly IHttpService siraHttpService;

	private readonly PlaylistSequentialDownloader playlistDownloader;

	private readonly LevelPackDetailViewController levelPackDetailViewController;

	private readonly PopupModalsController popupModalsController;

	private readonly PlaylistDetailsViewController playlistDetailsViewController;

	private readonly AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController;

	private readonly Loader loader;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private IPlaylist selectedPlaylist;

	private BeatSaberPlaylistsLib.PlaylistManager parentManager;

	private List<IPlaylistSong> _missingSongs;

	private DownloadQueueEntry _downloadQueueEntry;

	[UIComponent("root")]
	private Transform rootTransform = null!;

	[UIComponent("sync-button")]
	private Transform syncButtonTransform = null!;

	private List<IPlaylistSong> MissingSongs
	{
		get
		{
			return _missingSongs;
		}
		set
		{
			_missingSongs = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DownloadInteractable"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DownloadHint"));
		}
	}

	private DownloadQueueEntry DownloadQueueEntry
	{
		get
		{
			return _downloadQueueEntry;
		}
		set
		{
			_downloadQueueEntry = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DownloadInteractable"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DownloadHint"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DeleteHint"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistNotDownloading"));
		}
	}

	[UIValue("download-hint")]
	private string DownloadHint
	{
		get
		{
			if (DownloadQueueEntry != null)
			{
				return "Playlist is downloading";
			}
			if (MissingSongs != null && MissingSongs.Count > 0)
			{
				return $"Download {MissingSongs.Count} missing songs.";
			}
			return "All songs already downloaded";
		}
	}

	[UIValue("delete-hint")]
	private string DeleteHint
	{
		get
		{
			if (DownloadQueueEntry != null)
			{
				return "Can't delete playlist when it is downloading";
			}
			return "Delete Playlist";
		}
	}

	[UIValue("download-interactable")]
	private bool DownloadInteractable
	{
		get
		{
			if (DownloadQueueEntry == null && MissingSongs != null)
			{
				return MissingSongs.Count > 0;
			}
			return false;
		}
	}

	[UIValue("playlist-not-downloading")]
	private bool PlaylistNotDownloading => DownloadQueueEntry == null;

	public event Action<IReadOnlyList<BeatmapLevelPack>, int> LevelCollectionTableViewUpdatedEvent;

	public event PropertyChangedEventHandler PropertyChanged;

	internal PlaylistDetailViewButtonsController(IHttpService siraHttpService, PlaylistSequentialDownloader playlistDownloader, LevelPackDetailViewController levelPackDetailViewController, PopupModalsController popupModalsController, PlaylistDetailsViewController playlistDetailsViewController, AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, Loader loader, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.siraHttpService = siraHttpService;
		this.playlistDownloader = playlistDownloader;
		this.levelPackDetailViewController = levelPackDetailViewController;
		this.popupModalsController = popupModalsController;
		this.playlistDetailsViewController = playlistDetailsViewController;
		this.annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
		this.loader = loader;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
	}

	public void Initialize()
	{
		Transform detailWrapper = levelPackDetailViewController.GetPrivateField<Transform>("_detailWrapper");
		if (detailWrapper == null)
		{
			Plugin.Log?.Warn("PlaylistDetailViewButtonsController: _detailWrapper field is null. BSML view cannot be attached.");
			return;
		}
		bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.PlaylistDetailViewButtons.bsml"), detailWrapper.gameObject, this);
		if (syncButtonTransform != null)
		{
			syncButtonTransform.localScale *= 0.6f;
			syncButtonTransform.gameObject.SetActive(value: false);
		}
		else
		{
			Plugin.Log?.Warn("PlaylistDetailViewButtonsController: syncButtonTransform is null after BSML parse.");
		}
		if (rootTransform != null)
		{
			rootTransform.gameObject.SetActive(value: false);
		}
		else
		{
			Plugin.Log?.Warn("PlaylistDetailViewButtonsController: rootTransform is null after BSML parse.");
		}
		levelPackDetailViewController.didActivateEvent += PackViewActivated;
		playlistDownloader.QueueUpdatedEvent += OnQueueUpdated;
	}

	public void Dispose()
	{
		levelPackDetailViewController.didActivateEvent -= PackViewActivated;
		playlistDownloader.QueueUpdatedEvent -= OnQueueUpdated;
	}

	[UIAction("details-click")]
	private void ShowDetails()
	{
		playlistDetailsViewController.ShowDetails();
	}

	[UIAction("delete-click")]
	private void OnDelete()
	{
		int count = selectedPlaylist.PlaylistLevelPack.AllBeatmapLevels().Count;
		string checkboxText = ((count > 0) ? $"Also delete all {count} songs from the game." : "");
		popupModalsController.ShowYesNoModal(rootTransform, "Are you sure you would like to delete the playlist \"" + selectedPlaylist.Title + "\"?", DeleteButtonPressed, "Yes", "No", null, animateParentCanvas: true, checkboxText);
	}

	private void DeleteButtonPressed()
	{
		try
		{
			if (popupModalsController.CheckboxValue)
			{
				DeleteSongs();
			}
			DeletePlaylist();
		}
		catch (Exception arg)
		{
			popupModalsController.ShowOkModal(rootTransform, "Error: Playlist cannot be deleted.", null);
			Plugin.Log.Critical($"An exception was thrown while deleting a playlist.\nException message:{arg}");
		}
	}

	private async void DeleteSongs()
	{
		popupModalsController.ShowLoadingModal(rootTransform, "Deleting Playlist & Songs");
		List<string> folderPaths = (from l in selectedPlaylist.BeatmapLevels
			where !l.hasPrecalculatedData
			select GetLoadedSaveDataFolderPath(l.levelID) into p
			where p != null
			select p).ToList();
		await loader.DeleteSongsAsync(folderPaths);
		popupModalsController.DismissLoadingModal();
	}

	private void DeletePlaylist()
	{
		parentManager.DeletePlaylist(selectedPlaylist, recycle: true);
		int selectedItemIndex = annotatedBeatmapLevelCollectionsViewController.selectedItemIndex;
		List<BeatmapLevelPack> list = annotatedBeatmapLevelCollectionsViewController.GetPrivateField<IReadOnlyList<BeatmapLevelPack>>("_annotatedBeatmapLevelCollections").ToList();
		list.RemoveAt(selectedItemIndex);
		selectedItemIndex--;
		this.LevelCollectionTableViewUpdatedEvent?.Invoke(list.ToArray(), (selectedItemIndex >= 0) ? selectedItemIndex : 0);
	}

	[UIAction("download-click")]
	private void DownloadClick()
	{
		DownloadQueueEntry = new DownloadQueueEntry(selectedPlaylist, parentManager);
		playlistDownloader.QueuePlaylist(DownloadQueueEntry);
		popupModalsController.ShowOkModal(rootTransform, selectedPlaylist.Title + " has been added to the download queue!", null);
	}

	private static string GetLoadedSaveDataFolderPath(string levelId)
	{
		Type collectionsType = Type.GetType("SongCore.Collections, SongCore");
		MethodInfo method = collectionsType?.GetMethod("GetLoadedSaveData", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
		object saveData = method?.Invoke(null, new object[] { levelId });
		if (saveData == null)
		{
			return null;
		}
		object folderInfo = saveData.GetMemberValue<object>("customLevelFolderInfo");
		return folderInfo?.GetMemberValue<string>("folderPath");
	}

	private void OnQueueUpdated()
	{
		if (PlaylistSequentialDownloader.downloadQueue.Count == 0)
		{
			DownloadQueueEntry = null;
			UpdateMissingSongs();
		}
	}

	private void UpdateMissingSongs()
	{
		MissingSongs = PlaylistLibUtils.GetMissingSongs(selectedPlaylist);
	}

	[UIAction("sync-click")]
	private async Task SyncPlaylistAsync()
	{
		if (!selectedPlaylist.TryGetCustomData("syncURL", out object value))
		{
			popupModalsController.ShowOkModal(rootTransform, "Error: The selected playlist cannot be synced", null);
			return;
		}
		string syncURL = (string)value;
		CancellationTokenSource tokenSource = new CancellationTokenSource();
		popupModalsController.ShowOkModal(rootTransform, "Syncing Playlist", delegate
		{
			tokenSource.Cancel();
		}, "Cancel");
		try
		{
			IHttpResponse httpResponse = await siraHttpService.GetAsync(syncURL, null, tokenSource.Token);
			if (httpResponse.Successful)
			{
				selectedPlaylist.Clear();
				IPlaylistHandler defaultHandler = PlaylistLibUtils.playlistManager.DefaultHandler;
				defaultHandler.Populate(await httpResponse.ReadAsStreamAsync(), selectedPlaylist);
				selectedPlaylist.RaisePlaylistChanged();
				parentManager.StorePlaylist(selectedPlaylist);
			}
			else
			{
				popupModalsController.OkText = "Error: The selected playlist cannot be synced";
				popupModalsController.OkButtonText = "Ok";
			}
		}
		catch (Exception ex)
		{
			if (!(ex is TaskCanceledException))
			{
				popupModalsController.OkText = "Error: The selected playlist cannot be synced";
				popupModalsController.OkButtonText = "Ok";
			}
		}
		finally
		{
			if (!selectedPlaylist.TryGetCustomData("syncURL", out value))
			{
				selectedPlaylist.SetCustomData("syncURL", syncURL);
			}
			switch (PluginConfig.Instance.SyncOption)
			{
			case PluginConfig.SyncOptions.On:
				DownloadAccepted();
				break;
			case PluginConfig.SyncOptions.Off:
				DownloadRejected();
				break;
			case PluginConfig.SyncOptions.Ask:
				popupModalsController.ShowYesNoModal(rootTransform, "Would you like to download the songs after syncing?", DownloadAccepted, "Yes", "No", DownloadRejected);
				break;
			}
		}
	}

	private void DownloadAccepted()
	{
		DownloadQueueEntry = new DownloadQueueEntry(selectedPlaylist, parentManager);
		playlistDownloader.QueuePlaylist(DownloadQueueEntry);
		popupModalsController.ShowOkModal(rootTransform, "Playlist Synced and added to Download Queue!", null);
	}

	private void DownloadRejected()
	{
		popupModalsController.ShowOkModal(rootTransform, "Playlist Synced!", null);
	}

	private void PackViewActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
	{
		if (selectedPlaylist != null && parentManager != null && rootTransform != null)
		{
			rootTransform.gameObject.SetActive(value: true);
		}
	}

	public void LevelCollectionUpdated(BeatmapLevelPack selectedBeatmapLevelCollection, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		if (selectedBeatmapLevelCollection is PlaylistLevelPack playlistLevelPack)
		{
			selectedPlaylist = playlistLevelPack.playlist;
			this.parentManager = parentManager;
			DownloadQueueEntry = PlaylistSequentialDownloader.downloadQueue.OfType<DownloadQueueEntry>().FirstOrDefault((DownloadQueueEntry x) => x.playlist == selectedPlaylist);
			UpdateMissingSongs();
			if (rootTransform != null)
			{
				rootTransform.gameObject.SetActive(value: true);
			}
			bool showSync = playlistLevelPack.playlist.TryGetCustomData("syncURL", out object value) && value is string value2 && !string.IsNullOrWhiteSpace(value2);
			if (syncButtonTransform != null)
			{
				syncButtonTransform.gameObject.SetActive(showSync);
			}
		}
		else
		{
			selectedPlaylist = null;
			this.parentManager = null;
			MissingSongs = null;
            if (rootTransform != null)
            {
                rootTransform.gameObject.SetActive(value: false);
            }
        }
    }

    public void LevelCategoryUpdated(SelectLevelCategoryViewController.LevelCategory levelCategory, bool _)
    {
        if (levelCategory != SelectLevelCategoryViewController.LevelCategory.CustomSongs && rootTransform != null)
        {
            rootTransform.gameObject.SetActive(value: false);
        }
    }
}
