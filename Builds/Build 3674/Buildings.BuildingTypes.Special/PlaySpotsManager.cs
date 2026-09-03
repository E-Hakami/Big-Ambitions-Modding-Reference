using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.AI;

namespace Buildings.BuildingTypes.Special;

public class PlaySpotsManager : MonoBehaviour
{
	[SerializeField]
	private Transform[] playSpots;

	private readonly Dictionary<Transform, PlaySpotStatus> _playSpotsOccupied = new Dictionary<Transform, PlaySpotStatus>();

	private Transform playerSpot;

	public void Init()
	{
		playSpots.Shuffle();
		Transform[] array = playSpots;
		foreach (Transform key in array)
		{
			_playSpotsOccupied.Add(key, PlaySpotStatus.Free);
		}
	}

	public Transform GetPlayerSpot()
	{
		return playerSpot;
	}

	public bool IsAnySpotAvailable()
	{
		return _playSpotsOccupied.ContainsValue(PlaySpotStatus.Free);
	}

	public bool IsAnySpotOccupied()
	{
		return _playSpotsOccupied.ContainsValue(PlaySpotStatus.Occupied);
	}

	public bool IsPlayerPlaying()
	{
		return playerSpot != null;
	}

	public bool IsAnySpotAvailableForPlayer()
	{
		if (!_playSpotsOccupied.ContainsValue(PlaySpotStatus.Free))
		{
			return _playSpotsOccupied.ContainsValue(PlaySpotStatus.Reserved);
		}
		return true;
	}

	public int GetNumberOfFreeSpots()
	{
		int num = 0;
		foreach (KeyValuePair<Transform, PlaySpotStatus> item in _playSpotsOccupied)
		{
			if (item.Value == PlaySpotStatus.Free)
			{
				num++;
			}
		}
		return num;
	}

	public Transform GetFreeSpot()
	{
		foreach (KeyValuePair<Transform, PlaySpotStatus> item in _playSpotsOccupied)
		{
			if (item.Value == PlaySpotStatus.Free)
			{
				return item.Key;
			}
		}
		return null;
	}

	public bool IsSpotOccupied(Transform playSpot)
	{
		return _playSpotsOccupied[playSpot] == PlaySpotStatus.Occupied;
	}

	public void SetPlaySpotStatus(Transform playSpot, PlaySpotStatus playSpotStatus)
	{
		_playSpotsOccupied[playSpot] = playSpotStatus;
	}

	public void ReleasePlayerSpot()
	{
		if (!(playerSpot == null))
		{
			_playSpotsOccupied[playerSpot] = PlaySpotStatus.Free;
			playerSpot = null;
		}
	}

	public Vector3 GetClosestPlaySpot(Vector3 entityPosition)
	{
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		NavMeshPath navMeshPath = new NavMeshPath();
		Transform transform = null;
		Transform[] array = playSpots;
		foreach (Transform transform2 in array)
		{
			if (IsSpotOccupied(transform2) || PathHelper.IsThereAWallBetweenTargetAndEntity(transform2.position, base.transform.position + base.transform.forward * 0.1f))
			{
				continue;
			}
			NavMesh.CalculatePath(entityPosition, transform2.position, -1, navMeshPath);
			if (navMeshPath.status != NavMeshPathStatus.PathInvalid && (navMeshPath.status != NavMeshPathStatus.PathPartial || (navMeshPath.corners.Length != 0 && !(Vector3.SqrMagnitude(navMeshPath.corners[^1] - transform2.position) > 0.010000001f))))
			{
				float num2 = 0f;
				for (int j = 1; j < navMeshPath.corners.Length; j++)
				{
					num2 += Vector3.Distance(navMeshPath.corners[j - 1], navMeshPath.corners[j]);
				}
				if (num2 < num)
				{
					transform = transform2;
					result = transform2.position;
					num = num2;
				}
			}
		}
		if (transform != null)
		{
			playerSpot = transform;
			SetPlaySpotStatus(playerSpot, PlaySpotStatus.Occupied);
		}
		return result;
	}

	private void OnDrawGizmosSelected()
	{
		foreach (KeyValuePair<Transform, PlaySpotStatus> item in _playSpotsOccupied)
		{
			Gizmos.color = item.Value switch
			{
				PlaySpotStatus.Free => Color.green, 
				PlaySpotStatus.Reserved => Color.blue, 
				PlaySpotStatus.Occupied => Color.red, 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			Gizmos.DrawSphere(item.Key.position, 0.2f);
		}
	}
}
