using System;
using System.Collections.Generic;
using Extensions;
using Helpers;
using UnityEngine;

namespace Controllers;

public class FerrisWheel : MonoBehaviour, ICarnivalNpcItem
{
	private const float GetCurrentCabinIntervalSeconds = 0.5f;

	private const float RaycastMaxDistance = 2f;

	public FerrisWheelCabin[] cabins;

	public Transform raycastPointToDetectCurrentCabin;

	public Action onCurrentCabinChanged;

	public Action onPlayerEntered;

	public Action onPlayerLeft;

	public Action<CarnivalPedestrian> onNpcEntered;

	public Transform playerWaitingPosition;

	[SerializeField]
	private NpcItemPositionGiver waitingPositionGiver;

	[SerializeField]
	private NpcItemPositionGiver exitPositionGiver;

	[SerializeField]
	private Transform exitPosition;

	[SerializeField]
	private Animator animator;

	private readonly Queue<CarnivalPedestrian> _npcsOnQueue = new Queue<CarnivalPedestrian>();

	private readonly List<FerrisWheelCabin> _freeCabins = new List<FerrisWheelCabin>();

	private ThirdPersonCharacter _playerTpc;

	private FerrisWheelCabin _currentCabin;

	private float _getCurrentCabinTimer;

	public void OnActivate()
	{
		waitingPositionGiver.Init();
		animator.enabled = true;
	}

	public void OnDeactivate()
	{
		FerrisWheelCabin[] array = cabins;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ResetCabin();
		}
		waitingPositionGiver.FreePositions();
		animator.enabled = false;
		_npcsOnQueue.Clear();
	}

	private void Update()
	{
		_getCurrentCabinTimer += Time.deltaTime;
		if (!(_getCurrentCabinTimer < 0.5f))
		{
			GetCurrentCabin();
			_getCurrentCabinTimer = 0f;
		}
	}

	private void GetCurrentCabin()
	{
		if (Physics.Raycast(new Ray(raycastPointToDetectCurrentCabin.position, Vector3.down), out var hitInfo, 2f, LayerHelper.triggerLayerIndex))
		{
			if (!(_currentCabin == null))
			{
				return;
			}
			FerrisWheelCabin componentInParent = hitInfo.collider.GetComponentInParent<FerrisWheelCabin>();
			if (!(componentInParent == null))
			{
				_currentCabin = componentInParent;
				ReleaseCharactersIfOccupied(componentInParent);
				PlacePlayerIfIsInQueue();
				if (_npcsOnQueue.Count > 0)
				{
					PlaceFirstNpcsInQueue(componentInParent);
				}
				onCurrentCabinChanged?.Invoke();
			}
		}
		else
		{
			_currentCabin = null;
		}
	}

	private void PlaceFirstNpcsInQueue(FerrisWheelCabin cabin)
	{
		for (int num = Mathf.Min(cabin.GetNumberOfFreeSeats(), _npcsOnQueue.Count) - 1; num >= 0; num--)
		{
			CarnivalPedestrian carnivalPedestrian = _npcsOnQueue.Dequeue();
			PlaceCharacter(cabin, carnivalPedestrian.tpc, carnivalPedestrian);
			waitingPositionGiver.FreePositionAtIndex(carnivalPedestrian.GetCurrentWaitingIndex());
			onNpcEntered?.Invoke(carnivalPedestrian);
		}
	}

	private void PlacePlayerIfIsInQueue()
	{
		if (!(_playerTpc == null))
		{
			PlaceCharacter(_currentCabin, _playerTpc);
			_playerTpc = null;
			onPlayerEntered?.Invoke();
		}
	}

	private void ReleaseCharactersIfOccupied(FerrisWheelCabin cabin)
	{
		if (!cabin.occupied)
		{
			return;
		}
		CarnivalPedestrian[] carnivalPedestrians = cabin.carnivalPedestrians;
		foreach (CarnivalPedestrian carnivalPedestrian in carnivalPedestrians)
		{
			if (carnivalPedestrian != null)
			{
				carnivalPedestrian.OnCarnivalItemEnd(GetExitPosition());
			}
		}
		cabin.ResetCabin();
		exitPositionGiver.FreePositions();
	}

	public bool PlayerTryRide(ThirdPersonCharacter tpc)
	{
		if (_currentCabin == null || _currentCabin.GetNumberOfFreeSeats() <= 0)
		{
			return false;
		}
		PlaceCharacter(_currentCabin, tpc);
		return true;
	}

	public void EnqueuePlayer(ThirdPersonCharacter tpc)
	{
		_playerTpc = tpc;
	}

	public void UnEnqueuePlayer()
	{
		_playerTpc = null;
	}

	private void EnqueueNpc(CarnivalPedestrian carnivalPedestrian)
	{
		_npcsOnQueue.Enqueue(carnivalPedestrian);
	}

	private void PlaceCharacter(FerrisWheelCabin cabin, ThirdPersonCharacter tpc, CarnivalPedestrian carnivalPedestrian = null)
	{
		int randomFreeSeatIndex = cabin.GetRandomFreeSeatIndex();
		Transform transform = cabin.seats[randomFreeSeatIndex];
		cabin.OccupySeat(randomFreeSeatIndex);
		tpc.SitOnChair(transform);
		tpc.transform.SetParent(transform);
		cabin.occupied = true;
		cabin.carnivalPedestrians[randomFreeSeatIndex] = carnivalPedestrian;
	}

	public bool CanPlaceNpc()
	{
		FerrisWheelCabin[] array = cabins;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].GetNumberOfFreeSeats() > 0)
			{
				return true;
			}
		}
		return false;
	}

	public void PlaceNpcInstantly(CarnivalPedestrian carnivalPedestrian)
	{
		_freeCabins.Clear();
		FerrisWheelCabin[] array = cabins;
		foreach (FerrisWheelCabin ferrisWheelCabin in array)
		{
			if (ferrisWheelCabin.GetNumberOfFreeSeats() > 0)
			{
				_freeCabins.Add(ferrisWheelCabin);
			}
		}
		FerrisWheelCabin random = _freeCabins.GetRandom();
		PlaceCharacter(random, carnivalPedestrian.tpc, carnivalPedestrian);
	}

	public bool TryEnqueueNpc(CarnivalPedestrian carnivalPedestrian)
	{
		EnqueueNpc(carnivalPedestrian);
		return true;
	}

	public int GetWaitingPositionIndex()
	{
		int randomWaitingPositionIndex = waitingPositionGiver.GetRandomWaitingPositionIndex();
		if (randomWaitingPositionIndex != -1)
		{
			waitingPositionGiver.OccupyPosition(randomWaitingPositionIndex);
		}
		return randomWaitingPositionIndex;
	}

	public Vector3 GetWaitingPositionFromIndex(int index)
	{
		return waitingPositionGiver.GetPositionFromIndex(index);
	}

	public Quaternion GetWaitingRotationFromIndex(int index)
	{
		return waitingPositionGiver.GetRotationFromIndex(index);
	}

	public Vector3 GetExitPosition(Vector3 fromPosition = default(Vector3))
	{
		int nextPositionIndex = exitPositionGiver.GetNextPositionIndex();
		if (nextPositionIndex == -1)
		{
			return default(Vector3);
		}
		exitPositionGiver.OccupyPosition(nextPositionIndex);
		return exitPositionGiver.GetPositionFromIndex(nextPositionIndex);
	}
}
