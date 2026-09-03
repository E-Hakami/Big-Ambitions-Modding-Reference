using UnityEngine;

public struct CorrespondingSongData(AudioClip audioClip, float songTime)
{
	public AudioClip audioClip = audioClip;

	public float songTime = songTime;
}
