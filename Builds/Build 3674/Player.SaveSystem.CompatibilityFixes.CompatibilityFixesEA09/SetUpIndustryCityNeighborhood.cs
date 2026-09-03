using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class SetUpIndustryCityNeighborhood : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_industrycity";

	public void Apply(GameInstance gameInstance)
	{
		CityGenerator.InitializeCity("ba:neighborhood_industrycity");
		CityGenerator.DistributeBuildingsToRivals("ba:neighborhood_industrycity");
	}
}
