using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigAmbitions.InteriorDesigner;

public class ColorOverlay : MonoBehaviour
{
	private static readonly int MaskColorRedId = Shader.PropertyToID("_MaskColorRed");

	private static readonly int MaskColorGreenId = Shader.PropertyToID("_MaskColorGreen");

	private static readonly int MaskColorBlueId = Shader.PropertyToID("_MaskColorBlue");

	private static readonly int MaskColorAlphaId = Shader.PropertyToID("_MaskColorAlpha");

	private static readonly int MaskEmissionId = Shader.PropertyToID("_EmissiveColorLDR");

	private static MaterialPropertyBlock _maskColorPropertyBlock;

	[SerializeField]
	private Shader colorMaskShader;

	[SerializeField]
	private Transform colorTemplate;

	[SerializeField]
	private UIHoverAboveObject hoverAboveObject;

	[SerializeField]
	private Button resetColorsButton;

	private readonly List<CustomizableColor> _customizableColors = new List<CustomizableColor>();

	public readonly UnityEvent<int, List<CustomizableColor>> onChangesConfirmed = new UnityEvent<int, List<CustomizableColor>>();

	private TMP_Text[] _textComponents;

	private ItemController _currentItemController;

	private bool _isOpen;

	private Color[] _itemColors;

	private int _itemIndex;

	private UnityAction _onInteract;

	public static MaterialPropertyBlock MaskColorPropertyBlock => _maskColorPropertyBlock ?? (_maskColorPropertyBlock = new MaterialPropertyBlock());

	private bool HasChanges
	{
		get
		{
			if (_customizableColors.Any((CustomizableColor x) => x.initialColor != x.newColor))
			{
				if (!(_customizableColors[0].newColor != Color.clear))
				{
					return CanResetColors;
				}
				return true;
			}
			return false;
		}
	}

	private bool CanResetColors => _customizableColors.Any((CustomizableColor x) => Mathf.Abs(x.newColor.r - x.originalColor.r) > 0.003f || Mathf.Abs(x.newColor.g - x.originalColor.g) > 0.003f || Mathf.Abs(x.newColor.b - x.originalColor.b) > 0.003f || Mathf.Abs(x.newColor.a - x.originalColor.a) > 0.003f);

	private void Awake()
	{
		InteriorDesignerUI.OnUndoRedo.AddListener(OnUndoRedo);
	}

	private void OnUndoRedo()
	{
		if (_isOpen)
		{
			Close();
		}
	}

	private void Reset()
	{
		colorTemplate.ResetTemplate();
		_customizableColors.Clear();
	}

	public void Open(ItemController itemController, int itemIndex)
	{
		if (itemController == null)
		{
			return;
		}
		if (_isOpen && HasChanges)
		{
			onChangesConfirmed.Invoke(_itemIndex, _customizableColors);
		}
		_itemIndex = itemIndex;
		_currentItemController = itemController;
		hoverAboveObject.SetObjectToFollow(itemController.transform);
		resetColorsButton.onClick.RemoveAllListeners();
		Reset();
		if ((itemController.Item.customColorChannels & CustomColorChannel.Text) != 0)
		{
			_textComponents = _currentItemController.GetComponentsInChildren<TMP_Text>();
		}
		Color[] customizationColors = itemController.Item.customizationColors;
		_itemColors = ((customizationColors != null && customizationColors.Length > 0) ? itemController.Item.customizationColors : null);
		SetUpColorChannel(CustomColorChannel.Red);
		SetUpColorChannel(CustomColorChannel.Green);
		SetUpColorChannel(CustomColorChannel.Blue);
		SetUpColorChannel(CustomColorChannel.Alpha);
		SetUpColorChannel(CustomColorChannel.Emission);
		SetUpColorChannel(CustomColorChannel.Text);
		SetUpColorChannel(CustomColorChannel.Light);
		resetColorsButton.onClick.AddListener(delegate
		{
			if (CanResetColors)
			{
				if (HasChanges)
				{
					ResetColors(_currentItemController, _customizableColors);
				}
				else
				{
					_customizableColors[0].newColor = Color.clear;
					Close();
				}
			}
		});
		resetColorsButton.interactable = CanResetColors;
		base.gameObject.SetActive(value: true);
		_isOpen = true;
	}

