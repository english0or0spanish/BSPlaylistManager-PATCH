using PlaylistManager.Configuration;
using PlaylistManager.Utilities;
using Zenject;

namespace PlaylistManager.Installers;

internal class PlaylistManagerGameInstaller : Installer
{
	public override void InstallBindings()
	{
		if (!PluginConfig.Instance.DownloadDuringGameplay)
		{
			base.Container.BindInterfacesTo<DownloadPauser>().AsSingle();
		}
	}
}
