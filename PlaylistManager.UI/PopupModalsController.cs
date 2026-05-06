using System;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using IPA.Loader;
using PlaylistManager.Types;
using PlaylistManager.Utilities;
using SiraUtil.Zenject;
using UnityEngine;

namespace PlaylistManager.UI;

public class PopupModalsController : INotifyPropertyChanged
{
	private readonly MainMenuViewController mainMenuViewController;

	private readonly PluginMetadata pluginMetadata;

	private readonly BSMLParser bsmlParser;

	private bool parsed;

	private Action yesButtonPressed;

	private Action noButtonPressed;

	private Action okButtonPressed;

	private Action<string> keyboardPressed;

	private string _yesNoText = "";

	private string _checkboxText = "";

	private string _yesButtonText = "Yes";

	private string _noButtonText = "No";

	private bool _checkboxValue;

	private bool _checkboxActive;

	private string _okText = "";

	private string _okButtonText = "Ok";

	private string _loadingText = "";

	private string _keyboardText = "";

	[UIComponent("root")]
	private readonly RectTransform rootTransform = null!;

	[UIComponent("yes-no-modal")]
	private readonly RectTransform yesNoModalTransform = null!;

	[UIComponent("yes-no-modal")]
	private ModalView yesNoModalView = null!;

	private Vector3 yesNoModalPosition;

	[UIComponent("ok-modal")]
	private readonly RectTransform okModalTransform = null!;

	[UIComponent("ok-modal")]
	private ModalView okModalView = null!;

	private Vector3 okModalPosition;

	[UIComponent("loading-modal")]
	private readonly RectTransform loadingModalTransform = null!;

	[UIComponent("loading-modal")]
	private ModalView loadingModalView = null!;

	private Vector3 loadingModalPosition;

	[UIComponent("keyboard")]
	private readonly RectTransform keyboardTransform = null!;

	[UIComponent("keyboard")]
	private ModalView keyboardModalView = null!;

	[UIParams]
	private readonly BSMLParserParams parserParams;

	[UIValue("yes-no-text")]
	private string YesNoText
	{
		get
		{
			return _yesNoText;
		}
		set
		{
			_yesNoText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YesNoText"));
		}
	}

