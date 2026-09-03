using System.Linq;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixNoBuildingsForRentDuringSecondRetailQuest : ICompatibilityFix
{
	private const string ObjectiveId = "4531C6CD-7B88-4CB4-885C-EFE7FCB036CE";

	private const string AffectedBuildingSize = "ba:buildingsize_a";

	public void Apply(GameInstance gameInstance)
	{
		if (!TutorialHelper.HasCompletedObjective("4531C6CD-7B88-4CB4-885C-EFE7FCB036CE") && !gameInstance.BuildingRegistrations.Exists((BuildingRegistration x) => x.AvailableForRent && x.BuildingCached.BuildingSize == "ba:buildingsize_a" && x.BuildingCached.BuildingType == "ba:buildingtype_retail" && x.BuildingCached.trafficIndex >= 30))
		{
			BuildingRegistration random = gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && x.BuildingCached.BuildingSize == "ba:buildingsize_a" && x.BuildingCached.BuildingType == "ba:buildingtype_retail" && x.BuildingCached.trafficIndex >= 30).GetRandom();
			if (random != null)
			{
				BusinessHelper.SetBuildingForRent(random);
			}
		}
	}
}
