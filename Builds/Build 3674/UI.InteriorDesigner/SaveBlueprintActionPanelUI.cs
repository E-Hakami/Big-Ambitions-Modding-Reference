using System.Collections.Generic;
using System.Linq;
using BigAmbitions.BlueprintCreator;
using BigAmbitions.InteriorDesigner;
using Blueprints;
using Helpers;
using Localizor;
using UnityEngine;

namespace UI.InteriorDesigner;

public class SaveBlueprintActionPanelUI : ActionPanelUI
{
	[SerializeField]
	private SaveBlueprintUI saveBlueprintUI;

	private List<string> _businessTypeOptions = new List<string>();

	private List<string> _businessTypes = new List<string>();

	public override ToolName[] ToolNames => new ToolName[1] { ToolName.SaveBlueprint };

	public override void OnOpen()
	{
		Blueprint openWithBlueprint = BlueprintCreatorSystem.OpenWithBlueprint;
		string text = "ba:businesstype_empty";
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		string buildingType;
		if (openWithBlueprint != null)
		{
			text = openWithBlueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName);
			buildingType = openWithBlueprint.metadata.buildingType;
		}
		else
		{
			buildingType = buildingRegistration.GetBuildingType();
		}
		if (text == "ba:businesstype_empty")
		{
			text = buildingRegistration.businessTypeName;
		}
		if (text == "ba:businesstype_empty" && buildingType != "ba:buildingtype_residential")
		{
			ShowBusinessTypeSelector(buildingType);
		}
		else
		{
			saveBlueprintUI.OpenPanel();
		}
	}

	private void ShowBusinessTypeSelector(string buildingType)
	{
		IEnumerable<BusinessType> enumerable;
		if (!(buildingType == "ba:buildingtype_special"))
		{
			enumerable = BusinessTypeHelper.GetAllPlayerAvailableBusinesses(buildingType);
		}
		else
		{
			IEnumerable<BusinessType> specialBusinesses = BusinessTypeHelper.GetSpecialBusinesses();
			enumerable = specialBusinesses;
		}
		IEnumerable<BusinessType> source = enumerable;
		_businessTypes = source.Select((BusinessType x) => x.businessTypeName).ToList();
		_businessTypeOptions = _businessTypes.Select((string bt) => bt.GetLocalization()).ToList();
		DropdownSelector.Show(_businessTypeOptions, OnBusinessTypeSelected, "dropdown_selector_header", "blueprint_creator_select_business_type");
	}

	private void OnBusinessTypeSelected(string businessTypeName)
	{
		if (string.IsNullOrEmpty(businessTypeName))
		{
			saveBlueprintUI.OpenPanel();
			return;
		}
		int num = _businessTypeOptions.IndexOf(businessTypeName);
		string text = ((num >= 0 && num < _businessTypes.Count) ? _businessTypes[num] : "ba:businesstype_empty");
		if (text == "ba:businesstype_empty")
		{
			saveBlueprintUI.OpenPanel();
			Debug.LogError("Selected business type is empty. Saving without a business type.");
			return;
		}
		if (BlueprintCreatorSystem.OpenWithBlueprint != null)
		{
			BlueprintCreatorSystem.OpenWithBlueprint.metadata.otherData.Add(new BlueprintDataElement(DataElement.BusinessTypeName, text.ToString()));
			BlueprintCreatorSystem.OpenWithBlueprint.UpdateMetadata();
		}
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName = text;
		saveBlueprintUI.OpenPanel();
	}

	public override void OnClose()
	{
		if (saveBlueprintUI.IsOpen)
		{
			saveBlueprintUI.ClosePanel();
		}
	}

	public override void OnEnterInteriorDesignerMode()
	{
		saveBlueprintUI.gameObject.SetActive(value: false);
	}
}
