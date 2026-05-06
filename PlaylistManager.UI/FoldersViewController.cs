using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using IPA.Loader;
using PlaylistManager.HarmonyPatches;
using PlaylistManager.Interfaces;
using PlaylistManager.Types;
using PlaylistManager.Utilities;
using SiraUtil.Zenject;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace PlaylistManager.UI;

public class FoldersViewController : IInitializable, IDisposable, INotifyPropertyChanged, ILevelCollectionsTableUpdater, ILevelCategoryUpdater, IPMRefreshable, TableView.IDataSource
{
	private readonly AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController;

	private readonly MainFlowCoordinator mainFlowCoordinator;

	private readonly LevelSelectionNavigationController levelSelectionNavigationController;

	private readonly PopupModalsController popupModalsController;

	private readonly HoverHintController hoverHintController;

	private readonly BeatmapLevelsModel beatmapLevelsModel;

	private readonly PlaylistUpdater playlistUpdater;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private FloatingScreen floatingScreen;

	private readonly Sprite levelPacksSprite;

	private readonly Sprite customPacksSprite;

	private readonly Sprite playlistsSprite;

	private readonly Sprite foldersSprite;

	private readonly Sprite folderIcon;

	private readonly List<CustomListTableData.CustomCellInfo> tableCells;

	private BeatSaberPlaylistsLib.PlaylistManager _currentParentManager;

	private List<BeatSaberPlaylistsLib.PlaylistManager> currentManagers;

	private FolderMode folderMode;

	[UIComponent("root")]
	private RectTransform rootTransform = null!;

	[UIComponent("back-rect")]
	private RectTransform backTransform = null!;

	[UIComponent("rename-button")]
	private Button renameButton = null!;

	[UIComponent("delete-button")]
	private Button deleteButton = null!;

	[UIComponent("folder-list")]
	public CustomListTableData customListTableData;

	private const string kReuseIdentifier = "PlaylistFolderCell";

	public BeatSaberPlaylistsLib.PlaylistManager CurrentParentManager
	{
		get
		{
			return _currentParentManager;
		}
		private set
		{
			_currentParentManager = value;
			this.ParentManagerUpdatedEvent?.Invoke(value);
		}
	}

	[UIValue("folder-text")]
	private string FolderText
	{
		get
		{
			if (CurrentParentManager == null || !Directory.Exists(CurrentParentManager.PlaylistPath))
			{
				return "";
			}
			string fileName = Path.GetFileName(CurrentParentManager.PlaylistPath);
			if (fileName.Length > 15)
			{
				return fileName.Substring(0, 15) + "...";
			}
			return fileName;
		}
	}

	[UIValue("left-button-enabled")]
	private bool LeftButtonEnabled
	{
		get
		{
			if (customListTableData != null)
			{
				return tableCells.Count > 4;
			}
			return false;
		}
	}

