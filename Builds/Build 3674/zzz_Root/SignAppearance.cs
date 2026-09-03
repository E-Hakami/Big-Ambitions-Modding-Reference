using System;
using System.Collections.Generic;
using Enums;
using Extensions;
using Localizor;
using UI;
using UI.Elements;
using UnityEngine;

public class SignAppearance : MonoBehaviour
{
	public RectTransform panel;

	[SerializeField]
	private Dropdown signTypeDropdown;

	[SerializeField]
	private ColorListUI signColorList;

	[SerializeField]
	private ColorListUI bulbColorList;

	[SerializeField]
	private SignType[] signTypesWithNeon;

	[SerializeField]
	private SignType[] signTypesWithBulbs;

	private BuildingRegistration _buildingRegistration;

	private CityBuildingController _cbc;

	private Color _currentSignColor;

	private Color _currentBulbColor;

	public bool IsOpen => panel.gameObject.activeInHierarchy;

	private void Start()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.AddListener(Close);
		_currentSignColor = _buildingRegistration?.signAppearanceSettings?.signLight ?? ((SerializableColor)Color.white);
		_currentBulbColor = _buildingRegistration?.signAppearanceSettings?.lamp ?? ((SerializableColor)Color.white);
		List<string> list = new List<string>();
		for (int i = 1; i <= Enum.GetValues(typeof(SignType)).Length; i++)
		{
			list.Add(LocalizorManager.GetLocalization("sign_type", new
			{
				number = i
			}));
		}
		signTypeDropdown.SetOptions(list, localize: false);
		signTypeDropdown.SetPlaceholder("common_select_type");
		signTypeDropdown.onOptionSelected.AddListener(delegate(int typeIndex)
		{
			_buildingRegistration.signAppearanceSettings.signType = (SignType)typeIndex;
			UpdateColorLists((SignType)typeIndex);
			_cbc.UpdateSign();
		});
		signColorList.SetUp();
		signColorList.getInitialColor = () => _currentSignColor;
		signColorList.onColorChanged = PreviewSignColor;
		ColorListUI colorListUI = signColorList;
		colorListUI.onSelectColor = (Action<Color>)Delegate.Combine(colorListUI.onSelectColor, new Action<Color>(SelectSignColor));
		signColorList.onRefresh = delegate
		{
			bulbColorList.SetUp();
		};
		bulbColorList.SetUp();
		bulbColorList.getInitialColor = () => _currentBulbColor;
		bulbColorList.onColorChanged = PreviewBulbColor;
		ColorListUI colorListUI2 = bulbColorList;
		colorListUI2.onSelectColor = (Action<Color>)Delegate.Combine(colorListUI2.onSelectColor, new Action<Color>(SelectBulbColor));
		bulbColorList.onRefresh = delegate
		{
			signColorList.SetUp();
		};
	}

	private void OnDestroy()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		if ((bool)InstanceBehavior<GameManager>.Instance && (bool)InstanceBehavior<GameManager>.Instance.playerController)
		{
			InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.RemoveListener(Close);
		}
	}

	public void SetBuilding(BuildingRegistration registration)
	{
		_buildingRegistration = registration;
		_cbc = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(registration.Address);
		_currentSignColor = registration.signAppearanceSettings.signLight;
		_currentBulbColor = registration.signAppearanceSettings.lamp;
		signTypeDropdown.ResetSelectedOption((int)_buildingRegistration.signAppearanceSettings.signType);
		UpdateColorLists(_buildingRegistration.signAppearanceSettings.signType);
		panel.gameObject.SetActive(value: true);
	}

	public void Close()
	{
		CustomColorPicker customColorPicker = InstanceBehavior<UIs>.Instance.customColorPicker;
		if (IsOpen && (bool)customColorPicker && customColorPicker.gameObject.activeInHierarchy)
		{
			customColorPicker.Cancel();
		}
		panel.gameObject.SetActive(value: false);
	}

	private void OnEnterBuilding(Address _)
	{
		Close();
	}

	private void PreviewSignColor(Color color)
	{
		_buildingRegistration.signAppearanceSettings.signLight = color;
		_cbc.UpdateSign();
	}

	private void SelectSignColor(Color color)
	{
		_currentSignColor = color;
		PreviewSignColor(color);
	}

	private void PreviewBulbColor(Color color)
	{
		_buildingRegistration.signAppearanceSettings.lamp = color;
		_cbc.UpdateSign();
	}

	private void SelectBulbColor(Color color)
	{
		_currentBulbColor = color;
		PreviewBulbColor(color);
	}

	private void UpdateColorLists(SignType signType)
	{
		signColorList.gameObject.SetActive(signTypesWithNeon.InCollection(signType));
		bulbColorList.gameObject.SetActive(signTypesWithBulbs.InCollection(signType));
	}
}
