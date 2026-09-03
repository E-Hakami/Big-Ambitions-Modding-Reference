using System;
using System.Collections;
using BigAmbitions.Tags;
using DG.Tweening;
using Entities;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace UI.DailySummary;

public class DailySummary : MonoBehaviour
{
	public CanvasGroup panel;

	public TextMeshProUGUI incomeLabel;

	public TextMeshProUGUI businessesLabel;

	public TextLocalizationComponent headline;

	public DeliveryJobSummary deliveryJobSummaryPrefab;

	public FoodDeliverySummary foodDeliverySummaryPrefab;

	[SerializeField]
	private GameObject econoViewButtonObj;

	private CanvasGroup _incomeRow;

	private CanvasGroup _businessesRow;

	private JobSummary _activeJobSummary;

	private void Start()
	{
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, (Action)delegate
		{
			econoViewButtonObj.SetActive(value: false);
		});
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, (Action)delegate
		{
			econoViewButtonObj.SetActive(value: true);
		});
		panel.alpha = 0f;
		panel.gameObject.SetActive(value: true);
		_incomeRow = incomeLabel.transform.parent.GetComponent<CanvasGroup>();
		_businessesRow = businessesLabel.transform.parent.GetComponent<CanvasGroup>();
		_incomeRow.alpha = 0f;
		_businessesRow.alpha = 0f;
	}

	[ContextMenu("Run")]
	public void Run()
	{
		if (!PlayerHelper.playerDead)
		{
			StartCoroutine(ExecuteSequence());
		}
	}

	private IEnumerator ExecuteSequence()
	{
		DestroyJobSummaries();
		panel.alpha = 0f;
		_incomeRow.alpha = 0f;
		_businessesRow.alpha = 0f;
		FinancialSummary yesterdaySummary = SaveGameManager.Current.financialSummaries.Find((FinancialSummary x) => x.dayNumber == SaveGameManager.Current.Day - 1);
		headline.Arguments = new
		{
			day = SaveGameManager.Current.Day - 1
		};
		panel.blocksRaycasts = true;
		yield return panel.DOFade(1f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		float totalProfit = yesterdaySummary.totalProfit;
		incomeLabel.text = totalProfit.ToCurrencyFormat();
		incomeLabel.color = ((totalProfit < 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.green);
		if (totalProfit > 0f)
		{
			HappinessHelper.AddModifier("ba:happinessmodifier_positive_revenue");
		}
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.generatesrevenue))
			{
				num++;
			}
		}
		businessesLabel.text = num.ToString();
		yield return _incomeRow.DOFade(1f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		yield return _businessesRow.DOFade(1f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		yield return new WaitForSecondsRealtime(6f);
		yield return panel.DOFade(0f, 1f).SetLink(panel.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		panel.blocksRaycasts = false;
	}

	public void RunDeliveryJobSummary()
	{
		HideSummaryPanel();
		DestroyJobSummaries();
		DeliveryJobSummary deliveryJobSummary = UnityEngine.Object.Instantiate(deliveryJobSummaryPrefab, InstanceBehavior<UIs>.Instance.transform);
		deliveryJobSummary.Run();
		_activeJobSummary = deliveryJobSummary;
	}

	public void RunFoodDeliverySummary(float payment, float tip, bool wasFastDelivery)
	{
		HideSummaryPanel();
		DestroyJobSummaries();
		if ((bool)foodDeliverySummaryPrefab)
		{
			FoodDeliverySummary foodDeliverySummary = UnityEngine.Object.Instantiate(foodDeliverySummaryPrefab, InstanceBehavior<UIs>.Instance.transform);
			foodDeliverySummary.Run(payment, tip, wasFastDelivery);
			_activeJobSummary = foodDeliverySummary;
		}
	}

	private void HideSummaryPanel()
	{
		StopCoroutine("ExecuteSequence");
		DOTween.Kill(panel);
		DOTween.Kill(_incomeRow);
		DOTween.Kill(_businessesRow);
		panel.alpha = 0f;
		panel.blocksRaycasts = false;
	}

	private void DestroyJobSummaries()
	{
		if ((bool)_activeJobSummary)
		{
			UnityEngine.Object.Destroy(_activeJobSummary.gameObject);
		}
		_activeJobSummary = null;
	}

	public void OnEconoViewButtonClicked()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.EconoView);
	}
}
