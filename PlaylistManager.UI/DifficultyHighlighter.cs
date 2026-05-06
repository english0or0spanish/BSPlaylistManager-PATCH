using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using PlaylistManager.HarmonyPatches;
using PlaylistManager.Interfaces;
using PlaylistManager.Utilities;
using TMPro;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

public class DifficultyHighlighter : IBeatmapLevelUpdater, IDisposable, IInitializable
{
	private readonly BeatmapCharacteristicSegmentedControlController _beatmapCharacteristicSegmentedControlController;

	private readonly IconSegmentedControl _beatmapCharacteristicSegmentedControl;

	private readonly BeatmapDifficultySegmentedControlController _beatmapDifficultySegmentedControlController;

	private readonly SegmentedControl _beatmapDifficultySegmentedControl;

	private IPlaylistSong _selectedPlaylistSong;

	private Color32? _originalDifficultyColor;

	private bool IsSelectedDifficultyHighlighted
	{
		get
		{
			if (_selectedPlaylistSong != null && _selectedPlaylistSong.Difficulties != null && _selectedPlaylistSong.Difficulties.Count != 0)
			{
				return (from d in _selectedPlaylistSong.Difficulties.FindAll((Difficulty difficulty) => difficulty.Characteristic.Equals(_beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName, StringComparison.OrdinalIgnoreCase))
					select d.BeatmapDifficulty).Contains(_beatmapDifficultySegmentedControlController.selectedDifficulty);
			}
			return false;
		}
	}

	internal event Action<bool> selectedDifficultyChanged;

	public DifficultyHighlighter(StandardLevelDetailViewController standardLevelDetailViewController)
	{
		StandardLevelDetailView standardLevelDetailView = standardLevelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
		_beatmapCharacteristicSegmentedControlController = standardLevelDetailView.GetPrivateField<BeatmapCharacteristicSegmentedControlController>("_beatmapCharacteristicSegmentedControlController");
		_beatmapCharacteristicSegmentedControl = _beatmapCharacteristicSegmentedControlController.GetPrivateField<IconSegmentedControl>("_segmentedControl");
		_beatmapDifficultySegmentedControlController = standardLevelDetailView.GetPrivateField<BeatmapDifficultySegmentedControlController>("_beatmapDifficultySegmentedControlController");
		_beatmapDifficultySegmentedControl = _beatmapDifficultySegmentedControlController.GetPrivateField<SegmentedControl>("_difficultySegmentedControl");
	}

	public void Initialize()
	{
		_beatmapCharacteristicSegmentedControl.didSelectCellEvent += BeatmapCharacteristicSegmentedControl_DidSelectCellEvent;
		_beatmapDifficultySegmentedControl.didSelectCellEvent += BeatmapDifficultySegmentedControl_didSelectCellEvent;
		BeatmapDifficultySegmentedControlController_SetData.CharacteristicsSegmentedControllerDataSetEvent += BeatmapDifficultySegmentedControlController_CharacteristicsSegmentedControllerDataSetEvent;
	}

	public void Dispose()
	{
		_beatmapCharacteristicSegmentedControl.didSelectCellEvent -= BeatmapCharacteristicSegmentedControl_DidSelectCellEvent;
		_beatmapDifficultySegmentedControl.didSelectCellEvent -= BeatmapDifficultySegmentedControl_didSelectCellEvent;
		BeatmapDifficultySegmentedControlController_SetData.CharacteristicsSegmentedControllerDataSetEvent -= BeatmapDifficultySegmentedControlController_CharacteristicsSegmentedControllerDataSetEvent;
	}

	public void BeatmapLevelUpdated(BeatmapLevel beatmapLevel)
	{
		if (beatmapLevel is PlaylistLevel playlistLevel)
		{
			_selectedPlaylistSong = playlistLevel.playlistSong;
		}
		else
		{
			_selectedPlaylistSong = null;
		}
	}

