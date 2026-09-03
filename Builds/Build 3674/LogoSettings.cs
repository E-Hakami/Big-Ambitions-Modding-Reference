using System;
using System.Runtime.Serialization;
using Enums;
using UnityEngine;

[Serializable]
public class LogoSettings
{
	public string logoShape;

	public FontFace font;

	public SerializableColor logoColor;

	public SerializableColor fontColor;

	public SerializableColor backgroundColor;

	[IgnoreDataMember]
	public Sprite logoSprite => LogoHelper.GetLogoSprite(logoShape);

	public LogoSettings()
	{
		backgroundColor = new Color(1f, 1f, 1f);
		fontColor = new Color(0f, 0f, 0f);
		logoColor = new Color(0f, 0f, 0f);
		font = FontFace.Exo2;
		logoShape = "";
	}

	public LogoSettings Clone()
	{
		return new LogoSettings
		{
			logoShape = logoShape,
			font = font,
			logoColor = logoColor,
			fontColor = fontColor,
			backgroundColor = backgroundColor
		};
	}
}
