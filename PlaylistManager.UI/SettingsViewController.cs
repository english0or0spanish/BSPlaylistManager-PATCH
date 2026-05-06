using System;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using PlaylistManager.Configuration;
using Zenject;

namespace PlaylistManager.UI;

[ViewDefinition("PlaylistManager.UI.Views.SettingsView.bsml")]
internal class SettingsViewController : BSMLAutomaticViewController
{
	private MenuTransitionsHelper menuTransitionsHelper;

	private bool _defaultImageDisabled;

	private bool _defaultAllowDuplicates;

	private string _authorName;

	private bool _automaticAuthorName;

	private bool _playlistHoverHints;

	private bool _showDownloadIcon;

	private bool _blurredArt;

	private bool _foldersDisabled;

	private int _syncOption;

	private bool _downloadDuringGameplay;

	private bool _driveFullProtection;

	private bool _easterEggs;

	[UIValue("no-image")]
	private bool DefaultImageDisabled
	{
		get
		{
			return _defaultImageDisabled;
		}
		set
		{
			_defaultImageDisabled = value;
			NotifyPropertyChanged("DefaultImageDisabled");
		}
	}

	[UIValue("allow-duplicates")]
	private bool DefaultAllowDuplicates
	{
		get
		{
			return _defaultAllowDuplicates;
		}
		set
		{
			_defaultAllowDuplicates = value;
			NotifyPropertyChanged("DefaultAllowDuplicates");
		}
	}

	[UIValue("auto-name")]
	private bool AutomaticAuthorName
	{
		get
		{
			return _automaticAuthorName;
		}
		set
		{
			_automaticAuthorName = value;
			NotifyPropertyChanged("AutomaticAuthorName");
			NotifyPropertyChanged("NameActive");
		}
	}

	[UIValue("author-name")]
	private string AuthorName
	{
		get
		{
			return _authorName;
		}
		set
		{
			_authorName = value;
			NotifyPropertyChanged("AuthorName");
		}
	}

	[UIValue("name-active")]
	private bool NameActive => !AutomaticAuthorName;

	[UIValue("hover-hint")]
	private bool PlaylistHoverHints
	{
		get
		{
			return _playlistHoverHints;
		}
		set
		{
			_playlistHoverHints = value;
			NotifyPropertyChanged("PlaylistHoverHints");
			NotifyPropertyChanged("SoftRestart");
		}
	}

	[UIValue("download-icon")]
	private bool ShowDownloadIcon
	{
		get
		{
			return _showDownloadIcon;
		}
		set
		{
			_showDownloadIcon = value;
			NotifyPropertyChanged("ShowDownloadIcon");
		}
	}

	[UIValue("blurred-art")]
	private bool BlurredArt
	{
		get
		{
			return _blurredArt;
		}
		set
		{
			_blurredArt = value;
			NotifyPropertyChanged("BlurredArt");
		}
	}

	[UIValue("no-folders")]
	private bool FoldersDisabled
	{
		get
		{
			return _foldersDisabled;
		}
		set
		{
			_foldersDisabled = value;
			NotifyPropertyChanged("FoldersDisabled");
			NotifyPropertyChanged("SoftRestart");
		}
	}

	[UIValue("sync-option")]
	private int SyncOption
	{
		get
		{
			return _syncOption;
		}
		set
		{
			_syncOption = value;
			NotifyPropertyChanged("SyncOption");
		}
	}

	[UIValue("gameplay-download")]
	private bool DownloadDuringGameplay
	{
		get
		{
			return _downloadDuringGameplay;
		}
		set
		{
			_downloadDuringGameplay = value;
			NotifyPropertyChanged("DownloadDuringGameplay");
		}
	}

	[UIValue("drive-protection")]
	private bool DriveFullProtection
	{
		get
		{
			return _driveFullProtection;
		}
		set
		{
			_driveFullProtection = value;
			NotifyPropertyChanged("DriveFullProtection");
		}
	}

	[UIValue("easter-eggs")]
	private bool EasterEggs
	{
		get
		{
			return _easterEggs;
		}
		set
		{
			_easterEggs = value;
			NotifyPropertyChanged("EasterEggs");
		}
	}

	[UIValue("soft-restart")]
	private bool SoftRestart
	{
		get
		{
			if (PlaylistHoverHints == PluginConfig.Instance.PlaylistHoverHints)
			{
				return FoldersDisabled != PluginConfig.Instance.FoldersDisabled;
			}
			return true;
		}
	}

	public event Action DismissFlowEvent;

	public event Action NameFetchRequestedEvent;

	[Inject]
	public void Construct(MenuTransitionsHelper menuTransitionsHelper)
	{
		this.menuTransitionsHelper = menuTransitionsHelper;
	}

	protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
	{
		base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
		DefaultImageDisabled = PluginConfig.Instance.DefaultImageDisabled;
		DefaultAllowDuplicates = PluginConfig.Instance.DefaultAllowDuplicates;
		AutomaticAuthorName = PluginConfig.Instance.AutomaticAuthorName;
		AuthorName = PluginConfig.Instance.AuthorName;
		PlaylistHoverHints = PluginConfig.Instance.PlaylistHoverHints;
		ShowDownloadIcon = PluginConfig.Instance.ShowDownloadIcon;
		BlurredArt = PluginConfig.Instance.BlurredArt;
		FoldersDisabled = PluginConfig.Instance.FoldersDisabled;
		SyncOption = (int)PluginConfig.Instance.SyncOption;
		DownloadDuringGameplay = PluginConfig.Instance.DownloadDuringGameplay;
		DriveFullProtection = PluginConfig.Instance.DriveFullProtection;
		EasterEggs = PluginConfig.Instance.EasterEggs;
	}

	[UIAction("cancel-click")]
	private void CancelClicked()
	{
		this.DismissFlowEvent?.Invoke();
	}

	[UIAction("ok-click")]
	private void OkClicked()
	{
		bool flag = !PluginConfig.Instance.AutomaticAuthorName && AutomaticAuthorName;
		bool softRestart = SoftRestart;
		PluginConfig.Instance.DefaultImageDisabled = DefaultImageDisabled;
		PluginConfig.Instance.DefaultAllowDuplicates = DefaultAllowDuplicates;
		PluginConfig.Instance.AutomaticAuthorName = AutomaticAuthorName;
		PluginConfig.Instance.AuthorName = AuthorName;
		PluginConfig.Instance.PlaylistHoverHints = PlaylistHoverHints;
		PluginConfig.Instance.ShowDownloadIcon = ShowDownloadIcon;
		PluginConfig.Instance.BlurredArt = BlurredArt;
		PluginConfig.Instance.FoldersDisabled = FoldersDisabled;
		PluginConfig.Instance.SyncOption = (PluginConfig.SyncOptions)SyncOption;
		PluginConfig.Instance.DownloadDuringGameplay = DownloadDuringGameplay;
		PluginConfig.Instance.DriveFullProtection = DriveFullProtection;
		PluginConfig.Instance.EasterEggs = EasterEggs;
		if (softRestart)
		{
			menuTransitionsHelper.RestartGame();
			return;
		}
		this.DismissFlowEvent?.Invoke();
		if (flag)
		{
			this.NameFetchRequestedEvent?.Invoke();
		}
	}

	[UIAction("sync-formatter")]
	private string PositionFormatter(int index)
	{
		PluginConfig.SyncOptions syncOptions = (PluginConfig.SyncOptions)index;
		return syncOptions.ToString();
	}
}
