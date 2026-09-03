using Helpers;
using UnityEngine;

public static class PathHelper
{
	public static bool IsThereAWallBetweenTargetAndEntity(Vector3 target, Vector3 entityPosition)
	{
		target.y = entityPosition.y;
		return Physics.Raycast(target, entityPosition - target, Vector3.Distance(target, entityPosition), LayerHelper.wallsLayerMask);
	}
}
