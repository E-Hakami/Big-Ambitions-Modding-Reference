using System;
using UnityEngine;

[Serializable]
public class VideoClipData
{
	[Serializable]
	public enum VideoType
	{
		Work,
		TV,
		Game,
		Cinema
	}

	public bool random;

	public VideoType type;

	public Texture2D[] clip;

	public Vector2 speed;
}
