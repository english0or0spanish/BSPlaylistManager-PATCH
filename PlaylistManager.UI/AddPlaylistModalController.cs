using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using IPA.Loader;
using PlaylistManager.Configuration;
using PlaylistManager.Utilities;
using SiraUtil.Zenject;
using UnityEngine;

namespace PlaylistManager.UI;

public class AddPlaylistModalController : INotifyPropertyChanged
{
	private readonly StandardLevelDetailViewController standardLevelDetailViewController;

	private readonly PopupModalsController popupModalsController;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private BeatSaberPlaylistsLib.PlaylistManager parentManager;

	private List<BeatSaberPlaylistsLib.PlaylistManager> childManagers;

	private List<IPlaylist> childPlaylists;

	private readonly Sprite folderIcon;

	private bool parsed;

	[UIComponent("list")]
	public CustomListTableData playlistTableData;

	[UIComponent("dropdown-options")]
	public CustomListTableData dropdownTableData;

	[UIComponent("highlight-checkbox")]
	private readonly RectTransform highlightCheckboxTransform = null!;

	[UIComponent("modal")]
	private readonly RectTransform modalTransform = null!;

	private Vector3 modalPosition;

	[UIComponent("create-dropdown")]
	private ModalView createModal = null!;

	[UIComponent("create-dropdown")]
	private readonly RectTransform createModalTransform = null!;

	private Vector3 createModalPosition;

	[UIParams]
	private readonly BSMLParserParams parserParams;

	[UIValue("folder-text")]
	private string FolderText
	{
		get
		{
			if (parentManager != null)
			{
				return Path.GetFileName(parentManager.PlaylistPath);
			}
			return "";
		}
	}

	[UIValue("highlight-difficulty")]
	private bool HighlightDifficulty
	{
		get
		{
			return PluginConfig.Instance.HighlightDifficulty;
		}
		set
		{
			PluginConfig.Instance.HighlightDifficulty = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HighlightDifficulty"));
		}
	}

	[UIValue("back-active")]
	private bool BackActive
	{
		get
		{
			if (parentManager != null)
			{
				return parentManager.Parent != null;
			}
			return false;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public AddPlaylistModalController(StandardLevelDetailViewController standardLevelDetailViewController, PopupModalsController popupModalsController, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.standardLevelDetailViewController = standardLevelDetailViewController;
		this.popupModalsController = popupModalsController;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
		folderIcon = SpriteUtils.LoadSpriteFromAssembly(pluginMetadata.Value.Assembly, "PlaylistManager.Icons.FolderIcon.png");
		parsed = false;
	}

	private void Parse()
	{
		if (!parsed)
		{
			StandardLevelDetailView standardLevelDetailView = standardLevelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
			bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.AddPlaylistModal.bsml"), standardLevelDetailView.gameObject, this);
			modalPosition = modalTransform.localPosition;
			createModalPosition = createModalTransform.localPosition;
		}
		modalTransform.localPosition = modalPosition;
		createModalTransform.localPosition = createModalPosition;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		parsed = true;
		highlightCheckboxTransform.transform.localScale *= 0.5f;
		createModal.SetPrivateField("_animateParentCanvas", false);
		dropdownTableData.Data.Add(new CustomListTableData.CustomCellInfo("Playlist"));
		dropdownTableData.Data.Add(new CustomListTableData.CustomCellInfo("Folder"));
		dropdownTableData.TableView.ReloadData();
	}

	internal void ShowModal()
	{
		Parse();
		parserParams.EmitEvent("close-modal");
		parserParams.EmitEvent("open-modal");
		ShowPlaylistsForManager(PlaylistLibUtils.playlistManager);
	}

	internal void ShowPlaylistsForManager(BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		playlistTableData.Data.Clear();
		this.parentManager = parentManager;
		childManagers = parentManager.GetChildManagers().ToList();
		IEnumerable<IPlaylist> enumerable = from playlist in parentManager.GetAllPlaylists(includeChildren: false)
			where !playlist.ReadOnly
			select playlist;
		childPlaylists = enumerable.ToList();
		foreach (BeatSaberPlaylistsLib.PlaylistManager childManager in childManagers)
		{
			playlistTableData.Data.Add(new CustomListTableData.CustomCellInfo(Path.GetFileName(childManager.PlaylistPath), "Folder", folderIcon));
		}
		foreach (IPlaylist item in enumerable)
		{
			if (!item.SmallSpriteWasLoaded)
			{
				item.SpriteLoaded -= StagedSpriteLoadPlaylist_SpriteLoaded;
				item.SpriteLoaded += StagedSpriteLoadPlaylist_SpriteLoaded;
				_ = item.SmallSprite;
			}
			else
			{
				ShowPlaylist(item);
			}
		}
		playlistTableData.TableView.ReloadData();
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BackActive"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FolderText"));
	}

