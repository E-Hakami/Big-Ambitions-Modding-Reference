using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Localizor;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CustomUI;

public class PreviewTerminalUI : MonoBehaviour
{
	[SerializeField]
	private UI.Elements.Dropdown buildingTypeDropdown;

	[SerializeField]
	private UI.Elements.Dropdown buildingSizeDropdown;

	[SerializeField]
	private UI.Elements.Dropdown businessTypeDropdown;

	[SerializeField]
	private UI.Elements.Dropdown designDropdown;

	[SerializeField]
	private Button previewDesignButton;

	private InteriorInstallationFirmSettings _firmSettings;

	private List<BuildingSizeInfo> _buildingSizes;

	private string[] _businessTypeNames;

	private string[] _designs;

	private string _selectedBuildingType;

	private BuildingSizeInfo _selectedBuildingSize;

	private string _selectedBusinessTypeName;

	private string _selectedDesignName;

	private int _blueprintStartIndex;

	private bool _isSelectedDesignBlueprint;

	private bool _dropdownsInitialized;

	public static bool IsVisible { get; private set; }

	public static void Show()
	{
		InstanceBehavior<UIs>.Instance.previewTerminalUI.ShowTerminal();
	}

	private void ShowTerminal()
	{
		base.gameObject.SetActive(value: true);
		IsVisible = true;
		OnOpenTerminal();
	}

	public void OpenBlueprintPanel()
	{
		HideTerminal();
		InstanceBehavior<UIs>.Instance.miniMenuUI.Toggle(show: true);
		InstanceBehavior<UIs>.Instance.miniMenuUI.OpenBlueprints();
	}

	public void PreviewDesign()
	{
		PreviewDesignAsync();
	}

	private async Task PreviewDesignAsync()
	{
		BusinessLayoutSet businessLayoutSet;
		if (_isSelectedDesignBlueprint)
		{
			Blueprint blueprint = await BlueprintsFolderLoader.GetBlueprint(_selectedDesignName);
			if (blueprint == null)
			{
				return;
			}
			businessLayoutSet = await blueprint.GetLayout();
			if (string.IsNullOrEmpty(businessLayoutSet.LayoutName))
			{
				businessLayoutSet.LayoutName = _selectedDesignName;
			}
		}
		else
		{
			businessLayoutSet = InteriorInstallationFirmHelper.GetInteriorDesignLayout(_selectedDesignName, _selectedBuildingType, _selectedBuildingSize, _selectedBusinessTypeName);
		}
		HideTerminal(unpauseGame: false);
		InstanceBehavior<UIs>.Instance.buildingPreview.PreviewLayout(businessLayoutSet);
	}

	private void OnOpenTerminal()
	{
		_firmSettings = InstanceBehavior<BuildingManager>.Instance.building.SpecialService?.settings as InteriorInstallationFirmSettings;
		if (_firmSettings == null)
		{
			HideTerminal();
			return;
		}
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		if (!_dropdownsInitialized)
		{
			InitializeDropdowns();
		}
		SetBuildingTypeDropdown();
	}

