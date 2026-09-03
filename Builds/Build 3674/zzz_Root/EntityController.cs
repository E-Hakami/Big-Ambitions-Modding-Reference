using System;
using System.Collections;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Controllers;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public abstract class EntityController : MonoBehaviour
{
	protected const float MaxReachDistance = 3f;

	public bool primaryInteractionEnabled = true;

	[Header("Overlays")]
	public SimpleOverlayType simpleOverlayType;

	public DetailedOverlayType detailedOverlayType;

	public string customOverlayHeaderKey;

	public Vector3 itemWarningIconOffset;

	public bool alwaysShowWarningIcon;

	[SerializeField]
	private Transform[] employeeNavMeshTargets;

	[Header("Fields automatically set up")]
	[ReadOnly]
	[SerializeField]
	protected Renderer[] renderers;

	[ReadOnly]
	[SerializeField]
	protected Transform[] navMeshTargets;

	[NonSerialized]
	public RandomAvailableNavMeshPositionGetter randomAvailableNavMeshPositionGetter;

	protected readonly UnityEvent onIoExitEvent = new UnityEvent();

	private static MaterialPropertyBlock PropertyBlock;

	private static readonly int SelectionColor = Shader.PropertyToID("_SelectionColor");

	internal bool cancelHighlightAndFadeOut;

	[HideInInspector]
	public bool blockOutline;

	protected bool disableHighlightInteraction;

	[NonSerialized]
	public bool visible = true;

	protected static MaterialPropertyBlock PropertyBlockGetter => PropertyBlock ?? (PropertyBlock = new MaterialPropertyBlock());

	public Renderer[] Renderers => renderers;

	public Vector3[] NavMeshTargetsPositions => navMeshTargets.Select((Transform x) => x.position).ToArray();

	public Transform[] EmployeeNavMeshTargets => employeeNavMeshTargets;

	public virtual bool Occupied { get; set; }

	public bool IsOutlineSuppressed { get; set; }

	protected virtual int DefaultLayer => LayerHelper.InteractiveItemsLayerIndex;

	protected virtual Renderer[] OutlineRenderers => renderers;

	public virtual void Awake()
	{
		if (base.gameObject.layer != DefaultLayer)
		{
			base.gameObject.layer = DefaultLayer;
		}
	}

	public virtual void Start()
	{
		CoroutineUtility.RunAfterFrameDelay(UpdateNavMeshTargets, 3);
		randomAvailableNavMeshPositionGetter = new RandomAvailableNavMeshPositionGetter(navMeshTargets.Length);
	}

	public virtual bool Interact()
	{
		return false;
	}

	public virtual void SecondaryInteract()
	{
	}

	public virtual void SpecialInteract()
	{
	}

	public virtual bool ShouldReactToIoEnter()
	{
		if (!primaryInteractionEnabled || !visible || GameManager.IsAnyMiniGameActive())
		{
			return false;
		}
		if (simpleOverlayType != 0)
		{
			CtaManager.UpdateCta(this);
			return !string.IsNullOrEmpty(CtaManager.ctaKey);
		}
		return false;
	}

	public virtual bool ShouldShowDetailedOverlay()
	{
		return true;
	}

	public bool HasAnyRendererVisible()
	{
		for (int i = 0; i < renderers.Length; i++)
		{
			if (renderers[i].isVisible)
			{
				return true;
			}
		}
		return false;
	}

	public virtual void Hide()
	{
		visible = false;
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		base.gameObject.layer = LayerHelper.IgnoreRaycastLayerIndex;
		if (InstanceBehavior<OverlayManager>.Instance != null && InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideOverlays();
		}
	}

	public virtual void Show()
	{
		visible = true;
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		base.gameObject.layer = DefaultLayer;
	}

	public void SetPermanentOutline(bool isOn)
	{
		if (isOn)
		{
			OnIoEnter();
			disableHighlightInteraction = true;
		}
		else
		{
			disableHighlightInteraction = false;
			OnIoExit();
		}
	}

	public virtual void OnIoEnter()
	{
		if (simpleOverlayType != 0)
		{
			InstanceBehavior<OverlayManager>.Instance.ShowSimpleOverlay(this);
		}
		if (!disableHighlightInteraction && ShouldReactToIoEnter())
		{
			SetOutline();
		}
	}

	public void SetOutline()
	{
		if (!blockOutline && !ScreenshotController.isInFreeLookMode && ScreenshotController.uiIsVisible && !BuildingPreview.isPreviewing && !PlayerActivityUI.IsPanelOpen)
		{
			Renderer[] outlineRenderers = OutlineRenderers;
			for (int i = 0; i < outlineRenderers.Length; i++)
			{
				outlineRenderers[i].gameObject.layer = LayerHelper.InteractiveItemsOutlinedLayerIndex;
			}
		}
	}

	public virtual void OnIoExit()
	{
		onIoExitEvent.Invoke();
		onIoExitEvent.RemoveAllListeners();
		if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		}
		if (!disableHighlightInteraction)
		{
			Renderer[] outlineRenderers = OutlineRenderers;
			for (int i = 0; i < outlineRenderers.Length; i++)
			{
				outlineRenderers[i].gameObject.layer = DefaultLayer;
			}
		}
	}

	public virtual bool OnIoLeftClick()
	{
		if (!primaryInteractionEnabled || !visible)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance == null || (InstanceBehavior<GameManager>.Instance.playerController.NavigationDisabled && string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId)) || GameManager.IsAnyMiniGameActive())
		{
			return false;
		}
		if (PlacementSystem.IsInPlacementMode || CityMap.IsOpen)
		{
			return false;
		}
		if (!InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			CtaManager.UpdateCta(this);
		}
		if (CtaManager.ctaAction == null)
		{
			if (!InstanceBehavior<OverlayManager>.Instance.ShowDetailedOverlay(this))
			{
				return WalkOverAndInteract();
			}
			return true;
		}
		CtaManager.ctaAction();
		return true;
	}

	public virtual void OnIoRightClick()
	{
		if (visible)
		{
			SecondaryInteract();
		}
	}

	public void MoveTowardsEntity(UnityAction onReached)
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, onReached, checkRoute: true);
	}

	private bool WalkOverAndInteract()
	{
		EntityController entityToInteractWith = this;
		EntityController entityToWalkTo = this;
		if (this is ItemController itemController && !itemController.playerItemPurchaserSettings.enabled)
		{
			if (itemController.AttachmentPoints.Count((AttachmentPoint x) => (x.AttachmentPointType & AttachmentPointType.WorkSurface) != 0) > 1)
			{
				return false;
			}
			entityToInteractWith = itemController.childItemControllers.FirstOrDefault((ItemController x) => x is Producer) ?? this;
			if ((bool)itemController.parentItemController)
			{
				entityToWalkTo = itemController.parentItemController;
			}
		}
		if (entityToInteractWith.CanBeInteractedFromCurrentPosition())
		{
			return Interact();
		}
		if (!InstanceBehavior<GameManager>.Instance.playerController.IsOnNavmesh())
		{
			return false;
		}
		Vector3 closestNavMeshTargetPosition = entityToWalkTo.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		if (closestNavMeshTargetPosition == Vector3.zero || !InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(closestNavMeshTargetPosition))
		{
			return false;
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(entityToWalkTo, delegate
		{
			if (!entityToInteractWith.Interact() && entityToInteractWith != entityToWalkTo)
			{
				entityToWalkTo.Interact();
			}
		});
		return true;
	}

	protected virtual bool CanBeInteractedFromCurrentPosition()
	{
		if (InstanceBehavior<GameManager>.Instance.playerController.IsOnNavmesh())
		{
			return PlayerHelper.IsWithinPlayerDistance(this);
		}
		return false;
	}

	public virtual void SetOutlineColor(Color color)
	{
		Renderer[] outlineRenderers = OutlineRenderers;
		if (outlineRenderers == null || IsOutlineSuppressed)
		{
			return;
		}
		Renderer[] array = outlineRenderers;
		foreach (Renderer renderer in array)
		{
			if (renderer == null)
			{
				Debug.LogError("[BUG TRACKING] Null entity renderer on SetOutlineColor. Object: " + base.gameObject.name);
				continue;
			}
			renderer.GetPropertyBlock(PropertyBlockGetter);
			PropertyBlockGetter.SetColor(SelectionColor, color);
			renderer.SetPropertyBlock(PropertyBlockGetter);
		}
	}

	public void HighlightAndFadeOut(float duration)
	{
		cancelHighlightAndFadeOut = true;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			StartCoroutine(HighlightAndFadeOutCoroutine(duration));
		});
	}

	private IEnumerator HighlightAndFadeOutCoroutine(float durationInSeconds)
	{
		SetPermanentOutline(isOn: true);
		cancelHighlightAndFadeOut = false;
		renderers[0].GetPropertyBlock(PropertyBlockGetter);
		Color originalOutlineColor = PropertyBlockGetter.GetColor(SelectionColor);
		if (originalOutlineColor == Color.clear)
		{
			originalOutlineColor = Color.white;
		}
		Color targetColor = new Color(originalOutlineColor.r, originalOutlineColor.g, originalOutlineColor.b, 0f);
		for (float t = 0f; t < durationInSeconds; t += Time.unscaledDeltaTime)
		{
			if (cancelHighlightAndFadeOut)
			{
				break;
			}
			float t2 = t / durationInSeconds;
			Color outlineColor = Color.Lerp(originalOutlineColor, targetColor, t2);
			SetOutlineColor(outlineColor);
			yield return null;
		}
		SetPermanentOutline(isOn: false);
		SetOutlineColor(originalOutlineColor);
	}

	public Vector3 GetClosestNavMeshTargetPositionStraightLine(Vector3 origin)
	{
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		Transform[] array = navMeshTargets;
		foreach (Transform transform in array)
		{
			float num2 = Vector3.SqrMagnitude(transform.position - origin);
			if (num2 < num)
			{
				result = transform.position;
				num = num2;
			}
		}
		return result;
	}

	public virtual Vector3 GetClosestNavMeshTargetPosition(Vector3 entityPosition)
	{
		Vector3 result = Vector3.zero;
		float num = float.PositiveInfinity;
		NavMeshPath navMeshPath = new NavMeshPath();
		Transform[] array = navMeshTargets;
		foreach (Transform transform in array)
		{
			if (PathHelper.IsThereAWallBetweenTargetAndEntity(transform.position, base.transform.position + base.transform.forward * 0.1f))
			{
				continue;
			}
			NavMesh.CalculatePath(entityPosition, transform.position, PlayerController.navMeshQueryFilter, navMeshPath);
			if (navMeshPath.status != NavMeshPathStatus.PathInvalid && (navMeshPath.status != NavMeshPathStatus.PathPartial || (navMeshPath.corners.Length != 0 && !(Vector3.SqrMagnitude(navMeshPath.corners[^1] - transform.position) > 0.25f))))
			{
				float num2 = 0f;
				for (int j = 1; j < navMeshPath.corners.Length; j++)
				{
					num2 += Vector3.Distance(navMeshPath.corners[j - 1], navMeshPath.corners[j]);
				}
				if (num2 < num)
				{
					result = transform.position;
					num = num2;
				}
			}
		}
		return result;
	}

	public virtual Vector3 GetNavMeshTargetPosition(int index = 0)
	{
		return navMeshTargets[index].position;
	}

	public virtual void UpdateNavMeshTargets()
	{
		if ((bool)this && base.isActiveAndEnabled)
		{
			for (int i = 0; i < navMeshTargets.Length; i++)
			{
				DoNavMeshTargetValidation(i);
			}
		}
	}

	protected virtual void DoNavMeshTargetValidation(int index)
	{
		if (NavMesh.SamplePosition(GetNavMeshTargetPosition(index), out var hit, 3f, -1))
		{
			navMeshTargets[index].position = hit.position;
		}
	}

	public virtual bool TryGetRandomAvailableNavMeshTargetPosition(out Vector3 navMeshPosition)
	{
		return randomAvailableNavMeshPositionGetter.TryGetPosition(out navMeshPosition, navMeshTargets, base.transform.position);
	}

	public bool TryGetRandomAvailableRealNavMeshTargetPosition(out Vector3 navMeshPosition, bool inverted = false)
	{
		return randomAvailableNavMeshPositionGetter.TryGetPosition(out navMeshPosition, navMeshTargets, base.transform.position);
	}

	public virtual void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			MouseController.ResetCurrentEntitySelected(this);
		}
	}
}
