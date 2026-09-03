using System;
using System.Collections.Generic;
using Entities;
using IngameDebugConsole;
using Streets.Pedestrians;
using UnityEngine;

public class PedestrianSpawner : MonoBehaviour
{
	public List<Pedestrian> activePedestrians;

	public PedestrianPool pedestrianPool;

	public BuildingStationaryAiPool buildingStationaryAiPool;

	public List<BuildingStationaryAiBehavior> activeBuildingStationaryAis;

	public bool spawningActive = true;

	private int _lastSetAmount;

	[ConsoleMethod("TogglePedestrian", "Toggle pedestrian spawning", new string[] { })]
	public static void Command_ToggleSpawning()
	{
		InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive = !InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive;
		Pedestrian[] array = UnityEngine.Object.FindObjectsByType<Pedestrian>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Release();
		}
		StationaryAiSpawner[] array2 = UnityEngine.Object.FindObjectsByType<StationaryAiSpawner>(FindObjectsSortMode.None);
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].ClearPedestrians();
		}
		OutsideBenchController[] array3 = UnityEngine.Object.FindObjectsByType<OutsideBenchController>(FindObjectsSortMode.None);
		for (int i = 0; i < array3.Length; i++)
		{
			array3[i].ReleasePedestrian();
		}
		Debug.Log("Pedestrian spawning is now " + (InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive ? "active" : "inactive"));
	}

	public void Init()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
	}

	private void OnEnterBuilding(Address _)
	{
		Pedestrian[] array = activePedestrians.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Release();
		}
	}

	private void OnExitBuilding(Address _)
	{
		foreach (BuildingStationaryAiBehavior activeBuildingStationaryAi in activeBuildingStationaryAis)
		{
			buildingStationaryAiPool.GetPoolHandler().Release(activeBuildingStationaryAi);
		}
		activeBuildingStationaryAis.Clear();
		SetActivePedestrians(_lastSetAmount);
	}

	public void SetActivePedestrians(int amount)
	{
		_lastSetAmount = amount;
		if (BuildingManager.IsInsideBuilding)
		{
			return;
		}
		int count = activePedestrians.Count;
		if (amount > count)
		{
			for (int i = 0; i < amount - count; i++)
			{
				pedestrianPool.GetPoolHandler().Get();
			}
		}
		if (amount < count)
		{
			for (int j = 0; j < count - amount; j++)
			{
				activePedestrians[j].DespawnWhenPossible();
			}
		}
	}

	private void OnDestroy()
	{
		StationaryAiSpawner.AllPedestrianSpawners.Clear();
	}

	[ConsoleMethod("SpawnPedestrians", "Spawn pedestrians", new string[] { })]
	public static void SpawnPedestrians(int amount)
	{
		InstanceBehavior<CityManager>.Instance.pedestrianSpawner.SetActivePedestrians(amount);
	}
}
