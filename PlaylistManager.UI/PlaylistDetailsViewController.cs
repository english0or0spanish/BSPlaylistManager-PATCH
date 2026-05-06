using System;
using System.ComponentModel;
using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using IPA.Loader;
using PlaylistManager.Interfaces;
using PlaylistManager.Utilities;
using SiraUtil.Zenject;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

public class PlaylistDetailsViewController : IInitializable, IDisposable, ILevelCollectionUpdater, INotifyPropertyChanged
{
	private readonly LevelPackDetailViewController levelPackDetailViewController;

	private readonly ImageSelectionModalController imageSelectionModalController;

	private readonly PopupModalsController popupModalsController;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private bool parsed;

	private IPlaylist selectedPlaylist;

	private BeatSaberPlaylistsLib.PlaylistManager parentManager;

	[UIComponent("modal")]
	private readonly RectTransform modalTransform = null!;

	private Vector3 modalPosition;

	[UIComponent("name-setting")]
	private RectTransform nameSettingTransform = null!;

	[UIComponent("author-setting")]
	private RectTransform authorSettingTransform = null!;

	[UIComponent("playlist-cover")]
	private readonly ClickableImage playlistCoverView = null!;

	[UIComponent("text-page")]
	private TextPageScrollView descriptionTextPage = null!;

	[UIParams]
	private readonly BSMLParserParams parserParams;

