using System;

namespace PlayerActivity;

[Serializable]
public class SleepEnvironment : PlayerActivityEnvironment<SleepEnvironmentConfig>, IPlayerActivityType
{
	public PlayerActivityBalanceConfig BalanceConfig => base.Config.balanceConfig;

	public PlayerActivityBalanceConfig LuxuryOverrideBalanceConfig => base.Config.luxuryOverrideBalanceConfig;

	public SleepEnvironmentType SleepEnvironmentType => base.Config.sleepEnvironmentType;

	public EnergyRegen EnergyRegen => base.Config.energyRegen;

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return new SleepActivity(this, attachedEntity);
	}

	public int GetDefaultMinutes()
	{
		int savedDefaultMinutes = GetSavedDefaultMinutes();
		if (!(BalanceConfig != null))
		{
			return savedDefaultMinutes;
		}
		return BalanceConfig.GetDefaultMinutes(savedDefaultMinutes);
	}

	public void SetDefaultMinutes(int minutes)
	{
		switch (SleepEnvironmentType)
		{
		case SleepEnvironmentType.Bed:
			SaveGameManager.Current.PlayerDefaults.sleepInBedMinutes = minutes;
			break;
		case SleepEnvironmentType.Car:
			SaveGameManager.Current.PlayerDefaults.sleepInCarMinutes = minutes;
			break;
		case SleepEnvironmentType.Boat:
			SaveGameManager.Current.PlayerDefaults.sleepInBoatMinutes = minutes;
			break;
		case (SleepEnvironmentType)2:
			break;
		}
	}

	private int GetSavedDefaultMinutes()
	{
		return SleepEnvironmentType switch
		{
			SleepEnvironmentType.Bed => SaveGameManager.Current.PlayerDefaults.sleepInBedMinutes, 
			SleepEnvironmentType.Car => SaveGameManager.Current.PlayerDefaults.sleepInCarMinutes, 
			SleepEnvironmentType.Boat => SaveGameManager.Current.PlayerDefaults.sleepInBoatMinutes, 
			_ => 0, 
		};
	}
}
