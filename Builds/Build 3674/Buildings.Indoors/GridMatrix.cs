using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buildings.Indoors;

[Serializable]
public class GridMatrix
{
	public List<GridArray> gridArrays = new List<GridArray>();

	public Quaternion rotation;

	public int rowsGround;

	public int rowsHeight;

	public int height;

	public GridCell this[int x, int y, int z]
	{
		get
		{
			return gridArrays[GetRow(y, z)][x];
		}
		set
		{
			gridArrays[GetRow(y, z)][x] = value;
		}
	}

	public GridMatrix(int groundX, int groundZ, int height, int heightX, int heightZ, Quaternion rotation)
	{
		for (int i = 0; i < groundZ; i++)
		{
			gridArrays.Add(new GridArray(groundX));
		}
		for (int j = 1; j < height; j++)
		{
			for (int k = 0; k < heightZ; k++)
			{
				gridArrays.Add(new GridArray(heightX));
			}
		}
		this.height = height;
		rowsGround = groundZ;
		rowsHeight = heightZ;
		this.rotation = rotation;
	}

	public GridMatrix(GridMatrix gridMatrix)
	{
		for (int i = 0; i < gridMatrix.gridArrays.Count; i++)
		{
			gridArrays.Add(new GridArray(gridMatrix.gridArrays[i]));
		}
		rotation = gridMatrix.rotation;
		rowsGround = gridMatrix.rowsGround;
		rowsHeight = gridMatrix.rowsHeight;
		height = gridMatrix.height;
	}

	private int GetRow(int y, int z)
	{
		if (y == 0)
		{
			return z;
		}
		return z + (y - 1) * rowsHeight + rowsGround;
	}

	public int GetLength(int dimension)
	{
		if (dimension != 0)
		{
			return rowsGround;
		}
		return gridArrays[0].gridCells.Count;
	}

	public int GetHeightLength(int dimension)
	{
		if (dimension != 0)
		{
			return rowsHeight;
		}
		List<GridArray> list = gridArrays;
		return list[list.Count - 1].gridCells.Count;
	}
}
