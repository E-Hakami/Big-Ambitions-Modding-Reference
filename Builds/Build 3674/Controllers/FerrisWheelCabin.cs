using System;
using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace Controllers;

public class FerrisWheelCabin : MonoBehaviour
{
	[SerializeField]
	public Transform[] seats;

	[NonSerialized]
	public CarnivalPedestrian[] carnivalPedestrians;

	[NonSerialized]
	public bool occupied;

	private bool[] _occupiedSeats;

	private readonly List<int> _freeSeatsIndices = new List<int>();

	private void Awake()
	{
		_occupiedSeats = new bool[seats.Length];
		carnivalPedestrians = new CarnivalPedestrian[seats.Length];
	}

	public void ResetCabin()
	{
		occupied = false;
		for (int i = 0; i < carnivalPedestrians.Length; i++)
		{
			carnivalPedestrians[i] = null;
		}
		for (int j = 0; j < _occupiedSeats.Length; j++)
		{
			_occupiedSeats[j] = false;
		}
	}

	public int GetRandomFreeSeatIndex()
	{
		_freeSeatsIndices.Clear();
		for (int i = 0; i < _occupiedSeats.Length; i++)
		{
			if (!_occupiedSeats[i])
			{
				_freeSeatsIndices.Add(i);
			}
		}
		return _freeSeatsIndices.GetRandom();
	}

	public void OccupySeat(int seatIndex)
	{
		_occupiedSeats[seatIndex] = true;
	}

	public int GetNumberOfFreeSeats()
	{
		int num = 0;
		bool[] occupiedSeats = _occupiedSeats;
		for (int i = 0; i < occupiedSeats.Length; i++)
		{
			if (!occupiedSeats[i])
			{
				num++;
			}
		}
		return num;
	}
}
