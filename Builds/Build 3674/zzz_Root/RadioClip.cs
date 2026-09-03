using System;
using UnityEngine;

[Serializable]
public class RadioClip
{
	public AudioClip clip;

	private string _name;

	[HideInInspector]
	public string path;

	[HideInInspector]
	public AudioType type;

	public string Name
	{
		get
		{
			if (string.IsNullOrEmpty(_name))
			{
				return clip.name;
			}
			return _name;
		}
		set
		{
			_name = value;
		}
	}

	public bool HasLoadFailed { get; set; }

	public bool IsCaching { get; set; }

	public float GetLength
	{
		get
		{
			if (!(clip == null))
			{
				return clip.length;
			}
			return 1f;
		}
	}
}
