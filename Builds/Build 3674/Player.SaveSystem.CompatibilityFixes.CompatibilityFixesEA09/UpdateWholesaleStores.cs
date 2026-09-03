using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using Blueprints;
using Buildings;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateWholesaleStores : ICompatibilityFix
{
	private const string HellsKitchenWholesaleLayout = "HellsKitchenWholesaleStore";

	private const string MurrayHill = "ba:neighborhood_murrayhill";

	private const string LowerManhattan = "ba:neighborhood_lowermanhattan";

	private static readonly Address OldTotalProduceAddress = new Address("ba:street_sixthavenue", 6);

	private static readonly Address NewTotalProduceAddress = new Address("ba:street_sixthavenue", 4);

	private static readonly Address OldFactorySupplyDepotAddress = new Address("ba:street_fifthavenue", 57);

	private static readonly Address HellsKitchenWholesaleStoreAddress = new Address("ba:street_fifthavenue", 2);

	public void Apply(GameInstance gameInstance)
	{
		Address currentPlayerAddress = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address == OldTotalProduceAddress);
		if (buildingRegistration != null)
		{
			EvictPlayerAndUpdateOccupant(gameInstance, buildingRegistration, currentPlayerAddress);
			buildingRegistration = BuildingHelper.GetBuildingRegistration(OldTotalProduceAddress);
			buildingRegistration.AvailableForRent = true;
			FillRivalData(gameInstance);
			buildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding("ba:neighborhood_murrayhill");
		}
		BuildingRegistration buildingRegistration2 = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address == NewTotalProduceAddress);
		if (buildingRegistration2 != null)
		{
			EvictPlayerAndUpdateOccupant(gameInstance, buildingRegistration2, currentPlayerAddress);
		}
		BuildingRegistration buildingRegistration3 = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address.streetName == HellsKitchenWholesaleStoreAddress.streetName && x.Address.streetNumber == HellsKitchenWholesaleStoreAddress.streetNumber);
		if (buildingRegistration3 != null)
		{
			buildingRegistration3.Layout = "HellsKitchenWholesaleStore";
		}
		BuildingRegistration buildingRegistration4 = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address == OldFactorySupplyDepotAddress);
		if (buildingRegistration4 != null)
		{
			EvictPlayerAndUpdateOccupant(gameInstance, buildingRegistration4, currentPlayerAddress);
			buildingRegistration4 = BuildingHelper.GetBuildingRegistration(OldFactorySupplyDepotAddress);
			buildingRegistration4.AvailableForRent = true;
			FillRivalData(gameInstance);
			buildingRegistration4.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding("ba:neighborhood_lowermanhattan");
		}
	}

	private static void EvictPlayerAndUpdateOccupant(GameInstance gameInstance, BuildingRegistration registration, Address currentPlayerAddress)
	{
		Address address = registration.Address;
		BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(registration);
		string text = BusinessTypeHelper.GetData(registration).suitableBuildingType;
		if (text == "ba:buildingtype_special")
		{
			text = BuildingSizeHelper.GetBuildingTypeBySizeInfo(buildingSizeInfo);
		}
		CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupant(address, buildingSizeInfo, text);
		if (!(currentPlayerAddress != address) && gameInstance.charactersData.Count != 0)
		{
			gameInstance.charactersData[0].itemInHands?.cargoInstances.RemoveAll((CargoInstance x) => !x.paid);
		}
	}

	private static void FillRivalData(GameInstance gameInstance)
	{
		List<string> list = new List<string>(gameInstance.rivalStates.Count);
		foreach (RivalState rivalState in gameInstance.rivalStates)
		{
			list.Add(rivalState.rivalId);
		}
		RivalsHelper.FillData(list);
	}
}