	public void Close()
	{
		if (_isOpen)
		{
			if (HasChanges)
			{
				onChangesConfirmed.Invoke(_itemIndex, _customizableColors);
			}
			_isOpen = false;
			base.gameObject.SetActive(value: false);
		}
	}

	private void SetUpColorChannel(CustomColorChannel colorChannel)
	{
		if ((ItemsGetter.GetByName(_currentItemController.itemName).customColorChannels & colorChannel) != 0)
		{
			Color originalColor = GetOriginalColor(colorChannel);
			SerializableColor? serializableColor = _currentItemController.ItemInstance?.customColors?.FirstOrDefault((CustomColor x) => x.channel == colorChannel)?.color ?? _currentItemController.customColors?.FirstOrDefault((CustomColor x) => x.channel == colorChannel)?.color;
			SerializableColor valueOrDefault = serializableColor.GetValueOrDefault();
			if (!serializableColor.HasValue)
			{
				valueOrDefault = originalColor;
				serializableColor = valueOrDefault;
			}
			CustomizableColor item = new CustomizableColor
			{
				channel = colorChannel,
				newColor = serializableColor.Value,
				initialColor = serializableColor.Value,
				originalColor = originalColor
			};
			_customizableColors.Add(item);
			AddColorEntry(_customizableColors.Count);
		}
	}

	private void AddColorEntry(int colorIndex)
	{
		Transform obj = UnityEngine.Object.Instantiate(colorTemplate, colorTemplate.parent);
		obj.Find("Label").GetComponent<TextLocalizationComponent>().SetData("itemcustomizer_color_label".Localize(new
		{
			number = colorIndex
		}));
		ColorListUI component = obj.GetComponent<ColorListUI>();
		CustomizableColor customizableColor = _customizableColors[colorIndex - 1];
		component.getInitialColor = () => customizableColor.newColor;
		component.onColorChanged = delegate(Color color)
		{
			if (customizableColor.channel == CustomColorChannel.Text)
			{
				TMP_Text[] textComponents = _textComponents;
				for (int i = 0; i < textComponents.Length; i++)
				{
					textComponents[i].color = color;
				}
			}
			else if (customizableColor.channel == CustomColorChannel.Light)
			{
				if (_currentItemController is IndoorLightController indoorLightController)
				{
					indoorLightController.SetColor(color);
				}
			}
			else
			{
				Renderer[] renderersToSetColor = _currentItemController.GetRenderersToSetColor(customizableColor.channel);
				foreach (Renderer obj2 in renderersToSetColor)
				{
					obj2.GetPropertyBlock(MaskColorPropertyBlock);
					MaskColorPropertyBlock.SetColor(GetMaskId(customizableColor.channel), color);
					obj2.SetPropertyBlock(MaskColorPropertyBlock);
				}
			}
		};
		component.onRefresh = (Action<ColorListUI>)Delegate.Combine(component.onRefresh, new Action<ColorListUI>(RefreshOtherColorLists));
		component.onSelectColor = (Action<Color>)Delegate.Combine(component.onSelectColor, (Action<Color>)delegate(Color color)
		{
			SelectColor(colorIndex - 1, color);
		});
		component.defaultColorsOverride = _itemColors;
		component.SetUp();
		obj.gameObject.SetActive(value: true);
	}

	private void RefreshOtherColorLists(ColorListUI source)
	{
		foreach (Transform item in colorTemplate.parent)
		{
			if (item.gameObject.activeSelf)
			{
				ColorListUI component = item.GetComponent<ColorListUI>();
				if (!(component == source))
				{
					component.SetUp();
				}
			}
		}
	}

