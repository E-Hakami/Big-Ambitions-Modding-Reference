using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class CemeteryFuneralSetup : MonoBehaviour
{
	[SerializeField]
	private PlayableDirector funeralCutScene;

	[SerializeField]
	private string targetTrack;

	public float Duration => (float)funeralCutScene.duration;

	public bool CanPlay()
	{
		TimelineAsset timelineAsset = funeralCutScene.playableAsset as TimelineAsset;
		TrackAsset trackAsset;
		return TryGetTrackAsset(timelineAsset, out trackAsset);
	}

	public bool TryPlay(Object cameraBinding)
	{
		if (!CanPlay())
		{
			return false;
		}
		TimelineAsset timelineAsset = funeralCutScene.playableAsset as TimelineAsset;
		TryGetTrackAsset(timelineAsset, out var trackAsset);
		funeralCutScene.SetGenericBinding(trackAsset, cameraBinding);
		funeralCutScene.Play();
		return true;
	}

	public void Pause()
	{
		funeralCutScene.Pause();
	}

	private bool TryGetTrackAsset(TimelineAsset timelineAsset, out TrackAsset trackAsset)
	{
		trackAsset = null;
		foreach (TrackAsset outputTrack in timelineAsset.GetOutputTracks())
		{
			if (!(outputTrack.name != targetTrack))
			{
				trackAsset = outputTrack;
				return true;
			}
		}
		return false;
	}
}
