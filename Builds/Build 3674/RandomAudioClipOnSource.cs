using Extensions;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomAudioClipOnSource : MonoBehaviour
{
	[SerializeField]
	private AudioSource source;

	[SerializeField]
	private bool playOnAwake = true;

	[SerializeField]
	private AudioClip[] randomClips;

	public void Start()
	{
		if (source == null)
		{
			Debug.LogError("RandomAudioClipOnSource on " + base.name + " has no audio source attached", base.gameObject);
		}
		else if (playOnAwake)
		{
			PlayRandomSound();
		}
	}

	public void PlayRandomSound()
	{
		source.clip = randomClips.GetRandom();
		source.Play();
	}
}
