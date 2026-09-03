using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using EmployeeStations;
using Entities;
using Helpers;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.Purchase;
using UnityEngine;

namespace Controllers;

public class TicketKioskController : Producer, IWaitingLineHolder
{
	private const float NpcInteractDelay = 1f;

	private static readonly string[] EmployeeStationNames = new string[1] { "ba:itemname_ticketkiosk" };

	[SerializeField]
	private WaitingLine waitingLine;

	[SerializeField]
	private Transform firstCustomerSpot;

	public string ticketItemName = "ba:itemname_cinematicket";

	private Order _playerOrder;

	private Coroutine _servingCoroutine;

	public WaitingLine GetWaitingLine()
	{
		return waitingLine;
	}

	public Transform GetFirstCustomerSpot()
	{
		return firstCustomerSpot;
	}

	public override void Start()
	{
		base.Start();
		if (!playerItemPurchaserSettings.enabled)
		{
			InitWaitingLine();
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		}
	}

	private void Update()
	{
		if (!playerItemPurchaserSettings.enabled && _servingCoroutine == null)
		{
			Customer nextCustomer = waitingLine.data.GetNextCustomer();
			if ((bool)nextCustomer && !nextCustomer.isPlayer)
			{
				_servingCoroutine = StartCoroutine(ServeCustomer(nextCustomer));
			}
		}
	}

	private void InitWaitingLine()
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

	private IEnumerator ServeCustomer(Customer customer)
	{
		try
		{
			OrderEntry orderEntry = customer.order.entries.First((OrderEntry x) => x.itemName == ticketItemName);
			OrderHelper.Validate(customer.citizenData, orderEntry, null);
			bool priceAcceptable = orderEntry.priceAccceptable;
			if (priceAcceptable)
			{
				yield return new WaitForSeconds(1f);
			}
			else
			{
				yield return customer.tpc.ShowExpression(CharacterEmojiName.CustomerTooHighPrice, 3f, new
				{
					itemname = LocalizationHelper.GetItemLabel(ticketItemName)
				});
			}
			customer.order.completed = true;
			Order order = customer.order;
			if (order.timestamp == null)
			{
				order.timestamp = TimeHelper.Now();
			}
			base.BuildingContext.Registration.unprocessedCompletedOrders.Add(customer.order);
			customer.state = CustomerState.Served;
			customer.assignedWaitingLine = null;
			customer.customerEntry.completed = true;
			waitingLine.customersManagement.RemoveCustomer(customer);
			if (!priceAcceptable)
			{
				customer.Leave();
			}
		}
		finally
		{
			_servingCoroutine = null;
		}
	}

	public override bool Interact()
	{
		if (playerItemPurchaserSettings.enabled)
		{
			return base.Interact();
		}
		if (base.BuildingContext.Registration.RentedByPlayer && base.BuildingContext.Building.BuildingType != "ba:buildingtype_cinema")
		{
			return base.Interact();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		OpenOrderUI();
		return true;
	}

	private void OpenOrderUI()
	{
		List<CargoInstance> cargoInstances = new List<CargoInstance>
		{
			new CargoInstance(ticketItemName, 1, base.BuildingContext.IsPlayerOwnedBusiness ? 0f : ItemHelper.GetPriceOnCurrentBusiness(ticketItemName))
		};
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.Purchase, null, OnPlaceOrder, cargoInstances);
	}

	private void OnPlaceOrder(List<CargoInstance> orderedCargoInstances)
	{
		if (orderedCargoInstances != null && orderedCargoInstances.Count != 0)
		{
			Order order = new Order();
			order.entries = orderedCargoInstances.Select((CargoInstance x) => new OrderEntry
			{
				itemName = x.itemName,
				price = (float)x.amount * x.pricePerUnit,
				priceAccceptable = true,
				available = true
			}).ToList();
			order.Pay(base.BuildingContext.Registration, base.transform.position, isPlayer: true);
			SelfServiceEmployee.UpdatePlayerPurchase();
		}
	}
}
