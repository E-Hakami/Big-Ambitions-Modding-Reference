using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Enums;
using Extensions;
using Helpers;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class LogoHelper
{
	public static readonly List<string> AvailableIcons = GetAvailableIcons();

	private static readonly bool SkipLoadingBusinessLogos = Environment.GetCommandLineArgs().Contains("-skipLoadingBusinessLogos");

	public static readonly Dictionary<string, Sprite> LogoShapeSprites = new Dictionary<string, Sprite>();

	public static readonly Dictionary<(string businessName, LogoSize logoSize, bool isPlayerBusiness), Texture2D> BusinessLogoTextures = new Dictionary<(string, LogoSize, bool), Texture2D>();

	public const string NullTexture = "notfound";

	public static Texture2D nullTexture;

	private static List<string> GetAvailableIcons()
	{
		try
		{
			return Directory.GetFiles(GetBuildInIconsFolder(), "*.png").Select(Path.GetFileNameWithoutExtension).ToList();
		}
		catch (Exception arg)
		{
			Debug.Log($"No access to the Logo Shapes folder:\n{arg}");
			return null;
		}
	}

	public static string GetBuildInIconsFolder()
	{
		return Path.Combine(Application.streamingAssetsPath, "LogoShapes");
	}

	public static string GetModdedBusinessLogosFolder()
	{
		return Path.Combine(Application.streamingAssetsPath, "BusinessLogos");
	}

	public static string GetPlayerBusinessLogoPath(string businessName)
	{
		if (!string.IsNullOrEmpty(businessName))
		{
			return Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId), GetBusinessNamePathSafe(businessName));
		}
		return null;
	}

	public static string GetBusinessNamePathSafe(string businessName)
	{
		string text = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
		string pattern = "[" + text + ".]+";
		return Regex.Replace(businessName, pattern, "");
	}

	public static string GetCustomIconsFolderPath()
	{
		return Path.Combine(Application.persistentDataPath, "CustomIcons");
	}

	public static string GetCustomIconPath(string iconName)
	{
		return Path.Combine(GetCustomIconsFolderPath(), iconName + ".png");
	}

	public static void RemoveCustomIcon(string iconName)
	{
		File.Delete(GetCustomIconPath(iconName));
		if (LogoShapeSprites.ContainsKey(iconName))
		{
			Sprite sprite = LogoShapeSprites[iconName];
			UnityEngine.Object.Destroy(sprite.texture);
			UnityEngine.Object.Destroy(sprite);
			LogoShapeSprites.Remove(iconName);
		}
	}

	public static LogoSettings GenerateLogoSetting(string businessTypeName)
	{
		return new LogoSettings
		{
			backgroundColor = Colors.White,
			fontColor = Colors.Black,
			logoColor = Colors.Black,
			font = FontFace.Rubik,
			logoShape = BusinessTypeHelper.GetData(businessTypeName).logoShapes.GetRandom()
		};
	}

	public static Sprite GetLogoSprite(string logoShape)
	{
		if (AvailableIcons == null)
		{
			return null;
		}
		if (logoShape == null)
		{
			return GetLogoSprite("notfound");
		}
		if (!LogoShapeSprites.ContainsKey(logoShape))
		{
			Texture2D logoShapeTexture = GetLogoShapeTexture(AvailableIcons.Contains(logoShape) ? Path.Combine(GetBuildInIconsFolder(), logoShape + ".png") : GetCustomIconPath(logoShape));
			if (!(logoShapeTexture != null))
			{
				if (logoShape != "")
				{
					Debug.LogWarning("Custom Logo Shape '" + logoShape + "' Could not be loaded");
				}
				return GetLogoSprite("notfound");
			}
			TryToCompress(logoShapeTexture);
			LogoShapeSprites[logoShape] = Sprite.Create(logoShapeTexture, new Rect(0f, 0f, logoShapeTexture.width, logoShapeTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		}
		LogoShapeSprites[logoShape].name = logoShape;
		return LogoShapeSprites[logoShape];
	}

	public static Texture2D GetLogoShapeTexture(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}
		Texture2D obj = new Texture2D(2, 2)
		{
			hideFlags = HideFlags.HideAndDontSave,
			name = path
		};
		obj.LoadImage(File.ReadAllBytes(path));
		obj.wrapMode = TextureWrapMode.Clamp;
		return obj;
	}

	public static Sprite GetDefaultLogoSprite()
	{
		List<string> availableIcons = AvailableIcons;
		if (availableIcons == null || availableIcons.Count != 0)
		{
			return GetLogoSprite("notfound");
		}
		return null;
	}

	public static Texture2D GetBusinessLogoTexture(string businessName, LogoSize logoSize, bool playerBusiness = false)
	{
		if (SkipLoadingBusinessLogos || string.IsNullOrEmpty(businessName))
		{
			return GetNullTexture();
		}
		if (!BusinessLogoTextures.ContainsKey((businessName, logoSize, playerBusiness)))
		{
			string path = (playerBusiness ? GetPlayerBusinessLogoPath(businessName) : Path.Combine(GetModdedBusinessLogosFolder(), GetBusinessNamePathSafe(businessName)));
			path = Path.Combine(path, logoSize.ToStringFast() + ".jpg");
			if (File.Exists(path))
			{
				Texture2D texture2D = new Texture2D(2, 2)
				{
					hideFlags = HideFlags.HideAndDontSave,
					name = businessName
				};
				try
				{
					if (!texture2D.LoadImage(File.ReadAllBytes(path)))
					{
						UnityEngine.Object.Destroy(texture2D);
						return playerBusiness ? null : GetNullTexture();
					}
					TryToCompress(texture2D);
					BusinessLogoTextures[(businessName, logoSize, playerBusiness)] = texture2D;
				}
				catch (IOException arg)
				{
					UnityEngine.Object.Destroy(texture2D);
					Debug.LogWarning($"No access to the Logo Shapes folder:\n{arg}");
					return GetNullTexture();
				}
			}
			else
			{
				if (playerBusiness)
				{
					return null;
				}
				string text = "BusinessLogos/" + GetBusinessNamePathSafe(businessName) + "/" + logoSize.ToStringFast();
				Texture2D texture2D2 = null;
				string key = text + ".jpg";
				if (AddressableChecksHelper.IsValidAddressableKey(key))
				{
					texture2D2 = Addressables.LoadAssetAsync<Texture2D>(key).WaitForCompletion();
				}
				if (texture2D2 == null)
				{
					key = text + ".png";
					if (AddressableChecksHelper.IsValidAddressableKey(key))
					{
						texture2D2 = Addressables.LoadAssetAsync<Texture2D>(key).WaitForCompletion();
					}
				}
				if (texture2D2 == null)
				{
					Debug.LogWarning("Couldn't find AI business logo: \"" + text + "\" with extension .jpg or .png");
					return GetNullTexture();
				}
				BusinessLogoTextures[(businessName, logoSize, false)] = texture2D2;
			}
		}
		BusinessLogoTextures[(businessName, logoSize, playerBusiness)].name = businessName;
		return BusinessLogoTextures[(businessName, logoSize, playerBusiness)];
	}

	private static void TryToCompress(Texture2D tex)
	{
		if (CanCompressTexture(tex))
		{
			tex.Compress(highQuality: false);
		}
	}

	private static bool CanCompressTexture(Texture2D tex)
	{
		if (tex.width % 16 == 0)
		{
			return tex.height % 16 == 0;
		}
		return false;
	}

	public static Texture2D GetNullTexture()
	{
		if (nullTexture != null)
		{
			return nullTexture;
		}
		nullTexture = GetLogoShapeTexture(Path.Combine(GetBuildInIconsFolder(), "notfound.png"));
		return nullTexture;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LogoShapeSprites.Clear();
		BusinessLogoTextures.Clear();
		nullTexture = null;
	}
}
