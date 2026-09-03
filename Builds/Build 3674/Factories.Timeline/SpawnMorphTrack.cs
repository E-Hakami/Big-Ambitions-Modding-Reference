using UnityEngine.Timeline;

namespace Factories.Timeline;

[TrackColor(0.2f, 1f, 0.2f)]
[TrackBindingType(typeof(SpawnMorphPlayerData))]
[TrackClipType(typeof(SpawnMorphClip))]
[TrackClipType(typeof(SpawnMorph2To1Clip))]
public class SpawnMorphTrack : TrackAsset
{
}
