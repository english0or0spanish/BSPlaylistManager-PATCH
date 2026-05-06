using System;
using System.Collections.Generic;
using HMUI;
using PlaylistManager.Utilities;
using SiraUtil.Affinity;
using UnityEngine;

namespace PlaylistManager.AffinityPatches;

internal class AnnotatedBeatmapLevelCollectionsUIPatches : IAffinity
{
	private readonly MainFlowCoordinator _mainFlowCoordinator;

	private readonly AnnotatedBeatmapLevelCollectionsViewController _annotatedBeatmapLevelCollectionsViewController;

	private readonly SelectLevelCategoryViewController _selectLevelCategoryViewController;

	private int _originalColumnCount;

	private Vector2 _originalScreenSize;

	private bool _isGridResized;

	private bool _isScreenResized;

	private AnnotatedBeatmapLevelCollectionsUIPatches(MainFlowCoordinator mainFlowCoordinator, AnnotatedBeatmapLevelCollectionsViewController annotatedBeatmapLevelCollectionsViewController, SelectLevelCategoryViewController selectLevelCategoryViewController)
	{
		_mainFlowCoordinator = mainFlowCoordinator;
		_annotatedBeatmapLevelCollectionsViewController = annotatedBeatmapLevelCollectionsViewController;
		_selectLevelCategoryViewController = selectLevelCategoryViewController;
	}

