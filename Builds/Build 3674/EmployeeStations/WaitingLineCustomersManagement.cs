using System.Collections.Generic;
using System.Linq;
using Helpers;
using UnityEngine;

namespace EmployeeStations;

public class WaitingLineCustomersManagement
{
	private const float SpotPositionMaxOffset = 0.2f;

	private readonly WaitingLineData _data;

	private readonly WaitingLineTransforms _transforms;

	public WaitingLineCustomersManagement(WaitingLineData data, WaitingLineTransforms transforms)
	{
		_data = data;
		_transforms = transforms;
	}

	public void Join(Customer customer)
	{
		_data.customers.Add(customer);
		if (customer.tpc.navmeshAgent == null || !customer.tpc.navmeshAgent.isActiveAndEnabled)
		{
			customer.state = CustomerState.InWaitingLine;
			customer.currentWaitingLineSpot = 0;
			return;
		}
		int customersInWaitingLine = _data.GetCustomersInWaitingLine();
		if (!MoveCustomerToSpot(customer, customersInWaitingLine))
		{
			MoveCustomerToAnotherWaitingLine(customer);
		}
	}

	public void JoinInstantly(Customer customer)
	{
		_data.customers.Add(customer);
		int customersInWaitingLine = _data.GetCustomersInWaitingLine();
		if (!MoveCustomerToSpotInstantly(customer, customersInWaitingLine))
		{
			MoveCustomerToAnotherWaitingLineInstantly(customer);
		}
	}

	public void RemoveCustomer(Customer customerToRemove)
	{
		_data.customers.Remove(customerToRemove);
		if (customerToRemove == null)
		{
			AdvanceWaitingLine();
		}
		else
		{
			AdvanceWaitingLine(customerToRemove.currentWaitingLineSpot);
		}
		UpdateCustomersGoingToWaitingLine();
	}

	private void AdvanceWaitingLine(int startingSpot = 0)
	{
		foreach (Customer item in _data.customers.Where((Customer x) => x.currentWaitingLineSpot > startingSpot && x.state != CustomerState.GoingToWaitingLine).ToList())
		{
			item.state = CustomerState.MovingInWaitingLine;
			if (!MoveCustomerToSpot(item, item.currentWaitingLineSpot - 1))
			{
				MoveCustomerToAnotherWaitingLine(item);
			}
		}
	}

	private void UpdateCustomersGoingToWaitingLine()
	{
		List<Customer> list = _data.customers.Where((Customer x) => x.state == CustomerState.GoingToWaitingLine).ToList();
		int customersInWaitingLine = _data.GetCustomersInWaitingLine();
		foreach (Customer item in list)
		{
			if (customersInWaitingLine >= _data.spots.Count || !MoveCustomerToSpot(item, customersInWaitingLine))
			{
				MoveCustomerToAnotherWaitingLine(item);
			}
		}
	}

	private bool MoveCustomerToSpot(Customer customer, int waitingLineSpot)
	{
		if (customer.currentWaitingLineSpot == waitingLineSpot)
		{
			return true;
		}
		Vector3 result = _data.GetWaitingLineSpotPosition(waitingLineSpot);
		if (!PositionHelper.RandomPoint(result, 0.2f, -1, out result))
		{
			return false;
		}
		Vector3 waitingLineLookTarget = GetWaitingLineLookTarget(waitingLineSpot);
		customer.currentWaitingLineSpot = waitingLineSpot;
		customer.tpc.StopAllCoroutines();
		customer.tpc.StartCoroutine(customer.tpc.MoveToPosition(waitingLineLookTarget, result, 0.5f, rotateToLookTarget: true, null, customer.GetRandomReactionTime(), delegate
		{
			if (customer.state != CustomerState.BeingServed)
			{
				customer.state = CustomerState.InWaitingLine;
			}
			UpdateCustomersGoingToWaitingLine();
		}));
		return true;
	}

	private bool MoveCustomerToSpotInstantly(Customer customer, int waitingLineSpot)
	{
		Vector3 result = _data.GetWaitingLineSpotPosition(waitingLineSpot);
		if (!PositionHelper.RandomPoint(result, 0.2f, -1, out result))
		{
			return false;
		}
		Vector3 waitingLineLookTarget = GetWaitingLineLookTarget(waitingLineSpot);
		waitingLineLookTarget.y = customer.transform.position.y;
		customer.tpc.navmeshAgent.Warp(result);
		customer.transform.LookAt(waitingLineLookTarget);
		customer.currentWaitingLineSpot = waitingLineSpot;
		customer.state = CustomerState.InWaitingLine;
		UpdateCustomersGoingToWaitingLine();
		return true;
	}

	private Vector3 GetWaitingLineLookTarget(int waitingLineSpot)
	{
		if (waitingLineSpot != 0)
		{
			return _data.GetWaitingLineSpotPosition(waitingLineSpot - 1);
		}
		return _transforms.customerLookTarget.position;
	}

	public void MoveCustomerToAnotherWaitingLine(Customer customer)
	{
		_data.customers.Remove(customer);
		WaitingLine lessCrowdedWaitingLine = WaitingLinesHelper.GetLessCrowdedWaitingLine(_data.controllerNames);
		if ((bool)lessCrowdedWaitingLine && lessCrowdedWaitingLine.customersManagement != this)
		{
			MakeCustomerJoinQueue(customer, lessCrowdedWaitingLine);
			if (customer.isPlayer && (bool)lessCrowdedWaitingLine.EmployeeStationController)
			{
				lessCrowdedWaitingLine.EmployeeStationController.playerCustomer = customer;
			}
		}
		else
		{
			customer.Leave();
		}
	}

	public void MoveCustomerToAnotherWaitingLineInstantly(Customer customer)
	{
		_data.customers.Remove(customer);
		WaitingLine lessCrowdedWaitingLine = WaitingLinesHelper.GetLessCrowdedWaitingLine(_data.controllerNames);
		if ((bool)lessCrowdedWaitingLine && lessCrowdedWaitingLine.customersManagement != this)
		{
			MakeCustomerJoinQueueInstantly(customer, lessCrowdedWaitingLine);
		}
		else
		{
			customer.Leave();
		}
	}

	public void MoveCustomerToGivenEmployeeStation(Customer customer, WaitingLine waitingLine)
	{
		_data.customers.Remove(customer);
		MakeCustomerJoinQueue(customer, waitingLine);
	}

	private static void MakeCustomerJoinQueue(Customer customer, WaitingLine waitingLine)
	{
		if ((bool)customer.tpc.isSittingOn)
		{
			customer.tpc.Reset();
		}
		waitingLine.WaitingLineHolder.JoinWaitingLine(customer);
	}

	private static void MakeCustomerJoinQueueInstantly(Customer customer, WaitingLine waitingLine)
	{
		if ((bool)customer.tpc.isSittingOn)
		{
			customer.tpc.Reset();
		}
		waitingLine.WaitingLineHolder.JoinWaitingLineInstantly(customer);
	}

	public void ResetFirstCustomerState()
	{
		Customer firstCustomer = _data.GetFirstCustomer();
		if (firstCustomer.state == CustomerState.BeingServed)
		{
			firstCustomer.state = CustomerState.InWaitingLine;
		}
	}
}
