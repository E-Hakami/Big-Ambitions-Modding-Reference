using UnityEngine;

namespace PlayerActivity.Activities.Paid;

[CreateAssetMenu(fileName = "PaidActivityEnvironmentConfig", menuName = "BigAmbitions/PlayerActivity/PaidActivityEnvironmentConfig")]
public class PaidActivityEnvironmentConfig : PlayerActivityEnvironmentConfig
{
	public PaidActivityType paidActivityType;

	public float price;

	public string transactionType;

	public PlayerActivityBalanceConfig balanceConfig;

	public bool canBeStoppedInTheMiddle;

	public bool isFixedDuration;

	public string uiHeadlineKey;

	public string uiSliderLabelKey;

	public string uiStartButtonKey;
}
