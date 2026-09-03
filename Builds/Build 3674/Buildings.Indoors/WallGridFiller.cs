using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buildings.Indoors;

public class WallGridFiller
{
	private readonly List<GridMatrix> _gridMatrices;

	private readonly float _heightCellSize;

	public WallGridFiller(List<GridMatrix> gridMatrices, float heightCellSize)
	{
		_gridMatrices = gridMatrices;
		_heightCellSize = heightCellSize;
	}

	public bool TryPlaceWallItem(Vector3 itemSize, out Vector3 placementPosition, out Vector3 forwardDirection)
	{
		var (num, num2, height) = GetSizeInCells(itemSize);
		foreach (GridMatrix gridMatrix in _gridMatrices)
		{
			Vector3 normalized = (gridMatrix.rotation * Vector3.forward).normalized;
			for (int num3 = gridMatrix.height - 1; num3 >= 1; num3--)
			{
				for (int i = 0; i < gridMatrix.GetHeightLength(1); i++)
				{
					for (int j = 0; j < gridMatrix.GetHeightLength(0); j++)
					{
						if (gridMatrix[j, num3, i].isWall && !IsHeightCellOccupied(gridMatrix, j, num3, i))
						{
							GridCell gridCell = gridMatrix[j, num3, i];
							Vector3 vector = gridMatrix.rotation * gridCell.wallOrientation;
							int xRange = num;
							int zRange = num2;
							int startX;
							int startZ;
							if (vector == normalized || vector == -normalized)
							{
								startX = j - Mathf.FloorToInt((float)num * 0.5f);
								startZ = ((!(vector == Vector3.back)) ? (i - num2 + 1) : i);
							}
							else
							{
								startZ = i - Mathf.FloorToInt((float)num * 0.5f);
								startX = ((!(vector == Vector3.right)) ? (j - num2 + 1) : j);
								xRange = num2;
								zRange = num;
							}
							if (CanPlaceWallItem(gridMatrix, startX, num3, startZ, xRange, height, zRange))
							{
								MarkHeightCellsAsOccupied(gridMatrix, startX, num3, startZ, xRange, height, zRange);
								placementPosition = gridCell.position;
								placementPosition += Vector3.down * (itemSize.y * 0.5f - _heightCellSize * 0.5f);
								forwardDirection = gridCell.wallOrientation;
								gridCell.isOccupied = true;
								return true;
							}
						}
					}
				}
			}
		}
		placementPosition = Vector3.zero;
		forwardDirection = Vector3.zero;
		return false;
	}

	private (int widthInCells, int depthInCells, int heightInCells) GetSizeInCells(Vector3 itemSize)
	{
		int num = 1;
		if (itemSize.x > _heightCellSize)
		{
			float num2 = (itemSize.x - _heightCellSize) * 0.5f;
			num += Mathf.CeilToInt(num2 / _heightCellSize) * 2;
		}
		int num3 = 1;
		if (itemSize.z > _heightCellSize * 0.5f)
		{
			float num4 = itemSize.z - _heightCellSize * 0.5f;
			num3 += Mathf.CeilToInt(num4 / _heightCellSize);
		}
		int item = Mathf.CeilToInt((float)Math.Round(itemSize.y, 2) / _heightCellSize);
		return (widthInCells: num, depthInCells: num3, heightInCells: item);
	}

	private bool CanPlaceWallItem(GridMatrix gridMatrix, int startX, int startY, int startZ, int xRange, int height, int zRange)
	{
		for (int num = startY; num > startY - height; num--)
		{
			for (int i = startZ; i < startZ + zRange; i++)
			{
				for (int j = startX; j < startX + xRange; j++)
				{
					if (IsHeightCellOccupied(gridMatrix, j, num, i))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private void MarkHeightCellsAsOccupied(GridMatrix gridMatrix, int startX, int startY, int startZ, int xRange, int height, int zRange)
	{
		for (int num = startY; num > startY - height; num--)
		{
			for (int i = startZ; i < startZ + zRange; i++)
			{
				for (int j = startX; j < startX + xRange; j++)
				{
					gridMatrix[j, num, i].isOccupied = true;
				}
			}
		}
	}

	private bool IsHeightCellOccupied(GridMatrix gridMatrix, int x, int y, int z)
	{
		if (x >= 0 && z >= 0 && x < gridMatrix.GetHeightLength(0) && z < gridMatrix.GetHeightLength(1) && y >= 1 && y < gridMatrix.height)
		{
			return gridMatrix[x, y, z].isOccupied;
		}
		return true;
	}
}
