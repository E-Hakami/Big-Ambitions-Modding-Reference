using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

public readonly struct ContactIconData
{
	public Color Tint { get; }

	public Sprite Sprite { get; }

	public ContactIconData(Color tint, Sprite sprite)
	{
		Tint = tint;
		Sprite = sprite;
	}
}
