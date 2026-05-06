using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using IPA.Loader;
using IPA.Utilities.Async;
using PlaylistManager.Types;
using PlaylistManager.Utilities;
using SiraUtil.Extras;
using SiraUtil.Zenject;
using UnityEngine;

namespace PlaylistManager.UI;

public class ImageSelectionModalController : NotifiableBase
{
	private readonly LevelPackDetailViewController levelPackDetailViewController;

	private readonly PopupModalsController popupModalsController;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private readonly string IMAGES_PATH = Path.Combine(PlaylistLibUtils.playlistManager.PlaylistPath, "CoverImages");

	private readonly Sprite playlistManagerIcon;

	private readonly Dictionary<string, CoverImage> coverImages;

	private bool parsed;

	private int selectedIndex;

	[UIComponent("list")]
	public CustomListTableData customListTableData;

	[UIComponent("modal")]
	private readonly RectTransform modalTransform = null!;

	[UIComponent("modal")]
	private ModalView modalView = null!;

	private Vector3 modalPosition;

	[UIParams]
	private readonly BSMLParserParams parserParams;

	private bool _isLoading;

	[UIValue("is-loading")]
	private bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			_isLoading = value;
			NotifyPropertyChanged("IsLoading");
			NotifyPropertyChanged("IsNotLoading");
		}
	}

	[UIValue("is-not-loading")]
	private bool IsNotLoading => !IsLoading;

	public event Action<byte[]> ImageSelectedEvent;

	public ImageSelectionModalController(LevelPackDetailViewController levelPackDetailViewController, PopupModalsController popupModalsController, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.levelPackDetailViewController = levelPackDetailViewController;
		this.popupModalsController = popupModalsController;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
		try
		{
			Directory.CreateDirectory(IMAGES_PATH);
			File.Create(Path.Combine(IMAGES_PATH, ".plignore"));
		}
		catch (Exception ex)
		{
			Plugin.Log.Error("Could not make images path.\nExcepton:" + ex.Message);
		}
		coverImages = new Dictionary<string, CoverImage>();
		playlistManagerIcon = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.DefaultIcon.png");
		parsed = false;
	}

	private void Parse()
	{
		if (!parsed)
		{
			Transform detailWrapper = levelPackDetailViewController.GetPrivateField<Transform>("_detailWrapper");
			bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.ImageSelectionModal.bsml"), detailWrapper.gameObject, this);
			modalPosition = modalTransform.position;
		}
		modalTransform.position = modalPosition;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		parsed = true;
		modalView.SetPrivateField("_animateParentCanvas", false);
	}

	internal void ShowModal(IPlaylist playlist)
	{
		Parse();
		parserParams.EmitEvent("close-modal");
		parserParams.EmitEvent("open-modal");
		ShowImages(playlist);
	}

	private void LoadImages()
	{
		foreach (KeyValuePair<string, CoverImage> item in coverImages.Where((KeyValuePair<string, CoverImage> coverImage) => !File.Exists(coverImage.Key)).ToList())
		{
			coverImages.Remove(item.Key);
		}
		string[] ext = new string[2] { "jpg", "png" };
		foreach (string item2 in from s in Directory.EnumerateFiles(IMAGES_PATH, "*.*", SearchOption.AllDirectories)
			where ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant())
			select s)
		{
			if (!coverImages.ContainsKey(item2))
			{
				coverImages.Add(item2, new CoverImage(item2));
			}
		}
	}

	private async void ShowImages(IPlaylist playlist)
	{
		await UnityMainThreadTaskScheduler.Factory.StartNew(delegate
		{
			customListTableData.Data.Clear();
		});
		IsLoading = true;
		IList<CustomListTableData.CustomCellInfo> data = customListTableData.Data;
		data.Add(new CustomListTableData.CustomCellInfo("Clear Icon", "Clear", await PlaylistLibUtils.GeneratePlaylistIcon(playlist)));
		customListTableData.Data.Add(new CustomListTableData.CustomCellInfo("PlaylistManager Icon", "Default", playlistManagerIcon));
		LoadImages();
		foreach (KeyValuePair<string, CoverImage> coverImage in coverImages)
		{
			if (!coverImage.Value.SpriteWasLoaded && !coverImage.Value.Blacklist)
			{
				coverImage.Value.SpriteLoaded += CoverImage_SpriteLoaded;
				_ = coverImage.Value.Sprite;
			}
			else if (coverImage.Value.SpriteWasLoaded)
			{
				customListTableData.Data.Add(new CustomListTableData.CustomCellInfo(Path.GetFileName(coverImage.Key), coverImage.Key, coverImage.Value.Sprite));
			}
		}
		await UnityMainThreadTaskScheduler.Factory.StartNew(delegate
		{
			customListTableData.TableView.ReloadData();
		});
		customListTableData.TableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, animated: false);
		ViewControllerMonkeyCleanup();
	}

	private void CoverImage_SpriteLoaded(object sender, EventArgs e)
	{
		if (!(sender is CoverImage coverImage))
		{
			return;
		}
		if (coverImage.SpriteWasLoaded)
		{
			customListTableData.Data.Add(new CustomListTableData.CustomCellInfo(Path.GetFileName(coverImage.Path), coverImage.Path, coverImage.Sprite));
			customListTableData.TableView.ReloadDataKeepingPosition();
			if (customListTableData.Data.Count == 4)
			{
				// TableView no longer exposes AddCellToReusableCells in this version.
			}
			ViewControllerMonkeyCleanup();
		}
		coverImage.SpriteLoaded -= CoverImage_SpriteLoaded;
	}

	[UIAction("select-cell")]
	private void OnCellSelect(TableView tableView, int index)
	{
		customListTableData.TableView.ClearSelection();
		selectedIndex = index;
		popupModalsController.ShowYesNoModal(modalTransform, "Are you sure you want to change the image of the playlist? This cannot be reverted.", ChangeImage, "Yes", "No", null, animateParentCanvas: false);
	}

	private void ChangeImage()
	{
		if (selectedIndex == 0)
		{
			this.ImageSelectedEvent?.Invoke(null);
			parserParams.EmitEvent("close-modal");
			return;
		}
		if (selectedIndex == 1)
		{
			using (Stream stream = pluginMetadata.Assembly.GetManifestResourceStream("PlaylistManager.Icons.DefaultIcon.png"))
			{
				byte[] array = new byte[stream.Length];
				stream.Read(array, 0, (int)stream.Length);
				this.ImageSelectedEvent?.Invoke(array);
				parserParams.EmitEvent("close-modal");
				return;
			}
		}
		string subtext = customListTableData.Data[selectedIndex].Subtext;
		try
		{
			using FileStream fileStream = File.Open(subtext, FileMode.Open);
			byte[] array2 = new byte[fileStream.Length];
			fileStream.Read(array2, 0, (int)fileStream.Length);
			this.ImageSelectedEvent?.Invoke(array2);
			parserParams.EmitEvent("close-modal");
		}
		catch (Exception ex)
		{
			popupModalsController.ShowOkModal(modalTransform, "There was an error loading this image. Check logs for more details.", null, "Ok", animateParentCanvas: false);
			Plugin.Log.Critical("Could not load " + subtext + "\nException message: " + ex.Message);
		}
	}

	private async Task ViewControllerMonkeyCleanup()
	{
		await SiraUtil.Extras.Utilities.PauseChamp;
		ImageView[] componentsInChildren = customListTableData.TableView.GetComponentsInChildren<ImageView>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetPrivateField("_skew", 0f);
		}
		IsLoading = false;
	}
}
