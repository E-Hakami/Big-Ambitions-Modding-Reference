using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class RestoreElGatoFoodMarket : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		SaveGameManager.Current.BuildingRegistrations.RemoveAll((BuildingRegistration x) => (bool)x.BuildingCached && x.Address == TutorialHelper.ElGatoAddress);
	}
}
