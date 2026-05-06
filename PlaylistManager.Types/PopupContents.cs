using UnityEngine;

namespace PlaylistManager.Types;

public abstract class PopupContents
{
	public Transform parent;

	public readonly string message;

	public bool animateParentCanvas;

	public PopupContents(string message, bool animateParentCanvas = true)
	{
		this.message = message;
		this.animateParentCanvas = animateParentCanvas;
	}
}
