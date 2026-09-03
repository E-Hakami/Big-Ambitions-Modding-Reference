using System;
using BaTable;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class BizManDeliveriesProductCellView : BaTableCellView<BizManDeliveriesProductModel>
{
	public TextMeshProUGUI productNameLabel;

	public GameObject shortageWarning;

	public GameObject backorderWarning;

	public GameObject isNeededBadge;

	public UI.Components.InputField deliveryAmount;

	public Button targetPlusButton;

	public Button targetMinusButton;

	public TextLocalizationComponent priceLabel;

	public TextMeshProUGUI stockLabel;

	public TextMeshProUGUI cargoAmountLabel;

	public GameObject splitter;

	public GameObject bottomPanel;

	public TextLocalizationComponent weeklyLimitLabel;

	public TextLocalizationComponent importIndexLabel;

	public TextLocalizationComponent orderedLastWeekLabel;

	public TextLocalizationComponent soldLastWeekLabel;

	[SerializeField]
	private LayoutElement layoutElement;

	private BizManDeliveriesProductModel _data;

	private bool _toSelect;

	private bool _selected;

	private Action<int> _onNewCellSelected;

	private void Start()
	{
		SetUpListeners();
	}

	public override void VisualizeSelected(bool selected)
	{
		if (!selected)
		{
			return;
		}
		if (_toSelect)
		{
			OpenBottomPart();
			_toSelect = false;
			_selected = true;
			return;
		}
		if (_selected)
		{
			CloseBottomPart();
		}
		_onNewCellSelected?.Invoke(dataIndex);
	}

	private void OpenBottomPart()
	{
		layoutElement.minHeight = 200f;
		splitter.SetActive(value: true);
		bottomPanel.SetActive(value: true);
		backgroundImage.color = selectedBackgroundColor;
		string weeklyLimit = ((!DeliveryHelper.AreWholesaleAndImportLimitsDisabled()) ? (_data.deliveryContractItem.ItemCached.maxWholesaleOrderAmount - _data.deliveryContractItem.amountOrderedThisWeek).ToFormattedNumber() : "-");
		weeklyLimitLabel.Arguments = new { weeklyLimit };
		ProductMarketEntry productMarketEntry = ProductMarketHelper.GetProductMarketEntry(_data.deliveryContractItem.itemName);
		importIndexLabel.Arguments = new
		{
			importIndex = (productMarketEntry?.importPriceIndex.ToString("F1") ?? "-")
		};
		orderedLastWeekLabel.Arguments = new
		{
			orderedLastWeek = _data.deliveryContractItem.amountOrderedLastWeek.ToFormattedNumber()
		};
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(_data.businessAddress);
		soldLastWeekLabel.Arguments = new
		{
			soldLastWeek = FinancialSummaryHelper.ProductsSoldLastWeekInRegistration(_data.deliveryContractItem.itemName, buildingRegistration).ToFormattedNumber()
		};
	}

	private static bool IsProductBeingSold(Address address, string product)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		if (buildingRegistration.GetListOfItemsForSale().Contains(product))
		{
			return true;
		}
		if (product == "ba:itemname_paperbag")
		{
			return BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags);
		}
		return false;
	}

	public void CloseBottomPart()
	{
		layoutElement.minHeight = 100f;
		splitter.SetActive(value: false);
		bottomPanel.SetActive(value: false);
		backgroundImage.color = defaultBackgroundColor;
	}

	public override void SetData(BizManDeliveriesProductModel data)
	{
		_data = data;
		productNameLabel.text = data.productName;
		deliveryAmount.tmpInputField.text = data.deliveryContractItem.amount.ToString();
		deliveryAmount.tmpInputField.interactable = !_data.isContractActive;
		targetPlusButton.interactable = !_data.isContractActive;
		targetMinusButton.interactable = !_data.isContractActive;
		_onNewCellSelected = data.onNewCellSelected;
		_toSelect = data.toSelect;
		_selected = data.isSelected;
		if (data.isSelected)
		{
			OpenBottomPart();
		}
		else
		{
			splitter.SetActive(value: false);
			bottomPanel.SetActive(value: false);
			backgroundImage.color = defaultBackgroundColor;
		}
		UpdatePriceLabel();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(data.wholesalerAddress);
		if (ProductMarketHelper.IsProductInMarketEvent(data.deliveryContractItem.itemName, MarketEventType.ProductBackorder, buildingRegistration.Neighborhood))
		{
			shortageWarning.SetActive(value: false);
			backorderWarning.SetActive(value: true);
		}
		else if (ProductMarketHelper.IsProductInMarketEvent(data.deliveryContractItem.itemName, MarketEventType.ProductShortage, buildingRegistration.Neighborhood))
		{
			shortageWarning.SetActive(value: true);
			backorderWarning.SetActive(value: false);
		}
		else
		{
			shortageWarning.SetActive(value: false);
			backorderWarning.SetActive(value: false);
		}
		bool flag = IsProductBeingSold(data.businessAddress, data.deliveryContractItem.itemName);
		isNeededBadge.SetActive(flag);
		stockLabel.text = _data.stock.ToString();
		UpdateCargoAmountLabel();
	}

	private void SetUpListeners()
	{
		deliveryAmount.tmpInputField.onEndEdit.AddListener(delegate
		{
			string rawValue = deliveryAmount.GetRawValue();
			if (!string.IsNullOrEmpty(rawValue))
			{
				if (!int.TryParse(rawValue, out var result))
				{
					Notifications.ShowError("common_notification_invalid_amount");
				}
				else
				{
					ChangeAmount(result);
				}
			}
		});
		targetPlusButton.onClick.AddListener(delegate
		{
			if (deliveryAmount.maxNumeralAmount >= _data.deliveryContractItem.amount + 1)
			{
				ChangeAmount(_data.deliveryContractItem.amount + 1);
			}
		});
		targetMinusButton.onClick.AddListener(delegate
		{
			if (deliveryAmount.allowNegativeValues || _data.deliveryContractItem.amount > 0)
			{
				ChangeAmount(_data.deliveryContractItem.amount - 1);
			}
		});
	}

	private void ChangeAmount(int newTarget)
	{
		_data.UpdateAmount(newTarget);
		deliveryAmount.tmpInputField.text = _data.deliveryContractItem.amount.ToString();
		UpdatePriceLabel();
		UpdateCargoAmountLabel();
	}

	private void UpdatePriceLabel()
	{
		_data.UpdatePrice();
		priceLabel.Arguments = new
		{
			totalPrice = _data.price.ToCurrencyFormat(),
			pricePerPiece = _data.pricePerUnit.ToCurrencyFormat()
		};
	}

	private void UpdateCargoAmountLabel()
	{
		cargoAmountLabel.text = ((_data.CargoAmount == 0) ? string.Empty : _data.CargoAmount.ToString());
	}

	public override void RefreshCellView()
	{
		layoutElement.minHeight = 100f;
	}
}
