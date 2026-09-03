using System.Collections.Generic;
using System.Linq;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

namespace Buildings.Indoors;

[ExecuteInEditMode]
public class GridGenerator : MonoBehaviour
{
	private const int WallLayerMask = 1048576;

	private const int WallPlacementLayerMask = 268435456;

	public float wallDetectionTolerance;

	public float groundCellSize = 0.25f;

	public float heightCellSize = 1f;

	public LayerMask collisionLayer = 1048576;

	public LayerMask wallPlacementLayer = 268435456;

	[InfoBox("Must be ordered by turn of filling. The first one will be the first to be filled with furniture", EInfoBoxType.Normal)]
	public Transform[] buildingGridTransforms;

	public List<GridMatrix> gridMatrices;

	public int height;

	[SerializeField]
	private bool showGrid;

	[SerializeField]
	private int showOnlyThisLevel;

	[HideInInspector]
	public Vector3 forkLiftPosition;

	[HideInInspector]
	public Quaternion forkLiftRotation;

	private Collider[] _colliders;

	public List<GridMatrix> GetGridMatricesCopy()
	{
		List<GridMatrix> list = new List<GridMatrix>();
		foreach (GridMatrix gridMatrix in gridMatrices)
		{
			list.Add(new GridMatrix(gridMatrix));
		}
		return list;
	}

	public void GenerateGrid()
	{
		_colliders = new Collider[8];
		gridMatrices = new List<GridMatrix>();
		Transform[] array = buildingGridTransforms;
		foreach (Transform buildingGridTransform in array)
		{
			Vector3 groundSize = GetGroundSize(buildingGridTransform);
			GridMatrix gridMatrix = CreateGridMatrix(groundSize, buildingGridTransform);
			float cellSize = groundCellSize;
			int num = gridMatrix.GetLength(0);
			int num2 = gridMatrix.GetLength(1);
			bool isGround = true;
			for (int j = 0; j < height; j++)
			{
				if (j > 0)
				{
					cellSize = heightCellSize;
					num = gridMatrix.GetHeightLength(0);
					num2 = gridMatrix.GetHeightLength(1);
					isGround = false;
				}
				for (int k = 0; k < num; k++)
				{
					for (int l = 0; l < num2; l++)
					{
						AddGridCell(k, j, l, cellSize, groundSize, buildingGridTransform, isGround, gridMatrix);
					}
				}
			}
			gridMatrices.Add(gridMatrix);
		}
	}

	private static Vector3 GetGroundSize(Transform buildingGridTransform)
	{
		Vector3 size = buildingGridTransform.GetComponent<MeshRenderer>().bounds.size;
		size = buildingGridTransform.rotation * size;
		return new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
	}

	private GridMatrix CreateGridMatrix(Vector3 size, Transform buildingGridTransform)
	{
		return new GridMatrix(Mathf.FloorToInt(size.x / groundCellSize), Mathf.FloorToInt(size.z / groundCellSize), heightX: (size.x - (float)Mathf.FloorToInt(size.x) >= groundCellSize) ? Mathf.CeilToInt(size.x / heightCellSize) : Mathf.FloorToInt(size.x / heightCellSize), heightZ: (size.z - (float)Mathf.FloorToInt(size.z) >= groundCellSize) ? Mathf.CeilToInt(size.z / heightCellSize) : Mathf.FloorToInt(size.z / heightCellSize), height: height, rotation: buildingGridTransform.rotation);
	}

	private void AddGridCell(int x, int y, int z, float cellSize, Vector3 size, Transform buildingGridTransform, bool isGround, GridMatrix gridMatrix)
	{
		Vector3 cellPosition = CalculateCellPosition(x, y, z, cellSize, size, buildingGridTransform);
		Vector3 wallOrientation = Vector3.zero;
		int hits = 0;
		bool flag = !isGround && OverlapsWithWall(cellPosition, cellSize, out hits);
		if (flag)
		{
			flag = HandleIsWall(hits, ref cellPosition, ref wallOrientation);
		}
		else
		{
			hits = OverlapsWithCollisionLayer(cellPosition, cellSize);
		}
		bool isOccupied = !flag && hits > 0;
		gridMatrix[x, y, z] = new GridCell(isOccupied, flag, cellPosition, wallOrientation);
	}

