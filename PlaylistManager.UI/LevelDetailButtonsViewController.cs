using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using IPA.Loader;
using PlaylistManager.Configuration;
using PlaylistManager.Interfaces;
using PlaylistManager.Utilities;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

public class LevelDetailButtonsViewController : IInitializable, IDisposable, IBeatmapLevelUpdater, ILevelCollectionUpdater, INotifyPropertyChanged
{
	private StandardLevelDetailViewController standardLevelDetailViewController;

	private LevelCollectionTableView levelCollectionTableView;

	private readonly LevelCollectionNavigationController levelCollectionNavigationController;

	private readonly AddPlaylistModalController addPlaylistController;

	private readonly PopupModalsController popupModalsController;

	private readonly DifficultyHighlighter difficultyHighlighter;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private BeatmapLevel selectedBeatmapLevel;

	private IPlaylist selectedPlaylist;

	private BeatSaberPlaylistsLib.PlaylistManager parentManager;

	private bool _addActive;

	private bool _isPlaylistSong;

	private bool selectedDifficultyHighlighted;

	[UIComponent("root")]
	private RectTransform rootTransform = null!;

	[UIValue("add-active")]
	private bool AddActive
	{
		get
		{
			return _addActive;
		}
		set
		{
			_addActive = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AddActive"));
		}
	}

	[UIValue("highlight-button-text")]
	private string HighlightButtonText
	{
		get
		{
			if (!selectedDifficultyHighlighted)
			{
				return "⬜";
			}
			return "⬛";
		}
	}

	[UIValue("highlight-button-hover")]
	private string HighlightButtonHover
	{
		get
		{
			if (!selectedDifficultyHighlighted)
			{
				return "Highlight selected difficulty";
			}
			return "Unhighlight selected difficulty";
		}
	}

	[UIValue("playlist-song")]
	private bool IsPlaylistSong
	{
		get
		{
			return _isPlaylistSong;
		}
		set
		{
			_isPlaylistSong = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsPlaylistSong"));
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public LevelDetailButtonsViewController(StandardLevelDetailViewController standardLevelDetailViewController, LevelCollectionViewController levelCollectionViewController, LevelCollectionNavigationController levelCollectionNavigationController, AddPlaylistModalController addPlaylistController, PopupModalsController popupModalsController, DifficultyHighlighter difficultyHighlighter, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.standardLevelDetailViewController = standardLevelDetailViewController;
		levelCollectionTableView = levelCollectionViewController.GetPrivateField<LevelCollectionTableView>("_levelCollectionTableView");
		this.levelCollectionNavigationController = levelCollectionNavigationController;
		this.addPlaylistController = addPlaylistController;
		this.popupModalsController = popupModalsController;
		this.difficultyHighlighter = difficultyHighlighter;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
	}

	public void Initialize()
	{
		StandardLevelDetailView standardLevelDetailView = standardLevelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
		bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.LevelDetailButtonsView.bsml"), standardLevelDetailView.gameObject, this);
		rootTransform.transform.localScale *= 0.7f;
		AddActive = false;
		difficultyHighlighter.selectedDifficultyChanged += DifficultyHighlighter_selectedDifficultyChanged;
	}

	public void Dispose()
	{
		difficultyHighlighter.selectedDifficultyChanged -= DifficultyHighlighter_selectedDifficultyChanged;
	}

	[UIAction("add-button-click")]
	private void OpenAddModal()
	{
		addPlaylistController.ShowModal();
	}

	[UIAction("remove-button-click")]
	private void DisplayRemoveWarning()
	{
		if (selectedBeatmapLevel is PlaylistLevel)
		{
			popupModalsController.ShowYesNoModal(standardLevelDetailViewController.transform, $"Are you sure you would like to remove {selectedBeatmapLevel.songName} from the playlist?", RemoveSong);
		}
		else
		{
			popupModalsController.ShowOkModal(standardLevelDetailViewController.transform, "Error: The selected song is not part of a playlist.", null);
		}
	}

	private void RemoveSong()
	{
		PlaylistLevel playlistLevel = (PlaylistLevel)selectedBeatmapLevel;
		selectedPlaylist.Remove(playlistLevel.playlistSong);
		try
		{
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
			Events.RaisePlaylistSongRemoved(playlistLevel.playlistSong, selectedPlaylist);
		}
		catch (Exception ex)
		{
			popupModalsController.ShowOkModal(standardLevelDetailViewController.transform, "An error occured while removing a song from the playlist.", null);
			Plugin.Log.Critical($"An exception was thrown while adding a song to a playlist.\nException Message: {ex.Message}");
		}
		levelCollectionTableView.ClearSelection();
		if ((PluginConfig.Instance.AuthorName.IndexOf("GOOBIE", StringComparison.OrdinalIgnoreCase) >= 0 || PluginConfig.Instance.AuthorName.IndexOf("ERIS", StringComparison.OrdinalIgnoreCase) >= 0 || PluginConfig.Instance.AuthorName.IndexOf("PINK", StringComparison.OrdinalIgnoreCase) >= 0 || PluginConfig.Instance.AuthorName.IndexOf("CANDL3", StringComparison.OrdinalIgnoreCase) >= 0) && PluginConfig.Instance.EasterEggs)
		{
			levelCollectionNavigationController.InvokeMethod("SetDataForPack", selectedPlaylist.PlaylistLevelPack, true, true, PluginConfig.Instance.AuthorName + " Cute", false);
		}
		else if (PluginConfig.Instance.AuthorName.IndexOf("JOSHABI", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			levelCollectionNavigationController.InvokeMethod("SetDataForPack", selectedPlaylist.PlaylistLevelPack, true, true, "*Sneeze*", false);
		}
		else
		{
			levelCollectionNavigationController.InvokeMethod("SetDataForPack", selectedPlaylist.PlaylistLevelPack, true, true, "Play", false);
		}
		levelCollectionNavigationController.InvokeMethod("HideDetailViewController");
	}

	[UIAction("highlight-button-click")]
	private void HighlightButtonClick()
	{
		difficultyHighlighter.ToggleSelectedDifficultyHighlight();
		selectedPlaylist.RaisePlaylistChanged();
		parentManager.StorePlaylist(selectedPlaylist);
		selectedDifficultyHighlighted = !selectedDifficultyHighlighted;
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HighlightButtonText"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HighlightButtonHover"));
	}

	private void DifficultyHighlighter_selectedDifficultyChanged(bool selectedDifficultyHighlighted)
	{
		this.selectedDifficultyHighlighted = selectedDifficultyHighlighted;
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HighlightButtonText"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HighlightButtonHover"));
	}

	public void BeatmapLevelUpdated(BeatmapLevel beatmapLevel)
	{
		selectedBeatmapLevel = beatmapLevel;
		if (beatmapLevel.levelID.EndsWith(" WIP", StringComparison.Ordinal))
		{
			AddActive = false;
			IsPlaylistSong = false;
			return;
		}
		if (beatmapLevel is PlaylistLevel)
		{
			IPlaylist playlist = selectedPlaylist;
			if (playlist != null && !playlist.ReadOnly)
			{
				AddActive = true;
				IsPlaylistSong = true;
				return;
			}
		}
		AddActive = true;
		IsPlaylistSong = false;
	}

	public void LevelCollectionUpdated(BeatmapLevelPack annotatedBeatmapLevelCollection, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		if (annotatedBeatmapLevelCollection is PlaylistLevelPack playlistLevelPack)
		{
			selectedPlaylist = playlistLevelPack.playlist;
			this.parentManager = parentManager;
		}
		else
		{
			selectedPlaylist = null;
			this.parentManager = null;
		}
	}
}