	public void HideTerminal(bool unpauseGame = true)
	{
		base.gameObject.SetActive(value: false);
		IsVisible = false;
		if (unpauseGame)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		}
	}

	private void SetBuildingTypeDropdown()
	{
		buildingTypeDropdown.SetOptions(_firmSettings.buildingTypesThatCanInstall.ToList());
		buildingSizeDropdown.gameObject.SetActive(value: false);
		businessTypeDropdown.gameObject.SetActive(value: false);
		designDropdown.gameObject.SetActive(value: false);
		previewDesignButton.interactable = false;
		if (_firmSettings.buildingTypesThatCanInstall.Count == 1)
		{
			buildingTypeDropdown.SelectOption(0);
		}
	}

	private void InitializeDropdowns()
	{
		buildingTypeDropdown.SetPlaceholder("preview_terminal_select_building_type");
		buildingTypeDropdown.onOptionSelected.AddListener(SelectBuildingType);
		buildingSizeDropdown.SetPlaceholder("preview_terminal_select_building_size");
		buildingSizeDropdown.onOptionSelected.AddListener(SelectBuildingSize);
		businessTypeDropdown.SetPlaceholder("preview_terminal_select_business_type");
		businessTypeDropdown.onOptionSelected.AddListener(SelectBusinessType);
		designDropdown.SetPlaceholder("preview_terminal_select_design");
		designDropdown.onOptionSelected.AddListener(SelectDesign);
		_dropdownsInitialized = true;
	}

	private void SelectBuildingType(int index)
	{
		_selectedBuildingType = _firmSettings.buildingTypesThatCanInstall[index];
		_buildingSizes = BuildingSizeHelper.GetBuildingSizesForBuildingType(_selectedBuildingType);
		buildingSizeDropdown.SetOptions(_buildingSizes.Select((BuildingSizeInfo x) => x.ToString()).ToList(), localize: false);
		buildingSizeDropdown.gameObject.SetActive(value: true);
		businessTypeDropdown.gameObject.SetActive(value: false);
		designDropdown.gameObject.SetActive(value: false);
		previewDesignButton.interactable = false;
	}

	private void SelectBuildingSize(int index)
	{
		_selectedBuildingSize = _buildingSizes[index];
		if (BuildingTypeHelper.GetData(_selectedBuildingType).HasTag(TagRef.Buildingtypetag.containsnobusiness))
		{
			businessTypeDropdown.gameObject.SetActive(value: false);
			_selectedBusinessTypeName = "ba:businesstype_empty";
			previewDesignButton.interactable = false;
			LoadResidentialDesignOptions();
		}
		else
		{
			_businessTypeNames = InteriorInstallationFirmHelper.GetBusinessTypesForBuilding(_selectedBuildingType, _selectedBuildingSize);
			_selectedBusinessTypeName = "ba:businesstype_empty";
			businessTypeDropdown.SetOptions(_businessTypeNames.ToList());
			businessTypeDropdown.gameObject.SetActive(value: true);
			designDropdown.gameObject.SetActive(value: false);
		}
		previewDesignButton.interactable = false;
	}

	private async void LoadResidentialDesignOptions()
	{
		string[] defaultKeys = InteriorInstallationFirmHelper.GetInteriorDesignsNamesForBuilding(_selectedBuildingType, _selectedBuildingSize);
		List<string> defaultNames = defaultKeys.Select((string x) => ("interior_design_" + x).GetLocalization()).ToList();
		_blueprintStartIndex = defaultNames.Count;
		List<string> second = await InteriorInstallationFirmHelper.GetBlueprintNames(_selectedBuildingType, _selectedBuildingSize, "ba:businesstype_empty");
		_designs = defaultKeys.Concat(second).ToArray();
		List<string> newOptions = defaultNames.Concat(second).ToList();
		designDropdown.SetOptions(newOptions, localize: false);
		designDropdown.gameObject.SetActive(value: true);
	}

	private async void SelectBusinessType(int index)
	{
		_selectedBusinessTypeName = _businessTypeNames[index];
		_designs = InteriorInstallationFirmHelper.GetInteriorDesignsNamesForBuilding(_selectedBuildingType, _selectedBuildingSize, _selectedBusinessTypeName);
		List<string> defaultDesignNames = _designs.Select((string x) => ("interior_design_" + x).GetLocalization()).ToList();
		_blueprintStartIndex = defaultDesignNames.Count;
		List<string> second = await InteriorInstallationFirmHelper.GetBlueprintNames(_selectedBuildingType, _selectedBuildingSize, _selectedBusinessTypeName);
		_designs = _designs.Concat(second).ToArray();
		List<string> newOptions = defaultDesignNames.Concat(second).ToList();
		designDropdown.SetOptions(newOptions, localize: false);
		designDropdown.gameObject.SetActive(value: true);
		previewDesignButton.interactable = false;
	}

	private void SelectDesign(int index)
	{
		_selectedDesignName = _designs[index];
		_isSelectedDesignBlueprint = index >= _blueprintStartIndex;
		previewDesignButton.interactable = true;
	}
}
