using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/WholesaleStoreInYourBusinessNeighborhood")]
public class WholesaleStoreInYourBusinessNeighborhood : QuestEntryTarget
{
	[SerializeField]
	private CustomBuildingTarget customBuildingTarget;

	[NonSerialized]
	private BuildingRegistration _wholesaleRegistration;

	[NonSerialized]
	private List<BuildingRegistration> _wholesaleRegistrations;

	protected override void OnInit()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	protected override void OnDispose()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void OnBuildingRegistrationChange(Address _)
	{
		SetTarget();
	}

	public override Address GetAddress()
	{
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return null;
		}
		string neighborhood = buildingRegistration.Neighborhood;
		if (_wholesaleRegistration != null && _wholesaleRegistration.Neighborhood == neighborhood)
		{
			return _wholesaleRegistration.Address;
		}
		BuildingRegistration buildingRegistration2 = FindWholesaleStoreInSameNeighborhood(neighborhood);
		if (buildingRegistration2 != null)
		{
			return buildingRegistration2.Address;
		}
		_wholesaleRegistration = BuildingHelper.FindClosestWholesaleStore(buildingRegistration.Address);
		return _wholesaleRegistration?.Address;
	}

	private BuildingRegistration FindWholesaleStoreInSameNeighborhood(string neighborhood)
	{
		foreach (BuildingRegistration wholesaleBuildingRegistration in BuildingHelper.GetWholesaleBuildingRegistrations())
		{
			if (!(wholesaleBuildingRegistration.Neighborhood != neighborhood))
			{
				_wholesaleRegistration = wholesaleBuildingRegistration;
				return wholesaleBuildingRegistration;
			}
		}
		return null;
	}
}