	[UIValue("right-button-enabled")]
	private bool RightButtonEnabled
	{
		get
		{
			if (customListTableData != null)
			{
				return tableCells.Count > 4;
			}
			return false;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public event Action<IReadOnlyList<BeatmapLevelPack>, int> LevelCollectionTableViewUpdatedEvent;

	public event Action<BeatSaberPlaylistsLib.PlaylistManager> ParentManagerUpdatedEvent;

	private FoldersViewController(AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, MainFlowCoordinator mainFlowCoordinator, LevelSelectionNavigationController levelSelectionNavigationController, PopupModalsController popupModalsController, HoverHintController hoverHintController, BeatmapLevelsModel beatmapLevelsModel, PlaylistUpdater playlistUpdater, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
		this.mainFlowCoordinator = mainFlowCoordinator;
		this.levelSelectionNavigationController = levelSelectionNavigationController;
		this.popupModalsController = popupModalsController;
		this.hoverHintController = hoverHintController;
		this.beatmapLevelsModel = beatmapLevelsModel;
		this.playlistUpdater = playlistUpdater;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
		levelPacksSprite = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.LevelPacks.png");
		customPacksSprite = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.CustomPacks.png");
		playlistsSprite = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.Playlists.png");
		foldersSprite = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.Folders.png");
		folderIcon = SpriteUtils.LoadSpriteFromAssembly(this.pluginMetadata.Assembly, "PlaylistManager.Icons.FolderIcon.png");
		tableCells = new List<CustomListTableData.CustomCellInfo>();
		folderMode = FolderMode.None;
	}

	public void Initialize()
	{
		floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(75f, 25f), createHandle: false, new Vector3(0f, 0.2f, 2.5f), new Quaternion(0f, 0f, 0f, 0f));
		Transform transform = floatingScreen.transform;
		transform.eulerAngles = new Vector3(60f, 0f, 0f);
		transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
		bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.FoldersView.bsml"), floatingScreen.gameObject, this);
		LevelFilteringNavigationController_ShowPacksInChildController.AllPacksViewSelectedEvent += LevelFilteringNavigationController_ShowPacksInChildController_AllPacksViewSelectedEvent;
	}

	public void Dispose()
	{
		LevelFilteringNavigationController_ShowPacksInChildController.AllPacksViewSelectedEvent -= LevelFilteringNavigationController_ShowPacksInChildController_AllPacksViewSelectedEvent;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		GameObject gameObject = rootTransform.gameObject;
		gameObject.SetActive(value: false);
		gameObject.name = "PlaylistManagerFoldersView";
		customListTableData.TableView.SetDataSource(this, reloadData: false);
	}

	public void SetupDimensions()
	{
		if (!(mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is MultiplayerLevelSelectionFlowCoordinator))
		{
			Transform transform = floatingScreen.transform;
			transform.position = new Vector3(0f, 0.1f, 2.5f);
			transform.eulerAngles = new Vector3(75f, 0f, 0f);
			transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
		}
		else
		{
			Vector3 position = levelSelectionNavigationController.transform.position;
			position.y += 0.73f;
			Transform transform2 = floatingScreen.transform;
			transform2.eulerAngles = new Vector3(0f, 0f, 0f);
			transform2.localScale = new Vector3(0.015f, 0.015f, 0.015f);
			transform2.position = position;
		}
	}

	private void SetupList(BeatSaberPlaylistsLib.PlaylistManager currentParentManager, bool setBeatmapLevelCollections = true)
	{
		customListTableData.TableView.ClearSelection();
		tableCells.Clear();
		CurrentParentManager = currentParentManager;
		if (currentParentManager == null)
		{
			CustomListTableData.CustomCellInfo item = new CustomListTableData.CustomCellInfo("", "Level Packs", levelPacksSprite);
			tableCells.Add(item);
			item = new CustomListTableData.CustomCellInfo("", "Custom Songs", customPacksSprite);
			tableCells.Add(item);
			item = new CustomListTableData.CustomCellInfo("", "Playlists", playlistsSprite);
			tableCells.Add(item);
			item = new CustomListTableData.CustomCellInfo("", "Folders", foldersSprite);
			tableCells.Add(item);
			backTransform.gameObject.SetActive(value: false);
		}
		else
		{
			currentManagers = currentParentManager.GetChildManagers().ToList();
			foreach (BeatSaberPlaylistsLib.PlaylistManager currentManager in currentManagers)
			{
				CustomListTableData.CustomCellInfo item2 = new CustomListTableData.CustomCellInfo(Path.GetFileName(currentManager.PlaylistPath), null, folderIcon);
				tableCells.Add(item2);
			}
			backTransform.gameObject.SetActive(value: true);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FolderText"));
			if (currentParentManager.Parent == null)
			{
				renameButton.interactable = false;
				deleteButton.interactable = false;
			}
			else
			{
				renameButton.interactable = true;
				deleteButton.interactable = true;
			}
			if (setBeatmapLevelCollections)
			{
				IReadOnlyList<BeatmapLevelPack> arg = (from p in currentParentManager.GetAllPlaylists(includeChildren: false)
					select p.PlaylistLevelPack).ToArray();
				this.LevelCollectionTableViewUpdatedEvent?.Invoke(arg, 0);
			}
		}
		customListTableData.TableView.ReloadData();
		customListTableData.TableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, animated: false);
		if (currentParentManager == null)
		{
			customListTableData.TableView.SelectCellWithIdx(0);
			TableCell[] array = customListTableData.TableView.visibleCells.ToArray();
			for (int num = 0; num < array.Length; num++)
			{
				HoverHint hoverHint = ((UnityEngine.Component)(object)array[num]).GetComponent<HoverHint>();
				if (hoverHint == null)
				{
					hoverHint = ((UnityEngine.Component)(object)array[num]).gameObject.AddComponent<HoverHint>();
					Accessors.HoverHintControllerAccessor.Invoke(ref hoverHint) = hoverHintController;
				}
				else
				{
					hoverHint.enabled = true;
				}
				hoverHint.text = tableCells[num].Subtext;
			}
			if (setBeatmapLevelCollections)
			{
				Select(customListTableData.TableView, 0);
			}
		}
		else
		{
			TableCell[] array2 = customListTableData.TableView.visibleCells.ToArray();
			for (int num2 = 0; num2 < array2.Length; num2++)
			{
				HoverHint component = ((UnityEngine.Component)(object)array2[num2]).GetComponent<HoverHint>();
				if (component != null)
				{
					component.enabled = false;
				}
			}
		}
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LeftButtonEnabled"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("RightButtonEnabled"));
	}

