using UnityEngine;

namespace Vehicles;

public class AngleAssistSettings : ScriptableObject
{
	[Header("Old Fixed Angle System")]
	public float fixedAngleSystemMaxDifference = 3.5f;

	public float[] defaultRoadAngles = new float[9] { 0f, 67.5f, 90f, 111.15f, 112.6f, 129.797f, 180f, 270f, 360f };

	[Space]
	[Header("New Waypoints Angle System")]
	public float waypointActivationAngleMin = 3.5f;

	public float waypointActivationAngleMax = 20f;

	public float waypointMinSpeed = 10f;

	public float waypointMaxSpeed = 27f;

	public float waypointMaxSteer = 5f;

	public float waypointMaxDistance = 12f;

	public float waypointRefreshInterval = 0.15f;

	public float suppressSteerInput = 0.2f;

	public float releaseSteerInput = 0.1f;

	public float reenableDelay = 0.5f;

	public float minSpeedForAssist = 2f;
}
