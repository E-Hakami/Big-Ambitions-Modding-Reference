using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Factories.Timeline;

public static class FactoryTimelineHelper
{
	public static void SetSpawnMorphTrackItems(this SpawnMorphPlayerData data, string startItem)
	{
		data.startItem = startItem;
		data.endItem = startItem;
		data.onItemsChanged?.Invoke();
	}

	public static void SetSpawnMorphTrackItems(this SpawnMorphPlayerData data, string startItem, string endItem)
	{
		data.startItem = startItem;
		data.endItem = endItem;
		data.onItemsChanged?.Invoke();
	}

	public static void SetSpawnMorphTrackItems(this SpawnMorphPlayerData data, string startItemA, string startItemB, string endItem)
	{
		data.startItem = startItemA;
		data.secondaryStartItem = startItemB;
		data.endItem = endItem;
		data.onItemsChanged?.Invoke();
	}

	private static T GetTrack<T>(this PlayableDirector director) where T : TrackAsset
	{
		if (director.playableAsset is TimelineAsset timelineAsset)
		{
			foreach (TrackAsset outputTrack in timelineAsset.GetOutputTracks())
			{
				if (outputTrack is T result)
				{
					return result;
				}
			}
			Debug.LogError("No track of type " + typeof(T).Name + " found in the timeline.");
		}
		else
		{
			Debug.LogError("PlayableDirector does not have a valid TimelineAsset.");
		}
		return null;
	}
}
