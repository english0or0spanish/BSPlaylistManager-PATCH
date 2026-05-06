using PlaylistManager.AffinityPatches;
using PlaylistManager.Configuration;
using PlaylistManager.Managers;
using PlaylistManager.UI;
using Zenject;

namespace PlaylistManager.Installers;

internal class PlaylistManagerMenuInstaller : Installer
{
	public override void InstallBindings()
	{
		base.Container.BindInterfacesTo<LevelDetailButtonsViewController>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<AddPlaylistModalController>().AsSingle();
		base.Container.BindInterfacesTo<PlaylistDetailViewButtonsController>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistDetailsViewController>().AsSingle();
		base.Container.Bind<ImageSelectionModalController>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PopupModalsController>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistDownloaderViewController>().FromNewComponentOnNewGameObject().AsSingle();
		base.Container.BindInterfacesTo<PlaylistViewButtonsController>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistManagerFlowCoordinator>().FromNewComponentOnNewGameObject().AsSingle();
		base.Container.Bind<SettingsViewController>().FromNewComponentAsViewController().AsSingle();
		base.Container.Bind<ChangelogViewController>().FromNewComponentAsViewController().AsSingle();
		base.Container.BindInterfacesAndSelfTo<ContributorsViewController>().FromNewComponentAsViewController().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistUpdater>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<DifficultyHighlighter>().AsSingle();
		base.Container.BindInterfacesTo<RefreshButtonUI>().AsSingle();
		base.Container.BindInterfacesTo<LevelCollectionCellSetDataPatch>().AsSingle();
		base.Container.BindInterfacesTo<AnnotatedBeatmapLevelCollectionsUIPatches>().AsSingle();
		base.Container.BindInterfacesTo<PlaylistUIManager>().AsSingle();
		base.Container.BindInterfacesAndSelfTo<PlaylistDataManager>().AsSingle();
		if (PluginConfig.Instance.FoldersDisabled)
		{
			base.Container.BindInterfacesTo<AllPacksRefresher>().AsSingle();
		}
		else
		{
			base.Container.BindInterfacesAndSelfTo<FoldersViewController>().AsSingle();
		}
	}
}
