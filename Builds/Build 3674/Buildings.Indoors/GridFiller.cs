using System;
using System.Collections.Generic;
using BigAmbitions.PlacementSystem;
using UnityEngine;

namespace Buildings.Indoors;

public class GridFiller
{
	private readonly List<GridMatrix> _gridMatrices;

	private float _groundCellSize;

	private float _heightCellSize;

	private IReadOnlyList<GroundGrid> _groundGrids;

	private bool _groundGridsInitialized;

	public GridFiller(List<GridMatrix> gridMatrices, float groundCellSize, float heightCellSize, IReadOnlyList<GroundGrid> groundGrids)
	{
		_gridMatrices = gridMatrices;
		_groundCellSize = groundCellSize;
		_heightCellSize = heightCellSize;
		_groundGrids = groundGrids;
		IReadOnlyList<GroundGrid> groundGrids2 = _groundGrids;
		_groundGridsInitialized = groundGrids2 != null && groundGrids2.Count > 0;
	}

	public void SetData(IEnumerable<GridMatrix> gridMatrices, float groundCellSize, float heightCellSize, IReadOnlyList<GroundGrid> groundGrids)
	{
		_gridMatrices.Clear();
		_gridMatrices.AddRange(gridMatrices);
		_groundCellSize = groundCellSize;
		_heightCellSize = heightCellSize;
		_groundGrids = groundGrids;
		IReadOnlyList<GroundGrid> groundGrids2 = _groundGrids;
		_groundGridsInitialized = groundGrids2 != null && groundGrids2.Count > 0;
	}

	public bool TryPlaceItem(Vector3 itemSize, out Vector3 placementPosition, bool checkCarPlacement = false, bool checkRoofItemPlacement = false)
	{
		int width = Mathf.CeilToInt((float)Math.Round(itemSize.x, 2) / _groundCellSize);
		int depth = Mathf.CeilToInt((float)Math.Round(itemSize.z, 2) / _groundCellSize);
		int height = ((!(itemSize.y < _groundCellSize * 0.5f) && !(_heightCellSize <= 0f)) ? Mathf.CeilToInt(((float)Math.Round(itemSize.y, 2) - _groundCellSize * 0.5f) / _heightCellSize) : 0);
		for (int i = 0; i < _gridMatrices.Count; i++)
		{
			GridMatrix gridMatrix = _gridMatrices[i];
			if (_groundGridsInitialized && ((checkCarPlacement && !_groundGrids[i].canPlaceCar) || (checkRoofItemPlacement && !_groundGrids[i].canPlaceRoofItem)))
			{
				continue;
			}
			for (int j = 0; j < gridMatrix.GetLength(1); j++)
			{
				for (int k = 0; k < gridMatrix.GetLength(0); k++)
				{
					if (CanPlaceItem(gridMatrix, k, j, width, depth))
					{
						MarkCellsAsOccupied(gridMatrix, k, j, width, depth, height);
						Vector3 vector = new Vector3(itemSize.x * 0.5f - _groundCellSize * 0.5f, 0f, (0f - itemSize.z) * 0.5f + _groundCellSize * 0.5f);
						Vector3 vector2 = gridMatrix.rotation * vector;
						placementPosition = gridMatrix[k, 0, j].position + vector2;
						return true;
					}
				}
			}
		}
		placementPosition = Vector3.zero;
		return false;
	}

	private bool CanPlaceItem(GridMatrix gridMatrix, int startX, int startZ, int width, int depth)
	{
		for (int i = startZ; i < startZ + depth; i++)
		{
			for (int j = startX; j < startX + width; j++)
			{
				if (IsCellOccupied(gridMatrix, j, i))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void MarkCellsAsOccupied(GridMatrix gridMatrix, int startX, int startZ, int width, int depth, int height)
	{
		for (int i = startZ; i < startZ + depth; i++)
		{
			for (int j = startX; j < startX + width; j++)
			{
				gridMatrix[j, 0, i].isOccupied = true;
				if (height > 0 && _heightCellSize > 0f)
				{
					float num = _groundCellSize / _heightCellSize;
					int x = Mathf.FloorToInt((float)j * num);
					int z = Mathf.FloorToInt((float)i * num);
					for (int k = 1; k <= height && k <= gridMatrix.height - 1; k++)
					{
						gridMatrix[x, k, z].isOccupied = true;
					}
				}
			}
		}
	}

	private bool IsCellOccupied(GridMatrix gridMatrix, int x, int z)
	{
		if (x >= 0 && z >= 0 && x < gridMatrix.GetLength(0) && z < gridMatrix.GetLength(1))
		{
			return gridMatrix[x, 0, z].isOccupied;
		}
		return true;
	}
}
