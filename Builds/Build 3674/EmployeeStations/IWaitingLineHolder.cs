using System.Collections.Generic;
using UnityEngine;

namespace EmployeeStations;

public interface IWaitingLineHolder
{
	WaitingLine GetWaitingLine();

	Transform GetFirstCustomerSpot();

	void JoinWaitingLine(Customer customer)
	{
		WaitingLine waitingLine = (customer.assignedWaitingLine = GetWaitingLine());
		customer.state = CustomerState.GoingToWaitingLine;
		customer.currentWaitingLineSpot = -1;
		waitingLine.customersManagement.Join(customer);
	}

	void JoinWaitingLineInstantly(Customer customer)
	{
		WaitingLine waitingLine = (customer.assignedWaitingLine = GetWaitingLine());
		customer.state = CustomerState.GoingToWaitingLine;
		customer.currentWaitingLineSpot = -1;
		waitingLine.customersManagement.JoinInstantly(customer);
	}

	Vector3 GetFirstQueuePosition()
	{
		Transform firstCustomerSpot = GetFirstCustomerSpot();
		if (!firstCustomerSpot)
		{
			return (this as MonoBehaviour).transform.position;
		}
		Vector3 position = firstCustomerSpot.position;
		if (this is ItemController itemController && (bool)itemController.parentItemController)
		{
			position.y = itemController.parentItemController.transform.position.y;
		}
		return position;
	}

	Vector3 GetLastDefaultQueuePosition()
	{
		List<ExitZone> exitZones = InstanceBehavior<BuildingManager>.Instance.exitZones;
		if (exitZones.Count <= 0)
		{
			return GetFirstQueuePosition();
		}
		return exitZones[0].playerSpawnPoint.position;
	}
}
