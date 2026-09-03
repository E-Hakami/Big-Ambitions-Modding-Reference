using System;
using System.Collections.Generic;

namespace PlayerActivity.Activities.Paid;

[Serializable]
public class PaidActivityEnvironment : PlayerActivityEnvironment<PaidActivityEnvironmentConfig>
{
	public PaidActivityType paidActivityType;

	public float price;

	public string transactionType;

	public bool canBeStoppedInTheMiddle;

	public string uiHeadlineKey;

	public string uiSliderLabelKey;

	public string uiStartButtonKey;

	[NonSerialized]
	public Dictionary<string, string> transactionData = new Dictionary<string, string>();

	public float Price
	{
		get
		{
			if (!(price > 0f))
			{
				return base.Config.price;
			}
			return price;
		}
	}

	public string TransactionType
	{
		get
		{
			if (!string.IsNullOrEmpty(transactionType))
			{
				return transactionType;
			}
			return base.Config.transactionType;
		}
	}

	public PlayerActivityBalanceConfig BalanceConfig => base.Config.balanceConfig;

	public bool CanBeStoppedInTheMiddle
	{
		get
		{
			if (!canBeStoppedInTheMiddle)
			{
				return base.Config.canBeStoppedInTheMiddle;
			}
			return true;
		}
	}

	public bool IsFixedDuration => base.Config.isFixedDuration;

	public string UiHeadlineKey
	{
		get
		{
			if (!string.IsNullOrEmpty(uiHeadlineKey))
			{
				return uiHeadlineKey;
			}
			return base.Config.uiHeadlineKey;
		}
	}

	public string UiSliderLabelKey
	{
		get
		{
			if (!string.IsNullOrEmpty(uiSliderLabelKey))
			{
				return uiSliderLabelKey;
			}
			return base.Config.uiSliderLabelKey;
		}
	}

	public string UiStartButtonKey
	{
		get
		{
			if (!string.IsNullOrEmpty(uiStartButtonKey))
			{
				return uiStartButtonKey;
			}
			return base.Config.uiStartButtonKey;
		}
	}

	public int GetDefaultMinutes()
	{
		return BalanceConfig.DefaultDurationMinutes;
	}

	public void SetDefaultMinutes(int minutes)
	{
	}
}
