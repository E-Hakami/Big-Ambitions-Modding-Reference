using System;
using NaughtyAttributes;

namespace PlayerActivity;

[Serializable]
public class EntertainDevice : IPlayerActivityType
{
	public PlayerActivityBalanceConfig balanceConfig;

	public EntertainType entertainType;

	public float minTimeBetweenAnims;

	public float maxTimeBetweenAnims;

	public bool preferSeating;

	[ShowIf("preferSeating")]
	public float maxSeatingDistance;

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return new EntertainActivity(this, attachedEntity);
	}

	public int GetDefaultMinutes()
	{
		int savedDefaultMinutes = GetSavedDefaultMinutes();
		return balanceConfig.GetDefaultMinutes(savedDefaultMinutes);
	}

	public void SetDefaultMinutes(int minutes)
	{
		switch (entertainType)
		{
		case EntertainType.Play:
			SaveGameManager.Current.PlayerDefaults.playMinutes = minutes;
			break;
		case EntertainType.WatchTV:
			SaveGameManager.Current.PlayerDefaults.watchTvMinutes = minutes;
			break;
		case EntertainType.DJ:
			SaveGameManager.Current.PlayerDefaults.djMinutes = minutes;
			break;
		case EntertainType.Read:
			SaveGameManager.Current.PlayerDefaults.readMinutes = minutes;
			break;
		}
	}

	private int GetSavedDefaultMinutes()
	{
		return entertainType switch
		{
			EntertainType.Play => SaveGameManager.Current.PlayerDefaults.playMinutes, 
			EntertainType.WatchTV => SaveGameManager.Current.PlayerDefaults.watchTvMinutes, 
			EntertainType.DJ => SaveGameManager.Current.PlayerDefaults.djMinutes, 
			EntertainType.Read => SaveGameManager.Current.PlayerDefaults.readMinutes, 
			_ => 0, 
		};
	}
}
