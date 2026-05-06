using System;
using System.Collections.Generic;

namespace PlaylistManager.Interfaces;

internal interface ILevelCollectionsTableUpdater
{
	event Action<IReadOnlyList<BeatmapLevelPack>, int> LevelCollectionTableViewUpdatedEvent;
}
