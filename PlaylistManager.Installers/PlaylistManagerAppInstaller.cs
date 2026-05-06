using PlaylistManager.Downloaders;
using PlaylistManager.Utilities;
using Zenject;

namespace PlaylistManager.Installers;

internal class PlaylistManagerAppInstaller : Installer
{
	public override void InstallBindings()
	{
		base.Container.BindInterfacesAndSelfTo<PlaylistDownloader>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistSequentialDownloader>().AsSingle();
	}
}
