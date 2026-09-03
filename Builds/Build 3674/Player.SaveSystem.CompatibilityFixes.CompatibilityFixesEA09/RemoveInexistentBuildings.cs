using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using Streets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveInexistentBuildings : ICompatibilityFix
{
	public bool Priority => true;

	public void Apply(GameInstance gameInstance)
	{
		EmployeeHelper.EnsureInit(gameInstance);
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!(buildingRegistration.BuildingCached != null))
			{
				Address address = new Address(buildingRegistration.StreetName, buildingRegistration.StreetNumber);
				Debug.LogError("Building with address " + address.ToFormattedString() + " doesn't exist. Removing it from the savegame");
				list.Add(buildingRegistration);
				if (buildingRegistration.RentedByPlayer)
				{
					BuildingHelper.SellBuilding(address, $"{address} was sold (caused by compatibility support)");
				}
				RealEstate realEstate = gameInstance.realEstate.FirstOrDefault((RealEstate x) => x.address == address);
				if (realEstate != null)
				{
					RealEstateHelper.SellBuildingForCompat(realEstate);
				}
				gameInstance.buildingsForSale.RemoveAll((BuildingForSale x) => x.address == address);
			}
		}
		foreach (BuildingRegistration item in list)
		{
			gameInstance.BuildingRegistrations.Remove(item);
		}
	}
}
