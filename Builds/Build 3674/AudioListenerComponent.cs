using UnityEngine;

public class AudioListenerComponent : MonoBehaviour
{
	private void Awake()
	{
		EnsureItsTheOnlyAudioListener();
	}

	private void EnsureItsTheOnlyAudioListener()
	{
		AudioListener component = GetComponent<AudioListener>();
		AudioListener[] array = Object.FindObjectsOfType<AudioListener>();
		foreach (AudioListener audioListener in array)
		{
			if (component != audioListener)
			{
				Object.Destroy(audioListener);
			}
		}
	}
}
