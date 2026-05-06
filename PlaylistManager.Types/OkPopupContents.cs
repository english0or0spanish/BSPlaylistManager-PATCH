using System;

namespace PlaylistManager.Types;

public class OkPopupContents : PopupContents
{
	public readonly Action buttonPressedCallback;

	public readonly string okButtonText;

	public OkPopupContents(string message, Action buttonPressedCallback, string okButtonText = "Ok", bool animateParentCanvas = true)
		: base(message, animateParentCanvas)
	{
		this.buttonPressedCallback = buttonPressedCallback;
		this.okButtonText = okButtonText;
	}
}
