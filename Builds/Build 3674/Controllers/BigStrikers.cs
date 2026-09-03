using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace Controllers;

public class BigStrikers : MonoBehaviour, ICarnivalNpcItem
{
	public BigStrikersUnit[] bigStrikersUnits;

	public NpcItemPositionGiver npcPositionGiver;

	[SerializeField]
	private Transform enterPosition;

	[Header("SFX")]
	public AudioClip topScoreBellSound;

	public AudioClip normalBellSound;

	public AudioClip hammerImpactSound;

	private readonly List<BigStrikersUnit> _freeUnits = new List<BigStrikersUnit>();

	private BigStrikersUnit _playerUnit;

	public BigStrikersUnit GetPlayerUnit()
	{
		return _playerUnit;
	}

	private void Awake()
	{
		BigStrikersUnit[] array = bigStrikersUnits;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Init(this);
		}
		SetPlayerUnit();
		npcPositionGiver.Init();
	}

	public void OnActivate()
	{
		npcPositionGiver.Init();
	}

	public void OnDeactivate()
	{
		npcPositionGiver.FreePositions();
		BigStrikersUnit[] array = bigStrikersUnits;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Cancel();
		}
	}

	private void SetPlayerUnit()
	{
		for (int i = 0; i < bigStrikersUnits.Length; i++)
		{
			if (bigStrikersUnits[i].isPlayerSpot)
			{
				_playerUnit = bigStrikersUnits[i];
				break;
			}
		}
	}

	private BigStrikersUnit GetRandomFreeUnit()
	{
		_freeUnits.Clear();
		for (int i = 0; i < bigStrikersUnits.Length; i++)
		{
			if (!bigStrikersUnits[i].isOccupied && !bigStrikersUnits[i].isPlayerSpot)
			{
				_freeUnits.Add(bigStrikersUnits[i]);
			}
		}
		if (_freeUnits.Count != 0)
		{
			return _freeUnits.GetRandom();
		}
		return null;
	}

	private BigStrikersUnit GetClosestFreeUnitForNpc(Vector3 fromPosition)
	{
		BigStrikersUnit result = null;
		float num = float.MaxValue;
		for (int i = 0; i < bigStrikersUnits.Length; i++)
		{
			BigStrikersUnit bigStrikersUnit = bigStrikersUnits[i];
			if (!bigStrikersUnit.isOccupied && !bigStrikersUnit.isPlayerSpot)
			{
				float sqrMagnitude = (fromPosition - bigStrikersUnit.characterPosition.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = bigStrikersUnit;
				}
			}
		}
		return result;
	}

	public bool CanPlaceNpc()
	{
		for (int i = 0; i < bigStrikersUnits.Length; i++)
		{
			if (!bigStrikersUnits[i].isOccupied && !bigStrikersUnits[i].isPlayerSpot)
			{
				return true;
			}
		}
		return false;
	}

	public void PlaceNpcInstantly(CarnivalPedestrian carnivalPedestrian)
	{
		GetRandomFreeUnit().Use(carnivalPedestrian.tpc, carnivalPedestrian);
		int closestPositionIndex = npcPositionGiver.GetClosestPositionIndex(carnivalPedestrian.transform.position);
		npcPositionGiver.OccupyPosition(closestPositionIndex);
		carnivalPedestrian.SetCurrentWaitingIndex(closestPositionIndex);
	}

	public bool TryEnqueueNpc(CarnivalPedestrian carnivalPedestrian)
	{
		if (!CanPlaceNpc())
		{
			return false;
		}
		GetClosestFreeUnitForNpc(carnivalPedestrian.transform.position).Use(carnivalPedestrian.tpc, carnivalPedestrian);
		return true;
	}

	public int GetWaitingPositionIndex()
	{
		int randomWaitingPositionIndex = npcPositionGiver.GetRandomWaitingPositionIndex();
		if (randomWaitingPositionIndex != -1)
		{
			npcPositionGiver.OccupyPosition(randomWaitingPositionIndex);
		}
		return randomWaitingPositionIndex;
	}

	public Vector3 GetWaitingPositionFromIndex(int index)
	{
		return npcPositionGiver.GetPositionFromIndex(index);
	}

	public Quaternion GetWaitingRotationFromIndex(int index)
	{
		return npcPositionGiver.GetRotationFromIndex(index);
	}

	public Vector3 GetExitPosition(Vector3 fromPosition = default(Vector3))
	{
		return base.transform.position;
	}
}
