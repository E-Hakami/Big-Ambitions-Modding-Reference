using System.Collections.Generic;
using UnityEngine;

namespace Buildings.Outdoors;

public class RoomWallOcclusionManager : MonoBehaviour
{
	private const float UpdateInterval = 0.1f;

	private static readonly int OverrideCutoutID = Shader.PropertyToID("_OverrideCutout");

	private static readonly HashSet<InteriorElement> CurrentWallsCutout = new HashSet<InteriorElement>();

	private static bool IsEnabled;

	public LayerMask wallMask;

	public float castRadius = 0.35f;

	private float _nextUpdateTime;

	private readonly HashSet<InteriorElement> _newWallsCutout = new HashSet<InteriorElement>();

	private readonly RaycastHit[] _castResults = new RaycastHit[32];

	private readonly Queue<InteriorElement> _wallsToCheck = new Queue<InteriorElement>();

	private int _hits;

	private void LateUpdate()
	{
		if (!IsEnabled || GameManager.isCitySceneBeingUnloaded)
		{
			return;
		}
		GameManager instance = InstanceBehavior<GameManager>.Instance;
		if (instance == null || instance.playerController == null)
		{
			Disable();
		}
		else
		{
			if (Time.time < _nextUpdateTime)
			{
				return;
			}
			_nextUpdateTime = Time.time + 0.1f;
			Vector3 position = instance.playerController.transform.position;
			HashSet<InteriorElement> closestOccludingWalls = GetClosestOccludingWalls(base.transform.position, position);
			_newWallsCutout.Clear();
			foreach (InteriorElement item in closestOccludingWalls)
			{
				FloodCollectRoomWalls(item, _newWallsCutout);
			}
			ApplyCutout();
		}
	}

	public static void Enable()
	{
		IsEnabled = true;
	}

	public static void Disable()
	{
		IsEnabled = false;
		foreach (InteriorElement item in CurrentWallsCutout)
		{
			SetWallCutout(item, occluding: false);
		}
		CurrentWallsCutout.Clear();
	}

	private HashSet<InteriorElement> GetClosestOccludingWalls(Vector3 from, Vector3 to)
	{
		HashSet<InteriorElement> hashSet = new HashSet<InteriorElement>();
		Vector3 direction = to - from;
		float magnitude = direction.magnitude;
		if (magnitude < 0.001f)
		{
			return hashSet;
		}
		direction /= magnitude;
		_hits = Physics.SphereCastNonAlloc(from, castRadius, direction, _castResults, magnitude, wallMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < _hits; i++)
		{
			Collider collider = _castResults[i].collider;
			if ((bool)collider)
			{
				InteriorElement component = collider.GetComponent<InteriorElement>();
				if (component != null)
				{
					hashSet.Add(component);
				}
			}
		}
		return hashSet;
	}

	private void FloodCollectRoomWalls(InteriorElement initialWall, HashSet<InteriorElement> outSet)
	{
		_wallsToCheck.Clear();
		_wallsToCheck.Enqueue(initialWall);
		while (_wallsToCheck.Count > 0)
		{
			InteriorElement interiorElement = _wallsToCheck.Dequeue();
			if (interiorElement == null || !outSet.Add(interiorElement))
			{
				continue;
			}
			List<MaterialIndexData.NeighborInfo> wallNeighbors = interiorElement.materialData[0].wallNeighbors;
			for (int i = 0; i < wallNeighbors.Count; i++)
			{
				InteriorElement neighborElement = wallNeighbors[i].neighborElement;
				if (neighborElement != null && !outSet.Contains(neighborElement) && neighborElement.transform.forward == interiorElement.transform.forward)
				{
					_wallsToCheck.Enqueue(neighborElement);
				}
			}
		}
	}

	private void ApplyCutout()
	{
		foreach (InteriorElement item in CurrentWallsCutout)
		{
			if (!(item == null) && !_newWallsCutout.Contains(item))
			{
				SetWallCutout(item, occluding: false);
			}
		}
		foreach (InteriorElement item2 in _newWallsCutout)
		{
			if (!(item2 == null) && !CurrentWallsCutout.Contains(item2))
			{
				SetWallCutout(item2, occluding: true);
			}
		}
		CurrentWallsCutout.Clear();
		foreach (InteriorElement item3 in _newWallsCutout)
		{
			CurrentWallsCutout.Add(item3);
		}
	}

	private static void SetWallCutout(InteriorElement wall, bool occluding)
	{
		Material[] materials = wall.elementRenderer.materials;
		for (int i = 0; i < materials.Length; i++)
		{
			materials[i].SetInt(OverrideCutoutID, occluding ? 1 : 0);
		}
		if (wall.doorController != null)
		{
			wall.doorController.SetOverrideCutout(occluding);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (IsEnabled)
		{
			Gizmos.color = Color.red;
			Vector3 position = base.transform.position;
			Vector3 vector = InstanceBehavior<GameManager>.Instance.playerController.transform.position - position;
			Vector3 vector2 = position + vector;
			Gizmos.DrawWireSphere(position, castRadius);
			Gizmos.DrawWireSphere(vector2, castRadius);
			Gizmos.DrawLine(position + Vector3.up * castRadius, vector2 + Vector3.up * castRadius);
			Gizmos.DrawLine(position - Vector3.up * castRadius, vector2 - Vector3.up * castRadius);
			Gizmos.DrawLine(position + Vector3.right * castRadius, vector2 + Vector3.right * castRadius);
			Gizmos.DrawLine(position - Vector3.right * castRadius, vector2 - Vector3.right * castRadius);
			Gizmos.color = Color.green;
			int num = Mathf.Clamp(_hits, 0, _castResults.Length);
			for (int i = 0; i < num; i++)
			{
				Gizmos.DrawWireSphere(_castResults[i].point, 0.1f);
				Gizmos.DrawLine(_castResults[i].point, _castResults[i].point + _castResults[i].normal * (castRadius * 0.5f));
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsEnabled = false;
	}
}
