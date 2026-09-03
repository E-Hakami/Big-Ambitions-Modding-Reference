using System.Collections.Generic;
using UnityEngine;

public class ManuallyPlacedPositionGiver : NpcItemPositionGiver
{
	[SerializeField]
	private List<Transform> positionTransforms = new List<Transform>();

	public override void SetPositionsAndRotations()
	{
		foreach (Transform positionTransform in positionTransforms)
		{
			if (positionTransform != null)
			{
				positions.Add(positionTransform.position);
				rotations.Add(positionTransform.rotation);
			}
		}
	}
}
