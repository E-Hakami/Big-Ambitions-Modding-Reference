using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisNpcController : TennisController
{
	private const float ServeDelay = 1f;

	private const float ServeMaxOffset = 2f;

	private const float GuardPositionRangeX = 0.3f;

	private const float GuardPositionZMax = 0.7f;

	private const float AimPositionRangeX = 0.75f;

	private const float GuardDelayMin = 0.25f;

	private const float GuardDelayMax = 0.5f;

	private const float InterceptDelayMin = 0.25f;

	private const float InterceptDelayMax = 0.5f;

	private float _serveTimer;

	private Vector3 _guardPosition;

	private float _guardDelay;

	private float _interceptDelay;

	private TennisCourtSide _lastHitterSide;

	public override Vector2 GetMovementInput()
	{
		if (_lastHitterSide != base.Court.LastHitterSide)
		{
			if ((bool)base.Court.LastHitterSide && base.Court.LastHitterSide != base.CourtSide)
			{
				_interceptDelay = Random.Range(0.25f, 0.5f);
			}
			_lastHitterSide = base.Court.LastHitterSide;
		}
		if (!base.Ball.gameObject.activeSelf)
		{
			return Vector2.zero;
		}
		Vector3 destinationPosition = GetDestinationPosition();
		Vector3 vector = base.transform.InverseTransformPoint(destinationPosition);
		return new Vector2(vector.x, vector.z);
	}

	public override Vector3 GetAimedPosition()
	{
		if (player.IsServing())
		{
			Vector3 serviceAreaCenter = player.OpponentSide.GetServiceAreaCenter();
			serviceAreaCenter.x += Random.Range(-2f, 2f);
			serviceAreaCenter.z += Random.Range(-2f, 2f);
			return serviceAreaCenter;
		}
		TennisCourtSide oppositeCourtSide = base.Court.GetOppositeCourtSide(base.CourtSide);
		float x = Random.Range(oppositeCourtSide.ballLocalBounds.min.x, oppositeCourtSide.ballLocalBounds.max.x) * 0.75f;
		float z = Random.Range(oppositeCourtSide.ballLocalBounds.min.z, oppositeCourtSide.ballLocalBounds.center.z);
		return oppositeCourtSide.transform.TransformPoint(new Vector3(x, 0f, z));
	}

	public override bool GetServeInput()
	{
		if (base.Court.IsPopupActive() || !base.Court.AreBothPlayersReadyForServe())
		{
			return false;
		}
		if (_serveTimer <= 0f)
		{
			_serveTimer = 1f;
		}
		_serveTimer -= Time.deltaTime;
		return _serveTimer <= 0f;
	}

	private Vector3 GetDestinationPosition()
	{
		if (!base.Ball.gameObject.activeSelf)
		{
			ClearGuardPosition();
			return base.transform.position;
		}
		if (IsBallIncoming())
		{
			if (_interceptDelay <= 0f)
			{
				ClearGuardPosition();
				return GetInterceptPosition();
			}
			_interceptDelay -= Time.deltaTime;
		}
		if (_guardPosition != Vector3.zero)
		{
			return _guardPosition;
		}
		if (_guardDelay > 0f)
		{
			_guardDelay -= Time.deltaTime;
			return base.transform.position;
		}
		_guardPosition = ChooseGuardPosition();
		return _guardPosition;
	}

	private Vector3 ChooseGuardPosition()
	{
		float x = Random.Range(base.CourtSide.ballLocalBounds.min.x, base.CourtSide.ballLocalBounds.max.x) * 0.3f;
		float z = base.CourtSide.ballLocalBounds.min.z + Random.value * base.CourtSide.ballLocalBounds.size.z * 0.7f;
		return base.CourtSide.transform.TransformPoint(new Vector3(x, 0f, z));
	}

	private void ClearGuardPosition()
	{
		if (!(_guardPosition == Vector3.zero))
		{
			_guardPosition = Vector3.zero;
			_guardDelay = Random.Range(0.25f, 0.5f);
		}
	}

	private Vector3 GetInterceptPosition()
	{
		Vector3 position = base.Ball.transform.position;
		Vector3 velocity = base.Ball.Velocity;
		bool num = !base.Court.HasServeReturned && !base.Court.BallBouncedSide;
		Vector3 vector = position;
		bool flag = true;
		if (!num && PredictBallPosition(position, velocity, 1f, out var p, out var p2, out var velocity2))
		{
			vector = GetNearestPosition(p, p2);
			flag = false;
		}
		if ((bool)base.Court.BallBouncedSide || !PredictBallPosition(position, velocity, 0f, out p, out p2, out var velocity3))
		{
			return vector;
		}
		Vector3 ballPosition = p2;
		velocity3.y = (0f - velocity3.y) * 0.65f;
		if (!PredictBallPosition(ballPosition, velocity3, 1f, out p, out p2, out velocity2))
		{
			return vector;
		}
		p = ClampBallPositionToOwnBounds(p, velocity);
		p2 = ClampBallPositionToOwnBounds(p2, velocity);
		vector = (flag ? p : GetNearestPosition(vector, p));
		return GetNearestPosition(vector, p2);
	}

	private bool PredictBallPosition(Vector3 ballPosition, Vector3 ballVelocity, float height, out Vector3 p1, out Vector3 p2, out Vector3 velocity2)
	{
		p1 = ballPosition;
		p2 = ballPosition;
		velocity2 = ballVelocity;
		float num = 0.5f * Physics.gravity.y;
		float y = ballVelocity.y;
		float num2 = ballPosition.y - (base.transform.position.y + height);
		float num3 = y * y - 4f * num * num2;
		if (num3 < 0f)
		{
			return false;
		}
		float num4 = Mathf.Sqrt(num3);
		float num5 = (0f - y - num4) / (2f * num);
		float num6 = (0f - y + num4) / (2f * num);
		bool flag = num5 >= 0f;
		bool flag2 = num6 >= 0f;
		if (flag & flag2)
		{
			p1 = ballPosition + ballVelocity * num5 + Physics.gravity * (num5 * num5 * 0.5f);
			p2 = ballPosition + ballVelocity * num6 + Physics.gravity * (num6 * num6 * 0.5f);
			velocity2 = ballVelocity + Physics.gravity * num6;
			return true;
		}
		if (flag)
		{
			p1 = ballPosition + ballVelocity * num5 + Physics.gravity * (num5 * num5 * 0.5f);
			p2 = p1;
			velocity2 = ballVelocity + Physics.gravity * num5;
			return true;
		}
		if (flag2)
		{
			p1 = ballPosition + ballVelocity * num6 + Physics.gravity * (num6 * num6 * 0.5f);
			p2 = p1;
			velocity2 = ballVelocity + Physics.gravity * num6;
			return true;
		}
		return false;
	}

	private Vector3 GetNearestPosition(Vector3 p1, Vector3 p2)
	{
		float sqrMagnitude = (base.transform.position - p1).sqrMagnitude;
		float sqrMagnitude2 = (base.transform.position - p2).sqrMagnitude;
		if (!(sqrMagnitude < sqrMagnitude2))
		{
			return p2;
		}
		return p1;
	}

	private Vector3 ClampBallPositionToOwnBounds(Vector3 ballPosition, Vector3 ballVelocity)
	{
		Vector3 vector = base.CourtSide.transform.InverseTransformPoint(ballPosition);
		Bounds playerLocalBounds = base.CourtSide.playerLocalBounds;
		if (vector.x > playerLocalBounds.min.x && vector.x < playerLocalBounds.max.x && vector.z > playerLocalBounds.min.z)
		{
			return ballPosition;
		}
		float num = 0f;
		if (vector.x < playerLocalBounds.min.x)
		{
			num = vector.x - playerLocalBounds.min.x;
		}
		else if (vector.x > playerLocalBounds.max.x)
		{
			num = vector.x - playerLocalBounds.max.x;
		}
		float num2 = 0f;
		if (vector.z < playerLocalBounds.min.z)
		{
			num2 = vector.z - playerLocalBounds.min.z;
		}
		float num3 = Mathf.Max(Mathf.Abs(ballVelocity.x), 0.5f);
		float num4 = Mathf.Max(Mathf.Abs(ballVelocity.z), 0.5f);
		float num5 = Mathf.Max(num / num3, num2 / num4);
		return ballPosition - ballVelocity * num5;
	}

	private bool IsBallIncoming()
	{
		return Vector3.Dot(base.transform.forward, base.Ball.Velocity) < 0f;
	}
}
