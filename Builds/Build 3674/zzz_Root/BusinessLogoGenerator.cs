using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class BusinessLogoGenerator : MonoBehaviour
{
	[Serializable]
	public class ImageSize
	{
		public int width;

		public int height;
	}

	[SerializeField]
	private ImageSize billboardImageSize;

	[SerializeField]
	private ImageSize squareImageSize;

	[SerializeField]
	private ImageSize wideImageSize;

	[SerializeField]
	private Camera businessLogoCamera;

	[SerializeField]
	private HDAdditionalCameraData businessLogoCameraData;

	[SerializeField]
	private GameObject setUp;

	[SerializeField]
	private SpriteRenderer businessLogoShape;

	[SerializeField]
	private TMP_Text businessNameTextSquare;

	[SerializeField]
	private TMP_Text businessNameTextWide;

	[SerializeField]
	private GameObject squareSprite;

	[SerializeField]
	private GameObject wideSprite;

	public static BusinessLogoGenerator Instance;

	private readonly Queue<(string businessName, LogoSettings settings, string savePath, bool isPlayerBusiness, UnityAction onCreate)> LogoGenerationQueue = new Queue<(string, LogoSettings, string, bool, UnityAction)>();

	private bool _logoIsGenerating;

	private Sprite _runtimeLogoShapeSprite;

	private void Awake()
	{
		Instance = this;
		setUp.SetActive(value: false);
		squareSprite.gameObject.SetActive(value: false);
		wideSprite.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		DestroyRuntimeLogoShape();
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public static void Create(string businessName, LogoSettings settings, string savePath, bool isPlayerBusiness, UnityAction onCreate = null)
	{
		if (Instance == null)
		{
			Debug.LogWarning("BusinessLogoGenerator not found");
			onCreate?.Invoke();
		}
		else if (Instance._logoIsGenerating)
		{
			Instance.LogoGenerationQueue.Enqueue((businessName, settings, savePath, isPlayerBusiness, onCreate));
		}
		else
		{
			Instance._logoIsGenerating = true;
			Instance.StartCoroutine(Instance.GenerateBusinessLogo(businessName, settings, savePath, isPlayerBusiness, onCreate));
		}
	}

	private IEnumerator GenerateBusinessLogo(string businessName, LogoSettings settings, string savePath, bool isPlayerBusiness, UnityAction onCreate = null)
	{
		if (!Directory.Exists(savePath))
		{
			Directory.CreateDirectory(savePath);
		}
		SetUpSettings(businessName, settings);
		setUp.SetActive(value: true);
		squareSprite.gameObject.SetActive(value: true);
		Texture2D squareSign = GenerateSprite(Path.Combine(savePath, LogoSize.SquareSign.ToStringFast() + ".jpg"), squareImageSize);
		Texture2D billboard = GenerateSprite(Path.Combine(savePath, LogoSize.Billboard.ToStringFast() + ".jpg"), billboardImageSize);
		squareSprite.gameObject.SetActive(value: false);
		yield return new WaitForEndOfFrame();
		wideSprite.gameObject.SetActive(value: true);
		Texture2D wideSign = GenerateSprite(Path.Combine(savePath, LogoSize.WideSign.ToStringFast() + ".jpg"), wideImageSize);
		wideSprite.gameObject.SetActive(value: false);
		setUp.SetActive(value: false);
		yield return new WaitForEndOfFrame();
		CacheOrDestroyGeneratedTexture(businessName, LogoSize.SquareSign, isPlayerBusiness, squareSign);
		CacheOrDestroyGeneratedTexture(businessName, LogoSize.Billboard, isPlayerBusiness, billboard);
		CacheOrDestroyGeneratedTexture(businessName, LogoSize.WideSign, isPlayerBusiness, wideSign);
		onCreate?.Invoke();
		yield return null;
		_logoIsGenerating = false;
		if (LogoGenerationQueue.Count != 0)
		{
			(string, LogoSettings, string, bool, UnityAction) tuple = LogoGenerationQueue.Dequeue();
			StartCoroutine(GenerateBusinessLogo(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5));
		}
	}

	public void GenerateWarehouseLogo(string businessName, BusinessType businessType, string savePath, bool isPlayerBusiness)
	{
		if (!Directory.Exists(savePath))
		{
			Directory.CreateDirectory(savePath);
		}
		setUp.SetActive(value: true);
		squareSprite.gameObject.SetActive(value: true);
		SetUpWarehouseSettings(businessName, businessType);
		Vector3 localScale = businessLogoShape.transform.localScale;
		businessLogoShape.transform.localScale = Vector3.one * 2f;
		Texture2D texture = GenerateSprite(Path.Combine(savePath, LogoSize.SquareSign.ToStringFast() + ".jpg"), squareImageSize);
		businessLogoShape.transform.localScale = localScale;
		squareSprite.gameObject.SetActive(value: false);
		setUp.SetActive(value: false);
		CacheOrDestroyGeneratedTexture(businessName, LogoSize.SquareSign, isPlayerBusiness, texture);
	}

	private void CacheOrDestroyGeneratedTexture(string businessName, LogoSize logoSize, bool isPlayerBusiness, Texture2D texture)
	{
		(string, LogoSize, bool) key = (businessName, logoSize, isPlayerBusiness);
		if (LogoHelper.BusinessLogoTextures.ContainsKey(key))
		{
			UnityEngine.Object.Destroy(LogoHelper.BusinessLogoTextures[key]);
			texture.Compress(highQuality: false);
			LogoHelper.BusinessLogoTextures[key] = texture;
		}
		else
		{
			UnityEngine.Object.Destroy(texture);
		}
	}

	private Texture2D GenerateSprite(string savePath, ImageSize imageSize)
	{
		Texture2D texture2D = CaptureScreenRender(imageSize);
		byte[] bytes = texture2D.EncodeToJPG();
		File.WriteAllBytes(savePath, bytes);
		return texture2D;
	}

	private void SetUpSettings(string businessName, LogoSettings settings)
	{
		businessLogoCameraData.backgroundColorHDR = settings.backgroundColor.AsLinear();
		businessLogoShape.color = settings.logoColor;
		SetLogoShape(LogoHelper.GetLogoSprite(settings.logoShape).texture);
		businessNameTextSquare.text = businessName;
		businessNameTextSquare.font = InstanceBehavior<GlobalReferences>.Instance.GetFontByName(settings.font);
		businessNameTextSquare.color = settings.fontColor;
		businessNameTextWide.text = businessName;
		businessNameTextWide.font = InstanceBehavior<GlobalReferences>.Instance.GetFontByName(settings.font);
		businessNameTextWide.color = settings.fontColor;
	}

	private void SetUpWarehouseSettings(string businessName, BusinessType businessType)
	{
		DestroyRuntimeLogoShape();
		businessLogoCameraData.backgroundColorHDR = Color.white;
		businessLogoShape.color = Color.black;
		businessLogoShape.sprite = businessType.icon;
		businessNameTextSquare.text = businessName;
		businessNameTextSquare.font = InstanceBehavior<GlobalReferences>.Instance.GetFontByName(FontFace.Rubik);
		businessNameTextSquare.color = Color.black;
	}

	private void SetLogoShape(Texture2D originalLogoShapeTexture)
	{
		DestroyRuntimeLogoShape();
		float x = businessLogoShape.size.x;
		int num = Mathf.FloorToInt((float)squareImageSize.width * x);
		int num2 = Mathf.FloorToInt((float)squareImageSize.height * x);
		RenderTexture renderTexture = (RenderTexture.active = new RenderTexture(num, num2, 24));
		Graphics.Blit(originalLogoShapeTexture, renderTexture);
		Texture2D texture2D = new Texture2D(num, num2);
		texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
		texture2D.Apply();
		RenderTexture.active = null;
		UnityEngine.Object.Destroy(renderTexture);
		_runtimeLogoShapeSprite = Sprite.Create(texture2D, new Rect(0f, 0f, num, num2), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
		businessLogoShape.sprite = _runtimeLogoShapeSprite;
	}

	private void DestroyRuntimeLogoShape()
	{
		if (!(_runtimeLogoShapeSprite == null))
		{
			UnityEngine.Object.Destroy(_runtimeLogoShapeSprite.texture);
			UnityEngine.Object.Destroy(_runtimeLogoShapeSprite);
			_runtimeLogoShapeSprite = null;
		}
	}

	private Texture2D CaptureScreenRender(ImageSize imageSize)
	{
		Rect source = new Rect(0f, 0f, imageSize.width, imageSize.height);
		RenderTexture renderTexture = new RenderTexture(imageSize.width, imageSize.height, 24);
		Texture2D texture2D = new Texture2D(imageSize.width, imageSize.height, TextureFormat.ARGB32, mipChain: false);
		businessLogoCamera.targetTexture = renderTexture;
		businessLogoCamera.Render();
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(source, 0, 0);
		businessLogoCamera.targetTexture = null;
		RenderTexture.active = null;
		UnityEngine.Object.Destroy(renderTexture);
		return texture2D;
	}
}