	[UIValue("yes-button-text")]
	private string YesButtonText
	{
		get
		{
			return _yesButtonText;
		}
		set
		{
			_yesButtonText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("YesButtonText"));
		}
	}

	[UIValue("no-button-text")]
	private string NoButtonText
	{
		get
		{
			return _noButtonText;
		}
		set
		{
			_noButtonText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("NoButtonText"));
		}
	}

	[UIValue("checkbox-text")]
	private string CheckboxText
	{
		get
		{
			return _checkboxText;
		}
		set
		{
			_checkboxText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckboxText"));
		}
	}

	[UIValue("checkbox-active")]
	private bool CheckboxActive
	{
		get
		{
			return _checkboxActive;
		}
		set
		{
			_checkboxActive = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CheckboxActive"));
		}
	}

	[UIValue("checkbox")]
	private string Checkbox
	{
		get
		{
			if (!CheckboxValue)
			{
				return "⬜";
			}
			return "☑";
		}
	}

	public bool CheckboxValue
	{
		get
		{
			return _checkboxValue;
		}
		private set
		{
			_checkboxValue = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Checkbox"));
		}
	}

	[UIValue("ok-text")]
	internal string OkText
	{
		get
		{
			return _okText;
		}
		set
		{
			_okText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OkText"));
		}
	}

	[UIValue("ok-button-text")]
	internal string OkButtonText
	{
		get
		{
			return _okButtonText;
		}
		set
		{
			_okButtonText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OkButtonText"));
		}
	}

	[UIValue("loading-text")]
	private string LoadingText
	{
		get
		{
			return _loadingText;
		}
		set
		{
			_loadingText = value;
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LoadingText"));
		}
	}

	[UIValue("keyboard-text")]
	private string KeyboardText
	{
		get
		{
			return _keyboardText;
		}
		set
		{
			_keyboardText = value;
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public PopupModalsController(MainMenuViewController mainMenuViewController, UBinder<Plugin, PluginMetadata> pluginMetadata, BSMLParser bsmlParser)
	{
		this.mainMenuViewController = mainMenuViewController;
		this.pluginMetadata = pluginMetadata.Value;
		this.bsmlParser = bsmlParser;
	}

	private void Parse()
	{
		if (!parsed)
		{
			bsmlParser.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(pluginMetadata.Assembly, "PlaylistManager.UI.Views.PopupModals.bsml"), mainMenuViewController.gameObject, this);
			yesNoModalPosition = yesNoModalTransform.localPosition;
			okModalPosition = okModalTransform.localPosition;
			loadingModalPosition = loadingModalTransform.localPosition;
			parsed = true;
		}
	}

	internal void ShowModal(PopupContents popupContents)
	{
		if (popupContents is OkPopupContents popupContents2)
		{
			ShowOkModal(popupContents2);
		}
		else if (popupContents is YesNoPopupContents popupContents3)
		{
			ShowYesNoModal(popupContents3);
		}
	}

	private void ShowYesNoModal(YesNoPopupContents popupContents)
	{
		ShowYesNoModal(popupContents.parent, popupContents.message, popupContents.yesButtonPressedCallback, popupContents.yesButtonText, popupContents.noButtonText, popupContents.noButtonPressedCallback, popupContents.animateParentCanvas, popupContents.checkboxText);
	}

	internal void ShowYesNoModal(Transform parent, string text, Action yesButtonPressedCallback, string yesButtonText = "Yes", string noButtonText = "No", Action noButtonPressedCallback = null, bool animateParentCanvas = true, string checkboxText = "")
	{
		Parse();
		yesNoModalTransform.localPosition = yesNoModalPosition;
		yesNoModalTransform.transform.SetParent(parent);
		YesNoText = text;
		YesButtonText = yesButtonText;
		NoButtonText = noButtonText;
		yesButtonPressed = yesButtonPressedCallback;
		noButtonPressed = noButtonPressedCallback;
		CheckboxText = checkboxText;
		CheckboxValue = false;
		CheckboxActive = !string.IsNullOrEmpty(checkboxText);
		yesNoModalView.SetPrivateField("_animateParentCanvas", animateParentCanvas);
		yesNoModalView.SetPrivateField("_viewIsValid", false);
		parserParams.EmitEvent("close-yes-no");
		parserParams.EmitEvent("open-yes-no");
	}

	internal void HideYesNoModal()
	{
		parserParams.EmitEvent("close-yes-no");
	}

	[UIAction("yes-button-pressed")]
	private void YesButtonPressed()
	{
		yesButtonPressed?.Invoke();
		yesButtonPressed = null;
	}

	[UIAction("no-button-pressed")]
	private void NoButtonPressed()
	{
		noButtonPressed?.Invoke();
		noButtonPressed = null;
	}

	[UIAction("toggle-checkbox")]
	private void ToggleCheckbox()
	{
		CheckboxValue = !CheckboxValue;
	}

	private void ShowOkModal(OkPopupContents popupContents)
	{
		ShowOkModal(popupContents.parent, popupContents.message, popupContents.buttonPressedCallback, popupContents.okButtonText, popupContents.animateParentCanvas);
	}

	internal void ShowOkModal(Transform parent, string text, Action buttonPressedCallback, string okButtonText = "Ok", bool animateParentCanvas = true)
	{
		Parse();
		okModalTransform.localPosition = okModalPosition;
		okModalTransform.transform.SetParent(parent);
		OkText = text;
		OkButtonText = okButtonText;
		okButtonPressed = buttonPressedCallback;
		okModalView.SetPrivateField("_animateParentCanvas", animateParentCanvas);
		yesNoModalView.SetPrivateField("_viewIsValid", false);
		parserParams.EmitEvent("close-ok");
		parserParams.EmitEvent("open-ok");
	}

	[UIAction("ok-button-pressed")]
	private void OkButtonPressed()
	{
		okButtonPressed?.Invoke();
		okButtonPressed = null;
	}

	internal void ShowLoadingModal(Transform parent, string text, bool animateParentCanvas = true)
	{
		Parse();
		loadingModalTransform.localPosition = loadingModalPosition;
		loadingModalTransform.SetParent(parent);
		LoadingText = text;
		okModalView.SetPrivateField("_animateParentCanvas", animateParentCanvas);
		yesNoModalView.SetPrivateField("_viewIsValid", false);
		parserParams.EmitEvent("close-loading");
		parserParams.EmitEvent("open-loading");
	}

	internal void DismissLoadingModal()
	{
		parserParams.EmitEvent("close-loading");
	}

	internal void ShowKeyboard(Transform parent, Action<string> keyboardPressedCallback, string keyboardText = "", bool animateParentCanvas = true)
	{
		Parse();
		keyboardTransform.transform.SetParent(parent);
		KeyboardText = keyboardText;
		keyboardPressed = keyboardPressedCallback;
		keyboardModalView.SetPrivateField("_animateParentCanvas", animateParentCanvas);
		yesNoModalView.SetPrivateField("_viewIsValid", false);
		parserParams.EmitEvent("close-keyboard");
		parserParams.EmitEvent("open-keyboard");
	}

	[UIAction("keyboard-enter")]
	private void KeyboardEnter(string keyboardText)
	{
		keyboardPressed?.Invoke(keyboardText);
		keyboardPressed = null;
	}
}
