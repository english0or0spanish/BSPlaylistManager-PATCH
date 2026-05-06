using System;
using System.Collections.Generic;
using System.IO;
using BeatSaberMarkupLanguage;
using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Types;
using PlaylistManager.Utilities;
using UnityEngine;

namespace PlaylistManager.Types;

public class CoverImage : IDeferredSpriteLoad
{
	private Sprite _sprite;

	private bool SpriteLoadQueued;

	private static readonly object _loaderLock = new object();

	private static bool CoroutineRunning = false;

	private static readonly Queue<Action> SpriteQueue = new Queue<Action>();

	public static YieldInstruction LoadWait = new WaitForEndOfFrame();

	public string Path { get; private set; }

	public bool SpriteWasLoaded { get; private set; }

	public bool Blacklist { get; private set; }

	public Sprite Sprite
	{
		get
		{
			if (_sprite == null)
			{
				if (!SpriteLoadQueued)
				{
					SpriteLoadQueued = true;
					QueueLoadSprite(this);
				}
				return BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;
			}
			return _sprite;
		}
	}

	public event EventHandler SpriteLoaded;

	public CoverImage(string path)
	{
		Path = path;
		SpriteWasLoaded = false;
		Blacklist = false;
		SpriteLoadQueued = false;
	}

	private static void QueueLoadSprite(CoverImage coverImage)
	{
		SpriteQueue.Enqueue(delegate
		{
			try
			{
				using FileStream fileStream = File.Open(coverImage.Path, FileMode.Open);
				byte[] array = new byte[fileStream.Length];
				fileStream.Read(array, 0, (int)fileStream.Length);
				coverImage._sprite = SpriteUtils.CreateSprite(array, 100f);
				if (coverImage._sprite != null)
				{
					coverImage.SpriteWasLoaded = true;
				}
				else
				{
					Plugin.Log.Critical("Could not load " + coverImage.Path);
					coverImage.SpriteWasLoaded = false;
					coverImage.Blacklist = true;
				}
				coverImage.SpriteLoaded?.Invoke(coverImage, null);
			}
			catch (Exception ex)
			{
				Plugin.Log.Critical("Could not load " + coverImage.Path + "\nException message: " + ex.Message);
				coverImage.SpriteWasLoaded = false;
				coverImage.Blacklist = true;
				coverImage.SpriteLoaded?.Invoke(coverImage, null);
			}
		});
		if (!CoroutineRunning)
		{
			SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
		}
	}

	private static IEnumerator<YieldInstruction> SpriteLoadCoroutine()
	{
		lock (_loaderLock)
		{
			if (CoroutineRunning)
			{
				yield break;
			}
			CoroutineRunning = true;
		}
		while (SpriteQueue.Count > 0)
		{
			yield return LoadWait;
			SpriteQueue.Dequeue()?.Invoke();
		}
		CoroutineRunning = false;
		if (SpriteQueue.Count > 0)
		{
			SharedCoroutineStarter.instance.StartCoroutine(SpriteLoadCoroutine());
		}
	}
}
