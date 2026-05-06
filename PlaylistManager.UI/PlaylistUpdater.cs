using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using PlaylistManager.Interfaces;
using PlaylistManager.Utilities;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

internal class PlaylistUpdater : IInitializable, IDisposable, ILevelCollectionUpdater
{
	private readonly HashSet<IPlaylist> playlistReferences = new HashSet<IPlaylist>();

	private readonly AnnotatedBeatmapLevelCollectionsViewController _annotatedBeatmapLevelCollectionsViewController;

	private readonly LevelCollectionNavigationController _levelCollectionNavigationController;

	private readonly LevelPackDetailViewController _levelPackDetailViewController;

	private IPlaylist _selectedPlaylist;

	private PlaylistUpdater(AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, LevelCollectionNavigationController levelCollectionNavigationController, LevelPackDetailViewController levelPackDetailViewController)
	{
		_annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
		_levelCollectionNavigationController = levelCollectionNavigationController;
		_levelPackDetailViewController = levelPackDetailViewController;
	}

	public void Initialize()
	{
		IPlaylist[] array = PlaylistLibUtils.TryGetAllPlaylists();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].PlaylistChanged += UpdatePlaylist;
		}
		PlaylistLibUtils.playlistManager.PlaylistsRefreshRequested += HandleDidRequestPlaylistsRefresh;
	}

	public void Dispose()
	{
		foreach (IPlaylist playlistReference in playlistReferences)
		{
			playlistReference.SpriteLoaded -= SelectedPlaylist_SpriteLoaded;
		}
		IPlaylist[] array = PlaylistLibUtils.TryGetAllPlaylists();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].PlaylistChanged -= UpdatePlaylist;
		}
		PlaylistLibUtils.playlistManager.PlaylistsRefreshRequested -= HandleDidRequestPlaylistsRefresh;
	}

	private void HandleDidRequestPlaylistsRefresh(object sender, string e)
	{
		RefreshPlaylistChangedListeners();
	}

	public void RefreshPlaylistChangedListeners(BeatmapLevelPack[] beatmapLevelPacks = null)
	{
		foreach (IPlaylist item in beatmapLevelPacks?.Select((BeatmapLevelPack p) => ((PlaylistLevelPack)p).playlist) ?? PlaylistLibUtils.TryGetAllPlaylists())
		{
			item.PlaylistChanged -= UpdatePlaylist;
			item.PlaylistChanged += UpdatePlaylist;
		}
	}

	public void UpdatePlaylist(object sender, EventArgs e)
	{
		UpdatePlaylist((IPlaylist)sender);
	}

	private void UpdatePlaylist(IPlaylist playlist)
	{
		BeatmapLevelPack beatmapLevelPack = RefreshAnnotatedBeatmapCollection(playlist.PlaylistLevelPack);
		if (beatmapLevelPack != null)
		{
			_levelPackDetailViewController.SetPrivateField("_pack", beatmapLevelPack);
			Type contentType = typeof(LevelPackDetailViewController).GetNestedType("ContentType");
			object nonBuyable = ReflectionUtils.CreateEnumValue(contentType, "NonBuyable");
			_levelPackDetailViewController.InvokeMethod("ShowContent", nonBuyable, "");
			_levelCollectionNavigationController.SetPrivateField("_levelPack", beatmapLevelPack);
			LevelCollectionViewController levelCollectionViewController = _levelCollectionNavigationController.GetPrivateField<LevelCollectionViewController>("_levelCollectionViewController");
			LevelCollectionTableView levelCollectionTableView = levelCollectionViewController.GetPrivateField<LevelCollectionTableView>("_levelCollectionTableView");
			levelCollectionTableView.SetPrivateField("_headerText", beatmapLevelPack.packName);
			TableView tableView = levelCollectionTableView.GetPrivateField<TableView>("_tableView");
			tableView.RefreshCellsContent();
		}
	}

	public BeatmapLevelPack RefreshAnnotatedBeatmapCollection(BeatmapLevelPack beatmapLevelPack)
	{
		if (beatmapLevelPack == null)
		{
			return null;
		}

		BeatmapLevelPack[] array = _annotatedBeatmapLevelCollectionsViewController.GetPrivateField<BeatmapLevelPack[]>("_annotatedBeatmapLevelCollections");
		if (array == null)
		{
			return null;
		}

		int num = Array.FindIndex(array, (BeatmapLevelPack pack) => pack is PlaylistLevelPack && pack.packID == beatmapLevelPack.packID);
		if (num == -1)
		{
			return null;
		}

		PlaylistLevelPack playlistLevelPack = (PlaylistLevelPack)(array[num] = ((PlaylistLevelPack)beatmapLevelPack).playlist.PlaylistLevelPack);
		_annotatedBeatmapLevelCollectionsViewController.SetPrivateField("_annotatedBeatmapLevelCollections", array);

		object annotatedGridView = _annotatedBeatmapLevelCollectionsViewController.GetPrivateField<object>("_annotatedBeatmapLevelCollectionsGridView");
		if (annotatedGridView == null)
		{
			return playlistLevelPack;
		}

		annotatedGridView.SetPrivateField("_annotatedBeatmapLevelCollections", array);
		GridView gridView = annotatedGridView.GetPrivateField<GridView>("_gridView");
		if (gridView == null)
		{
			return playlistLevelPack;
		}

		object dataSource = gridView.GetPrivateField<object>("_dataSource");
		if (dataSource == null)
		{
			return playlistLevelPack;
		}

		float cellWidth = dataSource.GetPrivateField<float>("_cellWidth");
		float cellHeight = dataSource.GetPrivateField<float>("_cellHeight");
		Dictionary<MonoBehaviour, List<MonoBehaviour>> spawnedCells = gridView.GetPrivateField<Dictionary<MonoBehaviour, List<MonoBehaviour>>>("_spawnedCellsPerPrefabDictionary");
		Dictionary<MonoBehaviour, Queue<MonoBehaviour>> availableCells = gridView.GetPrivateField<Dictionary<MonoBehaviour, Queue<MonoBehaviour>>>("_availableCellsPerPrefabDictionary");

		if (spawnedCells != null && availableCells != null)
		{
			foreach (MonoBehaviour key in spawnedCells.Keys.ToList())
			{
				List<MonoBehaviour> list = spawnedCells[key];
				if (list == null)
				{
					continue;
				}
				foreach (MonoBehaviour item in list.ToList())
				{
					BeatmapLevelPack itemPack = ((AnnotatedBeatmapLevelCollectionCell)(object)item).GetPrivateField<BeatmapLevelPack>("_beatmapLevelPack");
					if (itemPack != null && itemPack.packID == playlistLevelPack.packID)
					{
						item.gameObject.SetActive(value: false);
						if (availableCells.TryGetValue(key, out Queue<MonoBehaviour> queue))
						{
							queue.Enqueue(item);
						}
						list.Remove(item);
						break;
					}
				}
			}
		}

		int columnCount = gridView.GetPrivateField<int>("_columnCount");
		if (columnCount == 0)
		{
			return playlistLevelPack;
		}

		int num2 = num % columnCount;
		int num3 = num / columnCount;
		object cell = dataSource.InvokeMethod("CellForIdx", gridView, num);
		if (cell == null)
		{
			return playlistLevelPack;
		}

		RectTransform obj = (RectTransform)((Component)cell).transform;
		obj.anchorMin = new Vector2(0f, 1f);
		obj.anchorMax = new Vector2(0f, 1f);
		obj.pivot = new Vector2(0f, 1f);
		obj.anchoredPosition = new Vector2((float)num2 * cellWidth, (float)num3 * (0f - cellHeight));
		return playlistLevelPack;
	}

	private void SelectedPlaylist_SpriteLoaded(object sender, EventArgs e)
	{
		Sprite blurredPackArtwork = _levelPackDetailViewController.GetPrivateField<Sprite>("_blurredPackArtwork");
		if (blurredPackArtwork != null)
		{
			UnityEngine.Object.Destroy(blurredPackArtwork);
			_levelPackDetailViewController.SetPrivateField("_blurredPackArtwork", null);
		}
		Sprite sprite = ((sender is IPlaylist playlist) ? playlist.Sprite : (sender as Sprite));
		Sprite sprite2 = ((sprite != null) ? sprite : _levelPackDetailViewController.GetPrivateField<Sprite>("_defaultCoverSprite"));
		object kawaseBlurRenderer = _levelPackDetailViewController.GetPrivateField<object>("_kawaseBlurRenderer");
		Texture2D texture2D = (Texture2D)kawaseBlurRenderer.InvokeMethod("Blur", sprite2.texture, KawaseBlurRendererSO.KernelSize.Kernel7, 2);
		_levelPackDetailViewController.SetPrivateField("_blurredPackArtwork", Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 256f, 0u, SpriteMeshType.FullRect, new Vector4(0f, 0f, 0f, 0f), generateFallbackPhysicsShape: false));
		_levelPackDetailViewController.GetPrivateField<ImageView>("_packImage").sprite = sprite2;
		Type contentType = typeof(LevelPackDetailViewController).GetNestedType("ContentType");
		object nonBuyable = ReflectionUtils.CreateEnumValue(contentType, "NonBuyable");
		_levelPackDetailViewController.InvokeMethod("ShowContent", nonBuyable, "");
	}

	public void LevelCollectionUpdated(BeatmapLevelPack beatmapLevelPack, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		if (_selectedPlaylist != null)
		{
			playlistReferences.Remove(_selectedPlaylist);
			_selectedPlaylist.SpriteLoaded -= SelectedPlaylist_SpriteLoaded;
		}
		if (beatmapLevelPack is PlaylistLevelPack playlistLevelPack)
		{
			_selectedPlaylist = playlistLevelPack.playlist;
			if (playlistReferences.Add(_selectedPlaylist))
			{
				_selectedPlaylist.SpriteLoaded += SelectedPlaylist_SpriteLoaded;
			}
		}
		else
		{
			_selectedPlaylist = null;
		}
	}
}
