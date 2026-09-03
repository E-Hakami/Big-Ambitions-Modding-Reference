using System;
using System.IO;
using System.Linq;
using BigAmbitions.Characters;
using Blueprints;
using IngameDebugConsole;
using Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Character.Customization;

public class PortraitGenerator : MonoBehaviour
{
	[SerializeField]
	private int widthPixels = 256;

	[SerializeField]
	private int heightPixels = 256;

	[SerializeField]
	private Camera portraitCamera;

	[SerializeField]
	private AppearanceSetter portraitAppearanceSetter;

	[SerializeField]
	private GameObject setUp;

	[Space]
	[SerializeField]
	private MeshRenderer backgroundRenderer;

	[SerializeField]
	private Texture2D defaultBackgroundTexture;

	[SerializeField]
	private Texture2D[] backgroundTextures;

	private static PortraitGenerator _instance;

	private Rect _cachedRect;

	private RenderTexture _cachedRenderTexture;

	private Texture2D _cachedScreenShot;

	public static string GetCharacterPortraitPath(GameInstance saveGame)
	{
		return Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(saveGame.characterId), FileSystemHelper.MakeValidFilename(saveGame.SaveGameName + " portrait.jpg"));
	}

	public static string GetCharacterPortraitPath(GameInstance saveGame, string path)
	{
		return Path.Combine(path, FileSystemHelper.MakeValidFilename(saveGame.SaveGameName + " portrait.jpg"));
	}

	public static string GetCharacterPortraitPath(SaveGameManager.SaveGameStruct saveGame)
	{
		return Path.Combine(saveGame.CharacterPath, FileSystemHelper.MakeValidFilename(saveGame.name + " portrait.jpg"));
	}

	private void Awake()
	{
		_instance = this;
		setUp.SetActive(value: false);
	}

	public static Sprite LoadPlayerPortrait()
	{
		string characterPortraitPath = GetCharacterPortraitPath(SaveGameManager.Current);
		if (File.Exists(characterPortraitPath))
		{
			byte[] data = File.ReadAllBytes(characterPortraitPath);
			Texture2D texture2D = new Texture2D(2, 2)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			if (texture2D.LoadImage(data))
			{
				return Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			}
		}
		return null;
	}

	[ConsoleMethod("updateCharacterPortrait", "Updates the top left character portrait", new string[] { })]
	public static void Command_UpdateCharacterPortrait()
	{
		Create(SaveGameManager.Current.charactersData.First(), null, InstanceBehavior<UIs>.Instance.topBar.avatar);
	}

	public static void Create(CharacterData data, string savePortraitPath = null, Image image = null, Action<Sprite> onPortraitCreated = null)
	{
		if (!(_instance == null))
		{
			_instance.portraitAppearanceSetter.GetComponentInChildren<Animator>().enabled = false;
			if (data.color.r + data.color.g + data.color.b + data.color.a == 0)
			{
				data.color = UpdateCharactersSkinColors.GetColorFromOldSkinColor(data.skinColor);
			}
			_instance.portraitAppearanceSetter.SetAppearance(data.Copy());
			_instance.GeneratePortrait(savePortraitPath, image, onPortraitCreated);
		}
	}

	public static void Create(BigAmbitions.Characters.Gender gender, int ageInDays, string seed, int backgroundIndex, string savePortraitPath = null, Image image = null, Action<Sprite> onPortraitCreated = null)
	{
		if (!(_instance == null))
		{
			_instance.SetAppearance(gender, ageInDays, seed);
			int num = backgroundIndex % _instance.backgroundTextures.Length;
			_instance.GeneratePortrait(savePortraitPath, image, onPortraitCreated, _instance.backgroundTextures[num]);
		}
	}

	public void SetAppearance(BigAmbitions.Characters.Gender gender, int ageInDays, string seed)
	{
		portraitAppearanceSetter.SetRandomAppearance(gender, ageInDays, seed.GetHashCode());
	}

	private void GeneratePortrait(string savePortraitPath, Image image, Action<Sprite> onPortraitCreated = null, Texture2D backgroundTexture = null)
	{
		setUp.SetActive(value: true);
		InstanceBehavior<GameManager>.Instance?.timeOfDayController?.SetShadowTint(Color.black);
		backgroundRenderer.material.mainTexture = ((backgroundTexture != null) ? backgroundTexture : defaultBackgroundTexture);
		if (_cachedRect == default(Rect))
		{
			_cachedRect = new Rect(0f, 0f, widthPixels, heightPixels);
		}
		CaptureScreenRenderAsync(delegate(Texture2D texture)
		{
			Texture2D texture2D = new Texture2D(2, 2);
			byte[] array = texture.EncodeToJPG();
			texture2D.LoadImage(array);
			if (image != null)
			{
				if (image.sprite != null)
				{
					UnityEngine.Object.Destroy(image.sprite);
				}
				image.sprite = Sprite.Create(texture2D, _cachedRect, new Vector2(0.5f, 0.5f));
			}
			onPortraitCreated?.Invoke((image != null) ? image.sprite : Sprite.Create(texture2D, _cachedRect, new Vector2(0.5f, 0.5f)));
			if (!string.IsNullOrEmpty(savePortraitPath))
			{
				File.WriteAllBytes(savePortraitPath, array);
			}
			setUp.SetActive(value: false);
			UnityEngine.Object.Destroy(texture);
		});
	}

	private void CaptureScreenRenderAsync(Action<Texture2D> callback)
	{
		if (_cachedRenderTexture == null)
		{
			_cachedRenderTexture = new RenderTexture(widthPixels, heightPixels, 24);
		}
		if (_cachedScreenShot == null)
		{
			_cachedScreenShot = new Texture2D(widthPixels, heightPixels, TextureFormat.ARGB32, mipChain: false);
		}
		portraitCamera.targetTexture = _cachedRenderTexture;
		portraitCamera.Render();
		portraitCamera.targetTexture = null;
		AsyncGPUReadback.Request(_cachedRenderTexture, 0, TextureFormat.ARGB32, delegate(AsyncGPUReadbackRequest request)
		{
			if (request.hasError)
			{
				Debug.LogError("AsyncGPUReadback encountered an error while reading the texture.");
			}
			else if (!(_cachedScreenShot == null))
			{
				_cachedScreenShot.LoadRawTextureData(request.GetData<byte>());
				_cachedScreenShot.Apply();
				callback?.Invoke(_cachedScreenShot);
			}
		});
	}

	private void OnDestroy()
	{
		if (_cachedRenderTexture != null)
		{
			_cachedRenderTexture.Release();
			UnityEngine.Object.Destroy(_cachedRenderTexture);
		}
		if (_cachedScreenShot != null)
		{
			UnityEngine.Object.Destroy(_cachedScreenShot);
		}
	}
}
