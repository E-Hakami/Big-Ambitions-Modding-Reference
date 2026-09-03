using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Character.Customization;

public class CharacterCustomizer : MonoBehaviour
{
	public AppearanceSetter appearanceSetter;

	public AppearanceColorPicker appearanceColorPicker;

	[SerializeField]
	[ShowIf("ShouldShowTags")]
	protected AppearanceTag[] tags;

	[SerializeField]
	protected ElementPicker elementPicker;

	[SerializeField]
	protected MenuVertical menu;

	[SerializeField]
	protected GameObject gridSelectionPanel;

	public Action onMenuSelected;

	public Action onAppearanceChange;

	private List<AppearanceElementVariant> _currentVariants;

	protected AppearanceElementType currentElementType;

	protected UnityEvent<AppearanceElementType> onSubCategoryChanged = new UnityEvent<AppearanceElementType>();

	protected virtual void Start()
	{
		if (menu.HasMultipleCategories)
		{
			MenuVertical menuVertical = menu;
			menuVertical.onCategoryClick = (Action<string>)Delegate.Combine(menuVertical.onCategoryClick, new Action<string>(Show));
			MenuVertical menuVertical2 = menu;
			menuVertical2.onSubCategoryClick = (Action<string>)Delegate.Combine(menuVertical2.onSubCategoryClick, new Action<string>(Show));
			MenuVertical menuVertical3 = menu;
			menuVertical3.shouldShowCategory = (Func<string, bool>)Delegate.Combine(menuVertical3.shouldShowCategory, new Func<string, bool>(ShouldShowSubCategory));
			menu.Reset();
		}
		else
		{
			menu.gameObject.SetActive(value: false);
		}
	}

	private bool ShouldShowSubCategory(string elementTypeString)
	{
		if (elementTypeString == "body")
		{
			return true;
		}
		if (!Enum.TryParse<AppearanceElementType>(elementTypeString, ignoreCase: true, out var result))
		{
			return false;
		}
		List<AppearanceElementVariant> elementVariants = appearanceSetter.GetElementVariants(result, tags);
		if (elementVariants != null)
		{
			return elementVariants.Count > 1;
		}
		return false;
	}

	public virtual void Show(string elementTypeString)
	{
		if (Enum.TryParse<AppearanceElementType>(elementTypeString, ignoreCase: true, out var result))
		{
			Show(result);
		}
	}

	public void Show(AppearanceElementType elementType)
	{
		currentElementType = elementType;
		onMenuSelected?.Invoke();
		ShowCurrentElement();
	}

	protected virtual void ShowGridSelectionPanel()
	{
		gridSelectionPanel.SetActive(value: true);
	}

	protected virtual void ShowCurrentElement()
	{
		ShowSubElement(currentElementType, SelectElement, SelectElementMaterial);
	}

	private void SelectElement(int selectedElementIndex)
	{
		AppearanceElementData elementData = appearanceSetter.data.elements.FirstOrDefault((AppearanceElementData x) => x.type == currentElementType);
		if (elementData == null)
		{
			elementData = new AppearanceElementData
			{
				type = currentElementType
			};
			appearanceSetter.data.elements.Add(elementData);
		}
		AppearanceElementVariant appearanceElementVariant = _currentVariants[selectedElementIndex];
		elementData.variantId = appearanceElementVariant.id;
		AppearanceElementColor[] colors = appearanceElementVariant.colors;
		if (colors != null && colors.Length != 0)
		{
			if (!colors.Any((AppearanceElementColor c) => c.id == elementData.colorId))
			{
				elementData.colorId = colors[0].id;
			}
		}
		else
		{
			elementData.colorId = null;
		}
		if (appearanceElementVariant is AppearanceBlendshapeVariant appearanceBlendshapeVariant && appearanceSetter?.data?.blendshapes != null)
		{
			appearanceSetter.Blendshapes.HandleAppearanceBlendshapeVariant(appearanceBlendshapeVariant);
			if (currentElementType == AppearanceElementType.Head)
			{
				appearanceSetter.SetHeadElementBlendshape(AppearanceElementType.Eyes, appearanceBlendshapeVariant);
				appearanceSetter.SetHeadElementBlendshape(AppearanceElementType.Mouth, appearanceBlendshapeVariant);
				appearanceSetter.SetHeadElementBlendshape(AppearanceElementType.Nose, appearanceBlendshapeVariant);
			}
		}
		appearanceSetter?.UpdateVisuals();
		onAppearanceChange?.Invoke();
		SelectSubElement(currentElementType, SelectElementMaterial);
	}

	private void SelectElementMaterial(int colorIndex)
	{
		AppearanceElementData elementData = appearanceSetter.data.elements.First((AppearanceElementData x) => x.type == currentElementType);
		AppearanceElementVariant appearanceElementVariant = _currentVariants.First((AppearanceElementVariant x) => x.id == elementData.variantId);
		elementData.colorId = appearanceElementVariant.colors[colorIndex].id;
		appearanceSetter.UpdateVisuals();
		onAppearanceChange?.Invoke();
	}

	protected void RandomizeCurrentElement()
	{
		RandomizeElement(currentElementType);
	}

	private void RandomizeElement(AppearanceElementType elementType)
	{
		appearanceSetter.RandomizeElement(elementType, tags, randomizeColor: true, excludeCurrentVariant: true, null, skipColorMatch: true);
		appearanceSetter.UpdateVisuals();
		ShowCurrentElement();
	}

