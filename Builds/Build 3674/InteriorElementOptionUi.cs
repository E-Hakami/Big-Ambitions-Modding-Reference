using System;
using Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteriorElementOptionUi : MonoBehaviour
{
	private static readonly int ColorMaskTexId = Shader.PropertyToID("_ColorMaskTex");

	public Image previewImage;

	public Button button;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image priceBackground;

	[SerializeField]
	private TMP_Text priceLabel;

	[SerializeField]
	private Image colorPickerBackground;

	[SerializeField]
	private GameObject colorPickerButtonGameObject;

	private Action _onOpenColorPicker;

	public InteriorMaterialPreset Preset { get; private set; }

	public int ColorIndex { get; set; }

	public void SetUp(InteriorMaterialPreset preset, Action onOpenColorPicker)
	{
		Preset = preset;
		base.name = Preset.localizeKey;
		_onOpenColorPicker = onOpenColorPicker;
		Material material = new Material(previewImage.material.shader);
		previewImage.sprite = Preset.preview;
		previewImage.material.CopyPropertiesFromMaterial(material);
		SetMaterialPresetToUIMaterial(material, Preset, 0, applyPreviewData: true);
		previewImage.material = material;
		priceLabel.text = Preset.price.ToShortCurrencyFormat();
		bool active = Preset.variants.Count > 1;
		colorPickerButtonGameObject.SetActive(active);
		colorPickerBackground.gameObject.SetActive(active);
		base.gameObject.SetActive(value: true);
	}

	public void OpenColorPicker()
	{
		_onOpenColorPicker();
	}

	public void SetAllBackgroundsColor(Color color)
	{
		background.color = color;
		priceBackground.color = color;
		colorPickerBackground.color = color;
	}

	public static void SetMaterialPresetToUIMaterial(Material material, InteriorMaterialPreset preset, int colorIndex, bool applyPreviewData = false)
	{
		InteriorMaterialPreset.Variant.MaterialData[] materialData = preset.variants[colorIndex].materialData;
		foreach (InteriorMaterialPreset.Variant.MaterialData materialData2 in materialData)
		{
			if (!InteriorElement.PropertyIndices.TryGetValue(materialData2.propertyName, out var value))
			{
				value = Shader.PropertyToID(materialData2.propertyName);
				InteriorElement.PropertyIndices.Add(materialData2.propertyName, value);
			}
			switch (materialData2.propertyType)
			{
			case InteriorMaterialPreset.Variant.MaterialData.PropertyType.Texture:
				material.SetTexture(value, materialData2.texture);
				break;
			case InteriorMaterialPreset.Variant.MaterialData.PropertyType.Color:
				material.SetColor(value, materialData2.color);
				break;
			case InteriorMaterialPreset.Variant.MaterialData.PropertyType.Boolean:
				material.SetFloat(value, materialData2.boolean ? 1 : 0);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		if (applyPreviewData && preset.previewColorMask != null)
		{
			material.SetTexture(ColorMaskTexId, preset.previewColorMask);
		}
	}
}
