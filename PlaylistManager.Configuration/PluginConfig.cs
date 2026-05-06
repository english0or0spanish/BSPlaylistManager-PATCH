using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

namespace PlaylistManager.Configuration;

internal class PluginConfig
{
	public enum SyncOptions
	{
		Off,
		On,
		Ask
	}

	public static PluginConfig Instance { get; set; }

	public virtual string AuthorName { get; set; } = "PlaylistManager";

	public virtual bool AutomaticAuthorName { get; set; } = true;

	public virtual bool DefaultImageDisabled { get; set; }

	public virtual bool FoldersDisabled { get; set; }

	public virtual bool DefaultAllowDuplicates { get; set; } = true;

	public virtual bool PlaylistHoverHints { get; set; } = true;

	public virtual bool BlurredArt { get; set; } = true;

	public virtual bool HighlightDifficulty { get; set; } = true;

	public virtual bool EasterEggs { get; set; }

	public virtual bool DownloadDuringGameplay { get; set; }

	public virtual bool DriveFullProtection { get; set; } = true;

	public virtual bool ShowDownloadIcon { get; set; } = true;

	[UseConverter(typeof(EnumConverter<SyncOptions>))]
	[NonNullable]
	public virtual SyncOptions SyncOption { get; set; } = SyncOptions.On;

	public virtual void Changed()
	{
	}

	public virtual void CopyFrom(PluginConfig other)
	{
	}
}