	private void HighlightDifficultiesForSelectedCharacteristic()
	{
		IReadOnlyList<SegmentedControlCell> cells = _beatmapDifficultySegmentedControl.cells;
		Color32 color = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
		foreach (SegmentedControlCell item in cells)
		{
			CurvedTextMeshPro componentInChildren = ((Component)(object)item).GetComponentInChildren<CurvedTextMeshPro>();
			Color32 valueOrDefault = _originalDifficultyColor.GetValueOrDefault();
			if (!_originalDifficultyColor.HasValue)
			{
				valueOrDefault = componentInChildren.faceColor;
				_originalDifficultyColor = valueOrDefault;
			}
			if (componentInChildren.faceColor.Compare(color))
			{
				componentInChildren.faceColor = _originalDifficultyColor.Value;
			}
		}
		if (_selectedPlaylistSong == null || _selectedPlaylistSong.Difficulties == null || _selectedPlaylistSong.Difficulties.Count == 0)
		{
			return;
		}
		List<Difficulty> list = _selectedPlaylistSong.Difficulties.FindAll((Difficulty difficulty) => difficulty.Characteristic.Equals(_beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName, StringComparison.OrdinalIgnoreCase));
		List<BeatmapDifficulty> difficulties = _beatmapDifficultySegmentedControlController.GetPrivateField<List<BeatmapDifficulty>>("_difficulties");
		foreach (Difficulty item2 in list)
		{
			if (difficulties.Contains(item2.BeatmapDifficulty))
			{
				int idx = (int)_beatmapDifficultySegmentedControlController.InvokeMethod("GetClosestDifficultyIndex", item2.BeatmapDifficulty);
				((Component)(object)cells[idx]).GetComponentInChildren<CurvedTextMeshPro>().faceColor = color;
			}
		}
	}

	private void RaiseDifficultyChangedEvent()
	{
		this.selectedDifficultyChanged?.Invoke(IsSelectedDifficultyHighlighted);
	}

	internal void ToggleSelectedDifficultyHighlight()
	{
		if (_selectedPlaylistSong == null)
		{
			return;
		}
		IReadOnlyList<SegmentedControlCell> cells = _beatmapDifficultySegmentedControl.cells;
		if (IsSelectedDifficultyHighlighted)
		{
			_selectedPlaylistSong.Difficulties.RemoveAll((Difficulty d) => d.BeatmapDifficulty == _beatmapDifficultySegmentedControlController.selectedDifficulty);
			int selectedIndex = (int)_beatmapDifficultySegmentedControlController.InvokeMethod("GetClosestDifficultyIndex", _beatmapDifficultySegmentedControlController.selectedDifficulty);
		((Component)(object)cells[selectedIndex]).GetComponentInChildren<CurvedTextMeshPro>().faceColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
			return;
		}
		if (_selectedPlaylistSong.Difficulties == null)
		{
			_selectedPlaylistSong.Difficulties = new List<Difficulty>();
		}
		Difficulty diff = new Difficulty
		{
			BeatmapDifficulty = _beatmapDifficultySegmentedControlController.selectedDifficulty,
			Characteristic = _beatmapCharacteristicSegmentedControlController.selectedBeatmapCharacteristic.serializedName
		};
		_selectedPlaylistSong.AddDifficulty(diff);
		int highlightIndex = (int)_beatmapDifficultySegmentedControlController.InvokeMethod("GetClosestDifficultyIndex", _beatmapDifficultySegmentedControlController.selectedDifficulty);
		((Component)(object)cells[highlightIndex]).GetComponentInChildren<CurvedTextMeshPro>().faceColor = new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
	}

	private void BeatmapDifficultySegmentedControlController_CharacteristicsSegmentedControllerDataSetEvent()
	{
		HighlightDifficultiesForSelectedCharacteristic();
		RaiseDifficultyChangedEvent();
	}

	private void BeatmapCharacteristicSegmentedControl_DidSelectCellEvent(SegmentedControl _, int __)
	{
		HighlightDifficultiesForSelectedCharacteristic();
	}

	private void BeatmapDifficultySegmentedControl_didSelectCellEvent(SegmentedControl arg1, int arg2)
	{
		RaiseDifficultyChangedEvent();
	}
}
