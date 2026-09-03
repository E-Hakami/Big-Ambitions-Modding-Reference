namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class KickOutBuildingCompatibility : ICompatibilityFix
{
	private static readonly Address GymAddress = new Address("ba:street_fourthavenue", 28);

	private static readonly Address TotalProduceAddress = new Address("ba:street_sixthavenue", 6);

	public void Apply(GameInstance gameInstance)
	{
		CompatibilityHelper.KickOutPlayer(gameInstance, GymAddress);
		CompatibilityHelper.KickOutPlayer(gameInstance, TotalProduceAddress);
	}
}
