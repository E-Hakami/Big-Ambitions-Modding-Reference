using System;
using System.Collections.Generic;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using Buildings.Retail.Businesses.CinemaTheater;
using CameraControllers;
using DG.Tweening;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.ItemPanel;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Purchase;

public class PurchaseUI : MonoBehaviour
{
	public enum Mode
	{
		NoChanges,
		SingleToggle
	}

	public enum Type
	{
		Purchase,
		IRS,
		CasinoBoat
	}

	private const int MaxPurchaseAmount = 10;

	private const float CasinoBoatTicketPrice = 5000f;

	private const string UiTemplateTag = "UiTemplate";

	public Image panel;

	public GameObject totalPrice;

	public TextMeshProUGUI totalPriceValue;

	public TextMeshProUGUI inQueueLabel;

	public Transform itemsContainer;

	public Transform itemTemplate;

	public Transform taxItemTemplate;

	public Button orderButton;

	public TextLocalizationComponent orderButtonTextLocalizationComponent;

	public Button cancelButton;

	[SerializeField]
	private TextLocalizationComponent titleLabel;

	private readonly Dictionary<int, PurchaseUiItem> _cargoInstanceItems = new Dictionary<int, PurchaseUiItem>();

	private readonly Dictionary<int, int> _cargoInstancesAmounts = new Dictionary<int, int>();

	private readonly List<TaxPaymentType> _irsTaxPaymentTypes = new List<TaxPaymentType>();

	private List<CargoInstance> _cargoInstances;

	private bool _isItemPanelVisible;

	private Mode _mode;

	private UnityAction<bool> _onClose;

	private UnityAction<List<CargoInstance>> _onOrder;

	private bool _orderPlaced;

	private RectTransform _panelRect;

	private Type _type;

	public static bool IsPanelOpen { get; private set; }

	public TaxPaymentType CurrentTaxPaymentType { get; private set; }

	public float CurrentTaxPaymentAmount { get; private set; }

	public bool IsDoingPurchase
	{
		get
		{
			if (IsPanelOpen)
			{
				return !_orderPlaced;
			}
			return false;
		}
	}

	private void Start()
	{
		_panelRect = panel.GetComponent<RectTransform>();
		SubscribeToEvents();
		HideTemplates();
	}

