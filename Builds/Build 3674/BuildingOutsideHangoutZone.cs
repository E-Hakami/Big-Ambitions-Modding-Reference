using System;
using Helpers;
using UnityEngine;

public class BuildingOutsideHangoutZone : MonoBehaviour
{
	[SerializeField]
	private AiSpawnerForBuildingOutsideHangoutZone aiSpawner;

	[HideInInspector]
	public CityBuildingController buildingController;

	private BusinessType _businessType;

	private bool _hasPedestriansOutside;

	private void SubscribeToGlobalEvents()
	{
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(SetPedestriansHangoutZoneInfo));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void UnSubscribeToGlobalEvents()
	{
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(SetPedestriansHangoutZoneInfo));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (!(address != buildingController.building.Address))
		{
			SetBusinessType();
		}
	}

	public void Init(CityBuildingController cityBuildingController)
	{
		buildingController = cityBuildingController;
		base.transform.position = cityBuildingController.entranceDoors[0].doorTransform.transform.position;
		base.transform.rotation = cityBuildingController.entranceDoors[0].doorTransform.transform.rotation;
		SubscribeToGlobalEvents();
		SetBusinessType();
	}

	public void ReleaseFromPool()
	{
		UnSubscribeToGlobalEvents();
		InstanceBehavior<CityManager>.Instance.buildingOutsideHangoutZone.buildingOutsideHangoutZonePool.Release(this);
	}

	private void SetPedestriansHangoutZoneInfo()
	{
		bool flag = _businessType.hasPedestriansOutside && BusinessHelper.IsBusinessOpen(buildingController.buildingRegistration);
		if (_hasPedestriansOutside != flag)
		{
			aiSpawner.pedestrianPool = _businessType.pedestrianPool;
			_hasPedestriansOutside = flag;
			if (_hasPedestriansOutside)
			{
				aiSpawner.OnBusinessOpen();
			}
			else
			{
				aiSpawner.OnBusinessClose();
			}
		}
	}

	private void SetBusinessType()
	{
		_businessType = BusinessTypeHelper.GetData(buildingController.buildingRegistration);
		SetPedestriansHangoutZoneInfo();
	}

	public void OnVisible()
	{
		aiSpawner.OnVisible();
	}

	public void OnNotVisible()
	{
		aiSpawner.OnNotVisible();
	}
}
