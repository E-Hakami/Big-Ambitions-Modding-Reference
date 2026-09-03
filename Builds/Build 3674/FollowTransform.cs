using UnityEngine;

public class FollowTransform : MonoBehaviour
{
	public Transform target;

	public Vector3 offset;

	private void LateUpdate()
	{
		if (!(target == null))
		{
			base.transform.position = target.position + offset;
			base.transform.rotation = target.rotation;
		}
	}
}
