using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.Playables;

namespace Controllers;

public class Attraction : MonoBehaviour, ICarnivalNpcItem
{
	public Animator animator;

	public Transform[] seats;

	public float waitTime;

	public Action onAttractionStart;

	public Action onPlayerLeft;

	public bool chooseClosestSeat;

	public bool exitInClosestPosition;

	public bool chooseRandomWaitingPosition;

	public PlayableDirector playableDirector;

	public Transform playerWaitingPosition;

	[SerializeField]
	private NpcItemPositionGiver waitingPositionGiver;

	[SerializeField]
	private NpcItemPositionGiver exitPositionGiver;

	private readonly List<int> _availableIndices = new List<int>();

	private readonly List<KeyValuePair<int, CarnivalPedestrian>> _npcReservedSeats = new List<KeyValuePair<int, CarnivalPedestrian>>();

	private readonly List<CarnivalPedestrian> _npcsRidingAttraction = new List<CarnivalPedestrian>();

	private readonly List<Vector3> _seatsIdlePositions = new List<Vector3>();

	private bool[] _seatsOccupied;

	private AttractionStateManager _stateManager;

	private KeyValuePair<int, ThirdPersonCharacter> _playerReservedSeat;

	private bool _isPlayerRiding;

	public IReadOnlyList<CarnivalPedestrian> GetNpcsRidingAttraction()
	{
		return _npcsRidingAttraction;
	}

	public bool IsPlayerRiding()
	{
		return _isPlayerRiding;
	}

	private void Awake()
	{
		_seatsOccupied = new bool[seats.Length];
		_stateManager = new AttractionStateManager(this);
		if (chooseClosestSeat)
		{
			Transform[] array = seats;
			foreach (Transform transform in array)
			{
				_seatsIdlePositions.Add(transform.position);
			}
		}
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
	}

	public void OnActivate()
	{
		animator.enabled = true;
		_stateManager.Enable();
		waitingPositionGiver.Init();
		exitPositionGiver.Init();
	}

	public void OnDeactivate()
	{
		waitingPositionGiver.FreePositions();
		ResetSeats();
		_stateManager.Reset();
		_npcsRidingAttraction.Clear();
		animator.enabled = false;
	}

	private void OnTimeMachineEnded()
	{
		if (_isPlayerRiding)
		{
			OnAttractionEnd();
			_stateManager.Reset(enabled: true);
		}
	}

	public void OnAttractionStart()
	{
		if (_playerReservedSeat.Value != null)
		{
			PlaceCharacter(_playerReservedSeat.Key, _playerReservedSeat.Value);
			_isPlayerRiding = true;
		}
		foreach (KeyValuePair<int, CarnivalPedestrian> npcReservedSeat in _npcReservedSeats)
		{
			PlaceCharacter(npcReservedSeat.Key, npcReservedSeat.Value.tpc);
			_npcsRidingAttraction.Add(npcReservedSeat.Value);
			waitingPositionGiver.FreePositionAtIndex(npcReservedSeat.Value.GetCurrentWaitingIndex());
		}
		ResetSeats();
		onAttractionStart?.Invoke();
	}

	public void OnAttractionEnd()
	{
		foreach (CarnivalPedestrian item in _npcsRidingAttraction)
		{
			item.OnCarnivalItemEnd(GetExitPosition(item.transform.position));
		}
		exitPositionGiver.FreePositions();
		_npcsRidingAttraction.Clear();
	}

	public void OnPlayerFinish()
	{
		_isPlayerRiding = false;
		onPlayerLeft?.Invoke();
	}

	private void Update()
	{
		_stateManager.Update();
	}

	public void ReserveARandomSeatForPlayer(ThirdPersonCharacter tpc)
	{
		int randomAvailableSeatIndex = GetRandomAvailableSeatIndex();
		if (randomAvailableSeatIndex != -1)
		{
			_seatsOccupied[randomAvailableSeatIndex] = true;
			_playerReservedSeat = new KeyValuePair<int, ThirdPersonCharacter>(randomAvailableSeatIndex, tpc);
		}
	}

	private void ReserveARandomSeatForNpc(CarnivalPedestrian carnivalPedestrian)
	{
		int randomAvailableSeatIndex = GetRandomAvailableSeatIndex();
		if (randomAvailableSeatIndex != -1)
		{
			ReserveASeatForNpc(randomAvailableSeatIndex, carnivalPedestrian);
		}
	}

