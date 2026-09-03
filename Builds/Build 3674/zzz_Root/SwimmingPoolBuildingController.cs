using System;
using System.Collections;
using Helpers;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.EventSystems;

public class SwimmingPoolBuildingController : ViewBlockingEntity
{
	private static readonly int SelectionColorID = Shader.PropertyToID("_SelectionColor");

	[SerializeField]
	private Transform entrance;

	[SerializeField]
	private Transform exit;

	[SerializeField]
	private GameObject groundPlane;

	private bool _highlighted;

	protected override int DefaultLayer => LayerHelper.BuildingLayerIndex;

	public override void Start()
	{
		base.Start();
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOn)
		{
			if (!isOn)
			{
				SetHighlight(isOn: false);
				ClearOutlineColor();
			}
		});
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(StopHighlight));
	}

	private void StopHighlight()
	{
		if (_highlighted)
		{
			ClearOutlineColor();
			SetHighlight(isOn: false);
		}
	}

	private void SetHighlight(bool isOn)
	{
		_highlighted = isOn;
		if (isOn)
		{
			StopAllCoroutines();
			SetOutlineLayer();
		}
		else
		{
			OnIoExit();
		}
		if (isOn)
		{
			SetOutlineColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
		}
	}

	public override void OnIoEnter()
	{
		if (!EventSystem.current.IsPointerOverGameObject() && !CityMap.IsOpen && !FullMenu.IsOpen && !PlayerActivityUI.IsPanelOpen && !SubwaySystem.IsRiding && !InstanceBehavior<UIs>.Instance.screenshot.enabled && !GameManager.IsAnyMiniGameActive() && ScreenshotController.uiIsVisible && !ScreenshotController.isInFreeLookMode)
		{
			StopAllCoroutines();
			SetOutlineLayer();
			InstanceBehavior<OverlayManager>.Instance.ShowSimpleOverlay(this);
		}
	}

	public override void OnIoExit()
	{
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(OnIoExitDelayedCoroutine());
		}
		if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		}
	}

	private IEnumerator OnIoExitDelayedCoroutine()
	{
		if (_highlighted)
		{
			SetOutlineColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
			yield break;
		}
		foreach (ViewBlockingEntityPart cityBuildingPart in cityBuildingParts)
		{
			if (cityBuildingPart.gameObject.layer != 0)
			{
				cityBuildingPart.gameObject.layer = LayerHelper.BuildingLayerIndex;
			}
		}
		if (base.gameObject.layer != 0)
		{
			base.gameObject.layer = LayerHelper.BuildingLayerIndex;
		}
		if (groundPlane != null)
		{
			groundPlane.layer = LayerHelper.BuildingLayerIndex;
		}
	}

	private void SetOutlineLayer()
	{
		foreach (ViewBlockingEntityPart cityBuildingPart in cityBuildingParts)
		{
			if (cityBuildingPart.gameObject.layer != 0)
			{
				cityBuildingPart.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
			}
		}
		if (base.gameObject.layer != 0)
		{
			base.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
		if (groundPlane != null)
		{
			groundPlane.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
	}

	public override bool SetCameraBlockMode(bool isOn)
	{
		if (!base.SetCameraBlockMode(isOn))
		{
			return false;
		}
		base.gameObject.layer = ((!isOn) ? LayerHelper.BuildingLayerIndex : 0);
		return true;
	}

	public override void SetOutlineColor(Color color)
	{
		foreach (Renderer item in renderersToHide)
		{
			item.GetPropertyBlock(EntityController.PropertyBlockGetter);
			EntityController.PropertyBlockGetter.SetColor(SelectionColorID, color);
			item.SetPropertyBlock(EntityController.PropertyBlockGetter);
		}
	}

	private void ClearOutlineColor()
	{
		foreach (Renderer item in renderersToHide)
		{
			item.SetPropertyBlock(null);
		}
	}

	public void PerformAction()
	{
		PlayerHelper.PlayerController.SetGoal(this, WarpToOppositeSide);
	}

	private void WarpToOppositeSide()
	{
		ThirdPersonCharacter character = PlayerHelper.PlayerController.Character;
		Vector3 closestNavMeshTargetPosition = GetClosestNavMeshTargetPosition(character.transform.position);
		if (closestNavMeshTargetPosition == entrance.position)
		{
			character.navmeshAgent.Warp(exit.position);
		}
		else if (closestNavMeshTargetPosition == exit.position)
		{
			character.navmeshAgent.Warp(entrance.position);
		}
	}

	public string GetActionKey()
	{
		ThirdPersonCharacter character = PlayerHelper.PlayerController.Character;
		if (!(GetClosestNavMeshTargetPosition(character.transform.position) == entrance.position))
		{
			return "click_to_exit_swimming_pool";
		}
		return "click_to_enter_swimming_pool";
	}
}
