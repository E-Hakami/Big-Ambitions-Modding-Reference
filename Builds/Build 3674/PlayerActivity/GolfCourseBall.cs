using System;
using Helpers;
using UnityEngine;

namespace PlayerActivity;

public class GolfCourseBall : MonoBehaviour
{
	[SerializeField]
	private float minVelocityToKeepActive = 0.1f;

	[SerializeField]
	private TrailRenderer trailPrefab;

	[SerializeField]
	private Rigidbody rb;

	[SerializeField]
	private Transform shadow;

	[SerializeField]
	private float shadowMaxScaleHeight = 5f;

	[SerializeField]
	private float maxWindInfluenceHeight = 5f;

	[NonSerialized]
	public GolfCourse ownerCourse;

	[NonSerialized]
	public Vector3 wind;

	private TrailRenderer _trail;

	private Quaternion _shadowInitialRotation;

	private Vector3 _shadowInitialScale;

	public bool IsKinematic => rb.isKinematic;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
		_trail = UnityEngine.Object.Instantiate(trailPrefab);
		_shadowInitialRotation = shadow.localRotation;
		_shadowInitialScale = shadow.localScale;
	}

	private void OnEnable()
	{
		_trail.transform.position = base.transform.position;
		_trail.Clear();
	}

	private void OnDisable()
	{
		if ((bool)ownerCourse)
		{
			ownerCourse.OnBallDeactivated();
		}
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
		_trail.transform.position = base.transform.position;
		if (rb.isKinematic)
		{
			return;
		}
		if (!rb.isKinematic && rb.velocity.sqrMagnitude < minVelocityToKeepActive * minVelocityToKeepActive)
		{
			base.gameObject.SetActive(value: false);
		}
		else if (!(wind == Vector3.zero))
		{
			float y = PlayerHelper.GetPosition().y;
			float num = Mathf.Clamp01((base.transform.position.y - y) / maxWindInfluenceHeight);
			if (num > 0f)
			{
				rb.AddForce(wind * num, ForceMode.Acceleration);
			}
		}
	}

	private void LateUpdate()
	{
		float y = PlayerHelper.GetPosition().y;
		shadow.position = new Vector3(base.transform.position.x, y, base.transform.position.z);
		shadow.rotation = _shadowInitialRotation;
		float num = Mathf.Clamp01((base.transform.position.y - y) / shadowMaxScaleHeight);
		shadow.localScale = _shadowInitialScale * num;
	}

	public void SetKinematic(bool isKinematic)
	{
		rb.isKinematic = isKinematic;
	}

	public void Launch(Vector3 shotVelocity)
	{
		rb.angularVelocity = Vector3.zero;
		_trail.transform.position = base.transform.position;
		_trail.Clear();
		rb.velocity = shotVelocity;
	}

	private void OnCollisionEnter(Collision other)
	{
		ownerCourse.OnBallCollisionCheck(other);
	}

	private void OnCollisionStay(Collision other)
	{
		ownerCourse.OnBallCollisionCheck(other);
	}
}
