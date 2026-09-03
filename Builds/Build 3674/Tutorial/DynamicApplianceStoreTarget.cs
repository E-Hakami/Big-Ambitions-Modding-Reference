using System;
using System.Collections.Generic;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/DynamicApplianceStoreTarget")]
public class DynamicApplianceStoreTarget : QuestEntryTarget
{
	[SerializeField]
	private CustomBuildingTarget customBuildingTarget;

	[SerializeField]
	private QuestEntryTarget[] applianceStoreTargets;

	private Address _targetAddress;

	protected override void OnInit()
	{
		_targetAddress = null;
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	protected override void OnDispose()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void OnBuildingRegistrationChange(Address _)
	{
		_targetAddress = null;
		SetTarget();
	}

	public override Address GetAddress()
	{
		if (_targetAddress != null)
		{
			return _targetAddress;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			_targetAddress = null;
			return _targetAddress;
		}
		QuestEntryTarget[] array = applianceStoreTargets;
		foreach (QuestEntryTarget questEntryTarget in array)
		{
			List<string> listOfItemsForSale = BuildingHelper.GetBuildingRegistration(questEntryTarget.GetAddress()).GetListOfItemsForSale();
			List<BusinessRequirement> businessRequirements = BusinessTypeHelper.GetData(buildingRegistration).businessRequirements;
			bool flag = true;
			foreach (BusinessRequirement item2 in businessRequirements)
			{
				string[] requiredItemsForTutorialPointers = item2.GetRequiredItemsForTutorialPointers(buildingRegistration);
				foreach (string item in requiredItemsForTutorialPointers)
				{
					if (!listOfItemsForSale.Contains(item))
					{
						flag = false;
						break;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			if (flag)
			{
				_targetAddress = questEntryTarget.GetAddress();
				return _targetAddress;
			}
		}
		return _targetAddress;
	}
}