	private void SubscribeToEvents()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.RegisterOnGameLoadedCallback(SetUpKeysLabels);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
	}

	private void OnCityMapToggle(bool isOpen)
	{
		if (IsPanelOpen)
		{
			ChangeVisibility(!isOpen);
		}
	}

	private void OnExitBuilding(Address _)
	{
		if (IsPanelOpen)
		{
			Close();
		}
	}

	private void SetUpKeysLabels()
	{
		orderButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Interact.AsSuffix();
		cancelButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.Cancel.AsSuffix();
	}

	private void SetItemsPurchase(List<CargoInstance> cargoInstances)
	{
		if (cargoInstances == null)
		{
			return;
		}
		_cargoInstanceItems.Clear();
		for (int i = 0; i < cargoInstances.Count; i++)
		{
			_cargoInstancesAmounts.Add(i, 0);
			CargoInstance cargoInstance = cargoInstances[i];
			float price = (float)cargoInstance.amount * cargoInstance.pricePerUnit;
			PurchaseUiItem item = CreateItemEntry();
			item.Setup(cargoInstance.GetLabel(), price);
			if (_mode != Mode.NoChanges)
			{
				int currentIndex = i;
				item.SetAmountSelector(visible: true, 10, delegate(int newAmount)
				{
					UpdatePurchaseItemAmount(currentIndex, item, price, newAmount);
				});
				_cargoInstanceItems.Add(i, item);
			}
		}
	}

	private void UpdatePurchaseItemAmount(int itemIndex, PurchaseUiItem item, float price, int newAmount)
	{
		_cargoInstancesAmounts[itemIndex] = newAmount;
		int currentPurchaseAmount = GetCurrentPurchaseAmount();
		var arguments = new
		{
			currentAmount = currentPurchaseAmount,
			maxAmount = 10
		};
		titleLabel.SetData("purchaseui_title_with_amounts".Localize(arguments));
		item.SetPrice((newAmount == 0) ? price : (price * (float)newAmount));
		SetTotalPrice();
		int num = 10 - currentPurchaseAmount;
		foreach (PurchaseUiItem value in _cargoInstanceItems.Values)
		{
			value.SetMaxAmount(value.Amount + num);
		}
	}

	private void SetItemsIRS()
	{
		_irsTaxPaymentTypes.Clear();
		if (TaxHelper.HasCurrentTaxesToPay())
		{
			_irsTaxPaymentTypes.Add(TaxPaymentType.CurrentTaxes);
		}
		if (TaxHelper.HasBackTaxesToPay())
		{
			_irsTaxPaymentTypes.Add(TaxPaymentType.BackTaxes);
		}
		if (_irsTaxPaymentTypes.Count != 0)
		{
			if (!HasIrsPaymentType(CurrentTaxPaymentType))
			{
				CurrentTaxPaymentType = _irsTaxPaymentTypes[0];
			}
			for (int i = 0; i < _irsTaxPaymentTypes.Count; i++)
			{
				AddIrsPaymentItem(_irsTaxPaymentTypes[i], i == 0);
			}
		}
	}

	private void AddIrsPaymentItem(TaxPaymentType taxPaymentType, bool focusInput)
	{
		PurchaseUiTax purchaseUiTax = CreateTaxEntry();
		if ((bool)purchaseUiTax)
		{
			purchaseUiTax.Setup(GetIrsPaymentKey(taxPaymentType), GetIrsPaymentAmount(taxPaymentType), delegate(float amount)
			{
				SelectIrsPaymentType(taxPaymentType, amount);
			}, focusInput);
		}
	}

	private bool HasIrsPaymentType(TaxPaymentType taxPaymentType)
	{
		return _irsTaxPaymentTypes.Exists((TaxPaymentType paymentType) => paymentType == taxPaymentType);
	}

	private void SelectIrsPaymentType(TaxPaymentType taxPaymentType, float amount)
	{
		CurrentTaxPaymentType = taxPaymentType;
		CurrentTaxPaymentAmount = amount;
		SetIrsOrderButtonText();
		SetTotalPrice();
		PlaceOrder();
	}

	private void SetItemCasino()
	{
		CreateItemEntry().Setup("casinoboat_ticket_pay_item", 5000f);
	}

	private PurchaseUiItem CreateItemEntry()
	{
		return itemTemplate.CreateElement().GetComponent<PurchaseUiItem>();
	}

	private PurchaseUiTax CreateTaxEntry()
	{
		Transform transform = GetTaxItemTemplate();
		if (!transform)
		{
			return null;
		}
		PurchaseUiTax component = transform.CreateElement().GetComponent<PurchaseUiTax>();
		if (!component)
		{
			Debug.LogError("Missing PurchaseUiTax component on tax item template");
			return null;
		}
		component.name = transform.name;
		return component;
	}

	private Transform GetTaxItemTemplate()
	{
		if ((bool)taxItemTemplate)
		{
			return taxItemTemplate;
		}
		Debug.LogError("Missing tax item template in PurchaseUI");
		return null;
	}

	private void ResetItemEntries()
	{
		Transform parent = itemTemplate.parent;
		for (int num = parent.childCount - 1; num >= 0; num--)
		{
			Transform child = parent.GetChild(num);
			if (!(child == itemTemplate) && !(child == taxItemTemplate) && child.CompareTag("UiTemplate"))
			{
				child.gameObject.SetActive(value: false);
				UnityEngine.Object.Destroy(child.gameObject);
			}
		}
		HideTemplates();
	}

	private void HideTemplates()
	{
		itemTemplate.gameObject.SetActive(value: false);
		taxItemTemplate.gameObject.SetActive(value: false);
	}

	private int GetCurrentPurchaseAmount()
	{
		int num = 0;
		foreach (int value in _cargoInstancesAmounts.Values)
		{
			num += value;
		}
		return num;
	}

	public void Open(Type type, UnityAction<bool> onClose, UnityAction<List<CargoInstance>> onOrder = null, List<CargoInstance> cargoInstances = null, Mode mode = Mode.NoChanges, TaxPaymentType taxPaymentType = TaxPaymentType.CurrentTaxes)
	{
		if (!IsPanelOpen)
		{
			IsPanelOpen = true;
			_panelRect.anchoredPosition = Vector3.zero;
			_orderPlaced = false;
			_onClose = onClose;
			_onOrder = onOrder;
			_mode = mode;
			_type = type;
			CurrentTaxPaymentType = taxPaymentType;
			CurrentTaxPaymentAmount = 0f;
			_cargoInstancesAmounts.Clear();
			_cargoInstanceItems.Clear();
			_isItemPanelVisible = ItemPanelUI.IsVisible;
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
			totalPrice.SetActive(_type != Type.IRS);
			itemsContainer.gameObject.SetActive(value: true);
			inQueueLabel.gameObject.SetActive(value: false);
			orderButton.gameObject.SetActive(_type != Type.IRS);
			ResetItemEntries();
			orderButtonTextLocalizationComponent.Key = "purchaseui_place_order";
			if (_type == Type.IRS)
			{
				titleLabel.SetData("purchaseui_pay_taxes".Localize());
			}
			else
			{
				titleLabel.SetData((_mode == Mode.NoChanges) ? "common_purchase".Localize() : "purchaseui_title_with_amounts".Localize(new
				{
					currentAmount = 0,
					maxAmount = 10
				}));
			}
			_cargoInstances = cargoInstances;
			switch (_type)
			{
			case Type.Purchase:
				SetItemsPurchase(cargoInstances);
				break;
			case Type.IRS:
				SetItemsIRS();
				SetIrsOrderButtonText();
				break;
			case Type.CasinoBoat:
				SetItemCasino();
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			SetTotalPrice();
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.PurchaseUI);
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			ChangeVisibility(visible: true);
			PedestrianCam.blockCameraZoom = true;
		}
	}

	private void SetTotalPrice()
	{
		switch (_type)
		{
		case Type.Purchase:
		{
			float num = 0f;
			if (_cargoInstances != null)
			{
				for (int i = 0; i < _cargoInstances.Count; i++)
				{
					CargoInstance cargoInstance = _cargoInstances[i];
					num = ((_mode != Mode.NoChanges) ? (num + (float)cargoInstance.amount * cargoInstance.pricePerUnit * (float)_cargoInstancesAmounts[i]) : (num + (float)cargoInstance.amount * cargoInstance.pricePerUnit));
				}
			}
			totalPriceValue.text = num.ToCurrencyFormat();
			Button button = orderButton;
			int interactable;
			if (_mode != Mode.NoChanges)
			{
				interactable = ((_cargoInstancesAmounts.Count > 0) ? 1 : 0);
			}
			else
			{
				List<CargoInstance> cargoInstances = _cargoInstances;
				interactable = ((cargoInstances == null || cargoInstances.Count != 0) ? 1 : 0);
			}
			button.interactable = (byte)interactable != 0;
			break;
		}
		case Type.IRS:
		{
			float irsPaymentAmount = GetIrsPaymentAmount();
			totalPriceValue.text = Math.Floor(irsPaymentAmount).ToShortCurrencyFormat();
			orderButton.interactable = false;
			break;
		}
		case Type.CasinoBoat:
			totalPriceValue.text = 5000f.ToCurrencyFormat();
			orderButton.interactable = true;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	public void ClickClose()
	{
		Close();
	}

	public void Close(bool? overrideOrderPlaced = null)
	{
		_onClose?.Invoke(overrideOrderPlaced ?? _orderPlaced);
		ChangeVisibility(visible: false);
		PedestrianCam.blockCameraZoom = false;
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PurchaseUI);
		if (_isItemPanelVisible)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
		}
		cancelButton.interactable = true;
		IsPanelOpen = false;
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
	}

	public void PlaceOrder()
	{
		if (_mode != Mode.NoChanges)
		{
			List<CargoInstance> list = new List<CargoInstance>();
			for (int i = 0; i < _cargoInstances.Count; i++)
			{
				CargoInstance cargoInstance = _cargoInstances[i];
				for (int j = 0; j < _cargoInstancesAmounts[i]; j++)
				{
					list.Add(new CargoInstance(cargoInstance.itemName, cargoInstance.amount, cargoInstance.pricePerUnit));
				}
			}
			_onOrder?.Invoke(list);
		}
		else
		{
			_onOrder?.Invoke(_cargoInstances);
		}
		_orderPlaced = true;
		totalPrice.SetActive(value: false);
		itemsContainer.gameObject.SetActive(value: false);
		inQueueLabel.gameObject.SetActive(value: true);
		PedestrianCam.blockCameraZoom = false;
		_panelRect.DOAnchorPosY(-790f, 0.5f).SetLink(base.gameObject);
		orderButton.gameObject.SetActive(value: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
	}

	public void EnableQueueLabel(UnityAction<bool> onClose)
	{
		_onClose = onClose;
		_orderPlaced = true;
		IsPanelOpen = true;
		_isItemPanelVisible = ItemPanelUI.IsVisible;
		inQueueLabel.gameObject.SetActive(value: true);
		totalPrice.SetActive(value: false);
		itemsContainer.gameObject.SetActive(value: false);
		orderButton.gameObject.SetActive(value: false);
		ChangeVisibility(visible: true);
		_panelRect.DOAnchorPosY(-790f, 0.5f).SetLink(base.gameObject);
	}

	public void ChangeVisibility(bool visible)
	{
		panel.gameObject.SetActive(visible);
	}

	private float GetIrsPaymentAmount()
	{
		return GetIrsPaymentAmount(CurrentTaxPaymentType);
	}

	private static float GetIrsPaymentAmount(TaxPaymentType taxPaymentType)
	{
		if (taxPaymentType != TaxPaymentType.BackTaxes)
		{
			return TaxHelper.GetCurrentTaxesToPay();
		}
		return TaxHelper.GetBackTaxesToPay();
	}

	private static string GetIrsPaymentKey(TaxPaymentType taxPaymentType)
	{
		if (taxPaymentType != TaxPaymentType.BackTaxes)
		{
			return "taxes_pay_item";
		}
		return "taxes_back_taxes_pay_item";
	}

	private void SetIrsOrderButtonText()
	{
		orderButtonTextLocalizationComponent.Key = ((CurrentTaxPaymentType == TaxPaymentType.BackTaxes) ? "purchaseui_pay_back_taxes" : "purchaseui_pay_taxes");
	}

	public void SetCargoInstancesToPaid()
	{
		if (_cargoInstances == null || _cargoInstances.Count == 0)
		{
			Debug.LogError("No cargo instances to set to paid in PurchaseUI");
			return;
		}
		foreach (CargoInstance cargoInstance4 in _cargoInstances)
		{
			cargoInstance4.paid = true;
		}
		TicketEntryBlocker.UpdateBlockersDelayed();
		if (!PlayerHelper.IsUsingVehicle)
		{
			return;
		}
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		for (int num = _cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = _cargoInstances[num];
			CargoInstance cargoInstance2 = null;
			for (int i = 0; i < currentVehicle.cargoInstances.Count; i++)
			{
				CargoInstance cargoInstance3 = currentVehicle.cargoInstances[i];
				if (cargoInstance3.itemName == cargoInstance.itemName && cargoInstance3.paid == cargoInstance.paid && cargoInstance3.amount < cargoInstance3.ItemCached.boxSize && cargoInstance3.customColors == cargoInstance.customColors && cargoInstance3.nestedCargoInstances.Count == 0 && cargoInstance3 != cargoInstance)
				{
					cargoInstance2 = cargoInstance3;
					break;
				}
			}
			if (cargoInstance2 != null)
			{
				int num2 = cargoInstance2.ItemCached.boxSize - cargoInstance2.amount;
				int num3 = ((num2 > cargoInstance.amount) ? cargoInstance.amount : num2);
				CargoInstance cargoInstanceToMerge = new CargoInstance(cargoInstance.itemName, num3, cargoInstance.pricePerUnit);
				cargoInstance2.MergeAmount(cargoInstanceToMerge, num3);
				cargoInstance.amount -= num3;
				if (cargoInstance.amount <= 0)
				{
					currentVehicle.cargoInstances.Remove(cargoInstance);
					currentVehicle.OnItemsInCargoUpdated()?.Invoke();
				}
			}
		}
	}
}
