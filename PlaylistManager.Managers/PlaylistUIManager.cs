using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlaylistManager.Configuration;
using PlaylistManager.Downloaders;
using PlaylistManager.HarmonyPatches;
using PlaylistManager.Interfaces;
using PlaylistManager.UI;
using PlaylistManager.Utilities;
using Zenject;

namespace PlaylistManager.Managers;

internal class PlaylistUIManager : IInitializable, IDisposable, ILevelCollectionsTableUpdater
{
	private readonly AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController;

	private readonly LevelCollectionNavigationController levelCollectionNavigationController;

	private readonly SelectLevelCategoryViewController selectLevelCategoryViewController;

	private readonly SettingsViewController settingsViewController;

	private readonly PlaylistSequentialDownloader playlistDownloader;

	private int downloadingBeatmapCollectionIdx;

	private BeatmapLevelPack[] downloadingBeatmapLevelCollections;

	private BeatmapLevel downloadingBeatmap;

	private readonly List<ILevelCategoryUpdater> levelCategoryUpdaters;

	private readonly IPMRefreshable refreshable;

	public event Action<IReadOnlyList<BeatmapLevelPack>, int> LevelCollectionTableViewUpdatedEvent;

	internal PlaylistUIManager(
	    AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController,
	    LevelCollectionNavigationController levelCollectionNavigationController,
	    SelectLevelCategoryViewController selectLevelCategoryViewController,
	    SettingsViewController settingsViewController,
	    PlaylistSequentialDownloader playlistDownloader,
	    List<ILevelCategoryUpdater> levelCategoryUpdaters,
	    IPMRefreshable refreshable)
	{
	    this.annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
	    this.levelCollectionNavigationController = levelCollectionNavigationController;
	    this.selectLevelCategoryViewController = selectLevelCategoryViewController;
	    this.settingsViewController = settingsViewController;
	    this.playlistDownloader = playlistDownloader;
	    this.levelCategoryUpdaters = levelCategoryUpdaters;
	    this.refreshable = refreshable;
	}

	public void Initialize()
	{
		selectLevelCategoryViewController.didSelectLevelCategoryEvent += SelectLevelCategoryViewController_didSelectLevelCategoryEvent;
		selectLevelCategoryViewController.didActivateEvent += SelectLevelCategoryViewController_didActivateEvent;
		selectLevelCategoryViewController.didDeactivateEvent += SelectLevelCategoryViewController_didDeactivateEvent;
		playlistDownloader.QueueUpdatedEvent += PlaylistDownloader_QueueUpdatedEvent;
		PlaylistLibUtils.playlistManager.PlaylistsRefreshRequested += PlaylistManager_PlaylistsRefreshRequested;
		settingsViewController.NameFetchRequestedEvent += AssignAuthor;
		AssignAuthor();
	}

	public void Dispose()
	{
		selectLevelCategoryViewController.didSelectLevelCategoryEvent -= SelectLevelCategoryViewController_didSelectLevelCategoryEvent;
		selectLevelCategoryViewController.didActivateEvent -= SelectLevelCategoryViewController_didActivateEvent;
		selectLevelCategoryViewController.didDeactivateEvent -= SelectLevelCategoryViewController_didDeactivateEvent;
		playlistDownloader.QueueUpdatedEvent -= PlaylistDownloader_QueueUpdatedEvent;
		SongCore_RefreshLevelPacks.PacksToBeRefreshedEvent -= OnPacksToBeRefreshed;
		LevelFilteringNavigationController_UpdateSecondChildControllerContent.SecondChildControllerUpdatedEvent -= LevelFilteringNavigationController_SecondChildControllerUpdatedEvent;
		PlaylistLibUtils.playlistManager.PlaylistsRefreshRequested -= PlaylistManager_PlaylistsRefreshRequested;
		settingsViewController.NameFetchRequestedEvent -= AssignAuthor;
	}

	private void SelectLevelCategoryViewController_didSelectLevelCategoryEvent(SelectLevelCategoryViewController selectLevelCategoryViewController, SelectLevelCategoryViewController.LevelCategory levelCategory)
	{
		foreach (ILevelCategoryUpdater levelCategoryUpdater in levelCategoryUpdaters)
		{
			levelCategoryUpdater.LevelCategoryUpdated(levelCategory, viewControllerActivated: false);
		}
	}

	private void SelectLevelCategoryViewController_didActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
	{
		foreach (ILevelCategoryUpdater levelCategoryUpdater in levelCategoryUpdaters)
		{
			levelCategoryUpdater.LevelCategoryUpdated(selectLevelCategoryViewController.selectedLevelCategory, viewControllerActivated: true);
		}
	}

	private void SelectLevelCategoryViewController_didDeactivateEvent(bool removedFromHierarchy, bool screenSystemDisabling)
	{
		foreach (ILevelCategoryUpdater levelCategoryUpdater in levelCategoryUpdaters)
		{
			levelCategoryUpdater.LevelCategoryUpdated(SelectLevelCategoryViewController.LevelCategory.None, viewControllerActivated: false);
		}
	}

	private void PlaylistDownloader_QueueUpdatedEvent()
	{
		if (PlaylistSequentialDownloader.downloadQueue.Count == 0)
		{
			SongCore_RefreshLevelPacks.PacksToBeRefreshedEvent += OnPacksToBeRefreshed;
		}
	}

	private void OnPacksToBeRefreshed()
	{
		SongCore_RefreshLevelPacks.PacksToBeRefreshedEvent -= OnPacksToBeRefreshed;
		if (levelCollectionNavigationController.isActiveAndEnabled)
		{
			if (annotatedBeatmapLevelCollectionsViewController.isActiveAndEnabled)
			{
				downloadingBeatmapLevelCollections = null;
			    downloadingBeatmapCollectionIdx = annotatedBeatmapLevelCollectionsViewController.selectedItemIndex;
			}
			
			downloadingBeatmap = levelCollectionNavigationController.beatmapLevel;
			LevelFilteringNavigationController_UpdateSecondChildControllerContent.SecondChildControllerUpdatedEvent += LevelFilteringNavigationController_SecondChildControllerUpdatedEvent;
					}
				}

	private void LevelFilteringNavigationController_SecondChildControllerUpdatedEvent()
	{
		LevelFilteringNavigationController_UpdateSecondChildControllerContent.SecondChildControllerUpdatedEvent -= LevelFilteringNavigationController_SecondChildControllerUpdatedEvent;
		if (annotatedBeatmapLevelCollectionsViewController.isActiveAndEnabled)
		{
			this.LevelCollectionTableViewUpdatedEvent?.Invoke(downloadingBeatmapLevelCollections, downloadingBeatmapCollectionIdx);
		}
		if (levelCollectionNavigationController.isActiveAndEnabled && downloadingBeatmap != null)
		{
			levelCollectionNavigationController.SelectLevel(downloadingBeatmap);
		}
	}

	private void PlaylistManager_PlaylistsRefreshRequested(object sender, string requester)
	{
		Plugin.Log.Info("Playlist Refresh requested by: " + requester);
		refreshable.Refresh();
	}

	private void AssignAuthor()
	{
	    if (PluginConfig.Instance.AutomaticAuthorName)
	    {
	        if (string.IsNullOrEmpty(PluginConfig.Instance.AuthorName))
	        {
	            PluginConfig.Instance.AuthorName = "PlaylistManager";
	        }
	    }
	    else
	    {
	        PluginConfig.Instance.AuthorName = PluginConfig.Instance.AuthorName;
	    }
	}
}
