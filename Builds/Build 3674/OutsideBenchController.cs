using System;
using Culling;
using NaughtyAttributes;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

public class OutsideBenchController : EntityController, ICullable
{
	[Header("Bench")]
	[SerializeField]
	private RestEnvironment restEnvironment;

	public Transform sittingPosition;

	public bool spawnPedestrians = true;

	[ShowIf("spawnPedestrians")]
	public Transform aiSittingPosition;

	[SerializeField]
	private BaseHumanPool pedestrianPool;

	[HideInInspector]
	public GameObject currentlySeatedAi;

	private BaseHuman _npc;

	public override void Start()
	{
		base.Start();
		base.name = "Bench" + UnityEngine.Random.Range(0, 10000);
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			base.OnDestroy();
			InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.UnregisterCullable(this);
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		}
	}

	private void OnEnterBuilding(Address _)
	{
		ReleasePedestrian();
	}

	public void OnLod0()
	{
		if (spawnPedestrians && InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive && !_npc && UnityEngine.Random.Range(0, 100) <= InstanceBehavior<GameManager>.Instance.timeOfDayController.GetTargetPedestrians())
		{
			_npc = pedestrianPool.GetPoolHandler().Get();
			_npc.SitOnChair(aiSittingPosition);
		}
	}

	public void OnLod1()
	{
		ReleasePedestrian();
	}

	public void OnLod2()
	{
		ReleasePedestrian();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}

	public void ReleasePedestrian()
	{
		if ((bool)_npc)
		{
			pedestrianPool.GetPoolHandler().Release(_npc);
			_npc = null;
		}
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen)
		{
			base.OnIoEnter();
		}
	}

	public void PerformActivity()
	{
		if (!string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			Notifications.ShowError("sleepingbench_notification_cant_use_with_handtruck");
		}
		else
		{
			PlayerActivityUI.Show(restEnvironment, this);
		}
	}
}