	private void ReserveClosestSeatForNpc(CarnivalPedestrian carnivalPedestrian)
	{
		int closestSeatIndex = GetClosestSeatIndex(carnivalPedestrian.transform.position);
		if (closestSeatIndex != -1)
		{
			ReserveASeatForNpc(closestSeatIndex, carnivalPedestrian);
		}
	}

	private int GetClosestSeatIndex(Vector3 position)
	{
		int result = -1;
		if (seats == null || seats.Length == 0)
		{
			return result;
		}
		result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < _seatsIdlePositions.Count; i++)
		{
			if (!_seatsOccupied[i])
			{
				float sqrMagnitude = (position - _seatsIdlePositions[i]).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = i;
				}
			}
		}
		return result;
	}

	private void ReserveASeatForNpc(int seatIndex, CarnivalPedestrian carnivalPedestrian)
	{
		_seatsOccupied[seatIndex] = true;
		_npcReservedSeats.Add(new KeyValuePair<int, CarnivalPedestrian>(seatIndex, carnivalPedestrian));
	}

	private void PlaceCharacter(int seatIndex, ThirdPersonCharacter tpc)
	{
		Transform transform = seats[seatIndex];
		tpc.SitOnChair(transform);
		tpc.transform.SetParent(transform);
	}

	private int GetRandomAvailableSeatIndex()
	{
		_availableIndices.Clear();
		for (int i = 0; i < _seatsOccupied.Length; i++)
		{
			if (!_seatsOccupied[i])
			{
				_availableIndices.Add(i);
			}
		}
		if (_availableIndices.Count == 0)
		{
			return -1;
		}
		return _availableIndices.GetRandom();
	}

	private void ResetSeats()
	{
		_playerReservedSeat = default(KeyValuePair<int, ThirdPersonCharacter>);
		_npcReservedSeats.Clear();
		for (int i = 0; i < _seatsOccupied.Length; i++)
		{
			_seatsOccupied[i] = false;
		}
	}

	public int GetRunningTime()
	{
		return Mathf.RoundToInt((float)playableDirector.playableAsset.duration);
	}

	public void UnReservePlayerSeat()
	{
		_seatsOccupied[_playerReservedSeat.Key] = false;
		_playerReservedSeat = default(KeyValuePair<int, ThirdPersonCharacter>);
	}

	public bool CanPlaceNpc()
	{
		int num = 0;
		for (int i = 0; i < _seatsOccupied.Length; i++)
		{
			if (!_seatsOccupied[i])
			{
				num++;
				if (num > 1)
				{
					return true;
				}
			}
		}
		return false;
	}

	public void PlaceNpcInstantly(CarnivalPedestrian carnivalPedestrian)
	{
		int randomAvailableSeatIndex = GetRandomAvailableSeatIndex();
		if (randomAvailableSeatIndex != -1)
		{
			PlaceCharacter(randomAvailableSeatIndex, carnivalPedestrian.tpc);
			_seatsOccupied[randomAvailableSeatIndex] = true;
			_npcsRidingAttraction.Add(carnivalPedestrian);
		}
	}

	public bool TryEnqueueNpc(CarnivalPedestrian carnivalPedestrian)
	{
		if (!CanPlaceNpc())
		{
			return false;
		}
		if (chooseClosestSeat)
		{
			ReserveClosestSeatForNpc(carnivalPedestrian);
		}
		else
		{
			ReserveARandomSeatForNpc(carnivalPedestrian);
		}
		return true;
	}

	public int GetWaitingPositionIndex()
	{
		int num = (chooseRandomWaitingPosition ? waitingPositionGiver.GetRandomWaitingPositionIndex() : waitingPositionGiver.GetNextPositionIndex());
		if (num != -1)
		{
			waitingPositionGiver.OccupyPosition(num);
		}
		return num;
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
		int num = (exitInClosestPosition ? exitPositionGiver.GetClosestPositionIndex(fromPosition) : exitPositionGiver.GetNextPositionIndex());
		if (num == -1)
		{
			return default(Vector3);
		}
		exitPositionGiver.OccupyPosition(num);
		return exitPositionGiver.GetPositionFromIndex(num);
	}

	public void OnDrawGizmosSelected()
	{
		if (seats != null)
		{
			for (int i = 0; i < seats.Length; i++)
			{
				Gizmos.color = ((_seatsOccupied != null && _seatsOccupied.Length > i && _seatsOccupied[i]) ? Color.red : Color.green);
				Gizmos.DrawSphere(seats[i].position, 1f);
			}
		}
	}
}
