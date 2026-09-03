using System;
using System.Collections;
using GleyTrafficSystem;
using Streets.Pedestrians;
using UnityEngine;
using UnityEngine.AI;

namespace Streets;

public class VehiclePassengerDropOff : MonoBehaviour
{
	[SerializeField]
	private float maxBrakeDistance;

	[SerializeField]
	private float minAssumedSpeed;

	[SerializeField]
	private float turnDecelerationRate;

	[SerializeField]
	private float dropOffChance;

	[SerializeField]
	private float dropOffDelay;

	[SerializeField]
	private ThirdPersonCharacterPool pedestrianPool;

	[SerializeField]
	private Transform pedestrianTarget;

	[SerializeField]
	private VehiclePassengerDropOff[] nextDropOffs;

	private Waypoint _dropOffWaypoint;

	private VehicleComponent _currentVehicle;

	private float _decelerationRate;

	private float _dropOffTimer;

	private bool _skipNext;

	private IEnumerator Start()
	{
		yield return new WaitUntil(() => TrafficManager.IsInitialized);
		_dropOffWaypoint = TrafficManager.Instance.GetClosestWaypoint(base.transform.position);
		if (_dropOffWaypoint == null)
		{
			base.enabled = false;
			yield break;
		}
		TrafficManager instance = TrafficManager.Instance;
		instance.onVehicleWaypointRequested = (Action<VehicleComponent>)Delegate.Combine(instance.onVehicleWaypointRequested, new Action<VehicleComponent>(OnVehicleWaypointRequested));
	}

	private void FixedUpdate()
	{
		if (!_currentVehicle)
		{
			return;
		}
		if (!_currentVehicle.isActiveAndEnabled || !TrafficManager.Instance.IsVehicleIgnored(_currentVehicle.GetIndex()))
		{
			_currentVehicle = null;
			return;
		}
		Vector3 velocity = _currentVehicle.rb.velocity;
		if (velocity.sqrMagnitude < 0.01f)
		{
			_dropOffTimer += Time.deltaTime;
			if (!(_dropOffTimer < dropOffDelay))
			{
				DropOff();
				TrafficManager.Instance.SetVehicleIgnored(_currentVehicle.GetIndex(), ignored: false);
				Waypoint closestWaypoint = TrafficManager.Instance.GetClosestWaypoint(_currentVehicle.transform.position + _currentVehicle.transform.forward * 5f);
				if (closestWaypoint != null)
				{
					TrafficManager.Instance.SetVehicleTargetWaypoint(_currentVehicle, closestWaypoint);
				}
				_currentVehicle = null;
			}
		}
		else
		{
			velocity = Vector3.MoveTowards(velocity, Vector3.zero, _decelerationRate * Time.deltaTime);
			_currentVehicle.rb.velocity = velocity;
			Vector3 angularVelocity = _currentVehicle.rb.angularVelocity;
			angularVelocity = Vector3.MoveTowards(angularVelocity, Vector3.zero, turnDecelerationRate * Time.deltaTime);
			_currentVehicle.rb.angularVelocity = angularVelocity;
		}
	}

	private void OnVehicleWaypointRequested(VehicleComponent vehicle)
	{
		if ((bool)_currentVehicle || vehicle.vehicleType != VehicleTypes.Car || TrafficManager.Instance.GetTargetWaypoint(vehicle.GetIndex()) != _dropOffWaypoint)
		{
			return;
		}
		if (_skipNext)
		{
			_skipNext = false;
		}
		else if (!(UnityEngine.Random.value > dropOffChance) && TrafficManager.Instance.GetCurrentDrivingState(vehicle.GetIndex()).Item2 == SpecialDriveActionTypes.Forward)
		{
			TrafficManager.Instance.SetVehicleIgnored(vehicle.GetIndex(), ignored: true);
			_currentVehicle = vehicle;
			_dropOffTimer = 0f;
			float num = Mathf.Max(minAssumedSpeed, vehicle.rb.velocity.magnitude);
			_decelerationRate = num / maxBrakeDistance;
			VehiclePassengerDropOff[] array = nextDropOffs;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]._skipNext = true;
			}
		}
	}

	private void DropOff()
	{
		if (NavMesh.SamplePosition(_currentVehicle.rb.worldCenterOfMass + _currentVehicle.transform.right * 2f, out var hit, 2f, -1))
		{
			ThirdPersonCharacter thirdPersonCharacter = pedestrianPool.GetPoolHandler().Get();
			if (!thirdPersonCharacter.navmeshAgent.Warp(hit.position))
			{
				pedestrianPool.GetPoolHandler().Release(thirdPersonCharacter);
				return;
			}
			thirdPersonCharacter.appearanceSetter.SetRandomAppearance();
			Vector3 forward = pedestrianTarget.position - thirdPersonCharacter.transform.position;
			forward.y = 0f;
			thirdPersonCharacter.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
			thirdPersonCharacter.gameObject.AddComponent<PedestrianWalkToTarget>().Setup(pedestrianTarget, pedestrianPool);
		}
	}
}
