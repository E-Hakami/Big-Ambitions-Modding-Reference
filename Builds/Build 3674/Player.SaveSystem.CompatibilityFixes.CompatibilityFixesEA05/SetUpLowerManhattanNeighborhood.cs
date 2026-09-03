using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class SetUpLowerManhattanNeighborhood : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_lowermanhattan";

	public void Apply(GameInstance gameInstance)
	{
		CityGenerator.InitializeCity("ba:neighborhood_lowermanhattan");
	}
}
