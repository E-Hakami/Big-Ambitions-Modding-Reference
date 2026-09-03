using System;
using System.Collections.Generic;
using System.Linq;
using BaTable;
using BigAmbitions.InputSystem;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PurchasingAgent;

public class PurchasingAgentProductCellView : BaTableCellView<PurchasingAgentProductModel>
{
	public Toggle massActionToggle;

	public TextMeshProUGUI productNameLabel;

	public GameObject shortageWarning;

	public GameObject backorderWarning;

	public UI.Components.InputField targetInput;

	public TextLocalizationComponent priceLabel;

	public UI.Elements.Dropdown assignedWarehouseDropdown;

	public TextMeshProUGUI warehouseStockLabel;

	public GameObject splitter;

	public GameObject bottomPanel;

	public TextLocalizationComponent weeklyLimitLabel;

	public TextLocalizationComponent importIndexLabel;

	public TextLocalizationComponent orderedLastWeekLabel;

	public TextLocalizationComponent soldLastWeekLabel;

	public TMP_Text cargoAmount;

	[SerializeField]
	private UnityEvent<bool> orderLockedChanged;

	private PurchasingAgentProductModel _data;

	private LayoutElement _layoutElement;

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
		if ((object)_layoutElement == null)
		{
			_layoutElement = GetComponent<LayoutElement>();
		}
		if (_layoutElement != null)
		{
			_layoutElement.minHeight = 200f;
		}
		splitter.SetActive(value: true);
		bottomPanel.SetActive(value: true);
		backgroundImage.color = selectedBackgroundColor;
		string weeklyLimit;
		if (DeliveryHelper.AreWholesaleAndImportLimitsDisabled() || !DeliveryHelper.ShouldLimitImporterMaxAmount(_data.productRef.itemName, _data.importerAddress))
		{
			weeklyLimit = "-";
		}
		else
		{
			int maxOrderAmountPerImporter = _data.productRef.ItemCached.maxOrderAmountPerImporter;
			int itemAmountOrderedThisWeek = ImportPartnership.GetItemAmountOrderedThisWeek(_data.importerAddress, _data.productRef.itemName);
			maxOrderAmountPerImporter = Mathf.Max(0, maxOrderAmountPerImporter - itemAmountOrderedThisWeek);
			weeklyLimit = maxOrderAmountPerImporter.ToFormattedNumber();
		}
		weeklyLimitLabel.Arguments = new { weeklyLimit };
		ProductMarketEntry productMarketEntry = ProductMarketHelper.GetProductMarketEntry(_data.productRef.itemName);
		importIndexLabel.Arguments = new
		{
			importIndex = (productMarketEntry?.importPriceIndex.ToString("F1") ?? "-")
		};
		orderedLastWeekLabel.Arguments = new
		{
			orderedLastWeek = _data.productRef.amountOrderedLastWeek.ToFormattedNumber()
		};
		soldLastWeekLabel.Arguments = new
		{
			soldLastWeek = FinancialSummaryHelper.ProductsSoldLastWeek(_data.productRef.itemName).ToFormattedNumber()
		};
	}

	public void CloseBottomPart()
	{
		if ((object)_layoutElement == null)
		{
			_layoutElement = GetComponent<LayoutElement>();
		}
		if (_layoutElement != null)
		{
			_layoutElement.minHeight = 100f;
		}
		splitter.SetActive(value: false);
		bottomPanel.SetActive(value: false);
		backgroundImage.color = defaultBackgroundColor;
	}

	public override void SetData(PurchasingAgentProductModel data)
	{
		_data = data;
		massActionToggle.SetIsOnWithoutNotify(PurchasingAgentProductsMassActionsUI.massActionSelectedProducts.Contains(data.productRef));
		massActionToggle.onValueChanged.RemoveAllListeners();
		massActionToggle.onValueChanged.AddListener(delegate(bool toggled)
		{
			PurchasingAgentPlanUI purchasingAgentPlanUISettings = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.purchasingAgentsPlanList.purchasingAgentPlanUISettings;
			if (PlayerAction.SelectMultipleElements.Pressing() && purchasingAgentPlanUISettings.lastSelectedProductIndex != -1)
			{
				purchasingAgentPlanUISettings.massActionsUI.ToggleRange(purchasingAgentPlanUISettings.lastSelectedProductIndex, dataIndex, toggled);
			}
			else
			{
				purchasingAgentPlanUISettings.massActionsUI.ToggleSelectedProduct(data.productRef, toggled);
			}
			purchasingAgentPlanUISettings.lastSelectedProductIndex = dataIndex;
		});
		productNameLabel.text = data.ProductName;
		targetInput.tmpInputField.text = data.productRef.amount.ToString();
		targetInput.tmpInputField.interactable = !_data.isContractActive;
		_onNewCellSelected = data.onNewCellSelected;
		_toSelect = data.toSelect;
		_selected = data.toSelect;
		UpdateCargoAmount();
		if (data.toSelect)
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
		SetAssignedWarehouseDropdown();
		if (ProductMarketHelper.IsProductInMarketEvent(data.productRef.itemName, MarketEventType.ProductBackorder))
		{
			shortageWarning.SetActive(value: false);
			backorderWarning.SetActive(value: true);
		}
		else if (ProductMarketHelper.IsProductInMarketEvent(data.productRef.itemName, MarketEventType.ProductShortage))
		{
			shortageWarning.SetActive(value: true);
			backorderWarning.SetActive(value: false);
		}
		else
		{
			shortageWarning.SetActive(value: false);
			backorderWarning.SetActive(value: false);
		}
		warehouseStockLabel.text = _data.WarehouseStock.ToString();
		orderLockedChanged?.Invoke(_data.isContractActive);
	}

	private void SetAssignedWarehouseDropdown()
	{
		List<string> list = new List<string> { "common_unassigned".GetLocalization() };
		list.AddRange(_data.warehouses.Select((BuildingRegistration x) => x.BusinessName));
		assignedWarehouseDropdown.SetOptions(list, localize: false);
		assignedWarehouseDropdown.onOptionSelected.AddListener(ChangeAssignedWarehouse);
		BuildingRegistration buildingRegistration = _data.warehouses.FirstOrDefault((BuildingRegistration x) => x.Address == _data.productRef.assignedWarehouse);
		int defaultOption = ((buildingRegistration != null) ? (_data.warehouses.IndexOf(buildingRegistration) + 1) : 0);
		assignedWarehouseDropdown.ResetSelectedOption(defaultOption);
		assignedWarehouseDropdown.SetInteractable(!_data.isContractActive);
	}

	private void SetUpListeners()
	{
		targetInput.tmpInputField.onEndEdit.AddListener(delegate
		{
			string rawValue = targetInput.GetRawValue();
			if (!string.IsNullOrEmpty(rawValue))
			{
				if (!int.TryParse(rawValue, out var result))
				{
					Notifications.ShowError("common_notification_invalid_amount");
				}
				else
				{
					ChangeTarget(result);
				}
			}
		});
	}

	private void ChangeTarget(int newTarget)
	{
		_data.UpdateAmount(newTarget);
		targetInput.tmpInputField.text = _data.productRef.amount.ToString();
		UpdateCargoAmount();
		UpdatePriceLabel();
		SaveGameManager.MarkChange();
	}

	private void UpdateCargoAmount()
	{
		cargoAmount.text = ((_data.CargoAmount == 0) ? string.Empty : _data.CargoAmount.ToString());
	}

	private void ChangeAssignedWarehouse(int index)
	{
		_data.productRef.assignedWarehouse = ((index <= 0) ? null : _data.warehouses[index - 1].Address);
		_data.UpdateWarehouse();
		_data.updateContractLabels();
		warehouseStockLabel.text = _data.WarehouseStock.ToString();
		UpdatePriceLabel();
		SaveGameManager.MarkChange();
	}

	private void UpdatePriceLabel()
	{
		_data.UpdatePrice();
		priceLabel.Arguments = new
		{
			totalPrice = _data.Price.ToCurrencyFormat() + "\n",
			pricePerPiece = _data.PricePerUnit.ToCurrencyFormat()
		};
	}

	public override void RefreshCellView()
	{
		if ((object)_layoutElement == null)
		{
			_layoutElement = GetComponent<LayoutElement>();
		}
		if (_layoutElement != null)
		{
			_layoutElement.minHeight = 100f;
		}
		SetData(_data);
	}
}
