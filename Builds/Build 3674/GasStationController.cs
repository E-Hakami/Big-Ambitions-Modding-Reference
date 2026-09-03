using System;
using System.Collections.Generic;
using UI.Overlays;
using UnityEngine;

public class GasStationController : MonoBehaviour
{
	private static readonly List<GasStationController> AllControllers = new List<GasStationController>();

	public GasStationTrigger[] gasStationTriggers;

	public GasStationTrigger[] repairStationTriggers;

	public GasStationRepairGarageDoor gasStationRepairGarageDoor;

	private readonly List<GasStationTrigger> _activeTriggers = new List<GasStationTrigger>();

	public void Start()
	{
		AllControllers.Add(this);
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		GasStationTrigger[] array = gasStationTriggers;
		foreach (GasStationTrigger obj in array)
		{
			obj.onEntered = (Action<GasStationTrigger>)Delegate.Combine(obj.onEntered, new Action<GasStationTrigger>(OnStationTriggerEntered));
			obj.onExited = (Action<GasStationTrigger>)Delegate.Combine(obj.onExited, new Action<GasStationTrigger>(OnStationTriggerExited));
		}
		array = repairStationTriggers;
		foreach (GasStationTrigger obj2 in array)
		{
			obj2.onEntered = (Action<GasStationTrigger>)Delegate.Combine(obj2.onEntered, new Action<GasStationTrigger>(OnStationTriggerEntered));
			obj2.onExited = (Action<GasStationTrigger>)Delegate.Combine(obj2.onExited, new Action<GasStationTrigger>(OnStationTriggerExited));
		}
	}

	public static void RefreshActiveTriggers(Collider testCollider)
	{
		foreach (GasStationController allController in AllControllers)
		{
			allController.UpdateActiveTriggersFromCollider(testCollider);
		}
	}

	private void OnDestroy()
	{
		AllControllers.Remove(this);
	}

	private void OnEnterVehicle(VehicleController vehicle)
	{
		UpdateActiveTriggersFromCollider(InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleCollider);
	}

	private void OnExitVehicle(VehicleController vehicle)
	{
		ClearActiveTriggers();
	}

	private void OnStationTriggerEntered(GasStationTrigger trigger)
	{
		if (!_activeTriggers.Contains(trigger))
		{
			if (_activeTriggers.Count == 0)
			{
				GasStationOverlay.Hide();
				GasStationOverlay.Show(trigger);
			}
			_activeTriggers.Add(trigger);
		}
	}

	private void OnStationTriggerExited(GasStationTrigger trigger)
	{
		int num = _activeTriggers.IndexOf(trigger);
		if (num == -1)
		{
			return;
		}
		_activeTriggers.RemoveAt(num);
		if (num <= 0)
		{
			GasStationOverlay.Hide(trigger);
			if (_activeTriggers.Count > 0)
			{
				GasStationOverlay.Show(_activeTriggers[0]);
			}
		}
	}

	private void UpdateActiveTriggersFromCollider(Collider testCollider)
	{
		ClearActiveTriggers();
		Bounds bounds = testCollider.bounds;
		GasStationTrigger[] array = gasStationTriggers;
		foreach (GasStationTrigger gasStationTrigger in array)
		{
			if (gasStationTrigger.IntersectsBounds(bounds))
			{
				_activeTriggers.Add(gasStationTrigger);
			}
		}
		array = repairStationTriggers;
		foreach (GasStationTrigger gasStationTrigger2 in array)
		{
			if (gasStationTrigger2.IntersectsBounds(bounds))
			{
				_activeTriggers.Add(gasStationTrigger2);
			}
		}
		if (_activeTriggers.Count > 0)
		{
			GasStationOverlay.Show(_activeTriggers[0]);
		}
	}

	private void ClearActiveTriggers()
	{
		foreach (GasStationTrigger activeTrigger in _activeTriggers)
		{
			GasStationOverlay.Hide(activeTrigger);
		}
		_activeTriggers.Clear();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AllControllers.Clear();
	}
}
