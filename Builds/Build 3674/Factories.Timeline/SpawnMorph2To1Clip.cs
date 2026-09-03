using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Factories.Timeline;

[Serializable]
public class SpawnMorph2To1Clip : PlayableAsset, ITimelineClipAsset
{
	public SpawnMorph2To1Behavior template = new SpawnMorph2To1Behavior();

	public ClipCaps clipCaps => ClipCaps.Looping | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Blending;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		return ScriptPlayable<SpawnMorph2To1Behavior>.Create(graph, template);
	}
}
