using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Factories.Timeline;

[Serializable]
public class SpawnMorphClip : PlayableAsset, ITimelineClipAsset
{
	public SpawnMorphBehavior template = new SpawnMorphBehavior();

	public ClipCaps clipCaps => ClipCaps.Looping | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Blending;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		return ScriptPlayable<SpawnMorphBehavior>.Create(graph, template);
	}
}
