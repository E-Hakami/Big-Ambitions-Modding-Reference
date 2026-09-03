using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace EmployeeStations;

public class WaitingLineData
{
	public List<Vector3> anchorsPositions;

	public List<Vector3> spots;

	public readonly List<Customer> customers;

	public WaitingLineAnchor anchorSelected;

	public bool isModifyingSelectedAnchor;

	public bool isAcceptingNewAnchors = true;

	public Vector3 initialPositionOfAnchorModifying;

	public readonly string[] controllerNames;

	public readonly Action onDataChange;

	public Action onSelectedAnchorPositionUpdated;

	public Action onAnchorRemoved;

	private readonly List<SerializableVector3> _mergedAnchorsAndSpots = new List<SerializableVector3>();

	public WaitingLineData(List<SerializableVector3> customPositions, string[] controllerNames, Action onDataChange)
	{
		SplitAnchorsAndSpots(customPositions);
		this.controllerNames = controllerNames;
		this.onDataChange = (Action)Delegate.Combine(this.onDataChange, onDataChange);
		customers = new List<Customer>();
	}

	public void SetCustomAnchors(List<Vector3> customPositions)
	{
		anchorsPositions = customPositions;
		onDataChange?.Invoke();
	}

	private void SplitAnchorsAndSpots(List<SerializableVector3> customPositions)
	{
		anchorsPositions = new List<Vector3>();
		spots = new List<Vector3>();
		List<Vector3> list = anchorsPositions;
		foreach (SerializableVector3 customPosition in customPositions)
		{
			Vector3 vector = customPosition;
			if (vector == Vector3.zero)
			{
				list = spots;
			}
			else
			{
				list.Add(vector);
			}
		}
	}

	public List<SerializableVector3> GetMergedAnchorsAndSpotsList()
	{
		_mergedAnchorsAndSpots.Clear();
		foreach (Vector3 anchorsPosition in anchorsPositions)
		{
			_mergedAnchorsAndSpots.Add(anchorsPosition);
		}
		_mergedAnchorsAndSpots.Add(Vector3.zero);
		foreach (Vector3 spot in spots)
		{
			_mergedAnchorsAndSpots.Add(spot);
		}
		return _mergedAnchorsAndSpots;
	}

	public int GetCustomersInWaitingLine(bool includeGoingToWaitingLine = false)
	{
		if (!includeGoingToWaitingLine)
		{
			return customers.CountWhere((Customer x) => x.state != CustomerState.GoingToWaitingLine);
		}
		return customers.Count;
	}

	public int GetAmountOfSpotsAvailable()
	{
		return GetWaitingLineCapacity() - GetCustomersInWaitingLine();
	}

	public Vector3 GetWaitingLineSpotPosition(int index)
	{
		Vector3 vector;
		if (index < spots.Count)
		{
			vector = spots[index];
		}
		else
		{
			List<Vector3> list = spots;
			vector = list[list.Count - 1];
		}
		Vector3 result = vector;
		result.y = 0f;
		return result;
	}

	public Vector3 GetAnchorPosition(int index)
	{
		return anchorsPositions[index];
	}

	public Customer GetNextCustomer()
	{
		return customers.Find((Customer x) => x.currentWaitingLineSpot == 0 && (x.state == CustomerState.InWaitingLine || (x.state == CustomerState.GoingToWaitingLine && Vector3.SqrMagnitude(x.tpc.transform.position - spots[0]) < 0.25f)));
	}

	public Customer GetFirstCustomer()
	{
		return customers.Find((Customer x) => x.currentWaitingLineSpot == 0);
	}

	private int GetWaitingLineCapacity()
	{
		return spots.Count;
	}
}
