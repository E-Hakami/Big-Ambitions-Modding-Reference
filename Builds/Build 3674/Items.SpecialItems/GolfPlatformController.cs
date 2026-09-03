using System;
using Culling;
using Helpers;
using PlayerActivity;
using UI;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

namespace Items.SpecialItems;

public class GolfPlatformController : EntityController, ICullable
{
	[Header("Golf Platform")]
	public GolferPool npcPool;

	public Transform standingPoint;

	public PlayerActivityBalanceConfig balanceConfig;

	[SerializeField]
	private bool hasNpc;

	private BaseHuman _npc;

	private GolfCourse _course;

	public static GolfPlatformController PlayingInstance { get; private set; }

	public bool HasNpc => _npc;

	public RuntimeAnimatorController GolferAnimatorController => npcPool.animatorControllers[0];

	public override void Start()
	{
		base.Start();
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

	public override bool ShouldReactToIoEnter()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity == null)
		{
			return base.ShouldReactToIoEnter();
		}
		return false;
	}

	public override bool ShouldShowDetailedOverlay()
	{
		return GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()) != Vector3.zero;
	}

	public void OnLod0()
	{
		if (hasNpc && !_npc && !(PlayingInstance == this))
		{
			_npc = npcPool.GetPoolHandler().Get();
			_npc.transform.position = standingPoint.position;
			_npc.transform.forward = standingPoint.forward;
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

	private void ReleasePedestrian()
	{
		if ((bool)_npc)
		{
			npcPool.GetPoolHandler().Release(_npc);
			_npc = null;
		}
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen && !GameManager.IsAnyMiniGameActive())
		{
			base.OnIoEnter();
		}
	}

	public void StartGolfGame()
	{
		if (!PlayingInstance && GetCourse().StartGame(this))
		{
			PlayingInstance = this;
		}
	}

	public static void RequestFinish()
	{
		if ((bool)PlayingInstance)
		{
			PlayingInstance.Finish();
		}
	}

	private void Finish()
	{
		if (!(PlayingInstance != this))
		{
			PlayingInstance = null;
			GetCourse().StopGame();
		}
	}

	public GolfCourse GetCourse()
	{
		if ((bool)_course)
		{
			return _course;
		}
		_course = GetComponentInParent<GolfCourse>();
		return _course;
	}

	public void PerformActivity()
	{
		if (PlayerHelper.ItemInHands != null || !string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
		}
		else if (!ItemPanelUI.IsVisible)
		{
			PlayerActivityUI.Show(new GolfActivity(this), this);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		PlayingInstance = null;
	}
}
