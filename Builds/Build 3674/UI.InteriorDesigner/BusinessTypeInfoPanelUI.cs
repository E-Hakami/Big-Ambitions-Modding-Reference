using System;
using System.Collections.Generic;
using BigAmbitions.BlueprintCreator;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class BusinessTypeInfoPanelUI : FoldingInfoPanelUI
{
	private const string EmptyBusinessTypeName = "ba:businesstype_empty";

	[Header("Business Type Info Panel")]
	[SerializeField]
	private UI.Elements.Dropdown businessTypeDropdown;

	[SerializeField]
	private Image businessTypeImage;

	[SerializeField]
	private TMP_Text buildingInfoLabel;

	private readonly List<string> _businessTypes = new List<string>();

	private void Awake()
	{
		businessTypeDropdown.onOptionSelected.AddListener(OnBusinessTypeChanged);
	}

	private void OnDestroy()
	{
		businessTypeDropdown.onOptionSelected.RemoveListener(OnBusinessTypeChanged);
	}

	public override bool ShouldShow()
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode && InstanceBehavior<BuildingManager>.Instance.building.BuildingType != "ba:buildingtype_special")
		{
			return !BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.containsnobusiness);
		}
		return false;
	}

	public override void OnEnterInteriorDesignerMode()
	{
		string buildingType = InstanceBehavior<BuildingManager>.Instance.building.BuildingType;
		BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building);
		buildingInfoLabel.SetText(buildingSizeInfo.ToString() + " - " + buildingType.GetLocalization());
		BuildingTypeData data = BuildingTypeHelper.GetData(buildingType);
		string[] availableBusinessTypes = data.availableBusinessTypes;
		string[] array = (GameManager.IsDevMode ? data.availableDevBusinessTypes : null);
		bool flag = GameManager.IsDevMode && !ContainsBusinessType(availableBusinessTypes, "ba:businesstype_empty") && !ContainsBusinessType(array, "ba:businesstype_empty");
		int num = availableBusinessTypes.Length;
		if (array != null && array.Length > 0)
		{
			num += array.Length;
		}
		if (flag)
		{
			num++;
		}
		_businessTypes.Clear();
		if (num == 0)
		{
			businessTypeDropdown.SetOptions(new List<string>(0), localize: false);
			return;
		}
		string[] array2 = new string[num];
		string[] array3 = new string[num];
		int index = 0;
		for (int i = 0; i < availableBusinessTypes.Length; i++)
		{
			AddBusinessTypeOption(availableBusinessTypes[i], array2, array3, ref index);
		}
		if (array != null && array.Length > 0)
		{
			for (int j = 0; j < array.Length; j++)
			{
				AddBusinessTypeOption(array[j], array2, array3, ref index);
			}
		}
		if (flag)
		{
			AddBusinessTypeOption("ba:businesstype_empty", array2, array3, ref index);
		}
		Array.Sort(array3, array2, StringComparer.CurrentCulture);
		for (int k = 0; k < num; k++)
		{
			_businessTypes.Add(array2[k]);
		}
		List<string> list = new List<string>(num);
		for (int l = 0; l < num; l++)
		{
			list.Add(array3[l]);
		}
		int optionIndex = 0;
		if (BlueprintCreatorSystem.OpenWithBlueprint != null)
		{
			string dataElementValue = BlueprintCreatorSystem.OpenWithBlueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName);
			optionIndex = _businessTypes.IndexOf(dataElementValue);
		}
		businessTypeDropdown.SetOptions(list, localize: false);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			businessTypeDropdown.SelectOption(Mathf.Max(optionIndex, 0));
		});
	}

	public override void OnExitInteriorDesignerMode()
	{
	}

	private static void AddBusinessTypeOption(string businessTypeName, string[] types, string[] labels, ref int index)
	{
		types[index] = businessTypeName;
		labels[index] = businessTypeName.GetLocalization();
		index++;
	}

	private static bool ContainsBusinessType(string[] businessTypes, string businessTypeName)
	{
		if (businessTypes == null)
		{
			return false;
		}
		for (int i = 0; i < businessTypes.Length; i++)
		{
			if (businessTypes[i] == businessTypeName)
			{
				return true;
			}
		}
		return false;
	}

	private void OnBusinessTypeChanged(int index)
	{
		string businessTypeName = _businessTypes[index];
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName = businessTypeName;
		BusinessType data = BusinessTypeHelper.GetData(businessTypeName);
		InstanceBehavior<BuildingManager>.Instance.businessType = data;
		businessTypeImage.sprite = data.icon;
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity?.Invoke();
	}
}
