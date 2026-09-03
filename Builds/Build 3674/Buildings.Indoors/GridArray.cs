using System;
using System.Collections.Generic;

namespace Buildings.Indoors;

[Serializable]
public class GridArray
{
	public List<GridCell> gridCells = new List<GridCell>();

	public GridCell this[int index]
	{
		get
		{
			if (index >= gridCells.Count)
			{
				return null;
			}
			return gridCells[index];
		}
		set
		{
			gridCells[index] = value;
		}
	}

	public GridArray(int size = 0)
	{
		for (int i = 0; i < size; i++)
		{
			gridCells.Add(null);
		}
	}

	public GridArray(GridArray gridArray)
	{
		for (int i = 0; i < gridArray.gridCells.Count; i++)
		{
			gridCells.Add(new GridCell(gridArray.gridCells[i]));
		}
	}
}
