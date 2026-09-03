using System;
using System.Collections;
using Extensions;
using Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace Streets.Pedestrians;

public class PedestrianMovementHandler
{
	private const float CompletionRadius = 0.5f;

	private const float MaxRealCompletionRadiusAllowed = 9f;

	private const float RotationDuration = 1.5f;

	private const int MaxPathTryCount = 5;

	private readonly ThirdPersonCharacter _tpc;

	private readonly NavMeshAgent _agent;

	private readonly Action _onEndCycle;

	private readonly NavMeshPath _path = new NavMeshPath();

	private Action<bool> _changeState;

	private bool _isGoingToBuilding;

	private bool _waitingSemaphore;

	private CrosswalkToTrafficLightLink _crosswalkLink;

	private GameObject _lastCrosswalk;

	private Quaternion _targetRotation;

	private Coroutine _rotationCoroutine;

	private bool _enabled;

	private BuildingRegistration _buildingRegistration;

	private Vector3 _targetPosition;

	private int _pathTryCount;

	public PedestrianMovementHandler(ThirdPersonCharacter tpc, Action onEndCycle)
	{
		_agent = tpc.navmeshAgent;
		_onEndCycle = onEndCycle;
		_tpc = tpc;
	}

	public void Update()
	{
		if (!_enabled)
		{
			return;
		}
		if (_pathTryCount >= 5)
		{
			_pathTryCount = 0;
			_onEndCycle?.Invoke();
			return;
		}
		if (_agent.pathPending && !_agent.hasPath)
		{
			TrySetNewPath();
		}
		if (IsTooFarFromPlayer())
		{
			_onEndCycle?.Invoke();
		}
		else if (HasReachedDestination() && _rotationCoroutine == null)
		{
			if (_isGoingToBuilding)
			{
				_rotationCoroutine = _tpc.StartCoroutine(OnBuildingReached());
			}
			else
			{
				OnHangoutZoneReached();
			}
		}
		else if (_waitingSemaphore && CanCrossCrosswalk())
		{
			_agent.isStopped = false;
			_waitingSemaphore = false;
		}
	}

	private void TrySetNewPath()
	{
		if (_pathTryCount++ < 5)
		{
			if (_agent.CalculatePath(_targetPosition, _path) && _agent.SetPath(_path))
			{
				_pathTryCount = 0;
			}
			else
			{
				_changeState?.Invoke(obj: true);
			}
		}
	}

	private IEnumerator OnBuildingReached()
	{
		yield return RotateTowards();
		_rotationCoroutine = null;
		if (CanEnterBuilding())
		{
			_onEndCycle?.Invoke();
		}
		else
		{
			_changeState?.Invoke(obj: true);
		}
	}

	private bool CanEnterBuilding()
	{
		if (_buildingRegistration != null && !(_buildingRegistration.GetBuildingType() == "ba:buildingtype_residential"))
		{
			return BusinessHelper.IsBusinessOpen(_buildingRegistration);
		}
		return true;
	}

	private IEnumerator RotateTowards()
	{
		_agent.isStopped = true;
		_tpc.capsuleCollider.enabled = false;
		yield return _tpc.RotateTowards(_targetRotation, 1.5f);
	}

	private void OnHangoutZoneReached()
	{
		if (_targetRotation == Quaternion.identity)
		{
			_changeState?.Invoke(obj: false);
		}
		else
		{
			_rotationCoroutine = _tpc.StartCoroutine(RotateAfterReachingHangoutZone());
		}
	}

	private IEnumerator RotateAfterReachingHangoutZone()
	{
		yield return RotateTowards();
		_rotationCoroutine = null;
		_changeState?.Invoke(obj: false);
	}

	public void Enable((Vector3 position, Quaternion rotation) target, BuildingRegistration buildingRegistration)
	{
		_enabled = true;
		_agent.isStopped = false;
		_targetPosition = target.position;
		_targetRotation = target.rotation;
		_isGoingToBuilding = buildingRegistration != null;
		_buildingRegistration = buildingRegistration;
		_lastCrosswalk = null;
		_waitingSemaphore = false;
		_rotationCoroutine = null;
		TrySetNewPath();
	}

	public void SetNewTarget((Vector3 position, Quaternion rotation) target, BuildingRegistration buildingRegistration)
	{
		_tpc.capsuleCollider.enabled = true;
		_agent.isStopped = false;
		_targetPosition = target.position;
		_targetRotation = target.rotation;
		_buildingRegistration = buildingRegistration;
		_isGoingToBuilding = buildingRegistration != null;
		TrySetNewPath();
	}

	public void Disable()
	{
		_enabled = false;
		if (_rotationCoroutine != null)
		{
			_tpc.StopCoroutine(_rotationCoroutine);
		}
	}

	private bool HasReachedDestination()
	{
		if (!_agent.pathPending && _agent.pathStatus != NavMeshPathStatus.PathInvalid && _agent.remainingDistance <= 0.5f)
		{
			if (MathHelper.DistanceSqr(_agent.transform.position, _targetPosition) <= 9f)
			{
				return true;
			}
			TrySetNewPath();
		}
		return false;
	}

	private bool IsTooFarFromPlayer()
	{
		return InstanceBehavior<CityManager>.Instance.IsOutsidePlayerRange(_agent.transform.position);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.CompareTag("Crosswalk") && !(_lastCrosswalk == other.gameObject))
		{
			_crosswalkLink = other.GetComponent<CrosswalkToTrafficLightLink>();
			if (!(_crosswalkLink == null) && !CanCrossCrosswalk())
			{
				_agent.isStopped = true;
				_waitingSemaphore = true;
				_lastCrosswalk = other.gameObject;
			}
		}
	}

	private bool CanCrossCrosswalk()
	{
		return _crosswalkLink.redTrafficLight.gameObject.activeSelf;
	}

	public void SetChangeStateCallback(Action<bool> callback)
	{
		_changeState = callback;
	}
}
