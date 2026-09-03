using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes;

public static class SaveGameCompatibilityFixes
{
	public static bool forcePlayerExitForCompatibility;

	private static readonly ICompatibilityVersion[] CompatibilityVersions = new ICompatibilityVersion[11]
	{
		new UpdateGameInstanceToEA02(),
		new UpdateGameInstanceToEA03(),
		new UpdateGameInstanceToEA04(),
		new UpdateGameInstanceToEA05(),
		new UpdateGameInstanceToEA06(),
		new UpdateGameInstanceToEA07(),
		new UpdateGameInstanceToEA08(),
		new UpdateGameInstanceToEA09(),
		new UpdateGameInstanceToEA010(),
		new UpdateGameInstanceToEA011(),
		new UpdateGameInstanceTo1()
	};

	public static void ApplyCompatibilityFixes(GameInstance gameInstance)
	{
		CompatibilityHelper.Init(gameInstance);
		CompatibilityItemValidator.ValidateGameInstance(gameInstance);
		ICompatibilityVersion[] compatibilityVersions = CompatibilityVersions;
		for (int i = 0; i < compatibilityVersions.Length; i++)
		{
			compatibilityVersions[i].RunPriority(gameInstance);
		}
		compatibilityVersions = CompatibilityVersions;
		for (int i = 0; i < compatibilityVersions.Length; i++)
		{
			compatibilityVersions[i].Run(gameInstance);
		}
		CompatibilityHelper.SaveLayoutSets();
		if (gameInstance.buildNumberAtLastSave < GameVersion.GetCurrent().buildNumber)
		{
			Debug.Log($"Updated save from {gameInstance.buildNumberAtLastSave} to" + $" {GameVersion.GetCurrent().buildNumber} (original version: {gameInstance.buildNumberAtStart})");
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		forcePlayerExitForCompatibility = false;
	}
}
