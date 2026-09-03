using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Factories.Timeline;

[Serializable]
public class ShaderFloatClip : PlayableAsset, ITimelineClipAsset
{
	public ShaderFloatBehavior template = new ShaderFloatBehavior();

	public ClipCaps clipCaps => ClipCaps.Looping | ClipCaps.ClipIn | ClipCaps.SpeedMultiplier | ClipCaps.Blending;

	public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
	{
		return ScriptPlayable<ShaderFloatBehavior>.Create(graph, template);
	}
}
