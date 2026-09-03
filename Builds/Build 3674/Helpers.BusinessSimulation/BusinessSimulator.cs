using UnityEngine;

namespace Helpers.BusinessSimulation;

public abstract class BusinessSimulator : ScriptableObject
{
	protected BuildingRegistration buildingRegistration;

	protected int currentHour;

	public virtual void SetUp(BuildingRegistration registration, int hour)
	{
		buildingRegistration = registration;
		currentHour = hour;
	}

	public abstract void SimulateCurrentHour();

	public abstract void OnTimeMachineEnd(BuildingRegistration buildingRegistration);
}
