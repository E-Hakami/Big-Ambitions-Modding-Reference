using System.Collections;
using Extensions;
using Helpers;
using TMPro;
using UnityEngine;

namespace UI.DailySummary;

public class FoodDeliverySummary : JobSummary
{
	[SerializeField]
	private TextMeshProUGUI paymentLabel;

	[SerializeField]
	private TextMeshProUGUI totalLabel;

	public void Run(float payment, float tip, bool wasFastDelivery)
	{
		StartCoroutine(ExecuteSequence(payment, tip, wasFastDelivery));
	}

	private IEnumerator ExecuteSequence(float payment, float tip, bool wasFastDelivery)
	{
		paymentLabel.text = payment.ToCurrencyFormat();
		SetTipsRow(tip, wasFastDelivery);
		totalLabel.text = (payment + tip).ToCurrencyFormat();
		totalLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
		HappinessHelper.AddModifier("ba:happinessmodifier_positive_revenue");
		yield return FadeInRows();
	}
}
