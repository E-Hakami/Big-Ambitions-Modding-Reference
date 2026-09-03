using BigAmbitions.Characters;
using UnityEngine;

namespace Buildings.BuildingTypes.Retail.Businesses.CinemaTheater;

public class ActorEmployeeAnimationEvents : MonoBehaviour
{
	private const float Volume = 0.7f;

	private const float PitchVariation = 0.1f;

	private AudioSource _lastAudioSource;

	public void PlaySoundFromSet(AnimationEvent animationEvent)
	{
		if (animationEvent.objectReferenceParameter is ActorEmployeeSoundsSet actorEmployeeSoundsSet)
		{
			AudioClip[] array = ((GetComponentInParent<AppearanceSetter>().data.gender == Gender.Female) ? actorEmployeeSoundsSet.femaleClips : actorEmployeeSoundsSet.maleClips);
			if (array != null && array.Length != 0)
			{
				AudioClip clip = array[Random.Range(0, array.Length)];
				float pitch = 1f + Random.Range(-0.1f, 0.1f);
				_lastAudioSource = InstanceBehavior<SfxManager>.Instance.PlayAudio(clip, base.transform.position, 0.7f, pitch, 1f, isPlayerCreatedSound: false, InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup);
			}
		}
	}

	public void StopSound()
	{
		if ((bool)_lastAudioSource)
		{
			_lastAudioSource.Stop();
		}
	}
}
