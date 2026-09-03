using Helpers;
using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisInteractionNpcAnimator : MonoBehaviour
{
	private const float MaxDistanceToLookAtPlayer = 10f;

	private const float LookTransitionSpeed = 3f;

	public Animator animator;

	private float _lookWeight;

	private void OnAnimatorIK(int layerIndex)
	{
		float target = 0f;
		Vector3 position = PlayerHelper.GetPosition();
		if ((position - base.transform.position).sqrMagnitude < 100f && Vector3.Dot(base.transform.forward, position - base.transform.position) > 0f)
		{
			target = 1f;
		}
		_lookWeight = Mathf.MoveTowards(_lookWeight, target, Time.deltaTime * 3f);
		animator.SetLookAtWeight(_lookWeight);
		if (!(_lookWeight <= 0f))
		{
			Vector3 vector = base.transform.InverseTransformPoint(position + Vector3.up * 1.5f);
			Vector3 vector2 = vector;
			vector2.z = 0f;
			vector.z = Mathf.Max(vector.z, vector2.magnitude);
			position = base.transform.TransformPoint(vector);
			animator.SetLookAtPosition(position);
		}
	}
}
