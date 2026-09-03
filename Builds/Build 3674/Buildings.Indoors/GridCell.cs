using System;
using UnityEngine;

namespace Buildings.Indoors;

[Serializable]
public class GridCell
{
	public bool isOccupied;

	public bool isWall;

	public Vector3 position;

	public Vector3 wallOrientation;

	public GridCell(bool isOccupied, bool isWall, Vector3 position, Vector3 wallOrientation)
	{
		this.isOccupied = isOccupied;
		this.isWall = isWall;
		this.position = position;
		this.wallOrientation = wallOrientation;
	}

	public GridCell(GridCell gridCell)
	{
		isOccupied = gridCell.isOccupied;
		isWall = gridCell.isWall;
		position = gridCell.position;
		wallOrientation = gridCell.wallOrientation;
	}
}
