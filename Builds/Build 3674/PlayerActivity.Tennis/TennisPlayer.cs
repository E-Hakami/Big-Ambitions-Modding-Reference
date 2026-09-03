using System;
using Extensions;
using JimmysUnityUtilities;
using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisPlayer : MonoBehaviour
{
	public const float HitYAverage = 1f;

	private const int TrajectoryLinePointCount = 20;

	private const float AnimationMoveTransitionSpeed = 4f;

	private const float AnimationMoveBackTransitionSpeed = 3f;

	private const float RunSpeed = 5f;

	private const float HitLeadTime = 0.5f;

	private const float HitRadius = 1f;

	private const float HitYMin = 0.25f;

	private const float HitYMax = 2f;

	private const float HitHighThreshold = 1.6f;

	private const int HitCheckForwardIterations = 2;

	private const float HitCheckForwardAmount = 0.5f;

	private const float QuickHitSpeedMultiplier = 2f;

	private const int IkBaseLayerIndex = 0;

	private static readonly int Right = Animator.StringToHash("Right");

	private static readonly int Forward = Animator.StringToHash("Forward");

	private static readonly int ResetHit = Animator.StringToHash("ResetHit");

	private static readonly int Hit = Animator.StringToHash("Hit");

	private static readonly int Hit2 = Animator.StringToHash("Hit2");

	private static readonly int Hit3 = Animator.StringToHash("Hit3");

	private static readonly int BeginServe = Animator.StringToHash("BeginServe");

	private static readonly int HitSpeedMultiplier = Animator.StringToHash("HitSpeedMultiplier");

	private static readonly int PointWon = Animator.StringToHash("PointWon");

	private static readonly int PointLost = Animator.StringToHash("PointLost");

	private static readonly Vector3[] TrajectoryPoints = new Vector3[20];

	public TennisController controller;

	[SerializeField]
	private TennisCourtSide courtSide;

	[SerializeField]
	private Transform spine;

	[SerializeField]
	private Transform[] feetIkIdle;

	[SerializeField]
	private Transform[] feetIkHit;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private float hitDuration = 0.4f;

	[SerializeField]
	private float hitCooldown = 0.7f;

	[SerializeField]
	private float hitSpineRotation = 30f;

	[SerializeField]
	private LineRenderer trajectoryLinePrefab;

	[SerializeField]
	private float hitMinAngle = -10f;

	[SerializeField]
	private float hitMaxAngle = 40f;

	[SerializeField]
	private float hitMinVelocity = 5f;

	[SerializeField]
	private float hitMaxVelocity = 15f;

	[SerializeField]
	private float netToTargetDistanceEffectOnAngle = 0.5f;

	[SerializeField]
	private GameObject animatedBall;

	[SerializeField]
	private Vector3 serveBallLocalPosition;

	[SerializeField]
	private Vector3 hitBallLocalPosition;

	[NonSerialized]
	public Vector3 goToPosition;

	private Vector2 _animationMove;

	private float _animationMoveActive;

	private float _animationHit;

	private float _hitTimer;

	private float _hitTurnDirection;

	private float _hitCooldownTimer;

	private LineRenderer _trajectoryLine;

	private Vector3 _hitVelocity;

	private bool _pendingServe;

	private bool _inhibitControl;

	public TennisCourtSide OpponentSide { get; private set; }

	public bool IsPlayer => controller is TennisHumanController;

	public TennisCourt Court => courtSide.court;

	public TennisCourtSide CourtSide => courtSide;

	public TennisBall Ball => Court.Ball;

	private void Awake()
	{
		_trajectoryLine = UnityEngine.Object.Instantiate(trajectoryLinePrefab);
		_trajectoryLine.enabled = false;
		animatedBall.gameObject.SetActive(value: false);
	}

	private void Start()
	{
		OpponentSide = Court.GetOppositeCourtSide(courtSide);
	}

	private void OnDisable()
	{
		if ((bool)_trajectoryLine)
		{
			_trajectoryLine.enabled = false;
		}
	}

	private void OnDestroy()
	{
		if ((bool)_trajectoryLine)
		{
			UnityEngine.Object.Destroy(_trajectoryLine.gameObject);
		}
	}

	private void Update()
	{
		if (Time.timeScale != 0f)
		{
			if (_hitTimer > 0f)
			{
				_hitTimer -= Time.deltaTime;
			}
			if (_hitCooldownTimer > 0f)
			{
				_hitCooldownTimer -= Time.deltaTime;
			}
			float num = ((_hitTimer > 0f) ? 1f : 0f);
			float num2 = ((num < _animationHit) ? 3f : 4f);
			_animationHit = Mathf.MoveTowards(_animationHit, num, num2 * Time.deltaTime);
			Vector2 vector = (_inhibitControl ? Vector2.zero : GetMovementInput());
			if (vector.sqrMagnitude > 1f)
			{
				vector.Normalize();
			}
			float num3 = 4f * Time.deltaTime;
			_animationMove = Vector2.MoveTowards(_animationMove, vector, num3);
			_animationMoveActive = Mathf.MoveTowards(_animationMoveActive, vector.magnitude, num3);
			animator.SetFloat(Right, _animationMove.x);
			animator.SetFloat(Forward, _animationMove.y);
			Vector3 vector2 = Vector3.zero;
			if (vector != Vector2.zero)
			{
				Vector3 right = courtSide.transform.right;
				right.y = 0f;
				right.Normalize();
				Vector3 forward = courtSide.transform.forward;
				forward.y = 0f;
				forward.Normalize();
				vector2 = right * vector.x + forward * vector.y;
				base.transform.position += vector2 * (5f * Time.deltaTime);
			}
			ConstrainToBounds();
			CheckHit(vector2);
			UpdateTrajectory(out var outVelocity);
			if (!_pendingServe || outVelocity != Vector3.zero)
			{
				_hitVelocity = outVelocity;
			}
			if (!_pendingServe && !_inhibitControl && Court.AwaitingServe && Court.ServingSide == courtSide && outVelocity != Vector3.zero && controller.GetServeInput())
			{
				animatedBall.SetActive(value: true);
				animator.SetTrigger(BeginServe);
				_pendingServe = true;
				_hitCooldownTimer = hitCooldown;
			}
		}
	}

	private void LateUpdate()
	{
		if (_animationHit > 0f)
		{
			spine.Rotate(Vector3.up, hitSpineRotation * _animationHit * _hitTurnDirection, Space.Self);
		}
	}

	private Vector2 GetMovementInput()
	{
		if (goToPosition == Vector3.zero)
		{
			return controller.GetMovementInput();
		}
		Vector3 vector = base.transform.InverseTransformPoint(goToPosition);
		vector.y = 0f;
		if (vector.sqrMagnitude < 0.01f)
		{
			goToPosition = Vector3.zero;
		}
		return new Vector2(vector.x, vector.z);
	}

	private void CheckHit(Vector3 moveInput)
	{
		if (!Ball.gameObject.activeSelf)
		{
			return;
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = !Court.HasServeReturned && !Court.BallBouncedSide;
		if (CanHit(Ball.transform.position - base.transform.position))
		{
			if (flag3)
			{
				Court.AwardPoint(OpponentSide);
				return;
			}
			Vector3 position = base.transform.TransformPoint(hitBallLocalPosition);
			Ball.SetPosition(position, hardSet: false);
			Ball.Launch(_hitVelocity, courtSide, GetHitPitch(), isServe: false);
			flag = true;
			flag2 = true;
		}
		if ((_hitCooldownTimer > 0f) | flag3)
		{
			return;
		}
		Vector3 vector = Ball.GetPredictedPosition(0.5f) - base.transform.position;
		if (!flag)
		{
			Vector3 vector2 = moveInput * 1.25f;
			for (int i = 0; i < 2; i++)
			{
				if (CanHit(vector - vector2 * i))
				{
					flag = true;
					vector -= vector2 * i;
					break;
				}
			}
		}
		if (!flag)
		{
			return;
		}
		vector = base.transform.parent.InverseTransformVector(vector);
		int hitTrigger;
		if (vector.y > 1.6f)
		{
			hitTrigger = Hit3;
			_hitTurnDirection = 1f;
		}
		else if (vector.x > 0f)
		{
			hitTrigger = Hit;
			_hitTurnDirection = 1f;
		}
		else
		{
			hitTrigger = Hit2;
			_hitTurnDirection = -1f;
		}
		animator.SetTrigger(ResetHit);
		animator.SetFloat(HitSpeedMultiplier, flag2 ? 2f : 1f);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)this)
			{
				animator.ResetTrigger(ResetHit);
				animator.SetTrigger(hitTrigger);
				_hitTimer = hitDuration;
				_hitCooldownTimer = hitCooldown;
			}
		});
	}

	private bool CanHit(Vector3 ballOffset, bool checkVelocity = true)
	{
		if (ballOffset.y < 0.25f || ballOffset.y > 2f)
		{
			return false;
		}
		ballOffset.y = 0f;
		if (ballOffset.sqrMagnitude > 1f)
		{
			return false;
		}
		if (checkVelocity)
		{
			return Vector3.Dot(base.transform.forward, Ball.Velocity) <= 0f;
		}
		return true;
	}

	private void UpdateTrajectory(out Vector3 outVelocity)
	{
		outVelocity = Vector3.zero;
		_trajectoryLine.enabled = false;
		Vector3 aimedPosition = controller.GetAimedPosition();
		aimedPosition = OpponentSide.ClampToAimableBounds(aimedPosition);
		Vector3 position = (IsServing() ? serveBallLocalPosition : hitBallLocalPosition);
		Vector3 vector = base.transform.TransformPoint(position);
		float num = aimedPosition.y - vector.y;
		Vector3 origin = vector;
		origin.y = base.transform.position.y;
		Vector3 vector2 = aimedPosition - vector;
		vector2.y = 0f;
		float magnitude = vector2.magnitude;
		if (!Court.RayCastNet(new Ray(origin, vector2), out var hitInfo, magnitude) || !Court.RayCastNet(new Ray(aimedPosition, -vector2), out var hitInfo2, magnitude))
		{
			return;
		}
		float distance = hitInfo.distance;
		float distance2 = hitInfo2.distance;
		float num2 = Court.totalCourtLength * 0.5f;
		float num3 = distance / num2;
		float num4 = distance2 / num2;
		float t = (num3 + 1f - netToTargetDistanceEffectOnAngle * num4) * 0.5f;
		float num5 = Mathf.Lerp(hitMinAngle, hitMaxAngle, t);
		float num6 = ProjectileMath.LaunchSpeed(magnitude, 0f - num, 0f - Physics.gravity.y, num5 * (MathF.PI / 180f));
		if (float.IsNaN(num6) || num6 > hitMaxVelocity)
		{
			num6 = hitMaxVelocity;
			if (ProjectileMath.LaunchAngle(num6, magnitude, num, 0f - Physics.gravity.y, out var angle, out var angle2))
			{
				num5 = Mathf.Min(angle, angle2) * 57.29578f;
			}
			else if (Application.isEditor)
			{
				Debug.LogWarning("No valid launch angle found");
			}
		}
		num6 = Mathf.Clamp(num6, hitMinVelocity, hitMaxVelocity);
		Quaternion quaternion = Quaternion.LookRotation(vector2);
		outVelocity = Quaternion.Euler(0f - num5, 0f, 0f) * Vector3.forward * num6;
		outVelocity = quaternion * outVelocity;
		if (IsPlayer && !_inhibitControl)
		{
			ProjectileMath.ProjectileArcPoints(TrajectoryPoints, vector, outVelocity, magnitude);
			_trajectoryLine.positionCount = TrajectoryPoints.Length;
			_trajectoryLine.SetPositions(TrajectoryPoints);
			_trajectoryLine.enabled = true;
		}
	}

	public void ApplyAnimatorIK(int layerIndex)
	{
		if (layerIndex == 0)
		{
			float value = Mathf.Clamp01(1f - _animationMoveActive * 2f);
			animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, value);
			animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, value);
			animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, value);
			animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, value);
			animator.SetIKPosition(AvatarIKGoal.LeftFoot, Vector3.Lerp(feetIkIdle[0].position, feetIkHit[0].position, _animationHit));
			animator.SetIKPosition(AvatarIKGoal.RightFoot, Vector3.Lerp(feetIkIdle[1].position, feetIkHit[1].position, _animationHit));
			animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.Lerp(feetIkIdle[0].rotation, feetIkHit[0].rotation, _animationHit));
			animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.Lerp(feetIkIdle[1].rotation, feetIkHit[1].rotation, _animationHit));
		}
	}

	private void ConstrainToBounds()
	{
		Vector3 position = courtSide.transform.InverseTransformPoint(base.transform.position);
		if (Court.AwaitingServe && Court.ServingSide == courtSide && Court.IsHumanPlaying())
		{
			bool isServingOnRightSide = Court.IsServingOnRightSide;
			position.x = Mathf.Clamp(position.x, isServingOnRightSide ? 0f : courtSide.localServeLine.min.x, isServingOnRightSide ? courtSide.localServeLine.max.x : 0f);
			position.z = courtSide.localServeLine.center.z;
		}
		position.x = Mathf.Clamp(position.x, courtSide.playerLocalBounds.min.x, courtSide.playerLocalBounds.max.x);
		position.z = Mathf.Clamp(position.z, courtSide.playerLocalBounds.min.z, courtSide.playerLocalBounds.max.z);
		base.transform.position = courtSide.transform.TransformPoint(position);
	}

	public void OnBallServe()
	{
		_pendingServe = false;
		animatedBall.SetActive(value: false);
		Vector3 position = base.transform.TransformPoint(serveBallLocalPosition);
		Court.SetBallActive(active: true);
		Ball.SetPosition(position, hardSet: true);
		Ball.Launch(_hitVelocity, courtSide, GetHitPitch(), isServe: true);
	}

	private float GetHitPitch()
	{
		return Mathf.InverseLerp(hitMinVelocity, hitMaxVelocity, _hitVelocity.magnitude);
	}

	public bool IsServing()
	{
		if (!_pendingServe)
		{
			if (Court.AwaitingServe)
			{
				return Court.ServingSide == courtSide;
			}
			return false;
		}
		return true;
	}

	public void OnPointWon()
	{
		animator.SetTrigger(PointWon);
		animator.SetLayerWeight(1, 0f);
		_inhibitControl = true;
	}

	public void OnPointLost()
	{
		animator.SetTrigger(PointLost);
		animator.SetLayerWeight(1, 0f);
		_inhibitControl = true;
	}

	public void ResetState()
	{
		animator.SetLayerWeight(1, 1f);
		animator.Rebind();
		animator.Update(0f);
		_animationMove = Vector2.zero;
		_animationMoveActive = 0f;
		_animationHit = 0f;
		_hitTimer = 0f;
		_hitCooldownTimer = 0f;
		_pendingServe = false;
		_inhibitControl = false;
	}
}
