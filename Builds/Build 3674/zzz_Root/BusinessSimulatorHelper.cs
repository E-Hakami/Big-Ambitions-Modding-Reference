using System;
using Helpers;
using UI;

public static class BusinessSimulatorHelper
{
	public static readonly DistributedWork<(BuildingRegistration registration, int hour)> Work = new DistributedWork<(BuildingRegistration, int)>(SimulateBusiness);

	private static int _pendingCompletionEvents;

	private static bool HasBeenSimulated;

	public static void Init()
	{
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnd));
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStart));
	}

	public static void RunHourly()
	{
		Work.ForceCompleteAllWork();
		TryInvokeCompletionEvent();
		int hour = SaveGameManager.Current.Hour;
		bool flag = false;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
			if (!(data.simulator == null) && (InstanceBehavior<BuildingManager>.Instance.buildingRegistration != buildingRegistration || InstanceBehavior<UIs>.Instance.timeMachine.isRunning || !data.spawnCustomers) && BusinessHelper.IsBusinessOpen(buildingRegistration))
			{
				flag = true;
				(BuildingRegistration, int) tuple = (buildingRegistration, hour);
				if (!Work.Enqueue(tuple))
				{
					SimulateBusiness(tuple);
				}
			}
		}
		if (flag)
		{
			_pendingCompletionEvents++;
		}
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning || hour >= 23)
		{
			Work.ForceCompleteAllWork();
		}
		TryInvokeCompletionEvent();
	}

	public static void ProgressWork()
	{
		Work.ProgressWork();
		TryInvokeCompletionEvent();
	}

	private static void SimulateBusiness((BuildingRegistration registration, int hour) tuple)
	{
		BusinessType data = BusinessTypeHelper.GetData(tuple.registration.businessTypeName);
		if (!(data.simulator == null))
		{
			data.simulator.SetUp(tuple.registration, tuple.hour);
			data.simulator.SimulateCurrentHour();
			HasBeenSimulated = true;
		}
	}

	private static void OnTimeMachineStart()
	{
		Work.ForceCompleteAllWork();
		HasBeenSimulated = false;
	}

	private static void OnTimeMachineEnd()
	{
		if (!HasBeenSimulated)
		{
			TryInvokeCompletionEvent();
			return;
		}
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
				if (!(data.simulator == null) && (InstanceBehavior<BuildingManager>.Instance.buildingRegistration != buildingRegistration || !data.spawnCustomers))
				{
					data.simulator.OnTimeMachineEnd(buildingRegistration);
				}
			}
		}
		TryInvokeCompletionEvent();
	}

	private static void TryInvokeCompletionEvent()
	{
		if (_pendingCompletionEvents > 0 && !Work.HasPendingWork && !InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			while (_pendingCompletionEvents > 0)
			{
				_pendingCompletionEvents--;
				GameEvent.Invoke("ba:gameevent_businesssimulationscompleted");
			}
		}
	}
}
