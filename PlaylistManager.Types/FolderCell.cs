using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using TMPro;
using UnityEngine;

namespace PlaylistManager.Types;

public class FolderCell : MonoBehaviour
{
	private TextMeshProUGUI text;

	private BSMLBoxTableCell tableCell;

	private TextMeshProUGUI Text
	{
		get
		{
			if (text == null)
			{
				text = BeatSaberUI.CreateText(base.transform.Find("Wrapper").GetComponent<RectTransform>(), "", new Vector2(0f, -5f));
				text.alignment = TextAlignmentOptions.Center;
				text.overflowMode = TextOverflowModes.Ellipsis;
				text.fontSize = 2.5f;
				RectTransform rectTransform = text.rectTransform;
				rectTransform.anchorMin = Vector2.zero;
				rectTransform.anchorMax = Vector2.one;
				rectTransform.sizeDelta = new Vector2(-3f, -3f);
			}
			return text;
		}
	}

	private BSMLBoxTableCell TableCell => tableCell ?? (tableCell = GetComponent<BSMLBoxTableCell>());

	public BSMLBoxTableCell PopulateCell(Sprite sprite, string text = "")
	{
		TableCell.SetData(sprite);
		Text.text = text;
		return TableCell;
	}
}
