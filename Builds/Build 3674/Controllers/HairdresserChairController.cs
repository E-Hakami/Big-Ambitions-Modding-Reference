using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.Items;
using EmployeeStations;
using Entities;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Notification;
using UI.Purchase;
using UI.Smartphone;
using UnityEngine;

namespace Controllers;

public class HairdresserChairController : EmployeeStationController
{
	private static readonly string[] HeadWasherNames = new string[1] { "ba:itemname_hairdresserheadwash" };

	private static readonly string[] ChairStationNames = new string[2] { "ba:itemname_hairdresserchair", "ba:itemname_hairdresserchairmodern" };

	public bool isHeadWasher;

	public Action onHairChangeAction;

	[HideInInspector]
	public bool hasPlayerHairColorChanged;

	[HideInInspector]
	public bool hasPlayerHairVariantChanged;

	[HideInInspector]
	public bool hasPlayerBeardColorChanged;

	[HideInInspector]
	public bool hasPlayerEyebrowColorChanged;

	[HideInInspector]
	public bool hasPlayerEyebrowVariantChanged;

	[HideInInspector]
	public bool hasPlayerBeardVariantChanged;

	private static readonly List<string> PlayerServicesOrdered = new List<string>();

	private float _playerPrice;

	private bool _subscribedToHairChangeEvents;

	private string[] EmployeeStationNames
	{
		get
		{
			if (!isHeadWasher)
			{
				return ChairStationNames;
			}
			return HeadWasherNames;
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (!playerItemPurchaserSettings.enabled)
		{
			employeeType = typeof(HairdresserStylistEmployee);
		}
	}

	public override void OnDestroy()
	{
		if (!playerItemPurchaserSettings.enabled)
		{
			UnsubscribeToChangeHairEvents();
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			base.OnDestroy();
		}
	}

	public override bool Interact()
	{
		if (playerItemPurchaserSettings.enabled)
		{
			return base.Interact();
		}
		if (!IsPossibleToInteract())
		{
			return false;
		}
		SubscribeToChangeHairEvents();
		InstanceBehavior<UIs>.Instance.changeCharacterHairUI.Show(this);
		return true;
	}

	private bool IsPossibleToInteract()
	{
		if (isHeadWasher)
		{
			return false;
		}
		if (FullMenu.IsOpen)
		{
			return false;
		}
		if (InstanceBehavior<UIs>.Instance.changeCharacterHairUI.gameObject.activeSelf)
		{
			return false;
		}
		if (PurchaseUI.IsPanelOpen)
		{
			return false;
		}
		if (PlayerHelper.ItemInstanceInHands != null || InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			Notifications.Show(NotificationType.Error, "notification_need_empty_hands_to_interact");
			return false;
		}
		if (!employee)
		{
			Notifications.Show(NotificationType.Error, "notification_cashregister_no_employee");
			return false;
		}
		if (!InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController("ba:itemname_haircareproduct", base.transform.position))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", "ba:itemname_haircareproduct" } };
			Notifications.Show(NotificationType.Error, "notification_no_items_available", notificationData);
			return false;
		}
		return true;
	}

	private void OnHairChangeRequest(Action action)
	{
		if (!hasPlayerHairVariantChanged && !hasPlayerHairColorChanged && !hasPlayerEyebrowVariantChanged && !hasPlayerEyebrowColorChanged && !hasPlayerBeardVariantChanged && !hasPlayerBeardColorChanged)
		{
			return;
		}
		if (_playerPrice > 0f && SaveGameManager.Current.Money < _playerPrice)
		{
			Notifications.ShowInsufficientMoney();
			return;
		}
		onHairChangeAction = action;
		UnsubscribeToChangeHairEvents();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.EnableQueueLabel(delegate(bool canceled)
		{
			if (canceled)
			{
				OnOrderCancel();
			}
		});
		OnPlaceOrder();
	}

	private void SubscribeToChangeHairEvents()
	{
		if (!_subscribedToHairChangeEvents)
		{
			ChangeCharacterHairUI changeCharacterHairUI = InstanceBehavior<UIs>.Instance.changeCharacterHairUI;
			changeCharacterHairUI.onChangeHairButtonUpdate = (Action<TextLocalizationComponent>)Delegate.Combine(changeCharacterHairUI.onChangeHairButtonUpdate, new Action<TextLocalizationComponent>(OnChangeHairButtonUpdate));
			ChangeCharacterHairUI changeCharacterHairUI2 = InstanceBehavior<UIs>.Instance.changeCharacterHairUI;
			changeCharacterHairUI2.onHairChangeRequest = (Action<Action>)Delegate.Combine(changeCharacterHairUI2.onHairChangeRequest, new Action<Action>(OnHairChangeRequest));
			_subscribedToHairChangeEvents = true;
		}
	}

	public void UnsubscribeToChangeHairEvents()
	{
		if (_subscribedToHairChangeEvents && !GameManager.isCitySceneBeingUnloaded)
		{
			ChangeCharacterHairUI changeCharacterHairUI = InstanceBehavior<UIs>.Instance.changeCharacterHairUI;
			changeCharacterHairUI.onChangeHairButtonUpdate = (Action<TextLocalizationComponent>)Delegate.Remove(changeCharacterHairUI.onChangeHairButtonUpdate, new Action<TextLocalizationComponent>(OnChangeHairButtonUpdate));
			ChangeCharacterHairUI changeCharacterHairUI2 = InstanceBehavior<UIs>.Instance.changeCharacterHairUI;
			changeCharacterHairUI2.onHairChangeRequest = (Action<Action>)Delegate.Remove(changeCharacterHairUI2.onHairChangeRequest, new Action<Action>(OnHairChangeRequest));
			_subscribedToHairChangeEvents = false;
		}
	}

