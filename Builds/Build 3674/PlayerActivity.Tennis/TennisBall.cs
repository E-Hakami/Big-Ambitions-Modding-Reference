using System;
using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisBall : MonoBehaviour
{
	public const float ExpectedBounceFactor = 0.65f;

	[SerializeField]
	private TrailRenderer trailPrefab;

	[SerializeField]
	private MeshRenderer visual;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private Transform shadow;

	[SerializeField]
	private float shadowMaxScaleHeight = 5f;

	[SerializeField]
	private float visualRecenterSmoothTime = 0.1f;

	[NonSerialized]
	public TennisCourt court;

	private TrailRenderer _trail;

	private Quaternion _shadowInitialRotation;

	private Vector3 _shadowInitialScale;

	private Vector3 _visualRecenterVelocity;

	public Vector3 Velocity => rb.velocity;

	private void Awake()
	{
		_trail = UnityEngine.Object.Instantiate(trailPrefab, base.transform.parent);
		_shadowInitialRotation = shadow.localRotation;
		_shadowInitialScale = shadow.localScale;
		SetTrailEnabled(newEnabled: false);
	}

	private void OnEnable()
	{
		_trail.transform.position = base.transform.position;
		_trail.Clear();
	}

	private void OnDestroy()
	{
		if ((bool)_trail)
		{
			UnityEngine.Object.Destroy(_trail.gameObject);
		}
	}

	private void FixedUpdate()
	{
		_trail.transform.position = visual.transform.position;
	}

	private void LateUpdate()
	{
		if (visual.transform.localPosition != Vector3.zero)
		{
			visual.transform.localPosition = Vector3.SmoothDamp(visual.transform.localPosition, Vector3.zero, ref _visualRecenterVelocity, visualRecenterSmoothTime);
		}
		float y = court.transform.position.y;
		Vector3 position = visual.transform.position;
		shadow.position = new Vector3(position.x, y, position.z);
		shadow.rotation = _shadowInitialRotation;
		float num = Mathf.Clamp01((position.y - y) / shadowMaxScaleHeight);
		shadow.localScale = _shadowInitialScale * num;
	}

	public void SetPosition(Vector3 position, bool hardSet)
	{
		Vector3 position2 = base.transform.position;
		base.transform.position = position;
		rb.position = position;
		rb.velocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;
		if (!hardSet)
		{
			visual.transform.position = position2;
			return;
		}
		_trail.transform.position = position;
		_trail.Clear();
		visual.transform.localPosition = Vector3.zero;
		_visualRecenterVelocity = Vector3.zero;
	}

	public void Launch(Vector3 shotVelocity, TennisCourtSide fromSide, float pitchFactor, bool isServe)
	{
		rb.velocity = shotVelocity;
		court.OnBallHit(fromSide, pitchFactor, isServe);
	}

	public Vector3 GetPredictedPosition(float time)
	{
		Vector3 result = rb.position + rb.velocity * time + Physics.gravity * (0.5f * time * time);
		float y = court.transform.position.y;
		if (result.y > y)
		{
			return result;
		}
		float num = 0.5f * Physics.gravity.y;
		float y2 = rb.velocity.y;
		float num2 = rb.position.y - y;
		float num3 = y2 * y2 - 4f * num * num2;
		if (num3 < 0f)
		{
			return result;
		}
		float num4 = Mathf.Sqrt(num3);
		float a = (0f - y2 + num4) / (2f * num);
		float b = (0f - y2 - num4) / (2f * num);
		float num5 = Mathf.Max(a, b);
		if (num5 < 0f || time < num5)
		{
			return result;
		}
		result = rb.position + rb.velocity * num5 + Physics.gravity * (0.5f * num5 * num5);
		Vector3 velocity = rb.velocity;
		velocity.y = (0f - rb.velocity.y) * 0.65f;
		time -= num5;
		return result + (velocity * time + Physics.gravity * (0.5f * time * time));
	}

	public void SetTrailEnabled(bool newEnabled)
	{
		_trail.enabled = newEnabled;
	}

	private void OnCollisionEnter(Collision other)
	{
		court.OnBallCollisionEnter(other);
	}
}
