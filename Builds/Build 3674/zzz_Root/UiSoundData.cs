using System;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

[Serializable]
public class UiSoundData
{
	[SearchableEnum]
	public UiSound type;

	public AudioClip[] clips;

	public float volume = 1f;

	[MinMaxSlider(0f, 2f)]
	public Vector2 pitchRange = new Vector2(0.8f, 1.2f);
}
