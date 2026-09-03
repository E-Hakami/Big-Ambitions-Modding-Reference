using Blueprints;
using Buildings;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class MoveMetroWholesaleToHellsKitchen : ICompatibilityFix
{
	private static readonly BuildingSizeInfo SizeInfo = new BuildingSizeInfo("ba:buildingsize_f", 1);

	private static readonly Address OldAddress = new Address("ba:street_fifthavenue", 2);

	private static readonly Address NewAddress = new Address("ba:street_firststreet", 18);

	public void Apply(GameInstance gameInstance)
	{
		CompatibilityHelper.ReturnBuildingToMarket(OldAddress);
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address == NewAddress);
		if (buildingRegistration != null)
		{
			string text = BusinessTypeHelper.GetData(buildingRegistration).suitableBuildingType;
			if (text == "ba:buildingtype_special")
			{
				text = BuildingSizeHelper.GetBuildingTypeBySizeInfo(SizeInfo);
			}
			CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupant(NewAddress, SizeInfo, text);
		}
	}
}