	[UIAction("folder-select")]
	private void Select(TableView _, int selectedCellIndex)
	{
		if (CurrentParentManager == null)
		{
			switch (selectedCellIndex)
			{
			case 0:
			{
				object customLevelsRepository = beatmapLevelsModel.GetPrivateField<object>("_customLevelsRepository");
					IReadOnlyList<BeatmapLevelPack> arg2 = customLevelsRepository.GetMemberValue<IReadOnlyList<BeatmapLevelPack>>("beatmapLevelPacks").Concat<BeatmapLevelPack>(PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks()).ToArray();
				this.LevelCollectionTableViewUpdatedEvent?.Invoke(arg2, 0);
				folderMode = FolderMode.AllPacks;
				break;
			}
			case 1:
			{
				object customLevelsRepository = beatmapLevelsModel.GetPrivateField<object>("_customLevelsRepository");
					IReadOnlyList<BeatmapLevelPack> beatmapLevelPacks = customLevelsRepository.GetMemberValue<IReadOnlyList<BeatmapLevelPack>>("beatmapLevelPacks");
				this.LevelCollectionTableViewUpdatedEvent?.Invoke(beatmapLevelPacks, 0);
				folderMode = FolderMode.CustomPacks;
				break;
			}
			case 2:
			{
				IReadOnlyList<BeatmapLevelPack> arg = PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks();
				this.LevelCollectionTableViewUpdatedEvent?.Invoke(arg, 0);
				folderMode = FolderMode.Playlists;
				break;
			}
			case 3:
				SetupList(PlaylistLibUtils.playlistManager);
				folderMode = FolderMode.Folders;
				break;
			}
		}
		else
		{
			SetupList(currentManagers[selectedCellIndex]);
		}
	}

	[UIAction("back-button-click")]
	private void BackButtonClicked()
	{
		if (CurrentParentManager != null)
		{
			SetupList(CurrentParentManager.Parent);
		}
	}

	[UIAction("create-folder")]
	private void CreateFolder()
	{
		popupModalsController.ShowKeyboard(levelSelectionNavigationController.transform, CreateKeyboardEnter);
	}

