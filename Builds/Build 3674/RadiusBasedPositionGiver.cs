using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RadiusBasedPositionGiver : NpcItemPositionGiver
{
	[SerializeField]
	private float radius = 5f;

	[SerializeField]
	private int numberOfPositions = 8;

	[SerializeField]
	private bool faceRotationTowardsCenter = true;

	public override void SetPositionsAndRotations()
	{
		positions = new List<Vector3>();
		Vector3 position = base.transform.position;
		for (int i = 0; i < numberOfPositions; i++)
		{
			float f = (float)i * MathF.PI * 2f / (float)numberOfPositions;
			if (NavMesh.SamplePosition(position + new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)) * radius, out var hit, 2f, -1))
			{
				positions.Add(hit.position);
				Vector3 normalized = (position - hit.position).normalized;
				rotations.Add(faceRotationTowardsCenter ? Quaternion.LookRotation(normalized) : Quaternion.LookRotation(-normalized));
			}
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}
}
