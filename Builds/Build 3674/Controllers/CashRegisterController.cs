using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Buildings.Indoors.InteriorDesign;
using EmployeeStations;
using Entities;
using Extensions;
using Helpers;
using Player.FoodDeliveryJob;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.ItemWarningIcons;
using UI;
using UI.Notification;
using UI.Purchase;
using UnityEngine;

namespace Controllers;

public class CashRegisterController : EmployeeStationController
{
	private const float MaxDistanceToInteractFromVehicle = 5f;

	private static string[] EmployeeStationNamesCache;

	private Order _playerOrder;

	protected virtual string[] EmployeeStationNames
	{
		get
		{
			if (EmployeeStationNamesCache != null)
			{
				return EmployeeStationNamesCache;
			}
			string[] allItemNamesByTag = ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.iscashregister);
			string[] allItemNamesByTag2 = ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isconcessionscashregister);
			EmployeeStationNamesCache = allItemNamesByTag.Concat(allItemNamesByTag2).ToArray();
			return EmployeeStationNamesCache;
		}
	}

	public override bool CanInteractInAnyBusiness => CanAcceptFoodDelivery();

	public override void Start()
	{
		base.Start();
		if (!playerItemPurchaserSettings.enabled && (!base.BuildingContext.Registration.RentedByPlayer || BuildingTypeHelper.GetData(base.BuildingContext.Registration).HasTag(TagRef.Buildingtypetag.canspawncustomersindoors)))
		{
			SetEmployeeType();
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
			base.ItemInstance.AddCallToOnItemsInCargoUpdated(base.SetStockAmount);
		}
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (base.gameObject.activeInHierarchy && !(address != base.BuildingContext.Building.Address))
		{
			SetEmployeeType();
		}
	}

	private void SetEmployeeType()
	{
		Type type;
		switch (base.BuildingContext.BusinessType?.customerType)
		{
		case CustomerType.Nightclub:
		case CustomerType.Casino:
			type = typeof(BarmanEmployee);
			break;
		case CustomerType.SelfService:
		case CustomerType.CinemaTheater:
			type = typeof(SelfServiceEmployee);
			break;
		case CustomerType.FullService:
		case CustomerType.Hairdresser:
			type = typeof(FullServiceEmployee);
			break;
		default:
			type = null;
			break;
		}
		employeeType = type;
	}

	protected override void InitWaitingLine()
	{
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
			else
			{
				waitingLine.creator.ResetByTemplatePosition();
			}
		}
	}

	private void OnExitBuilding(Address _)
	{
		waitingLine.data.customers.Clear();
	}

	protected override bool CanBeInteractedFromCurrentPosition()
	{
		if (base.CanBeInteractedFromCurrentPosition())
		{
			return true;
		}
		if (playerItemPurchaserSettings.enabled || !BuildingManager.IsInsideBuilding || base.BuildingContext.IsPlayerOwnedBusiness)
		{
			return false;
		}
		if (!VehicleHelper.IsInsideVehicle())
		{
			return true;
		}
		if (Vector3.SqrMagnitude(base.transform.position - PlayerHelper.GetPosition()) < 25f)
		{
			return true;
		}
		return false;
	}

	public override bool Interact()
	{
		if (playerItemPurchaserSettings.enabled || employeeType == null)
		{
			return base.Interact();
		}
		if (base.BuildingContext.Registration.RentedByPlayer && !BuildingTypeHelper.GetData(base.BuildingContext.Registration).HasTag(TagRef.Buildingtypetag.canspawncustomersindoors))
		{
			return base.Interact();
		}
		if (CanOrder())
		{
			if (InteractAsCustomService())
			{
				return true;
			}
			CustomerType customerType = base.BuildingContext.BusinessType.customerType;
			if (customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater)
			{
				return InteractAsSelfService();
			}
			if (!PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
			{
				return InteractAsFullService();
			}
		}
		if (base.Interact())
		{
			return true;
		}
		if (PlayerHelper.IsHoldingItem || PlayerHelper.IsUsingVehicle)
		{
			Notifications.Show(NotificationType.Error, "notification_need_empty_hands_to_interact", null, 4f, "needemptyhands");
		}
		return false;
	}

	public virtual bool CanOrder()
	{
		CustomerType customerType = base.BuildingContext.BusinessType.customerType;
		if (customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater)
		{
			return !PlayerHelper.HasPaidForAllItems();
		}
		if (PlayerHelper.IsHoldingItem || PlayerHelper.IsUsingVehicle)
		{
			return false;
		}
		if (customerType == CustomerType.FullService || customerType == CustomerType.Hairdresser || customerType == CustomerType.Nightclub || customerType == CustomerType.Casino)
		{
			return base.Item.canPlayerDoOrder;
		}
		return false;
	}

	public bool CanAcceptFoodDelivery()
	{
		if (!InteriorDesignerHelper.BlueprintCreatorMode && !base.BuildingContext.IsPlayerOwnedBusiness)
		{
			return FoodDeliveryJobHelper.HasActiveOffer(base.BuildingContext.Building.Address);
		}
		return false;
	}

	public void AcceptFoodDelivery()
	{
		FoodDeliveryJobHelper.OnClickAcceptOffer(this);
	}

	public override WarningIconType GetWarningIconType()
	{
		if (!CanAcceptFoodDelivery())
		{
			return base.GetWarningIconType();
		}
		return WarningIconType.FoodDelivery;
	}

	public virtual void Order()
	{
		if (CanOrder())
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
			CustomerType customerType = base.BuildingContext.BusinessType.customerType;
			if (customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater)
			{
				InteractAsSelfService();
			}
			else
			{
				InteractAsFullService();
			}
		}
	}

	protected virtual bool InteractAsCustomService()
	{
		return false;
	}

	private bool InteractAsFullService()
	{
		OpenFullServiceOrderUI(this);
		return true;
	}

	private static void OpenFullServiceOrderUI(EmployeeStationController stationController)
	{
		BuildingManager instance = InstanceBehavior<BuildingManager>.Instance;
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ItemController allItemController in instance.allItemControllers)
		{
			string producedItemName = allItemController.GetProducedItemName();
			if (!string.IsNullOrEmpty(producedItemName))
			{
				Item byName = ItemsGetter.GetByName(producedItemName);
				if ((byName.type & ItemType.RetailProduct) != 0 && !byName.HasTag(TagRef.Itemtag.isbag) && !(PlayerItemPurchaser.GetShelfFillState(producedItemName, instance.buildingRegistration) <= 0f))
				{
					hashSet.Add(producedItemName);
				}
			}
		}
		List<CargoInstance> list = new List<CargoInstance>(hashSet.Count);
		bool isPlayerOwnedBusiness = instance.IsPlayerOwnedBusiness;
		foreach (string item in hashSet)
		{
			float pricePerUnit = (isPlayerOwnedBusiness ? 0f : ItemHelper.GetPriceOnCurrentBusiness(item));
			list.Add(new CargoInstance(item, 1, pricePerUnit));
		}
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.Purchase, OnClose, delegate(List<CargoInstance> selectedItems)
		{
			stationController.OnPlaceOrder(selectedItems);
		}, list, PurchaseUI.Mode.SingleToggle);
		void OnClose(bool canceled)
		{
			if (canceled)
			{
				Customer component = InstanceBehavior<GameManager>.Instance.playerController.gameObject.GetComponent<Customer>();
				if (component == null)
				{
					stationController.OnOrderCancel();
				}
				else if ((bool)component.assignedWaitingLine && (bool)component.assignedWaitingLine.EmployeeStationController)
				{
					component.assignedWaitingLine.EmployeeStationController.OnOrderCancel();
				}
			}
		}
	}

	private bool InteractAsSelfService()
	{
		if (playerItemPurchaserSettings.enabled)
		{
			return true;
		}
		if (!PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
		{
			return true;
		}
		List<CargoInstance> cargoInstances = (PlayerHelper.IsUsingVehicle ? VehicleHelper.GetCurrentVehicle().cargoInstances : PlayerHelper.ItemInstanceInHands.cargoInstances).Where((CargoInstance x) => !x.paid).ToList();
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.Purchase, delegate(bool canceled)
		{
			if (canceled)
			{
				OnOrderCancel();
			}
		}, delegate(List<CargoInstance> selectedItems)
		{
			OnPlaceOrder(selectedItems);
		}, cargoInstances);
		Vector3 firstQueuePosition = ((IWaitingLineHolder)this).GetFirstQueuePosition();
		InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(firstQueuePosition, showParticleEffect: true);
		return true;
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		switch (base.BuildingContext.BusinessType.customerType)
		{
		case CustomerType.Nightclub:
			tpc.GetComponent<BarmanEmployee>().SetEmployeeStation(this);
			break;
		case CustomerType.FullService:
		case CustomerType.Hairdresser:
			tpc.GetComponent<FullServiceEmployee>().SetEmployeeStation(this);
			break;
		case CustomerType.SelfService:
			break;
		}
	}

	public override void UnassignEmployee()
	{
		if (employee == null)
		{
			return;
		}
		CustomerType customerType = base.BuildingContext.BusinessType.customerType;
		if (customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater)
		{
			if (employee.isPlayer)
			{
				employee.employeeTpc.Reset();
			}
		}
		else if ((bool)employee)
		{
			employee.employeeTpc.SetHandContent(null);
		}
		base.UnassignEmployee();
	}

	public override void OnPlaceOrder(List<CargoInstance> orderedCargoInstances = null, List<VehicleInstance> orderedVehicleInstances = null)
	{
		base.OnPlaceOrder(orderedCargoInstances, orderedVehicleInstances);
		if (orderedCargoInstances == null || orderedCargoInstances.Count <= 0)
		{
			return;
		}
		Order order = new Order
		{
			entries = orderedCargoInstances.Select((CargoInstance x) => new OrderEntry
			{
				itemName = x.itemName,
				price = (float)x.amount * x.pricePerUnit,
				priceAccceptable = true,
				available = true
			}).ToList()
		};
		if (base.BuildingContext.IsPlayerOwnedBusiness)
		{
			BusinessType data = BusinessTypeHelper.GetData(base.BuildingContext.Registration);
			if (data.HasTag(TagRef.Businesstag.customersneedpaperbags) && base.ItemInstance.GetStockInstance().amount == 0)
			{
				Notifications.Show(NotificationType.Error, "cashregister_no_paperbags");
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
				return;
			}
			CustomerType customerType = data.customerType;
			bool flag = customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater;
			ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
			bool flag2 = FullServiceEmployee.CheckStockAvailableForEntries(order.entries, character.transform.position);
			if (!flag && !flag2)
			{
				Notifications.Show(NotificationType.Error, "order_based_self_service_no_stock");
				InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
				return;
			}
			if (!employee)
			{
				if (flag)
				{
					order.Pay(base.BuildingContext.Registration, base.transform.position, isPlayer: true);
					base.ItemInstance.SubtractFromStock();
					SelfServiceEmployee.ConvertPlayerShoppingContainerToPaperBag();
					SelfServiceEmployee.UpdatePlayerPurchase();
				}
				else
				{
					_playerOrder = order;
					_playerOrder.entries.ForEach(delegate(OrderEntry x)
					{
						x.available = false;
					});
					StartCoroutine(MakeFullServiceSelfPurchase(_playerOrder, character));
				}
				return;
			}
		}
		playerCustomer = InstanceBehavior<GameManager>.Instance.playerController.gameObject.GetOrAddComponent<Customer>();
		playerCustomer.isPlayer = true;
		playerCustomer.tpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		playerCustomer.order = order;
		playerCustomer.Init();
		if (!waitingLine.data.customers.Contains(playerCustomer))
		{
			((IWaitingLineHolder)this).JoinWaitingLine(playerCustomer);
		}
	}

	private IEnumerator MakeFullServiceSelfPurchase(Order order, ThirdPersonCharacter tpc)
	{
		base.ItemInstance.SubtractFromStock();
		FullServiceEmployee.SetPaperbag(tpc);
		yield return FullServiceEmployee.GrabOrderEntryItems(order.entries, tpc);
		order.Pay(base.BuildingContext.Registration, base.transform.position, isPlayer: true);
		FullServiceEmployee.UpdatePlayerPurchase(order.entries);
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Close();
	}

	public override void OnOrderCancel()
	{
		bool flag = BusinessTypeHelper.GetData(base.BuildingContext.Registration).customerType == CustomerType.FullService;
		bool flag2 = base.BuildingContext.Registration.businessTypeName == "ba:businesstype_nightclub";
		if ((bool)playerCustomer)
		{
			if (employee.customer == playerCustomer)
			{
				StartCoroutine(employee.CancelCurrentOrder());
			}
			waitingLine.customersManagement.RemoveCustomer(playerCustomer);
			playerCustomer.UnsubscribeToGlobalEvents();
			UnityEngine.Object.Destroy(playerCustomer);
		}
		else
		{
			if (!(flag | flag2))
			{
				return;
			}
			StopAllCoroutines();
			InstanceBehavior<GameManager>.Instance.playerController.Character.SetHandContent(null);
			if (_playerOrder != null)
			{
				_playerOrder.entries.RemoveAll((OrderEntry x) => !x.available);
				_playerOrder.Pay(base.BuildingContext.Registration, base.transform.position, isPlayer: true);
				FullServiceEmployee.UpdatePlayerPurchase(_playerOrder.entries);
			}
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			base.ItemInstance.RemoveCallFromOnItemsInCargoUpdated(base.SetStockAmount);
			base.OnDestroy();
		}
	}

	public bool IsFull()
	{
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return false;
		}
		return stockInstance.amount >= stockInstance.GetMaxStockCapacity(base.ItemInstance);
	}
}
