using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace EmployeeStations;

public class WaitingLineCreator
{
	private const float DistanceBetweenSpots = 0.8f;

	private readonly IWaitingLineHolder _waitingLineHolder;

	private readonly WaitingLineData _data;

	public WaitingLineCreator(IWaitingLineHolder waitingLineHolder)
	{
		_waitingLineHolder = waitingLineHolder;
		_data = _waitingLineHolder.GetWaitingLine().data;
		WaitingLineData data = _data;
		data.onSelectedAnchorPositionUpdated = (Action)Delegate.Combine(data.onSelectedAnchorPositionUpdated, new Action(SetUpWaitingLine));
		WaitingLineData data2 = _data;
		data2.onAnchorRemoved = (Action)Delegate.Combine(data2.onAnchorRemoved, new Action(SetUpWaitingLine));
	}

	public void Reset()
	{
		Vector3 lastDefaultQueuePosition = _waitingLineHolder.GetLastDefaultQueuePosition();
		Reset(lastDefaultQueuePosition);
	}

	public void ResetByTemplatePosition()
	{
		Transform anchorTemplate = _waitingLineHolder.GetWaitingLine().transforms.anchorTemplate;
		Vector3 lastPosition = ((anchorTemplate.localPosition != new Vector3(-1f, -1f, -1f)) ? anchorTemplate.position : _waitingLineHolder.GetLastDefaultQueuePosition());
		Reset(lastPosition);
	}

	private void Reset(Vector3 lastPosition)
	{
		Vector3 firstQueuePosition = _waitingLineHolder.GetFirstQueuePosition();
		_data.anchorsPositions = new List<Vector3> { firstQueuePosition, lastPosition };
		SetUpWaitingLine();
	}

	public void SetUpWaitingLine()
	{
		_data.spots.Clear();
		Vector3 item = ((_data.anchorsPositions.Count > 0) ? _data.GetAnchorPosition(0) : Vector3.zero);
		_data.spots.Add(item);
		NavMeshPath navMeshPath = new NavMeshPath();
		List<Vector3> list = new List<Vector3>();
		for (int i = 1; i < _data.anchorsPositions.Count; i++)
		{
			Vector3 anchorPosition = _data.GetAnchorPosition(i - 1);
			Vector3 anchorPosition2 = _data.GetAnchorPosition(i);
			NavMesh.CalculatePath(anchorPosition, anchorPosition2, -1, navMeshPath);
			list.AddRange(navMeshPath.corners);
		}
		for (int j = 0; j < list.Count; j++)
		{
			Vector3 a = list[j];
			Vector3 vector = list.ElementAtOrDefault(j + 1);
			if (vector == Vector3.zero)
			{
				break;
			}
			double num = Math.Floor(Vector3.Distance(a, vector) / 0.8f);
			for (int k = 1; (double)k <= num; k++)
			{
				Vector3 item2 = Vector3.Lerp(a, vector, (float)((double)k / num));
				_data.spots.Add(item2);
			}
		}
		_data.onDataChange();
	}
}
