using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using EmployeeStations;
using Entities;
using Extensions;
using HGAttributes;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.Purchase;
using UnityEngine;

namespace Controllers;

public class TicketBoothController : CashRegisterController
{
	private static readonly string[] CachedStationNames = new string[1] { "ba:itemname_boothticket" };

	[AutocompleteDropdown("Items")]
	public string ticketItemName = "ba:itemname_theaterticket";

	protected override string[] EmployeeStationNames => CachedStationNames;

	public override void Awake()
	{
		base.Awake();
		employeeType = typeof(TicketBoothEmployee);
	}

	public override bool CanOrder()
	{
		if (employeeInstance != null)
		{
			return !base.BuildingContext.IsPlayerOwnedBusiness;
		}
		return false;
	}

	protected override bool InteractAsCustomService()
	{
		OpenOrderUI();
		return true;
	}

	public override void Order()
	{
		if (CanOrder())
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
			OpenOrderUI();
		}
	}

	private void OpenOrderUI()
	{
		List<CargoInstance> cargoInstances = new List<CargoInstance>
		{
			new CargoInstance(ticketItemName, 1, base.BuildingContext.IsPlayerOwnedBusiness ? 0f : ItemHelper.GetPriceOnCurrentBusiness(ticketItemName))
		};
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.Purchase, delegate(bool canceled)
		{
			if (canceled)
			{
				Customer component = InstanceBehavior<GameManager>.Instance.playerController.gameObject.GetComponent<Customer>();
				if ((bool)component && (bool)component.assignedWaitingLine && (bool)component.assignedWaitingLine.EmployeeStationController)
				{
					component.assignedWaitingLine.EmployeeStationController.OnOrderCancel();
				}
				else
				{
					OnOrderCancel();
				}
			}
		}, delegate(List<CargoInstance> selectedItems)
		{
			OnPlaceOrder(selectedItems);
		}, cargoInstances);
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance newEmployeeInstance)
	{
		base.AssignEmployee(tpc, newEmployeeInstance);
		tpc.SitOnChair(employeeSpot);
	}

	public override void UnassignEmployee()
	{
		if ((bool)employee && employee.isPlayer)
		{
			employee.employeeTpc.Reset();
		}
		base.UnassignEmployee();
	}

	public override void OnPlaceOrder(List<CargoInstance> orderedCargoInstances = null, List<VehicleInstance> orderedVehicleInstances = null)
	{
		if (orderedCargoInstances == null || orderedCargoInstances.Count == 0)
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
		if (base.BuildingContext.IsPlayerOwnedBusiness && !employee)
		{
			order.Pay(base.BuildingContext.Registration, base.transform.position, isPlayer: true);
			SelfServiceEmployee.UpdatePlayerPurchase();
			return;
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

	public override void OnOrderCancel()
	{
		if ((bool)playerCustomer)
		{
			if (employee.customer == playerCustomer)
			{
				StartCoroutine(employee.CancelCurrentOrder());
			}
			waitingLine.customersManagement.RemoveCustomer(playerCustomer);
			playerCustomer.UnsubscribeToGlobalEvents();
			Object.Destroy(playerCustomer);
		}
		else
		{
			StopAllCoroutines();
		}
	}
}
