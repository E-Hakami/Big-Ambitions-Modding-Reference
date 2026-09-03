using System;
using DG.Tweening;
using Extensions;
using Helpers;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI.ItemPanel;
using UnityEngine;
using UnityEngine.AI;

public class SwimmingPoolController : EntityController, IPlayerActivityType
{
	[SerializeField]
	private PlayerActivityBalanceConfig balanceConfig;

	[Header("SFX")]
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip loopingSwimmingSound;

	[SerializeField]
	private Transform positionInsidePool;

	[SerializeField]
	private float swimmingPoolDirectionOffset = 2f;

	[SerializeField]
	private bool useClosestNavMeshTargetAsOverlayPosition;

	private const float FadeDuration = 0.5f;

	public PlayerActivityBalanceConfig BalanceConfig => balanceConfig;

	private Vector3 GetPositionInsidePool()
	{
		if (!positionInsidePool)
		{
			return base.transform.position;
		}
		return positionInsidePool.position;
	}

	public Vector3 GetPositionInWaterFromGroundPosition(Vector3 groundPosition)
	{
		Vector3 vector = GetPositionInsidePool() - groundPosition;
		vector.y = 0f;
		if (!NavMesh.SamplePosition(groundPosition + vector.normalized * swimmingPoolDirectionOffset, out var hit, 3f, NavMeshHelper.SwimmingAreaMask))
		{
			return GetPositionInsidePool();
		}
		return hit.position;
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen)
		{
			Vector3 customPosition = (useClosestNavMeshTargetAsOverlayPosition ? GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()) : default(Vector3));
			if (simpleOverlayType != 0)
			{
				InstanceBehavior<OverlayManager>.Instance.ShowSimpleOverlay(this, customPosition);
			}
			if (!disableHighlightInteraction && ShouldReactToIoEnter() && !GameManager.IsAnyMiniGameActive())
			{
				SetOutline();
			}
		}
	}

	public void PerformActivity()
	{
		if (!ItemPanelUI.IsVisible)
		{
			PlayerActivityUI.Show(this, this);
		}
	}

	public bool IsPoolReachable()
	{
		return GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()) != Vector3.zero;
	}

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		SwimmingActivity swimmingActivity = new SwimmingActivity(attachedEntity);
		swimmingActivity.onActivityStarted = (Action)Delegate.Combine(swimmingActivity.onActivityStarted, new Action(PlaySound));
		swimmingActivity.onActivityFinished = (Action)Delegate.Combine(swimmingActivity.onActivityFinished, new Action(StopSound));
		return swimmingActivity;
	}

	private void PlaySound()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
		audioSource.DOKill();
		audioSource.volume = 0f;
		audioSource.loop = true;
		audioSource.clip = loopingSwimmingSound;
		audioSource.Play();
		audioSource.DOFade(1f, 0.5f).SetEase(Ease.Linear);
	}

	private void StopSound()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
		audioSource.DOKill();
		audioSource.DOFade(0f, 0.5f).SetEase(Ease.Linear).OnComplete(delegate
		{
			audioSource.Stop();
		});
	}

	private void OnPause(bool paused)
	{
		if (!paused)
		{
			audioSource?.Play();
		}
		else
		{
			audioSource?.Pause();
		}
	}
}
