using System;
using BigAmbitions.Characters;
using PlayerActivity.Activities.Rest;
using UnityEngine;

namespace PlayerActivity;

[Serializable]
public class RestEnvironment : PlayerActivityEnvironment<RestEnvironmentConfig>, IPlayerActivityType
{
	public PlayerActivityBalanceConfig BalanceConfig => base.Config.balanceConfig;

	public PlayerActivityBalanceConfig WatchShowBalanceConfig => base.Config.watchShowBalanceConfig;

	public RestEnvironmentType EnvironmentType => base.Config.environmentType;

	public EnergyRegen EnergyRegen => base.Config.energyRegen;

	public PermanentAnimationType[] EntertainAnimationOverride => base.Config.entertainAnimationOverride;

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return new RestActivity(this, attachedEntity);
	}

	public PermanentAnimationType? GetEntertainAnimationOverride()
	{
		PermanentAnimationType[] entertainAnimationOverride = EntertainAnimationOverride;
		if (entertainAnimationOverride == null || entertainAnimationOverride.Length <= 0)
		{
			return null;
		}
		return EntertainAnimationOverride[UnityEngine.Random.Range(0, EntertainAnimationOverride.Length)];
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
		switch (EnvironmentType)
		{
		case RestEnvironmentType.OutsideBench:
		case RestEnvironmentType.Bench:
			SaveGameManager.Current.PlayerDefaults.sleepInBenchMinutes = minutes;
			break;
		case RestEnvironmentType.Chair:
		case RestEnvironmentType.OutsideChair:
		case RestEnvironmentType.DeckChair:
		case RestEnvironmentType.BeachTowel:
		case RestEnvironmentType.SunLounger:
			SaveGameManager.Current.PlayerDefaults.restOnChairMinutes = minutes;
			break;
		}
	}

	private int GetSavedDefaultMinutes()
	{
		switch (EnvironmentType)
		{
		case RestEnvironmentType.OutsideBench:
		case RestEnvironmentType.Bench:
			return SaveGameManager.Current.PlayerDefaults.sleepInBenchMinutes;
		case RestEnvironmentType.Chair:
		case RestEnvironmentType.OutsideChair:
		case RestEnvironmentType.DeckChair:
		case RestEnvironmentType.BeachTowel:
		case RestEnvironmentType.SunLounger:
			return SaveGameManager.Current.PlayerDefaults.restOnChairMinutes;
		default:
			return 0;
		}
	}
}
