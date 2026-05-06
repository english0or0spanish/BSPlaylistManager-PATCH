using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using PlaylistManager.Utilities;

namespace PlaylistManager.Types;

internal class Contributor : INotifyPropertyChanged
{
	[UIComponent("icon")]
	private readonly ImageView iconImage = null!;

	[UIValue("name")]
	public string name { get; private set; }

	[UIValue("role")]
	public string role { get; private set; }

	public string iconPath { get; private set; }

	public string youtube { get; private set; }

	[UIValue("youtube-active")]
	private bool YoutubeActive => !string.IsNullOrEmpty(youtube);

	public string twitch { get; private set; }

	[UIValue("twitch-active")]
	private bool TwitchActive => !string.IsNullOrEmpty(twitch);

	public string github { get; private set; }

	[UIValue("github-active")]
	private bool GithubActive => !string.IsNullOrEmpty(github);

	public string kofi { get; private set; }

	[UIValue("kofi-active")]
	private bool KofiActive => !string.IsNullOrEmpty(kofi);

	public event Action<string> OpenURL;

	public event PropertyChangedEventHandler PropertyChanged;

	public Contributor(string name, string role, string icon, string youtube = null, string twitch = null, string github = null, string kofi = null)
	{
		this.name = name;
		this.role = role;
		iconPath = icon;
		this.youtube = youtube;
		this.twitch = twitch;
		this.github = github;
		this.kofi = kofi;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("name"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("role"));
		iconImage.sprite = SpriteUtils.LoadSpriteFromAssembly(GetType().Assembly, iconPath);
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YoutubeActive"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TwitchActive"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("GithubActive"));
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("KofiActive"));
	}

	[UIAction("youtube-click")]
	private void YoutubeClicked()
	{
		this.OpenURL?.Invoke(youtube);
	}

	[UIAction("twitch-click")]
	private void TwitchClicked()
	{
		this.OpenURL?.Invoke(twitch);
	}

	[UIAction("github-click")]
	private void GithubClicked()
	{
		this.OpenURL?.Invoke(github);
	}

	[UIAction("kofi-click")]
	private void KofiClicked()
	{
		this.OpenURL?.Invoke(kofi);
	}
}
