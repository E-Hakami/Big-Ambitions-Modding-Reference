using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AwesomeCharts;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using Tooltip;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Rivals;

public class SelectedRivalUI : MonoBehaviour
{
	[SerializeField]
	private Button openInContactsButton;

	[Header("Info fields")]
	[SerializeField]
	private TextMeshProUGUI rivalNameField;

	[SerializeField]
	private ChartController chartController;

	[SerializeField]
	private TextLocalizationComponent ageLocalization;

	[SerializeField]
	private TextMeshProUGUI neighborhoodLabel;

	[SerializeField]
	private TextLocalizationComponent neighborhoodLocalization;

	[SerializeField]
	private TextMeshProUGUI businessesField;

	[SerializeField]
	private TextMeshProUGUI weeklyIncomeField;

	[SerializeField]
	private Image portraitImage;

	[Header("Rivalry")]
	[SerializeField]
	private Transform rivalryIconsParent;

	[SerializeField]
	private GameObject activeRivalryAttacksObj;

	[SerializeField]
	private GameObject activeRivalryPrefab;

	[SerializeField]
	private GameObject lowDemandPrefab;

	[SerializeField]
	private GameObject priceReductionPrefab;

	[SerializeField]
	private GameObject productShortagePrefab;

	[SerializeField]
	private GameObject hireBestEmployeesPrefab;

	private RivalData _selectedRival;

	private string _selectedFilter = "weeklyincome";

	private Sprite _lastPortrait;

	public void ShowRival(RivalLeaderboardData data)
	{
		SpecialRival specialRival = RivalsHelper.GetSpecialRival(data.rivalId);
		rivalryIconsParent.DestroyAllChildren();
		openInContactsButton.gameObject.SetActive(specialRival != null && specialRival.HasContactedPlayer());
		activeRivalryAttacksObj.SetActive(specialRival != null);
		if (specialRival != null)
		{
			SetPortrait(specialRival.rivalAppSprite);
			if (specialRival.HasContactedPlayer())
			{
				openInContactsButton.onClick.RemoveAllListeners();
				openInContactsButton.onClick.AddListener(delegate
				{
					InstanceBehavior<UIs>.Instance.smartphoneUI.OpenApp(AppName.Contacts);
					InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(specialRival.GetRivalContact());
				});
			}
			SetRivalryIcons(specialRival);
		}
		else if (!data.portrait && !string.IsNullOrEmpty(data.rivalId))
		{
			portraitImage.sprite = RivalPortraitHelper.GetPortrait(data.rivalId);
			if ((bool)portraitImage.sprite)
			{
				_lastPortrait = portraitImage.sprite;
			}
			else
			{
				portraitImage.sprite = _lastPortrait;
				RivalPortraitHelper.CreatePortrait(RivalsHelper.GetRivalData(data.rivalId), GetRivalBackgroundIndex(data.rivalId), SetPortrait);
			}
		}
		else
		{
			SetPortrait(data.portrait);
		}
		rivalNameField.SetText(data.entryName);
		ageLocalization.SetData("common_years_amount".Localize(new
		{
			years = data.ageInYears
		}));
		businessesField.SetText(data.ownedBusinesses.Count.ToString());
		weeklyIncomeField.SetText(data.weeklyIncome.ToShortCurrencyFormat());
		bool flag = !string.IsNullOrEmpty(data.mostActiveNeighborhood);
		neighborhoodLabel.gameObject.SetActive(flag);
		neighborhoodLocalization.gameObject.SetActive(flag);
		if (flag)
		{
			neighborhoodLocalization.SetData(data.mostActiveNeighborhood.Localize());
		}
		RivalData rivalData = RivalsHelper.GetRivalData(data.rivalId);
		_selectedRival = rivalData;
		if (_selectedFilter == null)
		{
			_selectedFilter = chartController.filterOptions.First().value;
		}
		chartController.SetFilter(_selectedFilter);
	}

	public void FilterChanged(string filterName)
	{
		_selectedFilter = filterName;
		string text = filterName.ToLower();
		if (!(text == "businesses"))
		{
			if (text == "weeklyincome")
			{
				SetChartWeeklyIncome();
			}
		}
		else
		{
			SetChartNumberOfBusinesses();
		}
	}

