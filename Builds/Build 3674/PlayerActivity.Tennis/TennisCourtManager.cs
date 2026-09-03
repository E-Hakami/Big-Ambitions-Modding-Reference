using System;
using System.Collections.Generic;
using Culling;
using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisCourtManager : MonoBehaviour, ICullable
{
	public float radius = 90f;

	public List<TennisCourt> courts;

	private void Start()
	{
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Combine(GlobalEvents.onHospitalRespawnStarts, new Action(OnHospitalRespawnStarts));
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
			GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Remove(GlobalEvents.onHospitalRespawnStarts, new Action(OnHospitalRespawnStarts));
		}
	}

	private static void OnHospitalRespawnStarts()
	{
		TennisCourt.RequestFinish();
	}

	private void OnEnterBuilding(Address _)
	{
		OnLod1();
	}

	public void OnLod0()
	{
		foreach (TennisCourt court in courts)
		{
			court.gameObject.SetActive(value: true);
		}
	}

	public void OnLod1()
	{
		foreach (TennisCourt court in courts)
		{
			court.gameObject.SetActive(court == TennisCourt.PlayingInstance);
		}
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position, radius);
	}
}
