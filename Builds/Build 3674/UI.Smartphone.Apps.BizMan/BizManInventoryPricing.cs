using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class BizManInventoryPricing : MonoBehaviour
{
	public Transform itemsSold;

	public Transform profitMargin;

	public Transform bestSellingProductEntry;

	public PercentageGroup bestSellingProductPercentageGroup;

	private BizManBusiness _bizManBusiness;

	public InventoryProductsScrollerController inventoryProductsScrollerController;

	private void Awake()
	{
		_bizManBusiness = GetComponentInParent<BizManBusiness>();
	}

	private void OnEnable()
	{
		RefreshData();
	}

	public void RefreshData()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			RefreshLeftSide();
			RefreshProducts();
			RefreshBestSellers();
		});
	}

	private void RefreshBestSellers()
	{
		var source = (from x in _bizManBusiness.buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)).ToList().SelectMany((OrderHistoryEntry x) => x.itemSales)
			group x by x.itemName into x
			where !ItemsGetter.GetByName(x.Key).HasTag(TagRef.Itemtag.isbag)
			select new
			{
				itemName = x.Key,
				amountSold = x.Sum((OrderHistoryEntry.ItemReport c) => c.amountSold)
			}).ToList();
		int num = source.Sum(x => x.amountSold);
		var list = source.OrderByDescending(x => x.amountSold).Take(4).ToList();
		bestSellingProductEntry.ResetTemplate();
		List<PercentageGroupEntry> list2 = new List<PercentageGroupEntry>();
		for (int num2 = 0; num2 < list.Count; num2++)
		{
			var anon = list[num2];
			Transform obj = Object.Instantiate(bestSellingProductEntry, bestSellingProductEntry.parent);
			obj.GetLanguageChangeEventByName("ItemLabel").SetData(LocalizationHelper.GetItemLabel(anon.itemName));
			obj.GetLabelByName("AmountLabel").text = anon.amountSold.ToString();
			int percentage = anon.amountSold * 100 / num;
			obj.GetLabelByName("PercentageLabel").text = percentage + "%";
			obj.GetComponentInChildren<Image>().color = InstanceBehavior<GlobalReferences>.Instance.chartColors[num2];
			obj.gameObject.SetActive(value: true);
			list2.Add(new PercentageGroupEntry
			{
				gradient = InstanceBehavior<GlobalReferences>.Instance.gradients[num2],
				percentage = percentage
			});
		}
		bestSellingProductPercentageGroup.SetEntries(list2);
	}

	private void RefreshProducts()
	{
		inventoryProductsScrollerController.LoadProducts();
	}

	private void RefreshLeftSide()
	{
		var source = (from x in _bizManBusiness.buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day - 1)).SelectMany((OrderHistoryEntry x) => x.itemSales)
			group x by x.itemName into x
			select new
			{
				itemName = x.Key,
				amountSold = x.Sum((OrderHistoryEntry.ItemReport c) => c.amountSold),
				totalPrice = x.Sum((OrderHistoryEntry.ItemReport c) => c.totalPrice)
			}).ToList();
		var source2 = (from x in _bizManBusiness.buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 14, SaveGameManager.Current.Day - 8)).SelectMany((OrderHistoryEntry x) => x.itemSales)
			group x by x.itemName into x
			select new
			{
				itemName = x.Key,
				amountSold = x.Sum((OrderHistoryEntry.ItemReport c) => c.amountSold),
				totalPrice = x.Sum((OrderHistoryEntry.ItemReport c) => c.totalPrice)
			}).ToList();
		int num = source2.Sum(x => x.amountSold);
		int num2 = source.Sum(x => x.amountSold);
		float num3 = ((num == 0) ? 0f : (Mathf.Round(num2 * 100 / num) - 100f));
		itemsSold.Find("Horizontal/Amount").GetComponent<TextMeshProUGUI>().text = num2.ToString();
		itemsSold.Find("LastPeriod").GetComponent<TextLocalizationComponent>().SetData(LanguageChangeEventDataHolder.Create("bizman_compared_last_period", new
		{
			amount = num
		}));
		TextMeshProUGUI component = itemsSold.Find("Horizontal/Percentage").GetComponent<TextMeshProUGUI>();
		component.text = num3 + "%";
		component.color = ((num3 >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		itemsSold.Find("Horizontal/PositiveIcon").gameObject.SetActive(num3 >= 0f);
		itemsSold.Find("Horizontal/NegativeIcon").gameObject.SetActive(num3 < 0f);
		float num4 = source.Sum(x => x.totalPrice);
		float num5 = source2.Sum(x => x.totalPrice);
		float num6 = ((num5 > 0f) ? (Mathf.Round(num4 * 100f / num5) - 100f) : 0f);
		profitMargin.Find("Horizontal/Amount").GetComponent<TextMeshProUGUI>().text = num4.ToShortCurrencyFormat();
		profitMargin.Find("LastPeriod").GetComponent<TextLocalizationComponent>().SetData(LanguageChangeEventDataHolder.Create("bizman_compared_last_period", new
		{
			amount = num5.ToShortCurrencyFormat()
		}));
		TextMeshProUGUI component2 = profitMargin.Find("Horizontal/Percentage").GetComponent<TextMeshProUGUI>();
		component2.text = num6 + "%";
		component2.color = ((num6 >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		profitMargin.Find("Horizontal/PositiveIcon").gameObject.SetActive(num6 >= 0f);
		profitMargin.Find("Horizontal/NegativeIcon").gameObject.SetActive(num6 < 0f);
	}
}
