using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadPath : MonoBehaviour
{
	public Color lineColor = Color.white;

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = lineColor;
		IEnumerable<Transform> enumerable = from x in GetComponentsInChildren<Transform>().ToList()
			where x != base.transform
			select x;
		Transform transform = null;
		foreach (Transform item in enumerable)
		{
			if (transform != null)
			{
				Gizmos.DrawLine(transform.position, item.position);
			}
			transform = item;
		}
	}
}