	private void OnChangeHairButtonUpdate(TextLocalizationComponent obj)
	{
		hasPlayerHairVariantChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasVariantChanged(AppearanceElementType.Hair);
		hasPlayerEyebrowVariantChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasVariantChanged(AppearanceElementType.Eyebrows);
		hasPlayerBeardVariantChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasVariantChanged(AppearanceElementType.Beard);
		hasPlayerHairColorChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasColorChanged(AppearanceElementType.Hair);
		hasPlayerEyebrowColorChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasColorChanged(AppearanceElementType.Eyebrows);
		hasPlayerBeardColorChanged = InstanceBehavior<UIs>.Instance.changeCharacterHairUI.HasColorChanged(AppearanceElementType.Beard);
		_playerPrice = (base.BuildingContext.IsPlayerOwnedBusiness ? 0f : CalculateHairChangePrice());
		obj.SetData(LanguageChangeEventDataHolder.Create("furniture_delivery_item", new
		{
			itemName = "ba:itemname_haircuttingfee",
			itemPrice = _playerPrice.ToCurrencyFormat()
		}));
	}

	private float CalculateHairChangePrice()
	{
		float num = 0f;
		if (hasPlayerHairVariantChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_haircuttingfee");
		}
		if (hasPlayerHairColorChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_hairchemicalfee");
		}
		if (hasPlayerEyebrowVariantChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_haircuttingfee") * 0.5f;
		}
		if (hasPlayerEyebrowColorChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_hairchemicalfee") * 0.5f;
		}
		if (hasPlayerBeardVariantChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_haircuttingfee") * 0.5f;
		}
		if (hasPlayerBeardColorChanged)
		{
			num += ItemHelper.GetPriceOnCurrentBusiness("ba:itemname_hairchemicalfee") * 0.5f;
		}
		return num;
	}

	public override void OnPlaceOrder(List<CargoInstance> orderedCargoInstances = null, List<VehicleInstance> orderedVehicleInstances = null)
	{
		base.OnPlaceOrder(orderedCargoInstances, orderedVehicleInstances);
		PlayerServicesOrdered.Clear();
		if (hasPlayerHairVariantChanged || hasPlayerEyebrowVariantChanged || hasPlayerBeardVariantChanged)
		{
			PlayerServicesOrdered.Add("ba:itemname_haircuttingfee");
		}
		if (hasPlayerHairColorChanged || hasPlayerEyebrowColorChanged || hasPlayerBeardColorChanged)
		{
			PlayerServicesOrdered.Add("ba:itemname_hairchemicalfee");
		}
		Order order = new Order
		{
			entries = PlayerServicesOrdered.Select((string x) => new OrderEntry
			{
				itemName = x,
				price = (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness ? 0f : ItemHelper.GetPriceOnCurrentBusiness(x)),
				priceAccceptable = true,
				available = true
			}).ToList()
		};
		playerCustomer = InstanceBehavior<GameManager>.Instance.playerController.gameObject.GetOrAddComponent<Customer>();
		playerCustomer.tpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		playerCustomer.isPlayer = true;
		playerCustomer.order = order;
		playerCustomer.Init();
		if (!waitingLine.data.customers.Contains(playerCustomer))
		{
			((IWaitingLineHolder)this).JoinWaitingLine(playerCustomer);
		}
	}

	public override void OnOrderCancel()
	{
		if (!playerCustomer)
		{
			return;
		}
		if (playerCustomer.state != CustomerState.Served)
		{
			waitingLine.customersManagement.RemoveCustomer(playerCustomer);
			if (playerCustomer.state == CustomerState.BeingServed)
			{
				StartCoroutine(employee.CancelCurrentOrder());
			}
		}
		playerCustomer.UnsubscribeToGlobalEvents();
		UnityEngine.Object.Destroy(playerCustomer);
		playerCustomer.tpc.Reset();
	}

	protected override void InitWaitingLine()
	{
		if (playerItemPurchaserSettings.enabled)
		{
			return;
		}
		waitingLine.Init(this, base.ItemInstance?.customPositions ?? customPositions, EmployeeStationNames, delegate
		{
			if (base.ItemInstance != null)
			{
				base.ItemInstance.customPositions = waitingLine.data.GetMergedAnchorsAndSpotsList();
			}
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (base.ItemInstance != null)
		{
			List<SerializableVector3> list = base.ItemInstance.customPositions;
			if (list == null || list.Count <= 0)
			{
				list = customPositions;
				if (list == null || list.Count <= 0)
				{
					waitingLine.creator.Reset();
					return;
				}
			}
			if (waitingLine.data.spots.Count == 0)
			{
				waitingLine.creator.SetUpWaitingLine();
			}
		}
		else
		{
			List<SerializableVector3> list = customPositions;
			if (list != null && list.Count > 1 && waitingLine.data.spots.Count == 0)
			{
				waitingLine.creator.SetUpWaitingLine();
			}
		}
	}

	private void OnExitBuilding(Address _)
	{
		waitingLine.data.customers.Clear();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		if (!playerItemPurchaserSettings.enabled)
		{
			base.AssignEmployee(tpc, employeeInstance);
			tpc.GetComponent<HairdresserStylistEmployee>().SetEmployeeStation(this);
		}
	}
}
