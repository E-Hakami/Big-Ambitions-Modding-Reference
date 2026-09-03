using System.Linq;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Streets;
using UnityEngine;

namespace Buildings;

public static class BuildingCommands
{
	[ConsoleMethod("EnterBuilding", "Teleports the player to a building with the address", new string[] { }, AutoCompleteMap = new string[] { "streetName=StreetNames" })]
	public static void Command_EnterBuilding(int houseNumber, string streetName)
	{
		if (!InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.isActiveAndEnabled)
		{
			Debug.LogWarning("Character movement needs to be enabled to run this command");
		}
		else
		{
			InstanceBehavior<BuildingManager>.Instance.EnterBuilding(BuildingHelper.GetBuilding(new Address(streetName, houseNumber)));
		}
	}

	[ConsoleMethod("EnterBuilding", "Teleports the player to a building with the business name. Can be a part of the name only", new string[] { })]
	public static void Command_EnterBuilding(string businessName)
	{
		string businessNameSimplified = businessName.ToLower().Replace(" ", string.Empty);
		Building building = (from x in BuildingHelper.allBuildings
			where x.GetRegistration()?.BusinessName?.ToLower().Replace(" ", string.Empty).Contains(businessNameSimplified) == true
			orderby StringHelper.CalculateSimilarity(businessName, x.GetRegistration().BusinessName)
			select x).FirstOrDefault();
		if (building == null)
		{
			Debug.LogWarning("No business with '" + businessName + "' in the name found");
		}
		else
		{
			InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building);
		}
	}

	[ConsoleMethod("RentBuilding", "Rents the building in the address", new string[] { }, AutoCompleteMap = new string[] { "streetName=StreetNames" })]
	public static void Command_RentBuilding(int houseNumber, string streetName)
	{
		Address address = new Address(streetName, houseNumber);
		Building building = BuildingHelper.GetBuilding(address);
		if (building == null)
		{
			Debug.LogWarning("No building found on address " + address.ToFormattedString());
			return;
		}
		BuildingHelper.RentBuilding(building, 0f, 0f);
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building);
	}

	[ConsoleMethod("RentBuilding", "Rents the building with the size and version", new string[] { }, AutoCompleteMap = new string[] { "buildingSize=BuildingSizes" })]
	public static void Command_RentBuilding(string buildingSize, int buildingVersion)
	{
		Building building = (from x in BuildingHelper.allBuildings.FindAll((Building x) => x.BuildingSize == buildingSize && x.BuildingVersion == buildingVersion)
			orderby x.GetRegistration().AvailableForRent
			select x).FirstOrDefault();
		if (building == null)
		{
			Debug.LogWarning($"No building found with size {buildingSize} and version {buildingVersion}");
			return;
		}
		BuildingHelper.RentBuilding(building, 0f, 0f);
		InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building);
	}
}
