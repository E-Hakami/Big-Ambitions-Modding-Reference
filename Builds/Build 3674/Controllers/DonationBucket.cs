using System;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using UnityEngine;

namespace Controllers;

public class DonationBucket : OutsideInteractableItem
{
	private const float DonationAmount = 100f;

	[SerializeField]
	private PlayerActivityBalanceConfig balanceConfig;

	public override string GetCtaKey()
	{
		return "click_to_donate_money";
	}

	public override void PerformActivity()
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.SetGoal(GetClosestNavMeshTargetPosition(playerController.transform.position), OnBucketReached);
	}

	private void OnBucketReached()
	{
		LanguageChangeEventDataHolder bodyData = "donate_confirm".Localize(new
		{
			amount = 100f.ToShortCurrencyFormat()
		});
		Action onConfirmAction = OnConfirmDonation;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	private void OnConfirmDonation()
	{
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_donation");
		if (GameManager.ChangeMoneySafe(-100f, transactionInfo, null, null, force: false, showNotification: true))
		{
			HappinessHelper.AddModifier(balanceConfig.FinalType, balanceConfig.GetBoostHours(0), additiveHours: true);
		}
	}
}
