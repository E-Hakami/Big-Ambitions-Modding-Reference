using System.Linq;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixNoBuildingsForRentDuringFirstRetailQuest : ICompatibilityFix
{
	private const string ObjectiveId = "3BFD00B0-6E08-4F53-97A0-73013AEFE151";

	private const string AffectedBuildingSize = "ba:buildingsize_a";

	private const string AffectedNeighborhood = "ba:neighborhood_garmentdistrict";

	public void Apply(GameInstance gameInstance)
	{
		if (!TutorialHelper.HasCompletedObjective("3BFD00B0-6E08-4F53-97A0-73013AEFE151") && !gameInstance.BuildingRegistrations.Exists((BuildingRegistration x) => x.AvailableForRent && x.BuildingCached.BuildingSize == "ba:buildingsize_a" && x.BuildingCached.BuildingType == "ba:buildingtype_retail" && x.BuildingCached.Neighbourhood == "ba:neighborhood_garmentdistrict"))
		{
			BuildingRegistration random = gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && x.BuildingCached.BuildingSize == "ba:buildingsize_a" && x.BuildingCached.BuildingType == "ba:buildingtype_retail" && x.BuildingCached.Neighbourhood == "ba:neighborhood_garmentdistrict").GetRandom();
			if (random != null)
			{
				BusinessHelper.SetBuildingForRent(random);
			}
		}
	}
}
