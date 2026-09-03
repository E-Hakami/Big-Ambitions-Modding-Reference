using UnityEngine;
using UnityEngine.Timeline;

namespace Factories.Timeline;

[TrackColor(0.855f, 0.447f, 0.776f)]
[TrackClipType(typeof(ShaderFloatClip))]
[TrackBindingType(typeof(Renderer))]
public class ShaderTrack : TrackAsset
{
}
