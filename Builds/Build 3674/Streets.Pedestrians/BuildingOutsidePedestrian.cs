using System;
using BigAmbitions.Characters.Appearance;
using UnityEngine;

namespace Streets.Pedestrians;

public class BuildingOutsidePedestrian : MonoBehaviour
{
	private const float MinimumSpawnDistance = 50f;

	private const float MaximumSpawnDistance = 90f;

	public AppearanceTag[] appearanceTags;

	public ThirdPersonCharacter tpc;

	public PedestrianWalkingAiBehavior pedestrianWalkingAiBehavior;

	public PedestrianStationaryBehavior pedestrianStationaryBehavior;

	private PedestrianPlayerRangeHandler _playerRangeHandler;

	private bool _isMoving = true;

	private bool _despawnWhenPossible;

	private Action<BuildingOutsidePedestrian> _releaseCallback;

	private Vector3 _hangoutZonePosition;

	public void Init()
	{
		_playerRangeHandler = new PedestrianPlayerRangeHandler(tpc.navmeshAgent, OnEndCycle);
		tpc.appearanceSetter.SetRandomAge();
		tpc.appearanceSetter.SetRandomAppearance(appearanceTags);
		pedestrianWalkingAiBehavior.SetEndCycleCallback(OnEndCycle);
		pedestrianWalkingAiBehavior.SetChangeStateCallback(ChangeState);
		pedestrianStationaryBehavior.Initialize();
		pedestrianStationaryBehavior.SetChangeStateCallback(ChangeState);
	}

	private void Update()
	{
		_playerRangeHandler.Update();
	}

	public void SetReleaseCallback(Action<BuildingOutsidePedestrian> callback)
	{
		_releaseCallback = callback;
	}

	public void OnSpawnInside(Vector3 hangoutZonePosition)
	{
		OnSpawn(hangoutZonePosition);
		_isMoving = false;
		pedestrianStationaryBehavior.Enable(isInfinite: true);
	}

	public void OnSpawnOutside(Vector3 hangoutZonePosition)
	{
		OnSpawn();
		_isMoving = false;
		_hangoutZonePosition = hangoutZonePosition;
		(Vector3, Quaternion) target = (hangoutZonePosition, Quaternion.identity);
		pedestrianWalkingAiBehavior.Enable(target);
	}

	private void OnSpawn(Vector3 initialPosition = default(Vector3))
	{
		tpc.capsuleCollider.enabled = true;
		_despawnWhenPossible = false;
		if (initialPosition == default(Vector3))
		{
			initialPosition = GetInitialTarget().position;
		}
		tpc.navmeshAgent.Warp(initialPosition);
		tpc.navmeshAgent.enabled = true;
	}

	private void ChangeState(bool isMoving)
	{
		if (isMoving && _isMoving)
		{
			(Vector3, Quaternion) buildingTarget = GetBuildingTarget(base.transform.position, out var buildingRegistration);
			pedestrianWalkingAiBehavior.SetNewTarget(buildingTarget, buildingRegistration);
		}
		else if (isMoving)
		{
			pedestrianStationaryBehavior.Disable();
			(Vector3, Quaternion) buildingTarget2 = GetBuildingTarget(base.transform.position, out var buildingRegistration2);
			pedestrianWalkingAiBehavior.Enable(buildingTarget2, buildingRegistration2);
		}
		else
		{
			pedestrianWalkingAiBehavior.Disable();
			pedestrianStationaryBehavior.Enable(isInfinite: true);
		}
		_isMoving = isMoving;
	}

	private void OnEndCycle()
	{
		if (_despawnWhenPossible)
		{
			Release();
		}
		else
		{
			OnSpawnOutside(_hangoutZonePosition);
		}
	}

	private (Vector3 position, Quaternion rotation) GetInitialTarget()
	{
		return PedestrianBuildingPositionProvider.GetRandomBuildingTarget(InstanceBehavior<CityManager>.Instance.trafficSpawnDistanceTarget.position, 50f, 90f);
	}

	private (Vector3, Quaternion) GetBuildingTarget(Vector3 initialPosition, out BuildingRegistration buildingRegistration)
	{
		return PedestrianBuildingPositionProvider.GetRandomAvailableBuildingTarget(initialPosition, 50f, 90f, out buildingRegistration);
	}

	public void OnHangoutZoneNotVisible()
	{
		if (!_isMoving)
		{
			Release();
			return;
		}
		ChangeState(isMoving: true);
		_despawnWhenPossible = true;
	}

	public void OnBusinessCloseWhileVisible()
	{
		ChangeState(isMoving: true);
		_despawnWhenPossible = true;
	}

	public void RedirectWhileWalking(Vector3 hangoutZonePosition)
	{
		(Vector3, Quaternion) target = (hangoutZonePosition, Quaternion.identity);
		pedestrianWalkingAiBehavior.Enable(target);
	}

	private void Release()
	{
		_releaseCallback?.Invoke(this);
		pedestrianWalkingAiBehavior.Disable();
		pedestrianStationaryBehavior.Disable();
	}
}
