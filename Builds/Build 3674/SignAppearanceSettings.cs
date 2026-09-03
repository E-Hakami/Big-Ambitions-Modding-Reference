using System;
using Enums;
using UnityEngine;

[Serializable]
public class SignAppearanceSettings
{
	public SignType signType;

	public SerializableColor signLight;

	public SerializableColor lamp;

	[HideInInspector]
	[Obsolete("Use signLight")]
	public string signLightColor;

	[HideInInspector]
	[Obsolete("Use lamp")]
	public string lampColor;

	public SignAppearanceSettings()
	{
		signType = SignType.Type1;
		lamp = Color.white;
		signLight = Color.white;
	}
}