	private void SelectColor(int colorIndex, Color newColor)
	{
		CustomizableColor customizableColor = _customizableColors[colorIndex];
		customizableColor.newColor = newColor;
		if (customizableColor.channel == CustomColorChannel.Text)
		{
			TMP_Text[] textComponents = _textComponents;
			for (int i = 0; i < textComponents.Length; i++)
			{
				textComponents[i].color = newColor;
			}
		}
		else if (customizableColor.channel == CustomColorChannel.Light)
		{
			if (!(_currentItemController is IndoorLightController indoorLightController))
			{
				return;
			}
			indoorLightController.SetColor(newColor);
		}
		else
		{
			Renderer[] renderersToSetColor = _currentItemController.GetRenderersToSetColor(customizableColor.channel);
			foreach (Renderer obj in renderersToSetColor)
			{
				obj.GetPropertyBlock(MaskColorPropertyBlock);
				MaskColorPropertyBlock.SetColor(GetMaskId(customizableColor.channel), newColor);
				obj.SetPropertyBlock(MaskColorPropertyBlock);
			}
		}
		resetColorsButton.interactable = CanResetColors;
	}

	public void UndoColors(ItemController itemController, List<CustomizableColor> customizableColors)
	{
		if (customizableColors[0].initialColor == Color.clear)
		{
			ResetColors(itemController, customizableColors);
			return;
		}
		for (int i = 0; i < customizableColors.Count; i++)
		{
			Color initialColor = customizableColors[i].initialColor;
			if (customizableColors[i].channel == CustomColorChannel.Text)
			{
				_textComponents = itemController.GetComponentsInChildren<TMP_Text>();
				TMP_Text[] textComponents = _textComponents;
				for (int j = 0; j < textComponents.Length; j++)
				{
					textComponents[j].FadeColor(initialColor);
				}
			}
			else if (customizableColors[i].channel == CustomColorChannel.Light)
			{
				if (itemController is IndoorLightController indoorLightController)
				{
					indoorLightController.SetColor(initialColor);
				}
			}
			else
			{
				Renderer[] renderersToSetColor = itemController.GetRenderersToSetColor(customizableColors[i].channel);
				for (int j = 0; j < renderersToSetColor.Length; j++)
				{
					renderersToSetColor[j].FadePropertyBlockColor(GetMaskId(customizableColors[i].channel), initialColor);
				}
			}
		}
		if (itemController.ItemInstance != null)
		{
			itemController.ItemInstance.customColors = new List<CustomColor>();
			foreach (CustomizableColor customizableColor in customizableColors)
			{
				itemController.ItemInstance.customColors.Add(new CustomColor
				{
					channel = customizableColor.channel,
					color = customizableColor.initialColor
				});
			}
			itemController.customColors = itemController.ItemInstance.customColors;
			return;
		}
		itemController.customColors = new List<CustomColor>();
		foreach (CustomizableColor customizableColor2 in customizableColors)
		{
			itemController.customColors.Add(new CustomColor
			{
				channel = customizableColor2.channel,
				color = customizableColor2.initialColor
			});
		}
	}

