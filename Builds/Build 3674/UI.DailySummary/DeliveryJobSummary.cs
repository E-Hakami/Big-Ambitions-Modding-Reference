using System.Collections;
using Extensions;
using Helpers;
using Player.PlayerMissions;
using TMPro;
using UnityEngine;

namespace UI.DailySummary;

public class DeliveryJobSummary : JobSummary
{
	[SerializeField]
	private TextMeshProUGUI deliveriesLabel;

	[SerializeField]
	private TextMeshProUGUI grossIncomeLabel;

	[SerializeField]
	private TextMeshProUGUI damagesLabel;

	[SerializeField]
	private TextMeshProUGUI netIncomeLabel;

	[ContextMenu("Run")]
	public void Run()
	{
		StartCoroutine(ExecuteSequence());
	}

	private IEnumerator ExecuteSequence()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			Object.Destroy(base.gameObject);
			yield break;
		}
		int completedDeliveries = deliveryDriverMission.GetCompletedDeliveries();
		deliveriesLabel.text = $"{completedDeliveries}/{deliveryDriverMission.destinations.Count}";
		deliveriesLabel.color = ((completedDeliveries == deliveryDriverMission.destinations.Count) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.yellow);
		grossIncomeLabel.text = deliveryDriverMission.earnings.ToCurrencyFormat();
		SetTipsRow(deliveryDriverMission.tips, deliveryDriverMission.WasFastDelivery());
		damagesLabel.text = (0f - deliveryDriverMission.damageFees).ToCurrencyFormat();
		if (deliveryDriverMission.damageFees > 0f)
		{
			damagesLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
		}
		float num = Mathf.Max(0f, deliveryDriverMission.earnings + deliveryDriverMission.tips - deliveryDriverMission.damageFees);
		netIncomeLabel.text = num.ToCurrencyFormat();
		netIncomeLabel.color = ((num > 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.yellow);
		if (num > 0f)
		{
			HappinessHelper.AddModifier("ba:happinessmodifier_positive_revenue");
		}
		yield return FadeInRows();
	}
}