	[AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridView), "SetData", AffinityMethodType.Normal, null, new Type[] { })]
	[AffinityPrefix]
	private void ResizeGrid(AnnotatedBeatmapLevelCollectionsGridView __instance, IReadOnlyList<BeatmapLevelPack> annotatedBeatmapLevelCollections)
	{
		GridView gridView = __instance.GetPrivateField<GridView>("_gridView");
		if (_originalColumnCount == 0)
		{
			_originalColumnCount = gridView.GetPrivateField<int>("_columnCount");
		}
		switch (_selectLevelCategoryViewController.selectedLevelCategory)
		{
		case SelectLevelCategoryViewController.LevelCategory.CustomSongs:
			gridView.SetPrivateField("_columnCount", Math.Max(Mathf.CeilToInt((float)(annotatedBeatmapLevelCollections?.Count ?? 0) / 5f), _originalColumnCount));
			if (!_isGridResized)
			{
				gridView.SetPrivateField("_visibleColumnCount", gridView.GetPrivateField<int>("_visibleColumnCount") - 1);
				RectTransform obj2 = (RectTransform)gridView.transform;
				float cellWidth = __instance.GetPrivateField<float>("_cellWidth");
				obj2.sizeDelta -= new Vector2(cellWidth, 0f);
				obj2.anchoredPosition -= new Vector2(cellWidth / 2f, 0f);
				_isGridResized = true;
			}
			break;
		case SelectLevelCategoryViewController.LevelCategory.MusicPacks:
			gridView.SetPrivateField("_columnCount", _originalColumnCount);
			if (_isGridResized)
			{
				gridView.SetPrivateField("_visibleColumnCount", gridView.GetPrivateField<int>("_visibleColumnCount") + 1);
				RectTransform obj = (RectTransform)gridView.transform;
				float cellWidth = __instance.GetPrivateField<float>("_cellWidth");
				obj.sizeDelta += new Vector2(cellWidth, 0f);
				obj.anchoredPosition += new Vector2(cellWidth / 2f, 0f);
				_isGridResized = false;
			}
			break;
		}
	}

	[AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), "GetContentXOffset", AffinityMethodType.Normal, null, new Type[] { })]
	private void RecalculateContentXOffsetBasedOnColumnCount(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, ref float __result)
	{
		object annotatedGridView = _annotatedBeatmapLevelCollectionsViewController.GetPrivateField<object>("_annotatedBeatmapLevelCollectionsGridView");
		IReadOnlyList<BeatmapLevelPack> annotatedBeatmapLevelCollections = ((object)annotatedGridView).GetPrivateField<IReadOnlyList<BeatmapLevelPack>>("_annotatedBeatmapLevelCollections");
		if (annotatedBeatmapLevelCollections == null)
		{
			return;
		}
		int visibleColumnCount = __instance.GetPrivateField<int>("_visibleColumnCount");
		if (annotatedBeatmapLevelCollections.Count <= visibleColumnCount)
		{
			__result = __instance.GetPrivateField<float>("_columnWidth");
			return;
		}
		int columnCount = __instance.GetPrivateField<int>("_columnCount");
		float num = (float)(columnCount - 1) / 2f;
		float num2 = (float)(columnCount - visibleColumnCount) / 2f;
		float num3 = num - (float)__instance.GetPrivateField<int>("_selectedColumn");
		if (visibleColumnCount % 2 == 0)
		{
			num3 -= 0.5f;
		}
		__result = Math.Clamp(num3, 0f - num2, num2) * __instance.GetPrivateField<float>("_columnWidth");
	}

	[AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), "AnimateOpen", AffinityMethodType.Normal, null, new Type[] { })]
	private void RecalculateSizeBasedOnColumnCount(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance, bool animated)
	{
		int columnCount = __instance.GetPrivateField<int>("_columnCount");
		int visibleColumnCount = __instance.GetPrivateField<int>("_visibleColumnCount");
		float columnWidth = __instance.GetPrivateField<float>("_columnWidth");
		float x = (float)((columnCount - visibleColumnCount) * 2 + visibleColumnCount) * columnWidth;
		if (animated)
		{
			object viewportSizeTween = __instance.GetPrivateField<object>("_viewportSizeTween");
			Vector2 toValue = viewportSizeTween.GetPrivateField<Vector2>("toValue");
			viewportSizeTween.SetPrivateField("toValue", new Vector2(x, toValue.y));
		}
		else
		{
			RectTransform viewportTransform = __instance.GetPrivateField<RectTransform>("_viewportTransform");
			viewportTransform.sizeDelta = new Vector2(x, viewportTransform.sizeDelta.y);
		}
		if (_isGridResized)
		{
			RectTransform rectTransform = (RectTransform)_selectLevelCategoryViewController.transform;
			if (rectTransform.anchorMin.x == 0f || rectTransform.anchorMax.x == 0f)
			{
				Vector3 localPosition = rectTransform.localPosition;
				rectTransform.anchorMin = new Vector2(0.5f, rectTransform.anchorMin.y);
				rectTransform.anchorMax = new Vector2(0.5f, rectTransform.anchorMax.y);
				rectTransform.localPosition = localPosition;
			}
			object screenSystem = _mainFlowCoordinator.GetPrivateField<object>("_screenSystem");
			if (screenSystem == null)
			{
				return;
			}
			object mainScreen = screenSystem.GetPrivateField<object>("mainScreen");
			if (mainScreen is not Component mainScreenComponent)
			{
				return;
			}
			RectTransform screenTransform = mainScreenComponent.transform as RectTransform;
			if (screenTransform == null)
			{
				return;
			}
			if (_originalScreenSize == default(Vector2))
			{
				_originalScreenSize = screenTransform.sizeDelta;
			}
			screenTransform.sizeDelta = new Vector2(_originalScreenSize.x + (float)(columnCount - visibleColumnCount - 1) * columnWidth * 2f, _originalScreenSize.y);
			_isScreenResized = true;
		}
	}

	[AffinityPatch(typeof(AnnotatedBeatmapLevelCollectionsGridViewAnimator), "AnimateClose", AffinityMethodType.Normal, null, new Type[] { })]
	private void RestoreScreenSize(AnnotatedBeatmapLevelCollectionsGridViewAnimator __instance)
	{
		if (_isScreenResized)
		{
			object screenSystem = _mainFlowCoordinator.GetPrivateField<object>("_screenSystem");
			if (screenSystem == null)
			{
				return;
			}
			object mainScreen = screenSystem.GetPrivateField<object>("mainScreen");
			if (mainScreen is not Component mainScreenComponent)
			{
				return;
			}
			RectTransform screenTransform = mainScreenComponent.transform as RectTransform;
			if (screenTransform == null)
			{
				return;
			}
			screenTransform.sizeDelta = _originalScreenSize;
			_isScreenResized = false;
		}
	}
}
