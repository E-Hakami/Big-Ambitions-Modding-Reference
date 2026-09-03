using System;
using Helpers;
using UnityEngine;

public class CityMapHider : MonoBehaviour
{
	public static readonly DistributedWork<CityMapHider> Work = new DistributedWork<CityMapHider>(delegate(CityMapHider x)
	{
		x.UpdateState();
	});

	[SerializeField]
	private bool hideOnlyWhenLowDetail;

	public void Start()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(UpdateStateDelayed));
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
	}

	private void UpdateStateDelayed(bool _)
	{
		Work.Enqueue(this);
	}

	private void OnEnterBuilding(Address _)
	{
		if (!InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse())
		{
			Work.Enqueue(this);
		}
	}

	private void OnExitBuilding(Address _)
	{
		UpdateState();
	}

	private void UpdateState()
	{
		if ((bool)this)
		{
			bool flag = ShouldBeActive();
			if (base.gameObject.activeSelf != flag)
			{
				base.gameObject.SetActive(flag);
			}
		}
	}

	private bool ShouldBeActive()
	{
		if (CityMap.IsOpen)
		{
			if (hideOnlyWhenLowDetail)
			{
				return !PlayerPrefSettings.LowDetailCityMap;
			}
			return false;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			return InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse();
		}
		return true;
	}
}
