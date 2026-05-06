using System;

namespace PlaylistManager.Types;

public class YesNoPopupContents : PopupContents
{
	public readonly Action yesButtonPressedCallback;

	public readonly string yesButtonText;

	public readonly Action noButtonPressedCallback;

	public readonly string noButtonText;

	public readonly string checkboxText;

	public YesNoPopupContents(string message, Action yesButtonPressedCallback, string yesButtonText = "Yes", string noButtonText = "No", Action noButtonPressedCallback = null, bool animateParentCanvas = true, string checkboxText = "")
		: base(message, animateParentCanvas)
	{
		this.yesButtonPressedCallback = yesButtonPressedCallback;
		this.yesButtonText = yesButtonText;
		this.noButtonPressedCallback = noButtonPressedCallback;
		this.noButtonText = noButtonText;
		this.checkboxText = checkboxText;
	}
}