	private void StagedSpriteLoadPlaylist_SpriteLoaded(object sender, EventArgs e)
	{
		if (sender is IStagedSpriteLoad stagedSpriteLoad)
		{
			if (parentManager.GetAllPlaylists(includeChildren: false).Contains<IPlaylist>((IPlaylist)stagedSpriteLoad))
			{
				ShowPlaylist((IPlaylist)stagedSpriteLoad);
			}
			playlistTableData.TableView.ReloadDataKeepingPosition();
			stagedSpriteLoad.SpriteLoaded -= StagedSpriteLoadPlaylist_SpriteLoaded;
		}
	}

	private void ShowPlaylist(IPlaylist playlist)
	{
		string text = $"{playlist.BeatmapLevels.Length} songs";
		if (playlist.BeatmapLevels.Any((BeatmapLevel level) => level.levelID == standardLevelDetailViewController.beatmapKey.levelId))
		{
			if (!playlist.AllowDuplicates)
			{
				childPlaylists.Remove(playlist);
				return;
			}
			text += " (contains song)";
		}
		playlistTableData.Data.Add(new CustomListTableData.CustomCellInfo(playlist.Title, text, playlist.SmallSprite));
	}

	[UIAction("select-cell")]
	private void OnCellSelect(TableView tableView, int index)
	{
		playlistTableData.TableView.ClearSelection();
		if (index < childManagers.Count)
		{
			ShowPlaylistsForManager(childManagers[index]);
			return;
		}
		index -= childManagers.Count;
		IPlaylist playlist = childPlaylists[index];
		IPlaylistSong playlistSong = ((!HighlightDifficulty) ? playlist.Add(standardLevelDetailViewController.beatmapLevel) : playlist.Add(standardLevelDetailViewController.beatmapLevel, standardLevelDetailViewController.beatmapKey));
		try
		{
			playlist.RaisePlaylistChanged();
			parentManager.StorePlaylist(playlist);
			popupModalsController.ShowOkModal(modalTransform, $"Song successfully added to {playlist.Title}", null, "Ok", animateParentCanvas: false);
			Events.RaisePlaylistSongAdded(playlistSong, playlist);
		}
		catch (Exception ex)
		{
			popupModalsController.ShowOkModal(modalTransform, "An error occured while adding song to playlist.", null, "Ok", animateParentCanvas: false);
			Plugin.Log.Critical($"An exception was thrown while adding a song to a playlist.\nException Message: {ex.Message}");
		}
		finally
		{
			ShowPlaylistsForManager(parentManager);
		}
	}

	[UIAction("back-button-pressed")]
	private void BackButtonPressed()
	{
		ShowPlaylistsForManager(parentManager.Parent);
	}

	[UIAction("select-option")]
	private void OnOptionSelect(TableView tableView, int index)
	{
		popupModalsController.ShowKeyboard(modalTransform, (index == 0) ? new Action<string>(CreatePlaylist) : new Action<string>(CreateFolder), "", animateParentCanvas: false);
		tableView.ClearSelection();
		parserParams.EmitEvent("close-dropdown");
	}

	private void CreatePlaylist(string playlistName)
	{
		if (!string.IsNullOrWhiteSpace(playlistName))
		{
			IPlaylist playlist = PlaylistLibUtils.CreatePlaylistWithConfig(playlistName, parentManager);
			IDeferredSpriteLoad deferredSpriteLoad = playlist;
			if (deferredSpriteLoad != null && !deferredSpriteLoad.SpriteWasLoaded)
			{
				deferredSpriteLoad.SpriteLoaded -= StagedSpriteLoadPlaylist_SpriteLoaded;
				deferredSpriteLoad.SpriteLoaded += StagedSpriteLoadPlaylist_SpriteLoaded;
				_ = playlist.Sprite;
			}
			childPlaylists.Add(playlist);
			playlistTableData.TableView.ReloadDataKeepingPosition();
		}
	}

	private void CreateFolder(string folderName)
	{
		folderName = folderName.Replace("/", "").Replace("\\", "").Replace(".", "");
		if (!string.IsNullOrEmpty(folderName))
		{
			BeatSaberPlaylistsLib.PlaylistManager playlistManager = parentManager.CreateChildManager(folderName);
			if (childManagers.Contains(playlistManager))
			{
				popupModalsController.ShowOkModal(modalTransform, "\"" + folderName + "\" already exists! Please use a different name.", null, "Ok", animateParentCanvas: false);
				return;
			}
			playlistTableData.Data.Insert(childManagers.Count, new CustomListTableData.CustomCellInfo(Path.GetFileName(playlistManager.PlaylistPath), "Folder", folderIcon));
			playlistTableData.TableView.ReloadDataKeepingPosition();
			childManagers.Add(playlistManager);
			PlaylistLibUtils.playlistManager.RequestRefresh("PlaylistManager (plugin)");
		}
	}
}
