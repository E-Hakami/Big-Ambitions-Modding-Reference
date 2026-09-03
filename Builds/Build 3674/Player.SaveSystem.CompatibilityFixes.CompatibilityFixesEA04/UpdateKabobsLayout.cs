namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateKabobsLayout : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		for (int i = 0; i < gameInstance.BuildingRegistrations.Count; i++)
		{
			if (gameInstance.BuildingRegistrations[i].BusinessName == "Kabob's Kebabs")
			{
				gameInstance.BuildingRegistrations[i].Layout = "Kabobs";
			}
		}
	}
}
