using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.SoundSystem;
using Extensions;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class SubwayAmbienceSound : MonoBehaviour
{
	private NativeArray<float3> _positions;

	private JobHandle _handle;

	private ClosestPositionJob _job;

	private NativeArray<float> _distances;

	[Tooltip("Time Between Possible SubwayStation Sounds in Seconds")]
	[MinMaxSlider(1f, 360f)]
	public Vector2 randomTimeDelay = new Vector2(30f, 60f);

	public float maxRange = 15f;

	public SoundType[] soundTypes;

	public IEnumerator Start()
	{
		if (InstanceBehavior<GameManager>.Instance == null || InstanceBehavior<GameManager>.Instance.IsUIDevScene || InstanceBehavior<CityManager>.Instance?.trafficComponent == null)
		{
			yield break;
		}
		CreateArray();
		yield return null;
		while (true)
		{
			if (!BuildingManager.IsInsideBuilding && !UndergroundParkingManager.IsInsideParking)
			{
				RunJob();
				yield return null;
				_handle.Complete();
				Vector3 vector = _job.GetClosestPosition();
				if (Vector3.SqrMagnitude(InstanceBehavior<GameManager>.Instance.playerController.transform.position - vector) <= maxRange * maxRange)
				{
					InstanceBehavior<SfxManager>.Instance.PlayAudio(soundTypes.GetRandom(), vector);
				}
			}
			yield return new WaitForSeconds(randomTimeDelay.RandomValue());
		}
	}

	private void CreateArray()
	{
		if (_distances.Length == 0)
		{
			_positions = new NativeArray<float3>(((IEnumerable<SubwaySoundSource>)UnityEngine.Object.FindObjectsByType<SubwaySoundSource>(FindObjectsSortMode.None)).Select((Func<SubwaySoundSource, float3>)((SubwaySoundSource x) => x.transform.position)).ToArray(), Allocator.Persistent);
			_distances = new NativeArray<float>(_positions.Length, Allocator.Persistent);
		}
	}

	private void RunJob()
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			CreateArray();
			_job = new ClosestPositionJob
			{
				positions = _positions,
				distances = _distances,
				playerPosition = InstanceBehavior<GameManager>.Instance.playerController.transform.position
			};
			_handle = _job.Schedule(_distances.Length, 4);
		}
	}

	private void OnDestroy()
	{
		if (GameManager.isCitySceneBeingUnloaded || (InstanceBehavior<GameManager>.Instance != null && InstanceBehavior<GameManager>.Instance.IsUIDevScene))
		{
			return;
		}
		_handle.Complete();
		try
		{
			if (_job.distances.IsCreated)
			{
				_job.distances.Dispose();
			}
		}
		catch (ObjectDisposedException ex)
		{
			Debug.LogWarning("SubwayAmbienceSound.OnDestroy distances.Dispose: " + ex);
		}
		try
		{
			if (_job.distances.IsCreated)
			{
				_job.positions.Dispose();
			}
		}
		catch (ObjectDisposedException ex2)
		{
			Debug.LogWarning("SubwayAmbienceSound.OnDestroy positions.Dispose: " + ex2);
		}
	}
}
