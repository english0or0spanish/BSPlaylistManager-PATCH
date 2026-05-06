using System;
using System.ComponentModel;
using System.Threading;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using HMUI;
using SongCore.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace PlaylistManager.Types;

public class DownloadQueueEntry : INotifyPropertyChanged, IProgress<double>, IProgress<float>
{
	public readonly IPlaylist playlist;

	public readonly BeatSaberPlaylistsLib.PlaylistManager parentManager;

	internal CancellationTokenSource cancellationTokenSource;

	private ImageView bgImage;

	[UIComponent("playlist-cover")]
	private readonly ImageView playlistCoverView = null!;

	private double progress;

	private int completedLevels;

	private int? missingLevels;

	public bool Aborted { get; private set; }

	[UIValue("playlist-name")]
	public string PlaylistName => playlist?.Title ?? "";

	[UIValue("playlist-subtext")]
	public string PlaylistSubtext => playlist?.Author + (missingLevels.HasValue ? $" [{completedLevels}/{missingLevels} downloaded]" : " [Download Queued]");

	public double Progress
	{
		get
		{
			return progress;
		}
		private set
		{
			progress = value;
			if (bgImage != null)
			{
				Color color = HSBColor.ToColor(new HSBColor(Mathf.PingPong((float)(Progress * 0.3499999940395355), 1f), 1f, 1f));
				color.a = 0.35f;
				bgImage.color = color;
				bgImage.fillAmount = (float)Progress;
			}
		}
	}

	public event Action<DownloadQueueEntry> DownloadAbortedEvent;

	public event PropertyChangedEventHandler PropertyChanged;

	public DownloadQueueEntry(IPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
	{
		this.playlist = playlist;
		this.parentManager = parentManager;
		cancellationTokenSource = new CancellationTokenSource();
		Aborted = false;
	}

	[UIAction("#post-parse")]
	private void PostParse()
	{
		if (!(playlistCoverView == null))
		{
			AspectRatioFitter aspectRatioFitter = playlistCoverView.gameObject.AddComponent<AspectRatioFitter>();
			aspectRatioFitter.aspectRatio = 1f;
			aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
			IDeferredSpriteLoad deferredSpriteLoad = playlist;
			if (deferredSpriteLoad != null && !deferredSpriteLoad.SpriteWasLoaded)
			{
				deferredSpriteLoad.SpriteLoaded += OnSpriteLoad;
			}
			playlistCoverView.sprite = playlist.Sprite;
			playlistCoverView.rectTransform.sizeDelta = new Vector2(8f, 0f);
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistName"));
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistSubtext"));
			bgImage = playlistCoverView.transform.parent.gameObject.AddComponent<ImageView>();
			bgImage.enabled = true;
			bgImage.sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0f, 0f, 1f, 1f), Vector2.one / 2f);
			bgImage.type = Image.Type.Filled;
			bgImage.fillMethod = Image.FillMethod.Horizontal;
			bgImage.fillAmount = 0f;
			bgImage.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
			Progress = progress;
		}
	}

	private void OnSpriteLoad(object sender, EventArgs e)
	{
		if (sender is IDeferredSpriteLoad deferredSpriteLoad)
		{
			deferredSpriteLoad.SpriteLoaded -= OnSpriteLoad;
			playlistCoverView.sprite = deferredSpriteLoad.Sprite;
		}
	}

	[UIAction("abort-clicked")]
	public void AbortDownload()
	{
		cancellationTokenSource.Cancel();
		this.DownloadAbortedEvent?.Invoke(this);
		Aborted = true;
	}

	public void Report(double value)
	{
		Progress = (missingLevels.HasValue ? ((((double)completedLevels / (double?)missingLevels) ?? 1.0) + ((value / (double?)missingLevels) ?? 1.0)) : 0.0);
	}

	public void Report(float value)
	{
		Report((double)value);
	}

	internal void SetMissingLevels(int value)
	{
		missingLevels = value;
		completedLevels = 0;
		Progress = 0.0;
	}

	internal void SetTotalProgress(int value)
	{
		completedLevels = value;
		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlaylistSubtext"));
	}
}
