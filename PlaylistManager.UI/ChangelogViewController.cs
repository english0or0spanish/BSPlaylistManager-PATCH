using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using SiraUtil.Web.SiraSync;
using Zenject;

namespace PlaylistManager.UI;

[ViewDefinition("PlaylistManager.UI.Views.ChangelogView.bsml")]
internal class ChangelogViewController : BSMLAutomaticViewController
{
	private ISiraSyncService siraSyncService;

	private string _changelog;

	[UIValue("is-loading")]
	private bool IsLoading => string.IsNullOrEmpty(Changelog);

	[UIValue("loaded")]
	private bool Loaded => !string.IsNullOrEmpty(Changelog);

	[UIValue("changelog")]
	private string Changelog
	{
		get
		{
			return _changelog;
		}
		set
		{
			_changelog = value;
			NotifyPropertyChanged("Changelog");
			NotifyPropertyChanged("IsLoading");
			NotifyPropertyChanged("Loaded");
		}
	}

	[Inject]
	public void Construct(ISiraSyncService siraSyncService)
	{
		this.siraSyncService = siraSyncService;
	}

	[UIAction("#post-parse")]
	private async void PostParse()
	{
		string rawChangelog = await siraSyncService.LatestChangelog();
		Changelog = await Task.Run(() => MarkdownParse(rawChangelog));
	}

	private string MarkdownParse(string original)
	{
		original = Regex.Replace(original, "!\\[.*\\]\\(.*\\)\\r\\n", "");
		original = Regex.Replace(original, "!\\[.*\\]\\(.*\\)", "");
		original = Regex.Replace(original, "(\\[)(.*)(\\]\\(.*\\))", "$2");
		original = original.Replace("\n", "\n\n");
		original = Regex.Replace(original, "(### )(.*)", "<size=5.75>$2</size>\n");
		original = Regex.Replace(original, "(## )(.*)", "<size=6>$2</size>\n<color=#ffffff80>________________________________________________________</color>");
		original = Regex.Replace(original, "(# )(.*)", "<size=7>$2</size>\n<color=#ffffff80>________________________________________________________</color>");
		return original;
	}
}
