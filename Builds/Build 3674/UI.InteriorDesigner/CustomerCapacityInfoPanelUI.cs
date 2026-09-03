using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using TMPro;
using UnityEngine;

namespace UI.InteriorDesigner;

public class CustomerCapacityInfoPanelUI : FoldingInfoPanelUI
{
	private static readonly List<Item.ItemCapacity> TempItemCapacities = new List<Item.ItemCapacity>();

	[Header("Customer Capacity Info Panel")]
	[SerializeField]
	private TMP_Text totalCustomerCapacityText;

	[SerializeField]
	private CustomerCapacityEntry entryTemplate;

	[SerializeField]
	private GameObject splitterPrefab;

	private readonly List<GameObject> _splitters = new List<GameObject>();

	private int _buildingLimit;

	public override bool ShouldShow()
	{
		if (InteriorDesignerHelper.BlueprintCreatorMode && BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.hasbusinessrequirements))
		{
			return !(InstanceBehavior<BuildingManager>.Instance.businessType?.businessTypeName == "ba:businesstype_headquarters");
		}
		return false;
	}

	public override void OnEnterInteriorDesignerMode()
	{
		Building building = InstanceBehavior<BuildingManager>.Instance.building;
		_buildingLimit = BuildingSizeHelper.GetData(building).GetCustomerCapacity(building.BuildingType, building.BuildingVersion);
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity = (Action)Delegate.Combine(InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity, new Action(UpdateCustomerCapacity));
		CoroutineUtility.RunAfterOneFrame(UpdateCustomerCapacity);
	}

	public override void OnExitInteriorDesignerMode()
	{
		InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity = (Action)Delegate.Remove(InteriorDesignerInfoPanelEvents.onUpdateCustomerCapacity, new Action(UpdateCustomerCapacity));
	}

	private void UpdateCustomerCapacity()
	{
		bool flag = ShouldShow();
		base.gameObject.SetActive(flag);
		if (!flag)
		{
			return;
		}
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		int num = 0;
		buildingRegistration.UpdateCachedAvailableProducts();
		TempItemCapacities.Clear();
		TempItemCapacities.AddRange(buildingRegistration.itemInstances.Values.GetItemsSortedByCapacity(buildingRegistration));
		bool flag2 = _buildingLimit == 9999;
		foreach (Item.ItemCapacity tempItemCapacity in TempItemCapacities)
		{
			if (num == 0 || num > tempItemCapacity.CustomersLimit)
			{
				num = tempItemCapacity.CustomersLimit;
			}
		}
		Color32 color = ((num < _buildingLimit) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		totalCustomerCapacityText.text = $"<color={color.ToHex()}>{num}</color> / {_buildingLimit}";
		TempItemCapacities.Sort((Item.ItemCapacity entry1, Item.ItemCapacity entry2) => entry1.CustomersLimit.CompareTo(entry2.CustomersLimit));
		entryTemplate.transform.ResetTemplate();
		foreach (GameObject splitter in _splitters)
		{
			UnityEngine.Object.Destroy(splitter);
		}
		_splitters.Clear();
		for (int num2 = 0; num2 < TempItemCapacities.Count; num2++)
		{
			if (num2 != 0)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(splitterPrefab, entryTemplate.transform.parent);
				gameObject.SetActive(value: true);
				_splitters.Add(gameObject);
			}
			Item.ItemCapacity itemCapacity = TempItemCapacities[num2];
			CustomerCapacityEntry customerCapacityEntry = UnityEngine.Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
			customerCapacityEntry.SetUp(itemCapacity, flag2 ? 9999 : _buildingLimit);
			customerCapacityEntry.gameObject.SetActive(value: true);
		}
		TempItemCapacities.Clear();
	}
}