	private void ShowSubElement(AppearanceElementType elementType, UnityAction<int> onSelect, UnityAction<int> onMaterialSelect = null)
	{
		_currentVariants = appearanceSetter.GetElementVariants(elementType, tags);
		if (_currentVariants == null || _currentVariants.Count == 0)
		{
			return;
		}
		bool shouldShowSportsIconIfPossible = elementType == AppearanceElementType.Torso || elementType == AppearanceElementType.Legs || elementType == AppearanceElementType.Feet;
		List<(int, Sprite, bool)> elements = _currentVariants.Select((AppearanceElementVariant variant, int i) => (i: i, elementIcon: variant.elementIcon, shouldShowSportsIconIfPossible && (variant.tags.Contains(AppearanceTag.All) || variant.tags.Contains(AppearanceTag.Sport)))).ToList();
		AppearanceElementData elementData = appearanceSetter.data.elements.FirstOrDefault((AppearanceElementData x) => x.type == elementType);
		AppearanceElementVariant appearanceElementVariant = _currentVariants.FirstOrDefault((AppearanceElementVariant x) => x.id == elementData?.variantId);
		if ((object)appearanceElementVariant == null)
		{
			appearanceElementVariant = _currentVariants.First();
		}
		elementPicker.SetList(elements, onSelect, _currentVariants.IndexOf(appearanceElementVariant));
		if (appearanceElementVariant.colors.Length > 1 && onMaterialSelect != null)
		{
			List<Sprite> sprites = appearanceElementVariant.colors.Select((AppearanceElementColor x) => x.colorIcon).ToList();
			AppearanceElementColor item = appearanceElementVariant.colors.FirstOrDefault((AppearanceElementColor x) => x.id == elementData?.colorId);
			int num = appearanceElementVariant.colors.ToList().IndexOf(item);
			if (num < 0)
			{
				num = 0;
			}
			appearanceColorPicker.SetList(sprites, onMaterialSelect, num);
		}
		else
		{
			appearanceColorPicker.Hide();
		}
		ShowGridSelectionPanel();
		onSubCategoryChanged?.Invoke(elementType);
	}

	private void SelectSubElement(AppearanceElementType elementType, UnityAction<int> onMaterialSelect = null)
	{
		AppearanceElementData elementData = appearanceSetter.data.elements.First((AppearanceElementData x) => x.type == elementType);
		AppearanceElementVariant appearanceElementVariant = _currentVariants.FirstOrDefault((AppearanceElementVariant x) => x.id == elementData.variantId);
		if ((object)appearanceElementVariant == null)
		{
			appearanceElementVariant = _currentVariants.First();
		}
		if (appearanceElementVariant.colors.Length > 1 && onMaterialSelect != null)
		{
			List<Sprite> sprites = appearanceElementVariant.colors.Select((AppearanceElementColor x) => x.colorIcon).ToList();
			AppearanceElementColor item = appearanceElementVariant.colors.FirstOrDefault((AppearanceElementColor x) => x.id == elementData.colorId);
			int num = appearanceElementVariant.colors.ToList().IndexOf(item);
			if (num < 0)
			{
				num = 0;
			}
			appearanceColorPicker.SetList(sprites, onMaterialSelect, num);
		}
		else
		{
			appearanceColorPicker.Hide();
		}
		onSubCategoryChanged?.Invoke(elementType);
	}

	protected virtual bool ShouldShowTags()
	{
		return true;
	}

	public bool HasColorChanged(AppearanceElementType appearanceElementType)
	{
		AppearanceElementData appearanceElementData = SaveGameManager.Current.charactersData.First().elements.FirstOrDefault((AppearanceElementData x) => x.type == appearanceElementType);
		if (appearanceElementData == null || appearanceElementData.colorId == null)
		{
			return false;
		}
		AppearanceElementData appearanceElementData2 = appearanceSetter.data.elements.FirstOrDefault((AppearanceElementData x) => x.type == appearanceElementType);
		if (appearanceElementData2 == null || appearanceElementData2.colorId == null)
		{
			return false;
		}
		return appearanceElementData.colorId != appearanceElementData2.colorId;
	}

	public bool HasVariantChanged(AppearanceElementType appearanceElementType)
	{
		AppearanceElementData appearanceElementData = SaveGameManager.Current.charactersData.First().elements.FirstOrDefault((AppearanceElementData x) => x.type == appearanceElementType);
		AppearanceElementData appearanceElementData2 = appearanceSetter.data.elements.FirstOrDefault((AppearanceElementData x) => x.type == appearanceElementType);
		if (appearanceElementData2 == null || appearanceElementData == null)
		{
			return false;
		}
		return appearanceElementData.variantId != appearanceElementData2.variantId;
	}

	protected bool HasEyeColorChanged()
	{
		Color32 eyesColor = SaveGameManager.Current.charactersData.First().eyesColor;
		return Vector4.Distance(b: (Color)appearanceSetter.data.eyesColor, a: (Color)eyesColor) > 0.1f;
	}

	protected bool HasSkinColorChanged()
	{
		Color32 color = SaveGameManager.Current.charactersData.First().color;
		return Vector4.Distance(b: (Color)appearanceSetter.data.color, a: (Color)color) > 0.1f;
	}

	protected bool HaveBodyValuesChanged()
	{
		CharacterData characterData = SaveGameManager.Current.charactersData.First();
		CharacterData data = appearanceSetter.data;
		if (!(Mathf.Abs(characterData.fatness - data.fatness) > 0.05f))
		{
			return Mathf.Abs(characterData.strength - data.strength) > 0.05f;
		}
		return true;
	}
}