	[UIValue("playlist-name")]
	private string PlaylistName
	{
		get
		{
			if (selectedPlaylist != null)
			{
				return selectedPlaylist.Title;
			}
			return " ";
		}
		set
		{
			selectedPlaylist.Title = value;
			if (!selectedPlaylist.HasCover)
			{
				selectedPlaylist.SpriteLoaded += SelectedPlaylist_SpriteLoaded;
				selectedPlaylist.RaiseCoverImageChangedForDefaultCover();
			}
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
			Events.RaisePlaylistRenamed(selectedPlaylist, parentManager);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistName"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NameHint"));
		}
	}

	[UIValue("name-hint")]
	private string NameHint
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(PlaylistName))
			{
				return PlaylistName;
			}
			return " ";
		}
	}

	[UIValue("playlist-author")]
	private string PlaylistAuthor
	{
		get
		{
			if (selectedPlaylist != null && selectedPlaylist.Author != null)
			{
				return selectedPlaylist.Author;
			}
			return " ";
		}
		set
		{
			selectedPlaylist.Author = value;
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistAuthor"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AuthorHint"));
		}
	}

	[UIValue("author-hint")]
	private string AuthorHint
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(PlaylistAuthor))
			{
				return PlaylistAuthor;
			}
			return " ";
		}
	}

	[UIValue("playlist-description")]
	private string PlaylistDescription
	{
		get
		{
			if (selectedPlaylist != null && selectedPlaylist.Description != null)
			{
				return selectedPlaylist.Description;
			}
			return "";
		}
	}

	[UIValue("playlist-read-only")]
	private bool PlaylistReadOnly
	{
		get
		{
			if (selectedPlaylist != null)
			{
				return selectedPlaylist.ReadOnly;
			}
			return false;
		}
		set
		{
			selectedPlaylist.ReadOnly = value;
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
			UpdateReadOnly();
		}
	}

	[UIValue("read-only-visible")]
	private bool ReadOnlyVisible => PlaylistReadOnly;

	[UIValue("editable")]
	private bool Editable => !PlaylistReadOnly;

	[UIValue("playlist-allow-duplicates")]
	private bool PlaylistAllowDuplicates
	{
		get
		{
			if (selectedPlaylist != null)
			{
				return selectedPlaylist.AllowDuplicates;
			}
			return false;
		}
		set
		{
			selectedPlaylist.AllowDuplicates = value;
			if (!value)
			{
				if (selectedPlaylist is BlistPlaylist blistPlaylist)
				{
					blistPlaylist.RemoveDuplicates();
				}
				else if (selectedPlaylist is LegacyPlaylist legacyPlaylist)
				{
					legacyPlaylist.RemoveDuplicates();
				}
			}
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistAllowDuplicates"));
		}
	}

	[UIValue("cover-hint")]
	private string CoverHint
	{
		get
		{
			if (!PlaylistReadOnly)
			{
				return "Set Cover";
			}
			return "Cover Image";
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public PlaylistDetailsViewController(LevelPackDetailViewController levelPackDetailViewController, ImageSelectionModalController imageSelectionModalController, PopupModalsController popupModalsController, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.levelPackDetailViewController = levelPackDetailViewController;
		this.imageSelectionModalController = imageSelectionModalController;
		this.popupModalsController = popupModalsController;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
		parsed = false;
	}

	public void Initialize()
	{
		imageSelectionModalController.ImageSelectedEvent += ImageSelectionModalController_ImageSelectedEvent;
	}

	public void Dispose()
	{
		imageSelectionModalController.ImageSelectedEvent -= ImageSelectionModalController_ImageSelectedEvent;
		if (selectedPlaylist != null)
		{
			selectedPlaylist.SpriteLoaded -= SelectedPlaylist_SpriteLoaded;
		}
	}

	private void Parse()
	{
		if (!parsed)
		{
			Transform detailWrapper = levelPackDetailViewController.GetPrivateField<Transform>("_detailWrapper");
			bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.PlaylistDetailsView.bsml"), detailWrapper.gameObject, this);
			modalPosition = modalTransform.position;
		}
		modalTransform.position = modalPosition;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		parsed = true;
		ModalView nameModal = nameSettingTransform.Find("BSMLModalKeyboard").GetComponent<ModalView>();
		nameModal.SetPrivateField("_animateParentCanvas", false);
		ModalView authorModal = authorSettingTransform.Find("BSMLModalKeyboard").GetComponent<ModalView>();
		authorModal.SetPrivateField("_animateParentCanvas", false);
	}

	internal void ShowDetails()
	{
		Parse();
		parserParams.EmitEvent("close-modal");
		parserParams.EmitEvent("open-modal");
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistName"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NameHint"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistAuthor"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AuthorHint"));
		UpdateReadOnly();
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistAllowDuplicates"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistDescription"));
		playlistCoverView.sprite = selectedPlaylist.Sprite;
		descriptionTextPage.ScrollTo(0f, animated: true);
	}

	[UIAction("string-formatter")]
	private string StringFormatter(string inputString)
	{
		if (inputString.Length > 15)
		{
			return inputString.Substring(0, 15) + "...";
		}
		return inputString;
	}

	[UIAction("read-only-toggled")]
	private void ReadOnlyToggled(bool playlistReadOnly)
	{
		if (playlistReadOnly)
		{
			PlaylistReadOnly = true;
		}
		else if (PlaylistReadOnly != playlistReadOnly)
		{
			popupModalsController.ShowYesNoModal(modalTransform, "To turn off read only, this playlist will be cloned and writing will be enabled on the clone. Proceed?", ClonePlaylist, "Yes", "No", UpdateReadOnly, animateParentCanvas: false);
		}
	}

	private void ClonePlaylist()
	{
		string path = Path.Combine(parentManager.PlaylistPath, selectedPlaylist.Filename + "." + selectedPlaylist.SuggestedExtension);
		if (File.Exists(path))
		{
			IPlaylist playlist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler?.Deserialize(File.OpenRead(path));
			playlist.ReadOnly = false;
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(playlist);
			PlaylistLibUtils.playlistManager.RequestRefresh("PlaylistManager (plugin)");
			popupModalsController.ShowOkModal(modalTransform, "Playlist Cloned!", null, "Ok", animateParentCanvas: false);
		}
		else
		{
			popupModalsController.ShowOkModal(modalTransform, "An error occured while trying to clone the playlist. Please try again later.", null, "Ok", animateParentCanvas: false);
		}
		UpdateReadOnly();
	}

	private void UpdateReadOnly()
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistReadOnly"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ReadOnlyVisible"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Editable"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CoverHint"));
	}

	[UIAction("duplicates-toggled")]
	private void DuplicatesToggled(bool playlistAllowDuplicates)
	{
		if (playlistAllowDuplicates)
		{
			PlaylistAllowDuplicates = true;
		}
		else if (PlaylistAllowDuplicates != playlistAllowDuplicates)
		{
			popupModalsController.ShowYesNoModal(modalTransform, "Are you sure you want to turn off duplicates for this playlist? This will also delete all duplicate songs from this playlist.", DeleteDuplicates, "Yes", "No", DontDeleteDuplicates, animateParentCanvas: false);
		}
	}

	private void DeleteDuplicates()
	{
		PlaylistAllowDuplicates = false;
	}

	private void DontDeleteDuplicates()
	{
		PlaylistAllowDuplicates = true;
	}

	[UIAction("playlist-cover-clicked")]
	private void OpenImageSelectionModal()
	{
		if (!PlaylistReadOnly)
		{
			imageSelectionModalController.ShowModal(selectedPlaylist);
		}
	}

	private void ImageSelectionModalController_ImageSelectedEvent(byte[] imageBytes)
	{
		selectedPlaylist.SpriteLoaded += SelectedPlaylist_SpriteLoaded;
		try
		{
			selectedPlaylist.SetCover(imageBytes);
			_ = selectedPlaylist.Sprite;
			selectedPlaylist.RaisePlaylistChanged();
			parentManager.StorePlaylist(selectedPlaylist);
		}
		catch (Exception ex)
		{
			popupModalsController.ShowOkModal(modalTransform, "There was an error loading this image. Check logs for more details.", null, "Ok", animateParentCanvas: false);
			Plugin.Log.Critical(ex.Message);
		}
	}

	private void SelectedPlaylist_SpriteLoaded(object sender, EventArgs e)
	{
		playlistCoverView.sprite = selectedPlaylist.Sprite;
		selectedPlaylist.SpriteLoaded -= SelectedPlaylist_SpriteLoaded;
	}

	public void LevelCollectionUpdated(BeatmapLevelPack annotatedBeatmapLevelCollection, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		if (selectedPlaylist != null)
		{
			selectedPlaylist.SpriteLoaded -= SelectedPlaylist_SpriteLoaded;
		}
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
