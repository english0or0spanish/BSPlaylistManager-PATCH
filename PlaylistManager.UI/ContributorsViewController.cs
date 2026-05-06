using System;
using System.Collections.Generic;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using PlaylistManager.Types;
using UnityEngine;
using Zenject;

namespace PlaylistManager.UI;

[ViewDefinition("PlaylistManager.UI.Views.ContributorsView.bsml")]
internal class ContributorsViewController : BSMLAutomaticViewController, IInitializable, IDisposable
{
	private PopupModalsController popupModalsController;

	private List<object> contributors;

	[UIComponent("contributors-list")]
	private readonly CustomCellListTableData customListTableData = null!;

	[Inject]
	public void Contruct(PopupModalsController popupModalsController)
	{
		this.popupModalsController = popupModalsController;
	}

	public void Initialize()
	{
		contributors = new List<object>();
		contributors.Add(new Contributor("PixelBoom", "PlaylistManager (PC)", "PlaylistManager.Icons.Pixel.png", "https://www.youtube.com/channel/UCrk1WH6hCAdfrAtzv-q9hvQ", "https://www.twitch.tv/pixelboom58", "https://github.com/rithik-b", "https://ko-fi.com/pixelboom"));
		contributors.Add(new Contributor("Metalit", "PlaylistManager (Quest)", "PlaylistManager.Icons.Metalit.png", null, null, "https://github.com/Metalit"));
		contributors.Add(new Contributor("Zingabopp", "BeatSaberPlaylistsLib (PC)", "PlaylistManager.Icons.Zinga.png", null, null, "https://github.com/Zingabopp", "https://ko-fi.com/zingabopp"));
		contributors.Add(new Contributor("Auros", "Major Contributor (PC)", "PlaylistManager.Icons.Auros.png", null, "https://www.twitch.tv/aurosvr", "https://github.com/Auros", "https://ko-fi.com/auros"));
		foreach (Contributor item in contributors.OfType<Contributor>())
		{
			item.OpenURL += URLRequested;
		}
	}

	public void Dispose()
	{
		foreach (Contributor item in contributors.OfType<Contributor>())
		{
			item.OpenURL -= URLRequested;
		}
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		customListTableData.Data.Clear();
		customListTableData.Data = contributors;
		customListTableData.TableView.ReloadData();
	}

	private void URLRequested(string url)
	{
		popupModalsController.ShowYesNoModal(base.transform, "Would you like to open\n" + url, delegate
		{
			Application.OpenURL(url);
		});
	}
}
