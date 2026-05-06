using System;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using IPA.Logging;
using PlaylistManager.Configuration;
using PlaylistManager.Installers;
using SiraUtil.Web.SiraSync;
using SiraUtil.Zenject;

namespace PlaylistManager;

[Plugin(RuntimeOptions.SingleStartInit)]
public class Plugin
{
	private readonly PluginMetadata _metadata;

	private readonly Harmony _harmony;

	public const string HarmonyId = "com.github.rithik-b.PlaylistManager";

	internal static Plugin Instance { get; private set; }

	internal static Logger Log { get; private set; }

	[Init]
	public Plugin(Logger logger, PluginMetadata metadata, Zenjector zenjector)
	{
		Instance = this;
		Log = logger;
		_metadata = metadata;
		_harmony = new Harmony("com.github.rithik-b.PlaylistManager");
		zenjector.UseLogger(logger);
		zenjector.UseMetadataBinder<Plugin>();
		zenjector.UseHttpService();
		zenjector.UseSiraSync(SiraSyncServiceType.GitHub, "rithik-b");
		zenjector.Install<PlaylistManagerAppInstaller>(Location.App, Array.Empty<object>());
		zenjector.Install<PlaylistManagerMenuInstaller>(Location.Menu, Array.Empty<object>());
		zenjector.Install<PlaylistManagerGameInstaller>(Location.GameCore, Array.Empty<object>());
	}

	[Init]
	public void InitWithConfig(Config conf)
	{
		PluginConfig.Instance = GeneratedStore.Generated<PluginConfig>(conf, true);
		Log.Debug("Config loaded");
	}

	[OnEnable]
	public void OnEnable()
	{
		_harmony.PatchAll(_metadata.Assembly);
	}

	[OnDisable]
	public void OnDisable()
	{
		_harmony.UnpatchSelf();
	}
}