	private Vector3 CalculateCellPosition(int x, int y, int z, float cellSize, Vector3 groundSize, Transform buildingGridTransform)
	{
		float y2 = ((y == 0) ? 0f : (cellSize * (float)(y - 1) + cellSize / 2f + groundCellSize / 2f));
		Vector3 vector = new Vector3((0f - groundSize.x) / 2f + cellSize * ((float)x + 0.5f), y2, groundSize.z / 2f - cellSize * ((float)z + 0.5f));
		Vector3 vector2 = buildingGridTransform.rotation * vector;
		return buildingGridTransform.position + vector2;
	}

	private bool OverlapsWithWall(Vector3 cellPosition, float cellSize, out int hits)
	{
		hits = base.gameObject.scene.GetPhysicsScene().OverlapSphere(cellPosition, cellSize * (0.5f + wallDetectionTolerance), _colliders, wallPlacementLayer, QueryTriggerInteraction.UseGlobal);
		return hits > 0;
	}

	private bool HandleIsWall(int hits, ref Vector3 cellPosition, ref Vector3 wallOrientation)
	{
		bool result;
		if (HitsWall(hits, cellPosition, out var hitInfo))
		{
			wallOrientation = hitInfo.normal;
			result = HitsWallAtRightAndLeft(wallOrientation, cellPosition, -wallOrientation);
			cellPosition = hitInfo.point;
		}
		else
		{
			result = false;
		}
		return result;
	}

	private bool HitsWall(int hits, Vector3 cellPosition, out RaycastHit hitInfo)
	{
		Collider collider = _colliders[0];
		if (hits > 1)
		{
			collider = GetClosestCollider(hits, cellPosition, collider);
		}
		Vector3 direction = collider.ClosestPointOnBounds(cellPosition) - cellPosition;
		return base.gameObject.scene.GetPhysicsScene().Raycast(cellPosition, direction, out hitInfo, direction.magnitude + 0.1f, wallPlacementLayer);
	}

	private Collider GetClosestCollider(int hits, Vector3 cellPosition, Collider hit)
	{
		float num = float.MaxValue;
		for (int i = 0; i < hits; i++)
		{
			float num2 = Vector3.SqrMagnitude(cellPosition - _colliders[i].transform.position);
			if (num2 < num)
			{
				num = num2;
				hit = _colliders[i];
			}
		}
		return hit;
	}

	private bool HitsWallAtRightAndLeft(Vector3 wallOrientation, Vector3 lastCellPosition, Vector3 direction)
	{
		Vector3 normalized = Vector3.Cross(wallOrientation, Vector3.up).normalized;
		bool num = base.gameObject.scene.GetPhysicsScene().Raycast(lastCellPosition + normalized * 0.5f, -wallOrientation, out var hitInfo, direction.magnitude + 0.1f, wallPlacementLayer);
		Vector3 vector = -Vector3.Cross(wallOrientation, Vector3.up).normalized;
		bool flag = base.gameObject.scene.GetPhysicsScene().Raycast(lastCellPosition + vector * 0.5f, -wallOrientation, out hitInfo, direction.magnitude + 0.1f, wallPlacementLayer);
		return num & flag;
	}

	private int OverlapsWithCollisionLayer(Vector3 cellPosition, float cellSize)
	{
		return base.gameObject.scene.GetPhysicsScene().OverlapSphere(cellPosition, cellSize * 0.5f, _colliders, collisionLayer, QueryTriggerInteraction.UseGlobal);
	}

	public void FillGroundGridTransforms()
	{
		buildingGridTransforms = (from x in base.gameObject.FindGameObjectsInChildrenWithTag("GroundGrid")
			select x.transform).ToArray();
	}
}
