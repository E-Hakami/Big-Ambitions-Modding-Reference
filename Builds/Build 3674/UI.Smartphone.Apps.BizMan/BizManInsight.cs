using System.Collections.Generic;
using System.Linq;
using AwesomeCharts;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class BizManInsight : MonoBehaviour
{
	private class ChartEntry
	{
		public string label;

		public int amount;
	}

	private static readonly List<Item.ItemCapacity> TempItemCapacities = new List<Item.ItemCapacity>();

	[Header("Promotion")]
	[SerializeField]
	private ProgressBar promotionProgressBar;

	[SerializeField]
	private ProgressBar trafficProgressBar;

	[SerializeField]
	private ProgressBar marketingProgressBar;

	[Header("Satisfaction")]
	[SerializeField]
	private ProgressBar overallSatisfactionProgressBar;

	[SerializeField]
	private ProgressBar customerServiceProgressBar;

	[SerializeField]
	private ProgressBar pricingProgressBar;

	[SerializeField]
	private ProgressBar interiorProgressBar;

	[SerializeField]
	private ProgressBar cleanlinessProgressBar;

	[SerializeField]
	private GameObject customerSatisfactionData;

	[SerializeField]
	private GameObject customerSatisfactionNotEnoughData;

	[Header("Customer Capacity")]
	[SerializeField]
	private Transform customerCapacityEntry;

	[SerializeField]
	private TextMeshProUGUI currentCapacityPerHourLabel;

	[SerializeField]
	private TextMeshProUGUI buildingLimitLabel;

	[Header("Promotion")]
	[SerializeField]
	private ChartController chartController;

	private BuildingRegistration _buildingRegistration;

	public void RefreshData(BuildingRegistration buildingRegistration)
	{
		_buildingRegistration = buildingRegistration;
		RefreshPromotion();
		RefreshSatisfaction();
		RefreshCustomerCapacity();
		chartController.SetFilter("7");
	}

	private void RefreshPromotion()
	{
		promotionProgressBar.SetValue(_buildingRegistration.promotion.total);
		trafficProgressBar.SetValue(_buildingRegistration.promotion.trafficIndex);
		marketingProgressBar.SetValue(_buildingRegistration.promotion.marketing);
	}

	private void RefreshSatisfaction()
	{
		bool flag = _buildingRegistration.orderHistory.Exists((OrderHistoryEntry x) => x.dayNumber < SaveGameManager.Current.Day && x.totalCustomers > 0);
		customerSatisfactionData.SetActive(flag);
		customerSatisfactionNotEnoughData.SetActive(!flag);
		if (flag)
		{
			overallSatisfactionProgressBar.SetValue(_buildingRegistration.satisfaction.overall);
			customerServiceProgressBar.SetValue(_buildingRegistration.satisfaction.customerService);
			pricingProgressBar.SetValue(_buildingRegistration.satisfaction.pricing);
			interiorProgressBar.SetValue(_buildingRegistration.satisfaction.facility);
			cleanlinessProgressBar.SetValue(_buildingRegistration.satisfaction.cleanliness);
		}
	}

	private void RefreshCustomerCapacity()
	{
		customerCapacityEntry.ResetTemplate();
		Building building = BuildingHelper.GetBuilding(_buildingRegistration.Address);
		int customerCapacity = BuildingSizeHelper.GetData(building.BuildingSize).GetCustomerCapacity(building.BuildingType, building.BuildingVersion);
		int num = 0;
		TempItemCapacities.Clear();
		TempItemCapacities.AddRange(_buildingRegistration.itemInstances.Values.GetItemsSortedByCapacity(_buildingRegistration));
		bool flag = customerCapacity == 9999;
		foreach (Item.ItemCapacity tempItemCapacity in TempItemCapacities)
		{
			if (num == 0 || num > tempItemCapacity.CustomersLimit)
			{
				num = tempItemCapacity.CustomersLimit;
			}
		}
		Transform transform = null;
		foreach (Item.ItemCapacity item in TempItemCapacities.OrderBy((Item.ItemCapacity x) => x.CustomersLimit))
		{
			Color color;
			Color color2;
			if (item.CustomersLimit >= customerCapacity)
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
				color2 = InstanceBehavior<GlobalReferences>.Instance.colors.black;
			}
			else if ((float)item.CustomersLimit <= (float)customerCapacity * 0.33f)
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
				color2 = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			}
			else
			{
				color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
				color2 = InstanceBehavior<GlobalReferences>.Instance.colors.black;
			}
			Transform transform2 = Object.Instantiate(customerCapacityEntry, customerCapacityEntry.parent);
			Transform obj = transform2.Find("Type");
			obj.Find("Icon").GetComponent<Image>().color = color;
			TextMeshProUGUI labelByName = obj.GetLabelByName("ItemTitle");
			labelByName.GetComponent<TextLocalizationComponent>().SetData(LocalizationHelper.GetItemLabel(item.itemName));
			labelByName.color = color2;
			TextMeshProUGUI labelByName2 = obj.GetLabelByName("TotalCapacityLabel");
			labelByName2.text = string.Format(arg1: ColorUtility.ToHtmlStringRGB(InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey), format: "<b>{0}</b> / <#{1}>{2}", arg0: item.CustomersLimit, arg2: customerCapacity);
			labelByName2.color = color2;
			foreach (Item.ItemCapacityShelf itemShelf in item.itemShelves)
			{
				Transform obj2 = Object.Instantiate(transform2.Find("ShelfTemplate"), transform2);
				obj2.GetLanguageChangeEventByName("ItemTitle").SetData("bizman_insight_shelf_type_capacity".Localize(new
				{
					shelfAmount = itemShelf.amount,
					shelfLabel = itemShelf.itemName,
					customersPerHour = itemShelf.customersPerHour
				}));
				obj2.GetLabelByName("TotalCapacityLabel").text = itemShelf.TotalCustomersPerHour.ToString();
				obj2.gameObject.SetActive(value: true);
			}
			transform = transform2.Find("SplitterGray");
			transform.SetAsLastSibling();
			transform2.gameObject.SetActive(value: true);
		}
		if (transform != null)
		{
			transform.gameObject.SetActive(value: false);
		}
		if (num < customerCapacity)
		{
			currentCapacityPerHourLabel.color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			buildingLimitLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;
			currentCapacityPerHourLabel.text = num.ToString();
		}
		else
		{
			currentCapacityPerHourLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey;
			buildingLimitLabel.color = InstanceBehavior<GlobalReferences>.Instance.colors.black;
			currentCapacityPerHourLabel.text = customerCapacity.ToString();
		}
		buildingLimitLabel.text = (flag ? "-" : customerCapacity.ToString());
		TempItemCapacities.Clear();
	}

	private void RefreshChartData(List<ChartEntry> chartEntries)
	{
		chartController.chart.GetChartData().DataSets[0].Entries = chartEntries.Select((ChartEntry entry, int index) => new LineEntry(index, entry.amount)).ToList();
		chartController.chart.AxisConfig.HorizontalAxisConfig.LabelsCount = chartEntries.Count;
		chartController.chart.AxisConfig.HorizontalAxisConfig.ValueFormatterConfig.CustomValues = chartEntries.Select((ChartEntry x) => x.label.ToString()).ToList();
		int num = Mathf.FloorToInt((float)chartEntries.Min((ChartEntry x) => x.amount) / 4f) * 4;
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Min = num;
		int num2 = Mathf.CeilToInt((float)chartEntries.Max((ChartEntry x) => x.amount) / 4f) * 4;
		if (num2 == num)
		{
			num2 = num + 4;
		}
		chartController.chart.AxisConfig.VerticalAxisConfig.Bounds.Max = num2;
		chartController.chart.SetDirty();
	}

	public void FilterChanged(string filterName)
	{
		if (_buildingRegistration == null)
		{
			return;
		}
		int num = int.Parse(filterName);
		if (!(filterName == "1"))
		{
			if (!(filterName == "7"))
			{
				return;
			}
			List<OrderHistoryEntry> period = _buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)).ToList();
			RefreshChartData((from x in Enumerable.Range(SaveGameManager.Current.Day - num, num)
				select new ChartEntry
				{
					label = x.ToString(),
					amount = period.Where((OrderHistoryEntry p) => p.dayNumber == x).Sum((OrderHistoryEntry h) => h.totalCustomers)
				}).ToList());
			return;
		}
		OrderHistoryEntry orderHistoryEntry = _buildingRegistration.orderHistory.Find((OrderHistoryEntry x) => x.dayNumber == SaveGameManager.Current.Day - 1);
		List<ChartEntry> list = new List<ChartEntry>();
		foreach (int hour in Enumerable.Range(0, 24))
		{
			int num2 = hour;
			if (TimeHelper.use12h)
			{
				num2 %= 12;
				if (num2 == 0)
				{
					num2 = 12;
				}
			}
			ChartEntry item = new ChartEntry
			{
				label = num2.ToString(),
				amount = ((orderHistoryEntry != null) ? (orderHistoryEntry.hourReports.FirstOrDefault((OrderHistoryEntry.HourReport r) => r.hour == hour)?.customers ?? 0) : 0)
			};
			list.Add(item);
		}
		RefreshChartData(list);
		GameAnalytics.TrackOpenBizmanInsightYesterdayGraphFilter();
	}
}
