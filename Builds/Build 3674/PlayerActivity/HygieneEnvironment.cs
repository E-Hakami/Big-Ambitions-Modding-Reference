using System;
using BigAmbitions.Characters;
using UnityEngine;

namespace PlayerActivity;

[Serializable]
public class HygieneEnvironment : PlayerActivityEnvironment<HygieneEnvironmentConfig>, IPlayerActivityType
{
	public Transform userAttachPoint;

	public Transform exitPoint;

	public PlayerActivityBalanceConfig BalanceConfig => base.Config.balanceConfig;

	public bool IsFixedDuration => base.Config.isFixedDuration;

	public PermanentAnimationType AnimationType => base.Config.animationType;

	public string UILabelKey => base.Config.uiLabelKey;

	public string UIStartKey => base.Config.uiStartKey;

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return new HygieneActivity(this, attachedEntity as HygieneItemController);
	}

	public int GetDefaultMinutes()
	{
		if (!IsFixedDuration)
		{
			return BalanceConfig.GetDefaultMinutes(SaveGameManager.Current.PlayerDefaults.hygieneMinutes);
		}
		return BalanceConfig.DefaultDurationMinutes;
	}

	public void SetDefaultMinutes(int minutes)
	{
		SaveGameManager.Current.PlayerDefaults.hygieneMinutes = minutes;
	}
}