	private void SetChartWeeklyIncome()
	{
		chartController.chart.valueFormatter = (float value) => value.ToShortCurrencyFormat();
		List<Tuple<int, float>> list;
		if (_selectedRival == null)
		{
			list = GetPlayerWeeklyIncomeHistory();
		}
		else
		{
			RivalState rivalState = RivalsHelper.GetRivalState(_selectedRival.id);
			if (rivalState.weeklyIncomeHistory == null || rivalState.weeklyIncomeHistory.Count < 7)
			{
				RivalsHelper.FillRivalState(_selectedRival.id);
			}
			list = rivalState.weeklyIncomeHistory.TakeLast(7).ToList();
			list.RemoveAll((Tuple<int, float> x) => x.Item1 == SaveGameManager.Current.Day);
			list.Add(new Tuple<int, float>(SaveGameManager.Current.Day, _selectedRival.WeeklyIncome));
			list.Sort((Tuple<int, float> x, Tuple<int, float> _) => x.Item1);
		}
		int num = ((list.Count > 0) ? list.Max((Tuple<int, float> x) => x.Item1) : SaveGameManager.Current.Day);
		num -= 6;
		List<LineEntry> list2 = list.Select((Tuple<int, float> x) => new LineEntry(x.Item1, x.Item2)).ToList();
		chartController.chart.GetChartData().DataSets[0].Entries = list2;
		chartController.chart.AxisConfig.HorizontalAxisConfig.LabelsCount = 7;
		chartController.chart.AxisConfig.HorizontalAxisConfig.ValueFormatterConfig.CustomValues = (from i in Enumerable.Range(num, 7)
			select i.ToString()).ToList();
		int num2 = Mathf.FloorToInt(list2.Min((LineEntry x) => x.Value) / 100000f) * 100000;
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Min = num2;
		int num3 = Mathf.CeilToInt((list2.Max((LineEntry x) => x.Value) + 99999f) / 100000f) * 100000;
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Max = num3;
		chartController.chart.CustomVerticalAxisValueFormatter = new CurrencyAxisFormatter();
		chartController.chart.SetDirty();
	}

	private void SetChartNumberOfBusinesses()
	{
		chartController.chart.valueFormatter = null;
		List<Tuple<int, int>> list;
		if (_selectedRival == null)
		{
			list = GetPlayerNumberOfBusinessesHistory();
		}
		else
		{
			RivalState rivalState = RivalsHelper.GetRivalState(_selectedRival.id);
			if (rivalState.numberOfBusinessesHistory == null || rivalState.numberOfBusinessesHistory.Count < 7)
			{
				RivalsHelper.FillRivalState(_selectedRival.id);
			}
			list = rivalState.numberOfBusinessesHistory.TakeLast(7).ToList();
			list.RemoveAll((Tuple<int, int> x) => x.Item1 == SaveGameManager.Current.Day);
			list.Add(new Tuple<int, int>(SaveGameManager.Current.Day, _selectedRival.ownedBusinesses.Count));
			list.Sort((Tuple<int, int> x, Tuple<int, int> _) => x.Item1);
		}
		int num = ((list.Count > 0) ? list.Max((Tuple<int, int> x) => x.Item1) : SaveGameManager.Current.Day);
		num -= 6;
		List<LineEntry> list2 = list.Select((Tuple<int, int> x) => new LineEntry(x.Item1, x.Item2)).ToList();
		chartController.chart.GetChartData().DataSets[0].Entries = list2;
		chartController.chart.AxisConfig.HorizontalAxisConfig.LabelsCount = 7;
		chartController.chart.AxisConfig.HorizontalAxisConfig.ValueFormatterConfig.CustomValues = (from i in Enumerable.Range(num, 7)
			select i.ToString()).ToList();
		int num2 = Mathf.RoundToInt(list2.Min((LineEntry x) => x.Value));
		int num3 = Mathf.RoundToInt(list2.Max((LineEntry x) => x.Value));
		int num4 = Mathf.FloorToInt((float)num2 / 4f) * 4;
		int num5 = Mathf.CeilToInt((float)num3 / 4f) * 4;
		if (num5 == num4)
		{
			num5 = num4 + 4;
		}
		if (num4 == num2)
		{
			num4--;
			num5--;
		}
		if (num5 == num3)
		{
			num4++;
			num5++;
		}
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Min = num4;
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Max = num5;
		chartController.chart.CustomVerticalAxisValueFormatter = new NearestIntAxisFormatter();
		chartController.chart.SetDirty();
	}

