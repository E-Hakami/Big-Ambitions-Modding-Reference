using Extensions;
using UnityEngine;

public class BoundsPositionGiver : MonoBehaviour
{
	public Vector3 size;

	[HideInInspector]
	public Bounds bounds;

	public Color gizmoColor = Color.red;

	private void OnValidate()
	{
		bounds.size = size;
	}

	public Vector3 GetRandomPointInBounds()
	{
		Vector3 vector = bounds.RandomPointInBounds();
		vector.y = 0f;
		vector = base.transform.rotation * vector;
		return vector + base.transform.position;
	}
}
