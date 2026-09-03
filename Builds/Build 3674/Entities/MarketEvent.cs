using System;
using System.Collections.Generic;
using Buildings;
using HGAttributes;
using Helpers;
using UnityEngine.Serialization;

namespace Entities;

[Serializable]
public class MarketEvent
{
	public MarketEventType type;

	public int durationInDays;

	[AutocompleteDropdown("Items")]
	public string itemName;

	[Obsolete]
	[AutocompleteDropdown("Items")]
	public List<string> itemNames;

	public string neighbourhood;

	public int startDay;

	public int demandImpact;

	public bool stopped;

	public int dayStopped;

	public string businessName;

	public Address address;

	public string rivalName;

	[FormerlySerializedAs("businessType")]
	[AutocompleteDropdown("BusinessTypes")]
	public string businessTypeName;

	public bool IsActive
	{
		get
		{
			if (!stopped && SaveGameManager.Current.Day >= startDay)
			{
				return SaveGameManager.Current.Day < startDay + durationInDays;
			}
			return false;
		}
	}

	public bool IsPlannedOrActive
	{
		get
		{
			if (!stopped)
			{
				return SaveGameManager.Current.Day < startDay + durationInDays;
			}
			return false;
		}
	}

	public MarketEvent()
	{
	}

	public MarketEvent(MarketEventType type, int startDay, int durationInDays = 0, string neighbourhood = null, Address address = null, string itemName = null, int demandImpact = 0)
	{
		this.type = type;
		this.durationInDays = durationInDays;
		this.neighbourhood = neighbourhood;
		this.address = address;
		this.itemName = itemName;
		this.startDay = startDay;
		this.demandImpact = demandImpact;
	}

	public MarketEvent(MarketEventType type, int startDay, string businessName, Address address, string rivalName, string businessTypeName, string neighbourhood)
	{
		this.type = type;
		this.startDay = startDay;
		this.businessName = businessName;
		this.address = address;
		this.rivalName = rivalName;
		this.businessTypeName = businessTypeName;
		this.neighbourhood = neighbourhood;
	}

	public BuildingRegistration GetBuildingRegistration()
	{
		if (string.IsNullOrEmpty(neighbourhood))
		{
			return BuildingHelper.GetBuildingRegistration(address);
		}
		foreach (KeyValuePair<Address, Building> specialServiceBuilding in BuildingHelper.SpecialServiceBuildings)
		{
			if (specialServiceBuilding.Value.SpecialService.businessTypeName == "ba:businesstype_wholesalestore" && specialServiceBuilding.Value.Neighbourhood == neighbourhood)
			{
				return BuildingHelper.GetBuildingRegistration(specialServiceBuilding.Key);
			}
		}
		return null;
	}
}
