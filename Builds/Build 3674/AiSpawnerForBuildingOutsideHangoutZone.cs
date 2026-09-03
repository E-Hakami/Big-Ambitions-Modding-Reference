using System.Collections.Generic;
using Extensions;
using Streets.Pedestrians;
using UnityEngine;

public class AiSpawnerForBuildingOutsideHangoutZone : AiSpawnerZone
{
	[HideInInspector]
	public BuildingOutsidePedestrianPool pedestrianPool;

	private readonly List<BuildingOutsidePedestrian> _pedestrians = new List<BuildingOutsidePedestrian>();

	private bool _enabled;

	public void OnVisible()
	{
		bool flag = isVisible;
		isVisible = true;
		if (!CityMap.IsOpen && _enabled && !flag)
		{
			SpawnGroupInsideAndOutside();
		}
	}

	public void OnNotVisible()
	{
		isVisible = false;
		if (_enabled)
		{
			OnHangoutZoneNotVisible();
		}
	}

	private void OnHangoutZoneNotVisible()
	{
		for (int num = _pedestrians.Count - 1; num >= 0; num--)
		{
			_pedestrians[num].OnHangoutZoneNotVisible();
		}
		usedPositions.Clear();
	}

	public void OnBusinessOpen()
	{
		_enabled = true;
		if (isVisible)
		{
			SpawnGroupOutside();
		}
	}

	public void OnBusinessClose()
	{
		_enabled = false;
		usedPositions.Clear();
		if (isVisible)
		{
			OnBusinessCloseWhileVisible();
		}
	}

	private void OnBusinessCloseWhileVisible()
	{
		for (int num = _pedestrians.Count - 1; num >= 0; num--)
		{
			_pedestrians[num].OnBusinessCloseWhileVisible();
		}
	}

	private void SpawnGroupInsideAndOutside()
	{
		if (!IsSpawningActive())
		{
			return;
		}
		RedirectExistingPedestrians();
		int num = spawnAmount.RandomValue() - _pedestrians.Count;
		int num2 = Mathf.CeilToInt((float)num / 2f);
		for (int i = 0; i < num; i++)
		{
			if (GetPosition(out var pos))
			{
				if (i < num2)
				{
					SpawnInside(pos);
				}
				else
				{
					SpawnOutside(pos);
				}
			}
		}
	}

	private void SpawnGroupOutside()
	{
		if (!IsSpawningActive())
		{
			return;
		}
		RedirectExistingPedestrians();
		int num = spawnAmount.RandomValue() - _pedestrians.Count;
		for (int i = 0; i < num; i++)
		{
			if (GetPosition(out var pos))
			{
				SpawnOutside(pos);
			}
		}
	}

	private void RedirectExistingPedestrians()
	{
		foreach (BuildingOutsidePedestrian pedestrian in _pedestrians)
		{
			if (GetPosition(out var pos))
			{
				pedestrian.RedirectWhileWalking(pos);
			}
		}
	}

	private void SpawnInside(Vector3 position)
	{
		SpawnPedestrian().OnSpawnInside(position);
	}

	private void SpawnOutside(Vector3 position)
	{
		SpawnPedestrian().OnSpawnOutside(position);
	}

	private BuildingOutsidePedestrian SpawnPedestrian()
	{
		BuildingOutsidePedestrian buildingOutsidePedestrian = pedestrianPool.GetPoolHandler().Get();
		buildingOutsidePedestrian.SetReleaseCallback(ReleasePedestrian);
		_pedestrians.Add(buildingOutsidePedestrian);
		return buildingOutsidePedestrian;
	}

	private void ReleasePedestrian(BuildingOutsidePedestrian pedestrian)
	{
		pedestrianPool.GetPoolHandler().Release(pedestrian);
		_pedestrians.Remove(pedestrian);
	}
}
