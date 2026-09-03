using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using EmployeeStations;
using Entities;
using Extensions;
using Helpers;
using UI;
using UI.Purchase;
using UnityEngine;

namespace Controllers;

public class IRSStationController : EmployeeStationController
{
	private static readonly string[] EmployeeStationNames = new string[1] { "ba:itemname_irscountera" };

	public TaxPaymentType CurrentTaxPaymentType { get; private set; }

	public override void Awake()
	{
		base.Awake();
		employeeType = typeof(IRSEmployee);
	}

	protected override void InitWaitingLine()
	{
		if (base.ItemInstance != null)
		{
			waitingLine.Init(this, base.ItemInstance?.customPositions ?? customPositions, EmployeeStationNames, delegate
			{
				if (base.ItemInstance != null)
				{
					base.ItemInstance.customPositions = waitingLine.data.GetMergedAnchorsAndSpotsList();
				}
			});
		}
		else
		{
			waitingLine.Init(this, customPositions, null, delegate
			{
				customPositions = waitingLine.data.GetMergedAnchorsAndSpotsList();
			});
		}
		waitingLine.creator.Reset();
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
	}

	private void OnExitBuilding(Address _)
	{
		waitingLine.data.customers.Clear();
	}

	public void PayTaxes()
	{
		if (TaxHelper.HasCurrentTaxesToPay())
		{
			PayCurrentTaxes();
		}
		else if (TaxHelper.HasBackTaxesToPay())
		{
			PayBackTaxes();
		}
	}

	public void PayCurrentTaxes()
	{
		OpenTaxesPayment(TaxPaymentType.CurrentTaxes);
	}

	public void PayBackTaxes()
	{
		OpenTaxesPayment(TaxPaymentType.BackTaxes);
	}

	private void OpenTaxesPayment(TaxPaymentType taxPaymentType)
	{
		if ((taxPaymentType == TaxPaymentType.CurrentTaxes && !TaxHelper.HasCurrentTaxesToPay()) || (taxPaymentType == TaxPaymentType.BackTaxes && !TaxHelper.HasBackTaxesToPay()))
		{
			return;
		}
		CurrentTaxPaymentType = taxPaymentType;
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.Open(PurchaseUI.Type.IRS, delegate(bool canceled)
		{
			if (canceled)
			{
				OnOrderCancel();
			}
		}, delegate
		{
			OnPlaceOrder();
		}, null, PurchaseUI.Mode.NoChanges, taxPaymentType);
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.ForceToPosition(GetEmployeePosition());
		tpc.transform.LookAt(base.transform);
	}

	public override void OnPlaceOrder(List<CargoInstance> orderedCargoInstances = null, List<VehicleInstance> orderedVehicleInstances = null)
	{
		playerCustomer = InstanceBehavior<GameManager>.Instance.playerController.gameObject.GetOrAddComponent<Customer>();
		playerCustomer.tpc = InstanceBehavior<GameManager>.Instance.playerController.Character;
		playerCustomer.isPlayer = true;
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
			UnityEngine.Object.Destroy(playerCustomer);
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			base.OnDestroy();
		}
	}
}
