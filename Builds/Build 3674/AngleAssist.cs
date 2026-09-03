using System.Collections.Generic;
using GleyTrafficSystem;
using Helpers;
using NWH.VehiclePhysics2;
using UnityEngine;
using Vehicles;

public class AngleAssist
{
	private static List<float> RoadAngles;

	private readonly Transform _transform;

	private readonly NWH.VehiclePhysics2.VehicleController _vehicleController;

	private float _cachedWaypointAngle;

	private bool _hasCachedWaypointAngle;

	private float _nextWaypointSampleTime;

	private bool _lastUsedWaypoint;

	private float _suppressedUntil;

	public static List<float> ValidRoadAngles => RoadAngles ?? (RoadAngles = new List<float>(VehicleHelper.AngleAssistSettings.defaultRoadAngles));

	public AngleAssist(Transform transform, NWH.VehiclePhysics2.VehicleController vehicleController)
	{
		_transform = transform;
		_vehicleController = vehicleController;
	}

	public void Run()
	{
		AngleAssistSettings angleAssistSettings = VehicleHelper.AngleAssistSettings;
		if (_vehicleController.SpeedSigned < angleAssistSettings.minSpeedForAssist)
		{
			_vehicleController.steering.externallyAddedAngle = 0f;
			return;
		}
		float num = Mathf.Abs(_vehicleController.input.Steering);
		if (num >= angleAssistSettings.suppressSteerInput)
		{
			_suppressedUntil = Time.time + angleAssistSettings.reenableDelay;
			_vehicleController.steering.externallyAddedAngle = 0f;
			return;
		}
		if (num >= angleAssistSettings.releaseSteerInput && Time.time < _suppressedUntil)
		{
			_suppressedUntil = Time.time + angleAssistSettings.reenableDelay;
		}
		if (Time.time < _suppressedUntil)
		{
			_vehicleController.steering.externallyAddedAngle = 0f;
			return;
		}
		float y = _transform.eulerAngles.y;
		float targetAngle = GetTargetAngle(y, angleAssistSettings);
		float num2 = Mathf.DeltaAngle(y, targetAngle);
		float num3 = Mathf.Abs(num2);
		if (_lastUsedWaypoint)
		{
			float num4 = CalculateWaypointActivationAngle(angleAssistSettings);
			_vehicleController.steering.externallyAddedAngle = ((num3 <= num4) ? Mathf.Clamp(num2, 0f - angleAssistSettings.waypointMaxSteer, angleAssistSettings.waypointMaxSteer) : 0f);
		}
		else
		{
			_vehicleController.steering.externallyAddedAngle = ((num3 <= angleAssistSettings.fixedAngleSystemMaxDifference) ? num2 : 0f);
		}
	}

	public static void ResetRoadAngles()
	{
		RoadAngles = new List<float>(VehicleHelper.AngleAssistSettings.defaultRoadAngles);
	}

	private float CalculateWaypointActivationAngle(AngleAssistSettings settings)
	{
		return settings.waypointActivationAngleMin + (settings.waypointActivationAngleMax - settings.waypointActivationAngleMin) * Mathf.Clamp01((_vehicleController.Speed - settings.waypointMinSpeed) / (settings.waypointMaxSpeed - settings.waypointMinSpeed));
	}

	private float GetTargetAngle(float currentYRotation, AngleAssistSettings settings)
	{
		if (TryGetWaypointAngle(out var angle, settings))
		{
			_lastUsedWaypoint = true;
			return angle;
		}
		_lastUsedWaypoint = false;
		return FindClosestAngle(currentYRotation);
	}

	private bool TryGetWaypointAngle(out float angle, AngleAssistSettings settings)
	{
		if (Time.time >= _nextWaypointSampleTime)
		{
			_nextWaypointSampleTime = Time.time + settings.waypointRefreshInterval;
			_hasCachedWaypointAngle = TrafficManager.HasInstance && TrafficManager.Instance.TryGetClosestRoadYaw(_transform.position, _transform.forward, settings.waypointMaxDistance, out _cachedWaypointAngle);
		}
		angle = _cachedWaypointAngle;
		return _hasCachedWaypointAngle;
	}

	private static float FindClosestAngle(float currentAngle)
	{
		float num = ValidRoadAngles[0];
		float num2 = Mathf.Abs(Mathf.DeltaAngle(currentAngle, num));
		foreach (float validRoadAngle in ValidRoadAngles)
		{
			float num3 = Mathf.Abs(Mathf.DeltaAngle(currentAngle, validRoadAngle));
			if (num3 < num2)
			{
				num2 = num3;
				num = validRoadAngle;
			}
		}
		return num;
	}
}
