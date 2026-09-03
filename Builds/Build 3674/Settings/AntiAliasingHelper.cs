using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Settings;

public static class AntiAliasingHelper
{
	public const AntiAliasingSetting DefaultAntiAliasingSetting = AntiAliasingSetting.Smaa4X;

	public const AntiAliasingSetting LowQualityAntiAliasingSetting = AntiAliasingSetting.Smaa1X;

	public static AntiAliasingSetting GetPlayerAntiAliasingSetting()
	{
		if (!PlayerPrefs.HasKey(PlayerPref.antiAliasingSetting))
		{
			return AntiAliasingSetting.Smaa4X;
		}
		if (Enum.IsDefined(typeof(AntiAliasingSetting), PlayerPrefSettings.antiAliasingSetting))
		{
			return (AntiAliasingSetting)PlayerPrefSettings.antiAliasingSetting;
		}
		return AntiAliasingSetting.Smaa4X;
	}

	public static void SetAntiAliasingSetting(AntiAliasingSetting setting)
	{
		HDAdditionalCameraData component = Camera.main.GetComponent<HDAdditionalCameraData>();
		switch (setting)
		{
		case AntiAliasingSetting.None:
			component.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
			break;
		case AntiAliasingSetting.Fxaa:
			component.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing;
			break;
		case AntiAliasingSetting.Smaa1X:
			component.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
			component.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Low;
			break;
		case AntiAliasingSetting.Smaa2X:
			component.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
			component.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.Medium;
			break;
		case AntiAliasingSetting.Smaa4X:
			component.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
			component.SMAAQuality = HDAdditionalCameraData.SMAAQualityLevel.High;
			break;
		}
	}
}
