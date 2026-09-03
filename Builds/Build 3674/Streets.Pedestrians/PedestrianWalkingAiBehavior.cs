using System;
using UnityEngine;

namespace Streets.Pedestrians;

public class PedestrianWalkingAiBehavior : MonoBehaviour
{
	public ThirdPersonCharacter tpc;

	private PedestrianMovementHandler _movementHandler;

	private PedestrianAnimationHandler _animationHandler;

	private Action _onEndCycleCallback;

	private bool _enabled;

	private void Awake()
	{
		_movementHandler = new PedestrianMovementHandler(tpc, OnEndCycle);
		_animationHandler = new PedestrianAnimationHandler(tpc);
	}

	public void SetEndCycleCallback(Action callback)
	{
		_onEndCycleCallback = callback;
	}

	public void SetChangeStateCallback(Action<bool> callback)
	{
		_movementHandler.SetChangeStateCallback(callback);
	}

	private void OnEndCycle()
	{
		_onEndCycleCallback?.Invoke();
	}

	public void Enable((Vector3, Quaternion) target, BuildingRegistration buildingRegistration = null)
	{
		_enabled = true;
		_movementHandler.Enable(target, buildingRegistration);
		_animationHandler.Enable();
	}

	public void SetNewTarget((Vector3, Quaternion) target, BuildingRegistration buildingRegistration = null)
	{
		_movementHandler.SetNewTarget(target, buildingRegistration);
	}

	public void Disable()
	{
		_enabled = false;
		_movementHandler.Disable();
		_animationHandler.Disable();
	}

	private void Update()
	{
		if (_enabled)
		{
			_movementHandler.Update();
			_animationHandler.Update();
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_enabled)
		{
			_movementHandler.OnTriggerEnter(other);
		}
	}
}