	public void ApplyColors(ItemController itemController, List<CustomizableColor> customizableColors)
	{
		if (customizableColors[0].newColor == Color.clear)
		{
			ResetColors(itemController, customizableColors);
			return;
		}
		if (itemController.ItemInstance != null)
		{
			itemController.ItemInstance.customColors = new List<CustomColor>();
			foreach (CustomizableColor customizableColor in customizableColors)
			{
				itemController.ItemInstance.customColors.Add(new CustomColor
				{
					channel = customizableColor.channel,
					color = customizableColor.newColor
				});
			}
			itemController.customColors = itemController.ItemInstance.customColors;
		}
		else
		{
			itemController.customColors = new List<CustomColor>();
			foreach (CustomizableColor customizableColor2 in customizableColors)
			{
				itemController.customColors.Add(new CustomColor
				{
					channel = customizableColor2.channel,
					color = customizableColor2.newColor
				});
			}
		}
		for (int i = 0; i < customizableColors.Count; i++)
		{
			Color newColor = customizableColors[i].newColor;
			if (customizableColors[i].channel == CustomColorChannel.Text)
			{
				_textComponents = itemController.GetComponentsInChildren<TMP_Text>();
				TMP_Text[] textComponents = _textComponents;
				for (int j = 0; j < textComponents.Length; j++)
				{
					textComponents[j].FadeColor(newColor);
				}
			}
			if (customizableColors[i].channel == CustomColorChannel.Light)
			{
				if (itemController is IndoorLightController indoorLightController)
				{
					indoorLightController.SetColor(newColor);
				}
				continue;
			}
			Renderer[] renderersToSetColor = itemController.GetRenderersToSetColor(customizableColors[i].channel);
			for (int j = 0; j < renderersToSetColor.Length; j++)
			{
				renderersToSetColor[j].FadePropertyBlockColor(GetMaskId(customizableColors[i].channel), newColor);
			}
		}
	}

	private void ResetColors(ItemController itemController, List<CustomizableColor> colors)
	{
		itemController.ItemInstance?.customColors?.Clear();
		itemController.customColors?.Clear();
		for (int i = 0; i < colors.Count; i++)
		{
			Color originalColor = colors[i].originalColor;
			if (colors[i].channel == CustomColorChannel.Text)
			{
				TMP_Text[] textComponents = _textComponents;
				for (int j = 0; j < textComponents.Length; j++)
				{
					textComponents[j].FadeColor(originalColor);
				}
			}
			if (colors[i].channel == CustomColorChannel.Light)
			{
				if (itemController is IndoorLightController indoorLightController)
				{
					indoorLightController.SetColor(originalColor);
				}
				continue;
			}
			Renderer[] renderersToSetColor = itemController.GetRenderersToSetColor(colors[i].channel);
			for (int j = 0; j < renderersToSetColor.Length; j++)
			{
				renderersToSetColor[j].FadePropertyBlockColor(GetMaskId(colors[i].channel), originalColor);
			}
		}
		foreach (CustomizableColor customizableColor in _customizableColors)
		{
			customizableColor.newColor = customizableColor.initialColor;
		}
		resetColorsButton.interactable = CanResetColors;
	}

	private Color GetOriginalColor(CustomColorChannel colorChannel)
	{
		switch (colorChannel)
		{
		case CustomColorChannel.Text:
			if (_textComponents.Length != 0)
			{
				return Color.white;
			}
			Debug.LogError(_currentItemController.itemName + " doesn't have text components, but has the Text custom color channel enabled");
			return default(Color);
		case CustomColorChannel.Light:
			if (!(_currentItemController is IndoorLightController indoorLightController))
			{
				return default(Color);
			}
			return indoorLightController.originalLightColor;
		default:
		{
			Material material = _currentItemController.Renderers[0].materials.FirstOrDefault((Material x) => x.shader.name.Equals(colorMaskShader.name));
			if (material != null)
			{
				return material.GetColor(GetMaskId(colorChannel));
			}
			Debug.LogError(_currentItemController.itemName + " doesn't have a material with the color mask shader");
			return default(Color);
		}
		}
	}

	public static int GetMaskId(CustomColorChannel colorChannel)
	{
		return colorChannel switch
		{
			CustomColorChannel.Red => MaskColorRedId, 
			CustomColorChannel.Green => MaskColorGreenId, 
			CustomColorChannel.Blue => MaskColorBlueId, 
			CustomColorChannel.Alpha => MaskColorAlphaId, 
			_ => MaskEmissionId, 
		};
	}
}
