using System.Collections.Generic;
using Extensions;
using UnityEngine;

public class NpcItemPositionGiver : MonoBehaviour
{
	protected List<Vector3> positions = new List<Vector3>();

	protected List<Quaternion> rotations = new List<Quaternion>();

	private readonly List<int> _freePositionsIndices = new List<int>();

	private bool[] _occupiedPositions;

	public void Init()
	{
		if (_occupiedPositions == null)
		{
			SetPositionsAndRotations();
			_occupiedPositions = new bool[positions.Count];
		}
	}

	public virtual void SetPositionsAndRotations()
	{
	}

	public int GetNextPositionIndex()
	{
		for (int i = 0; i < positions.Count; i++)
		{
			if (!_occupiedPositions[i])
			{
				return i;
			}
		}
		return -1;
	}

	public int GetRandomWaitingPositionIndex()
	{
		_freePositionsIndices.Clear();
		for (int i = 0; i < positions.Count; i++)
		{
			if (!_occupiedPositions[i])
			{
				_freePositionsIndices.Add(i);
			}
		}
		if (_freePositionsIndices.Count <= 0)
		{
			return -1;
		}
		return _freePositionsIndices.GetRandom();
	}

	public Vector3 GetPositionFromIndex(int index)
	{
		return positions[index];
	}

	public Quaternion GetRotationFromIndex(int index)
	{
		return rotations[index];
	}

	public void OccupyPosition(int index)
	{
		if (IsIndexValid(index))
		{
			_occupiedPositions[index] = true;
		}
	}

	private bool IsIndexValid(int index)
	{
		if (index >= 0)
		{
			return index < _occupiedPositions.Length;
		}
		return false;
	}

	public int GetClosestPositionIndex(Vector3 fromPosition)
	{
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < positions.Count; i++)
		{
			if (!_occupiedPositions[i])
			{
				float sqrMagnitude = (fromPosition - positions[i]).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = i;
				}
			}
		}
		return result;
	}

	public void FreePositions()
	{
		if (_occupiedPositions != null)
		{
			for (int i = 0; i < _occupiedPositions.Length; i++)
			{
				_occupiedPositions[i] = false;
			}
		}
	}

	public void FreePositionAtIndex(int index)
	{
		if (IsIndexValid(index))
		{
			_occupiedPositions[index] = false;
		}
	}

	private void OnDrawGizmosSelected()
	{
		DrawGizmos();
	}

	protected virtual void DrawGizmos()
	{
		if (positions == null)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		foreach (Vector3 position in positions)
		{
			Gizmos.DrawSphere(position, 0.2f);
		}
	}
}
