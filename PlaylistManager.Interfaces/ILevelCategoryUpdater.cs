namespace PlaylistManager.Interfaces;

internal interface ILevelCategoryUpdater
{
	void LevelCategoryUpdated(SelectLevelCategoryViewController.LevelCategory levelCategory, bool viewControllerActivated);
}