	private List<Tuple<int, float>> GetPlayerWeeklyIncomeHistory()
	{
		List<Tuple<int, float>> list = SaveGameManager.Current.playerWeeklyIncomeHistory?.TakeLast(7).ToList();
		if (list == null)
		{
			list = new List<Tuple<int, float>>();
		}
		if (list.Count == 7)
		{
			list[6] = new Tuple<int, float>(SaveGameManager.Current.Day, FinancialSummaryHelper.GetLastFinancialSummaries(7).Sum((FinancialSummary x) => x.totalProfit));
			return list;
		}
		int num = SaveGameManager.Current.Day - 6;
		int num2 = 7 - list.Count;
		for (int num3 = 0; num3 < num2; num3++)
		{
			int num4 = num + num3;
			list.Add(new Tuple<int, float>(num4, (num4 == SaveGameManager.Current.Day) ? FinancialSummaryHelper.GetLastFinancialSummaries(7).Sum((FinancialSummary x) => x.totalProfit) : 0f));
		}
		SaveGameManager.Current.playerWeeklyIncomeHistory = list;
		return list;
	}

	private List<Tuple<int, int>> GetPlayerNumberOfBusinessesHistory()
	{
		List<Tuple<int, int>> list = SaveGameManager.Current.playerNumberOfBusinessesHistory?.TakeLast(7).ToList();
		if (list == null)
		{
			list = new List<Tuple<int, int>>();
		}
		if (list.Count == 7)
		{
			list[6] = new Tuple<int, int>(SaveGameManager.Current.Day, SaveGameManager.Current.BuildingRegistrations.Count((BuildingRegistration x) => x.RentedByPlayer && (BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.generatesrevenue) || x.businessTypeName == "ba:businesstype_factory")));
			return list;
		}
		int num = SaveGameManager.Current.Day - 6;
		int num2 = 7 - list.Count;
		int item = SaveGameManager.Current.BuildingRegistrations.Count((BuildingRegistration x) => x.RentedByPlayer && (BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.generatesrevenue) || x.businessTypeName == "ba:businesstype_factory"));
		for (int num3 = 0; num3 < num2; num3++)
		{
			int item2 = num + num3;
			list.Add(new Tuple<int, int>(item2, item));
		}
		SaveGameManager.Current.playerNumberOfBusinessesHistory = list;
		return list;
	}

	private void SetRivalryIcons(SpecialRival specialRival)
	{
		SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(specialRival.rivalData.id);
		if (specialRivalState == null || !specialRivalState.isActive)
		{
			activeRivalryAttacksObj.SetActive(value: false);
			return;
		}
		UnityEngine.Object.Instantiate(activeRivalryPrefab, rivalryIconsParent).GetComponent<BasicTooltip>().localizationArguments = new { specialRival.rivalData.rivalName };
		if (specialRivalState.defenseStates == null || specialRivalState.defenseStates.Count < 1)
		{
			return;
		}
		foreach (DefenseState defenseState in specialRivalState.defenseStates)
		{
			(GameObject, string) rivalryIconValues = GetRivalryIconValues(defenseState.defensiveMechanic);
			GameObject item = rivalryIconValues.Item1;
			string item2 = rivalryIconValues.Item2;
			BasicTooltip component = UnityEngine.Object.Instantiate(item, rivalryIconsParent).GetComponent<BasicTooltip>();
			int days = defenseState.timestamp.Day - SaveGameManager.Current.Day;
			string titleKey = item2 + "_title";
			component.titleKey = titleKey;
			string descriptionKey = item2 + "_description";
			component.descriptionKey = descriptionKey;
			component.localizationArguments = new
			{
				days = days,
				rivalName = specialRival.rivalData.rivalName,
				numBusinesses = RivalDefenseHelper.GetLowDemandValues(defenseState.aggression),
				neighborhood = specialRival.primaryNeighborhood
			};
			if (component is ListTooltip listTooltip)
			{
				listTooltip.descriptionKey = "common_affected_items";
				listTooltip.list = defenseState.affectedItems.ToList();
			}
		}
	}

	private (GameObject, string) GetRivalryIconValues(DefensiveMechanic defensiveMechanic)
	{
		return defensiveMechanic switch
		{
			DefensiveMechanic.HireBestEmployees => (hireBestEmployeesPrefab, "rivals_defense_icons_hire_best_employees"), 
			DefensiveMechanic.LowDemand => (lowDemandPrefab, "rivals_defense_icons_low_demand"), 
			DefensiveMechanic.PriceReduction => (priceReductionPrefab, "rivals_defense_icons_price_reduction"), 
			_ => (null, null), 
		};
	}

	private static int GetRivalBackgroundIndex(string rivalId)
	{
		return Mathf.Abs(BitConverter.ToInt32(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(rivalId)), 0));
	}

	private void SetPortrait(Sprite sprite)
	{
		_lastPortrait = sprite;
		portraitImage.sprite = sprite;
	}
}
