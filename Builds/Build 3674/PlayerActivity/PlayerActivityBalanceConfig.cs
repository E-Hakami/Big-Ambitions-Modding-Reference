using Helpers;
using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "PlayerActivityBalanceConfig", menuName = "BigAmbitions/PlayerActivity/BalanceConfig")]
public class PlayerActivityBalanceConfig : ScriptableObject
{
	public const string DefaultDurationMinutesField = "defaultDurationMinutes";

	public const string MinDurationMinutesField = "minDurationMinutes";

	public const string MaxDurationMinutesField = "maxDurationMinutes";

	public const string BoostHoursPerUseField = "boostHoursPerUse";

	public const string BoostHoursPerHourField = "boostHoursPerHour";

	[SerializeField]
	private string displayName;

	[SerializeField]
	private PlayerActivityBalanceSource source;

	[SerializeField]
	private HappinessModifier temporalModifier;

	[SerializeField]
	private HappinessModifier finalModifier;

	[SerializeField]
	private int defaultDurationMinutes;

	[SerializeField]
	private int minDurationMinutes;

	[SerializeField]
	private int maxDurationMinutes;

	[SerializeField]
	private int boostHoursPerUse;

	[SerializeField]
	private float boostHoursPerHour;

	[SerializeField]
	private int maxBoostHoursPerUse;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(displayName))
			{
				return displayName;
			}
			return base.name;
		}
	}

	public PlayerActivityBalanceSource Source => source;

	public HappinessModifier TemporalModifier => temporalModifier;

	public HappinessModifier FinalModifier => finalModifier;

	public int DefaultDurationMinutes => defaultDurationMinutes;

	public int MinDurationMinutes => minDurationMinutes;

	public int MaxDurationMinutes => maxDurationMinutes;

	public int BoostHoursPerUse => boostHoursPerUse;

	public float BoostHoursPerHour => boostHoursPerHour;

	public int MaxBoostHoursPerUse => maxBoostHoursPerUse;

	public string TemporalType
	{
		get
		{
			if (!(temporalModifier != null))
			{
				return string.Empty;
			}
			return temporalModifier.type;
		}
	}

	public string FinalType
	{
		get
		{
			if (!(finalModifier != null))
			{
				return string.Empty;
			}
			return finalModifier.type;
		}
	}

	public int BoostPercent
	{
		get
		{
			if (!(finalModifier != null))
			{
				if (!(temporalModifier != null))
				{
					return 0;
				}
				return temporalModifier.amount;
			}
			return finalModifier.amount;
		}
	}

	public int MaxBoostHours
	{
		get
		{
			if (!(finalModifier != null))
			{
				return 0;
			}
			return finalModifier.maxHoursDuration;
		}
	}

	public int GetDefaultMinutes(int savedMinutes)
	{
		if (savedMinutes <= 0)
		{
			return defaultDurationMinutes;
		}
		return savedMinutes;
	}

	public int GetBoostHours(int minutes)
	{
		float num = (float)minutes / 60f;
		int num2 = boostHoursPerUse + Mathf.FloorToInt(boostHoursPerHour * num);
		if (maxBoostHoursPerUse > 0)
		{
			num2 = Mathf.Min(num2, maxBoostHoursPerUse);
		}
		if (!string.IsNullOrEmpty(FinalType))
		{
			return HappinessHelper.GetCappedHoursDuration(FinalType, num2);
		}
		return num2;
	}

	public void EnableTemporalBoost(ThirdPersonCharacter tpc = null)
	{
		HappinessHelper.EnableTemporalHappinessBoost(TemporalType, FinalType, tpc);
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		SaveGameManager.Current.currentActivityHappinessPerHour = boostHoursPerHour;
	}

	public void DisableTemporalBoost(ThirdPersonCharacter tpc = null)
	{
		HappinessHelper.DisableTemporalHappinessBoost(TemporalType, FinalType, tpc);
	}

	public void SetModifier(bool isTemporal, HappinessModifier modifier)
	{
		if (isTemporal)
		{
			temporalModifier = modifier;
		}
		else
		{
			finalModifier = modifier;
		}
	}
}
