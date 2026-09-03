using System;
using System.Linq;
using Enums;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class LogoCustomizer : MonoBehaviour
{
	[SerializeField]
	private Button saveLogoChangesButton;

	[SerializeField]
	private LogoShapes logoShapes;

	[SerializeField]
	private ColorListUI logoColorList;

	[SerializeField]
	private ColorListUI fontColorList;

	[SerializeField]
	private ColorListUI backgroundColorList;

	[SerializeField]
	private UI.Elements.Dropdown fontDropdown;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image shape;

	[SerializeField]
	private TMP_Text businessName;

	private string _logoShape;

	private BizManBusiness _bizManBusiness;

	private Color _backgroundColor;

	private Color _logoColor;

	private Color _fontColor;

	private FontFace _logoFont;

	public bool HasUnsavedChanges => saveLogoChangesButton.interactable;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
		logoShapes.onSelectShape.AddListener(SelectLogoShape);
		ColorListUI colorListUI = logoColorList;
		colorListUI.onRefresh = (Action<ColorListUI>)Delegate.Combine(colorListUI.onRefresh, new Action<ColorListUI>(SetUpColorLists));
		ColorListUI colorListUI2 = logoColorList;
		colorListUI2.onSelectColor = (Action<Color>)Delegate.Combine(colorListUI2.onSelectColor, new Action<Color>(SelectLogoColor));
		ColorListUI colorListUI3 = fontColorList;
		colorListUI3.onRefresh = (Action<ColorListUI>)Delegate.Combine(colorListUI3.onRefresh, new Action<ColorListUI>(SetUpColorLists));
		ColorListUI colorListUI4 = fontColorList;
		colorListUI4.onSelectColor = (Action<Color>)Delegate.Combine(colorListUI4.onSelectColor, new Action<Color>(SelectFontColor));
		ColorListUI colorListUI5 = backgroundColorList;
		colorListUI5.onRefresh = (Action<ColorListUI>)Delegate.Combine(colorListUI5.onRefresh, new Action<ColorListUI>(SetUpColorLists));
		ColorListUI colorListUI6 = backgroundColorList;
		colorListUI6.onSelectColor = (Action<Color>)Delegate.Combine(colorListUI6.onSelectColor, new Action<Color>(SelectBackgroundColor));
		InitializeFontDropdown();
	}

	private void InitializeFontDropdown()
	{
		fontDropdown.onOptionSelected.AddListener(SelectFont);
		fontDropdown.SetOptions(InstanceBehavior<GlobalReferences>.Instance.signFontAssets.Select((TMP_FontAsset x) => x.name).ToList(), localize: false);
	}

	private void OnEnable()
	{
		Refresh();
	}

	public void Refresh()
	{
		logoShapes.RefreshLogoShapes();
		SetUpColorLists();
		businessName.text = _bizManBusiness.buildingRegistration.BusinessName;
		fontDropdown.ResetSelectedOption(InstanceBehavior<GlobalReferences>.Instance.signFontAssets.ToList().FindIndex((TMP_FontAsset x) => x.name == _bizManBusiness.buildingRegistration.logoSettings.font.ToStringFast()));
		businessName.font = InstanceBehavior<GlobalReferences>.Instance.GetFontByName(_bizManBusiness.buildingRegistration.logoSettings.font);
		SelectLogoShape(_bizManBusiness.buildingRegistration.logoSettings.logoShape);
		SelectLogoColor(_bizManBusiness.buildingRegistration.logoSettings.logoColor);
		SelectFontColor(_bizManBusiness.buildingRegistration.logoSettings.fontColor);
		SelectBackgroundColor(_bizManBusiness.buildingRegistration.logoSettings.backgroundColor);
		_logoShape = _bizManBusiness.buildingRegistration.logoSettings.logoShape;
		_backgroundColor = _bizManBusiness.buildingRegistration.logoSettings.backgroundColor;
		_logoColor = _bizManBusiness.buildingRegistration.logoSettings.logoColor;
		_fontColor = _bizManBusiness.buildingRegistration.logoSettings.fontColor;
		_logoFont = _bizManBusiness.buildingRegistration.logoSettings.font;
		UpdateSaveButton();
	}

	private void SetUpColorLists(ColorListUI colorListUI)
	{
		SetUpColorLists();
	}

	private void SetUpColorLists()
	{
		logoColorList.SetUp();
		logoColorList.onColorChanged = delegate(Color color)
		{
			shape.color = color;
		};
		logoColorList.getInitialColor = () => _logoColor;
		fontColorList.SetUp();
		fontColorList.onColorChanged = delegate(Color color)
		{
			businessName.color = color;
		};
		fontColorList.getInitialColor = () => _fontColor;
		backgroundColorList.SetUp();
		backgroundColorList.onColorChanged = delegate(Color color)
		{
			background.color = color;
		};
		backgroundColorList.getInitialColor = () => _backgroundColor;
	}

	private void UpdateSaveButton()
	{
		bool interactable = _bizManBusiness.buildingRegistration.logoSettings.logoShape != _logoShape || _bizManBusiness.buildingRegistration.logoSettings.backgroundColor != _backgroundColor || _bizManBusiness.buildingRegistration.logoSettings.logoColor != _logoColor || _bizManBusiness.buildingRegistration.logoSettings.fontColor != _fontColor || _bizManBusiness.buildingRegistration.logoSettings.font != _logoFont;
		saveLogoChangesButton.interactable = interactable;
	}

	public void SaveLogo()
	{
		_bizManBusiness.buildingRegistration.logoSettings.logoShape = _logoShape;
		_bizManBusiness.buildingRegistration.logoSettings.backgroundColor = _backgroundColor;
		_bizManBusiness.buildingRegistration.logoSettings.logoColor = _logoColor;
		_bizManBusiness.buildingRegistration.logoSettings.fontColor = _fontColor;
		_bizManBusiness.buildingRegistration.logoSettings.font = _logoFont;
		UpdateSaveButton();
		BusinessLogoGenerator.Create(_bizManBusiness.buildingRegistration.BusinessName, _bizManBusiness.buildingRegistration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(_bizManBusiness.buildingRegistration.BusinessName), _bizManBusiness.buildingRegistration.RentedByPlayer, delegate
		{
			GlobalEvents.onBuildingRegistrationChange?.Invoke(_bizManBusiness.buildingRegistration.Address);
			InstanceBehavior<CityManager>.Instance.UpdateBillboardsFromBusiness(_bizManBusiness.buildingRegistration.BusinessName);
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.LoadBusinessLogo();
		});
	}

	private void SelectLogoShape(string logoShape)
	{
		if (logoShape == null)
		{
			if (shape.sprite == null || LogoHelper.GetLogoSprite(shape.sprite.name) == null)
			{
				shape.sprite = LogoHelper.GetDefaultLogoSprite();
			}
		}
		else
		{
			Sprite logoSprite = LogoHelper.GetLogoSprite(logoShape);
			if (logoSprite != null)
			{
				shape.sprite = logoSprite;
				_logoShape = shape.sprite.name;
			}
		}
		UpdateSaveButton();
	}

	private void SelectBackgroundColor(Color color)
	{
		background.color = color;
		_backgroundColor = color;
		UpdateSaveButton();
	}

	private void SelectLogoColor(Color color)
	{
		shape.color = color;
		_logoColor = color;
		UpdateSaveButton();
	}

	private void SelectFontColor(Color color)
	{
		businessName.color = color;
		_fontColor = color;
		UpdateSaveButton();
	}

	private void SelectFont(int index)
	{
		businessName.font = InstanceBehavior<GlobalReferences>.Instance.signFontAssets[index];
		Enum.TryParse<FontFace>(InstanceBehavior<GlobalReferences>.Instance.signFontAssets[index].name, out _logoFont);
		UpdateSaveButton();
	}
}
