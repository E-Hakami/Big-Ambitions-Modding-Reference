using UnityEngine;

namespace Buildings;

public class DanceSpot
{
	public Vector3 position;

	public bool isOccupied;

	public readonly ItemController danceFloorController;

	public DanceSpot(Vector3 position, bool isOccupied, ItemController danceFloorController)
	{
		this.position = position;
		this.isOccupied = isOccupied;
		this.danceFloorController = danceFloorController;
	}

	public void Occupy()
	{
		isOccupied = true;
	}

	public void Release()
	{
		isOccupied = false;
	}
}
