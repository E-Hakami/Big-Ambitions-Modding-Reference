using AI.Citizens;
using Extensions;
using UnityEngine;

namespace Streets.Pedestrians;

public class Pedestrian : MonoBehaviour
{
	private const float MinimumSpawnDistance = 50f;

	private const float MaximumSpawnDistance = 90f;

	private const float BuildingTargetChance = 0.5f;

	public ThirdPersonCharacter tpc;

	public PedestrianWalkingAiBehavior pedestrianWalkingAiBehavior;

	public PedestrianStationaryBehavior pedestrianStationaryBehavior;

	private PedestrianPlayerRangeHandler _playerRangeHandler;

	private bool _isMoving = true;

	private bool _despawnWhenPossible;

	private void Start()
	{
		_playerRangeHandler = new PedestrianPlayerRangeHandler(tpc.navmeshAgent, OnEndCycle);
		CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataNonAlloc("ba:neighborhood_garmentdistrict");
		tpc.appearanceSetter.data.ageInDays = citizenData.Age;
		tpc.appearanceSetter.SetRandomAppearance(citizenData.Gender, citizenData.AppearanceTags);
		pedestrianWalkingAiBehavior.SetEndCycleCallback(OnEndCycle);
		pedestrianWalkingAiBehavior.SetChangeStateCallback(ChangeState);
		pedestrianStationaryBehavior.Initialize();
		pedestrianStationaryBehavior.SetChangeStateCallback(ChangeState);
	}

	private void Update()
	{
		_playerRangeHandler.Update();
	}

	public void OnSpawn(RuntimeAnimatorController overrideController, float moveSpeed)
	{
		tpc.animator.runtimeAnimatorController = overrideController;
		tpc.navmeshAgent.speed = moveSpeed;
		OnSpawn();
	}

	private void OnSpawn()
	{
		_isMoving = true;
		tpc.capsuleCollider.enabled = true;
		_despawnWhenPossible = false;
		(Vector3, Quaternion) initialTarget = GetInitialTarget();
		if (initialTarget.Item1 == default(Vector3))
		{
			Release();
			return;
		}
		tpc.navmeshAgent.Warp(initialTarget.Item1);
		tpc.navmeshAgent.enabled = true;
		(Vector3, Quaternion) target = GetTarget(0.5f.Probability(), out var buildingRegistration);
		if (target.Item1 == default(Vector3))
		{
			Release();
		}
		else
		{
			pedestrianWalkingAiBehavior.Enable(target, buildingRegistration);
		}
	}

	private void ChangeState(bool isMoving)
	{
		if (isMoving && _isMoving)
		{
			(Vector3, Quaternion) target = GetTarget(0.5f.Probability(), out var buildingRegistration);
			if (target.Item1 == default(Vector3))
			{
				Release();
				return;
			}
			pedestrianWalkingAiBehavior.SetNewTarget(target, buildingRegistration);
		}
		else if (isMoving)
		{
			pedestrianStationaryBehavior.Disable();
			(Vector3, Quaternion) target2 = GetTarget(0.5f.Probability(), out var buildingRegistration2);
			if (target2.Item1 == default(Vector3))
			{
				Release();
				return;
			}
			pedestrianWalkingAiBehavior.Enable(target2, buildingRegistration2);
		}
		else
		{
			pedestrianWalkingAiBehavior.Disable();
			pedestrianStationaryBehavior.Enable();
		}
		_isMoving = isMoving;
	}

	private static (Vector3 position, Quaternion rotation) GetTarget(bool prioritizeBuilding, out BuildingRegistration buildingRegistration)
	{
		Transform trafficSpawnDistanceTarget = InstanceBehavior<CityManager>.Instance.trafficSpawnDistanceTarget;
		buildingRegistration = null;
		(Vector3, Quaternion) result;
		if (prioritizeBuilding)
		{
			result = GetRandomAvailableBuildingTarget(trafficSpawnDistanceTarget.position, out buildingRegistration);
			if (result.Item1 == default(Vector3))
			{
				result = GetRandomSpawnerTarget(trafficSpawnDistanceTarget.position);
			}
		}
		else
		{
			result = GetRandomSpawnerTarget(trafficSpawnDistanceTarget.position);
			if (result.Item1 == default(Vector3))
			{
				result = GetRandomAvailableBuildingTarget(trafficSpawnDistanceTarget.position, out buildingRegistration);
			}
		}
		return result;
	}

	private void OnEndCycle()
	{
		if (_despawnWhenPossible)
		{
			Release();
			return;
		}
		DisableBehaviors();
		OnSpawn();
	}

	private (Vector3 position, Quaternion rotation) GetInitialTarget()
	{
		Transform trafficSpawnDistanceTarget = InstanceBehavior<CityManager>.Instance.trafficSpawnDistanceTarget;
		(Vector3, Quaternion) result;
		if (0.5f.Probability())
		{
			result = PedestrianBuildingPositionProvider.GetRandomBuildingTarget(trafficSpawnDistanceTarget.position, 50f, 90f);
			if (result.Item1 == default(Vector3))
			{
				result = GetRandomInitialSpawnerTarget(trafficSpawnDistanceTarget);
			}
		}
		else
		{
			result = GetRandomInitialSpawnerTarget(trafficSpawnDistanceTarget);
			if (result.Item1 == default(Vector3))
			{
				result = PedestrianBuildingPositionProvider.GetRandomBuildingTarget(trafficSpawnDistanceTarget.position, 50f, 90f);
			}
		}
		return result;
	}

	private static (Vector3 position, Quaternion rotation) GetRandomInitialSpawnerTarget(Transform reference)
	{
		return PedestrianSpawnerPositionProvider.GetRandomSpawnerTarget(reference.position, 50f, 90f, occupyPosition: false);
	}

	private static (Vector3, Quaternion) GetRandomSpawnerTarget(Vector3 initialPosition)
	{
		return PedestrianSpawnerPositionProvider.GetRandomSpawnerTarget(initialPosition, 0f, 90f);
	}

	private static (Vector3 position, Quaternion rotation) GetRandomAvailableBuildingTarget(Vector3 initialPosition, out BuildingRegistration buildingRegistration)
	{
		return PedestrianBuildingPositionProvider.GetRandomAvailableBuildingTarget(initialPosition, 0f, 90f, out buildingRegistration);
	}

	public void OnRelease()
	{
		DisableBehaviors();
	}

	private void DisableBehaviors()
	{
		pedestrianWalkingAiBehavior.Disable();
		pedestrianStationaryBehavior.Disable();
	}

	public void DespawnWhenPossible()
	{
		_despawnWhenPossible = true;
	}

	public void Release()
	{
		InstanceBehavior<CityManager>.Instance.pedestrianSpawner.pedestrianPool.GetPoolHandler().Release(this);
	}
}