	private void CreateKeyboardEnter(string folderName)
	{
		if (CurrentParentManager == null)
		{
			return;
		}
		folderName = folderName.Replace("/", "").Replace("\\", "").Replace(".", "");
		if (!string.IsNullOrEmpty(folderName))
		{
			BeatSaberPlaylistsLib.PlaylistManager item = CurrentParentManager.CreateChildManager(folderName);
			if (currentManagers.Contains(item))
			{
				popupModalsController.ShowOkModal(levelSelectionNavigationController.transform, "\"" + folderName + "\" already exists! Please use a different name.", null);
				return;
			}
			CustomListTableData.CustomCellInfo item2 = new CustomListTableData.CustomCellInfo(folderName, null, BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
			tableCells.Add(item2);
			customListTableData.TableView.ReloadData();
			customListTableData.TableView.ClearSelection();
			currentManagers.Add(item);
		}
	}

	[UIAction("rename-folder")]
	private void RenameButtonClicked()
	{
		popupModalsController.ShowKeyboard(levelSelectionNavigationController.transform, RenameKeyboardEnter, Path.GetFileName(CurrentParentManager.PlaylistPath));
	}

	private void RenameKeyboardEnter(string folderName)
	{
		if (CurrentParentManager?.Parent != null)
		{
			folderName = folderName.Replace("/", "").Replace("\\", "").Replace(".", "");
			if (!string.IsNullOrEmpty(folderName) && folderName != Path.GetFileName(CurrentParentManager.PlaylistPath))
			{
				CurrentParentManager.RenameManager(folderName);
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FolderText"));
			}
		}
	}

	[UIAction("delete-folder")]
	private void DeleteButtonClicked()
	{
		popupModalsController.ShowYesNoModal(levelSelectionNavigationController.transform, $"Are you sure you want to delete {Path.GetFileName(CurrentParentManager.PlaylistPath)} along with all playlists and subfolders?", DeleteConfirm);
	}

	private void DeleteConfirm()
	{
		CurrentParentManager?.Parent?.DeleteChildManager(CurrentParentManager, recycle: true);
		BackButtonClicked();
	}

	public void LevelCategoryUpdated(SelectLevelCategoryViewController.LevelCategory levelCategory, bool viewControllerActivated)
	{
		if (rootTransform == null)
		{
			return;
		}
		if (levelCategory == SelectLevelCategoryViewController.LevelCategory.CustomSongs)
		{
			rootTransform.gameObject.SetActive(value: true);
			SetupDimensions();
			if (viewControllerActivated)
			{
				SetupList(CurrentParentManager, setBeatmapLevelCollections: false);
			}
		}
		else
		{
			rootTransform.gameObject.SetActive(value: false);
		}
	}

	private void LevelFilteringNavigationController_ShowPacksInChildController_AllPacksViewSelectedEvent()
	{
		SetupList(null, setBeatmapLevelCollections: false);
		folderMode = FolderMode.AllPacks;
	}

	public void Refresh()
	{
		if (!rootTransform.gameObject.activeInHierarchy)
		{
			return;
		}
		if (folderMode == FolderMode.AllPacks)
		{
			BeatmapLevelPack[] array = PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks();
			playlistUpdater.RefreshPlaylistChangedListeners(array);
			object customLevelsRepository = beatmapLevelsModel.GetPrivateField<object>("_customLevelsRepository");
					BeatmapLevelPack[] array2 = customLevelsRepository.GetMemberValue<IReadOnlyList<BeatmapLevelPack>>("beatmapLevelPacks").Concat<BeatmapLevelPack>(array).ToArray();
			int num = Array.FindIndex(array2, (BeatmapLevelPack pack) => pack.packID == annotatedBeatmapLevelCollectionsViewController.selectedAnnotatedBeatmapLevelPack.packID);
			if (num != -1)
			{
				annotatedBeatmapLevelCollectionsViewController.SetData(array2, num, hideIfOneOrNoPacks: false);
			}
		}
		else if (folderMode == FolderMode.Playlists)
		{
			BeatmapLevelPack[] array3 = PlaylistLibUtils.TryGetAllPlaylistsAsLevelPacks();
			playlistUpdater.RefreshPlaylistChangedListeners(array3);
			int num2 = Array.FindIndex(array3, (BeatmapLevelPack pack) => pack.packID == annotatedBeatmapLevelCollectionsViewController.selectedAnnotatedBeatmapLevelPack.packID);
			if (num2 != -1)
			{
				annotatedBeatmapLevelCollectionsViewController.SetData(array3, num2, hideIfOneOrNoPacks: false);
			}
		}
		else if (folderMode == FolderMode.Folders)
		{
			PlaylistLevelPack[] array4 = (from p in CurrentParentManager.GetAllPlaylists(includeChildren: false)
				select p.PlaylistLevelPack).ToArray();
			PlaylistUpdater obj = playlistUpdater;
			BeatmapLevelPack[] beatmapLevelPacks = array4;
			obj.RefreshPlaylistChangedListeners(beatmapLevelPacks);
			int num3 = Array.FindIndex(array4, (PlaylistLevelPack pack) => pack.packID == annotatedBeatmapLevelCollectionsViewController.selectedAnnotatedBeatmapLevelPack.packID);
			if (num3 != -1)
			{
				annotatedBeatmapLevelCollectionsViewController.SetData(array4, num3, hideIfOneOrNoPacks: false);
			}
			SetupList(CurrentParentManager, setBeatmapLevelCollections: false);
		}
	}

	private FolderCell GetCell()
	{
		TableCell tableCell = customListTableData.TableView.DequeueReusableCellForIdentifier("PlaylistFolderCell");
		FolderCell folderCell = null;
		if ((UnityEngine.Object)(object)tableCell == null)
		{
			tableCell = customListTableData.GetBoxTableCell();
			tableCell.reuseIdentifier = "PlaylistFolderCell";
			folderCell = ((UnityEngine.Component)(object)tableCell).gameObject.AddComponent<FolderCell>();
		}
		if (!folderCell)
		{
			return ((UnityEngine.Component)(object)tableCell).GetComponent<FolderCell>();
		}
		return folderCell;
	}

	public float CellSize(int idx)
	{
		return 15f;
	}

	public int NumberOfCells()
	{
		return tableCells.Count;
	}

	public TableCell CellForIdx(TableView tableView, int idx)
	{
		return GetCell().PopulateCell(tableCells[idx].Icon, tableCells[idx].Text);
	}
}
