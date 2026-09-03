using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaTable;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.Office.Headquarters;
using Extensions;
using Localizor;
using TMPro;
using UI;
using UI.Components;
using UI.Notification;
using UI.Smartphone.Apps.BizMan;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryProductCellView : BaTableCellView<InventoryProductCellView.InventoryProductModel>, IPointerClickHandler, IEventSystemHandler
{
	public class InventoryProductModel
	{
		public string ProductName;

		public RetailPrice RetailPriceReference;

		public RetailPrice StoredRetailPriceReference;

		public Item Item;

		public int SoldLastWeek;

		public float MarketPrice;

		public int StockCount;

		public string Neighborhood;

		public List<ItemSoldPerPriceEntry> amountSoldPerPrice;

		public float RetailPriceSortField => RetailPriceReference.price;

		public InventoryProductModel(string productName, RetailPrice retailPriceReference, RetailPrice storedRetailPriceReference, Item item, int soldLastWeek, float marketPrice, int stockCount, string neighborhood, List<ItemSoldPerPriceEntry> amountSoldPerPrice)
		{
			ProductName = productName;
			RetailPriceReference = retailPriceReference;
			StoredRetailPriceReference = storedRetailPriceReference;
			Item = item;
			SoldLastWeek = soldLastWeek;
			MarketPrice = marketPrice;
			StockCount = stockCount;
			Neighborhood = neighborhood;
			this.amountSoldPerPrice = amountSoldPerPrice;
		}
	}

	private const int MaxSoldTooltipBreakdownLines = 8;

	public TextMeshProUGUI productName;

	public TextMeshProUGUI soldLastWeek;

	public TextMeshProUGUI marketPrice;

	public UI.Components.InputField retailPrice;

	public TextMeshProUGUI stockCount;

	public Button retailPricePlusButton;

	public Button retailPriceMinusButton;

	public AmountSoldBreakdownTooltip amountSoldBreakdownTooltip;

	public BasicTooltip pricingManagerTooltip;

	private InventoryProductModel _data;

	private Image _amountBackground;

	private Color32 _initialAmountBackground;

	private float _yellowPrice;

	private float _redPrice;

	private PricingManagerPlan _coveringPlan;

	protected override void Awake()
	{
		base.Awake();
		_amountBackground = retailPrice.GetComponent<Image>();
		_initialAmountBackground = _amountBackground.color;
	}

	private void Start()
	{
		retailPrice.tmpInputField.onValueChanged.AddListener(delegate
		{
			if (float.TryParse(retailPrice.GetRawValue(), NumberStyles.Number, CultureHelper.CultureInfo, out var result))
			{
				if (result < 0f)
				{
					Notifications.ShowError("common_notification_invalid_amount", "invalidamount");
				}
				else
				{
					if (result > 10000f)
					{
						result = 10000f;
						retailPrice.SetText(result.ToString("N2", CultureHelper.CultureInfo), notify: false);
					}
					_data.RetailPriceReference.price = result;
					_data.StoredRetailPriceReference.price = result;
					marketPrice.text = ItemHelper.GetLowestMarketPrice(_data.Item.itemName, _data.Neighborhood, forceUpdate: true).ToCurrencyFormat();
				}
				UpdateAmountBackground();
			}
		});
		retailPricePlusButton.onClick.AddListener(delegate
		{
			if (!float.TryParse(retailPrice.GetRawValue(), NumberStyles.Number, CultureHelper.CultureInfo, out var result))
			{
				result = 0f;
			}
			float num = result + 0.1f;
			num = (float)Mathf.RoundToInt(num * 100f) / 100f;
			if (num > 10000f)
			{
				num = 10000f;
			}
			retailPrice.SetText(num.ToString("N2", CultureHelper.CultureInfo));
		});
		retailPriceMinusButton.onClick.AddListener(delegate
		{
			if (!float.TryParse(retailPrice.GetRawValue(), NumberStyles.Number, CultureHelper.CultureInfo, out var result))
			{
				result = 0f;
			}
			float num = result - 0.1f;
			if (num < 0f)
			{
				num = 0f;
			}
			num = (float)Mathf.RoundToInt(num * 100f) / 100f;
			retailPrice.SetText(num.ToString("N2", CultureHelper.CultureInfo));
		});
	}

	private void UpdateAmountBackground()
	{
		Color32 color = _initialAmountBackground;
		if (_data.RetailPriceReference.price != 0f && _data.RetailPriceReference.price >= _redPrice)
		{
			color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
		else if (_data.RetailPriceReference.price > _yellowPrice)
		{
			color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
		}
		color.a = _initialAmountBackground.a;
		_amountBackground.color = color;
	}

	public override void SetData(InventoryProductModel data)
	{
		_data = data;
		productName.text = data.ProductName;
		soldLastWeek.text = data.SoldLastWeek.ToString();
		marketPrice.text = (data.Item.HasTag(TagRef.Itemtag.isshoppingcontainer) ? "-" : data.MarketPrice.ToCurrencyFormat());
		stockCount.text = (((data.Item.type & ItemType.ServiceProduct) != 0) ? "-" : data.StockCount.ToString());
		_coveringPlan = PricingManagerHelper.GetPlanCoveringNeighborhood(data.Neighborhood);
		bool flag = _coveringPlan != null;
		pricingManagerTooltip.gameObject.SetActive(flag);
		if (flag)
		{
			string employeeName = _coveringPlan.AnalystInstance?.characterData.name ?? "common_unassigned".GetLocalization();
			pricingManagerTooltip.localizationArguments = new { employeeName };
		}
		bool flag2 = data.Item.HasTag(TagRef.Itemtag.isbag);
		bool interactable = !flag2 && !flag;
		retailPrice.SetText(flag2 ? "0" : data.RetailPriceReference.price.ToString("N2", CultureHelper.CultureInfo), notify: false);
		retailPrice.tmpInputField.interactable = interactable;
		retailPricePlusButton.interactable = interactable;
		retailPriceMinusButton.interactable = interactable;
		List<(string, object)> list = new List<(string, object)>();
		int num = ((data.amountSoldPerPrice.Count > 8) ? 8 : data.amountSoldPerPrice.Count);
		int num2 = data.SoldLastWeek - data.amountSoldPerPrice.Sum((ItemSoldPerPriceEntry x) => x.amount);
		if (num == 0)
		{
			if (data.Item.HasTag(TagRef.Itemtag.isshoppingcontainer))
			{
				list.Add(("inventory_pricing_sold_breakdown_in_service", new
				{
					amount = data.SoldLastWeek
				}));
			}
			else
			{
				list.Add(("inventory_pricing_sold_breakdown", new
				{
					amount = data.SoldLastWeek,
					price = data.RetailPriceReference.price.ToCurrencyFormat()
				}));
			}
		}
		else
		{
			for (int num3 = 0; num3 < num; num3++)
			{
				ItemSoldPerPriceEntry itemSoldPerPriceEntry = data.amountSoldPerPrice[num3];
				if (num2 > 0 && Math.Abs(data.RetailPriceReference.price - itemSoldPerPriceEntry.price) < 0.01f)
				{
					itemSoldPerPriceEntry.amount += num2;
				}
				if (itemSoldPerPriceEntry.price == 0f)
				{
					list.Add(("inventory_pricing_sold_breakdown_in_service", new { itemSoldPerPriceEntry.amount }));
				}
				else
				{
					list.Add(("inventory_pricing_sold_breakdown", new
					{
						amount = itemSoldPerPriceEntry.amount,
						price = itemSoldPerPriceEntry.price.ToCurrencyFormat()
					}));
				}
			}
			if (data.amountSoldPerPrice.Count > 8)
			{
				list.Add(("...", null));
			}
		}
		amountSoldBreakdownTooltip.breakdown = list;
		bool flag3 = true;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId) && !buildingRegistration.RentedByPlayer && !(buildingRegistration.Neighborhood != _data.Neighborhood) && buildingRegistration.cachedAvailableProducts.Any((string x) => x == _data.Item.itemName))
			{
				flag3 = false;
				break;
			}
		}
		_yellowPrice = (flag3 ? _data.Item.DefaultMarketPrice : Math.Min(_data.Item.DefaultMarketPrice, ItemHelper.GetLowestMarketPrice(_data.Item.itemName, _data.Neighborhood)));
		_redPrice = data.Item.DefaultMarketPrice * 2f;
		UpdateAmountBackground();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left && _coveringPlan != null)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(_coveringPlan.headquartersAddress, "PricingManagers");
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.pricingManagersPlanList.SelectPlanById(_coveringPlan.id);
		}
	}
}
